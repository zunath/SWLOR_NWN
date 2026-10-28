using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Derelict boarding (plan section 28). A boardable derelict site's wreck takes a Small or Medium ship through the
    /// ship-to-ship docking flow: <c>/board</c> docks it (the ship leaves space, so it cannot be attacked) and its party
    /// walks into an instanced interior: an existing starship interior dressed as a wreck, holding ground enemies,
    /// slicing terminals and a strongbox of the system's tier. The airlock leads back to the ship; undocking (the ship's
    /// computer or <c>/undock</c>) puts it beside the derelict with its crew. A contested derelict draws waves of enemy
    /// ships while it is worked: each wave cleared raises the strongbox, and with no ship defending, enemy boarders breach
    /// the interior. <c>/salvage</c> sends a ship's posted droids to strip a derelict on a timer instead.
    /// </summary>
    public static partial class Space
    {
        private const string DerelictInteriorVariable = "DERELICT_SIGNATURE_ID";
        private const string DerelictWaveVariable = "DERELICT_WAVE_ID";
        private const string DerelictInteriorTag = "derelict_interior";
        private const string DerelictStrongboxTag = "derelict_strongbox";
        private const string DerelictEntranceTag = "PROPERTY_ENTRANCE";
        private const string DerelictAirlockTag = "building_exit";
        private const string DerelictComputerTag = "ShipComputer";
        private const string QuestNpcGroupVariable = "QUEST_NPC_GROUP_ID";
        private const double DerelictProcessSeconds = 2;
        private const int DerelictPlacementTries = 12;

        private static readonly Dictionary<string, DerelictBoarding> _derelicts = new();

        /// <summary>The derelict each boarder is aboard (player id to signature id), so a logout aboard returns there.</summary>
        private static readonly Dictionary<string, string> _derelictBoarders = new();

        private static DateTime _lastDerelictTick = DateTime.UtcNow;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void ScheduleDerelicts()
        {
            Scheduler.ScheduleRepeating(ProcessDerelicts, TimeSpan.FromSeconds(DerelictProcessSeconds));
        }

        /// <summary>A derelict being worked, by its signature id, or null.</summary>
        public static DerelictBoarding FindDerelict(string signatureId) =>
            !string.IsNullOrEmpty(signatureId) && _derelicts.TryGetValue(signatureId, out var boarding) ? boarding : null;

        // Queries

        /// <summary>
        /// Whether the ship is docked at a derelict that is still being worked, and the derelict's name. A ship whose
        /// derelict is gone (a restart) is at its last dock: the stale mark is cleared.
        /// </summary>
        public static bool IsDockedAtDerelict(PlayerShip ship, out string derelictName)
        {
            derelictName = string.Empty;
            if (ship == null || string.IsNullOrWhiteSpace(ship.DockedDerelictId))
                return false;

            var boarding = FindDerelict(ship.DockedDerelictId);
            if (boarding != null && boarding.Mode == DerelictMode.Boarding && boarding.DockedShipIds.Contains(ship.Id))
            {
                derelictName = boarding.Name;
                return true;
            }

            ship.DockedDerelictId = string.Empty;
            DB.Set(ship);
            return false;
        }

        /// <summary>Whether the starship property is docked at a derelict, and the derelict's name (its interior's exit).</summary>
        public static bool TryGetDockedDerelictName(string propertyId, out string derelictName)
        {
            derelictName = string.Empty;
            if (string.IsNullOrWhiteSpace(propertyId))
                return false;

            var ship = DB.Search(new DBQuery<PlayerShip>()
                .AddFieldSearch(nameof(PlayerShip.PropertyId), propertyId, false)).FirstOrDefault();
            return IsDockedAtDerelict(ship, out derelictName);
        }

        /// <summary>Where a boarder who logged out aboard a derelict still being worked comes back: its airlock.</summary>
        public static bool TryGetDerelictLoginRedirect(uint player, out Location location)
        {
            location = default;
            if (!GetIsPC(player) || GetIsDM(player) || !_derelictBoarders.TryGetValue(GetObjectUUID(player), out var signatureId))
                return false;

            var boarding = FindDerelict(signatureId);
            if (boarding == null || !GetIsObjectValid(boarding.Interior))
            {
                _derelictBoarders.Remove(GetObjectUUID(player));
                return false;
            }

            location = GetDerelictEntrance(boarding.Interior);
            return true;
        }

        private static Location GetDerelictAnchor(DerelictBoarding boarding)
        {
            var area = Area.GetAreaByResref(boarding.AreaResref);
            return Location(area, Vector3(boarding.X, boarding.Y, boarding.Z), 0f);
        }

        private static Location GetDerelictEntrance(uint interior)
        {
            for (var obj = GetFirstObjectInArea(interior); GetIsObjectValid(obj); obj = GetNextObjectInArea(interior))
            {
                if (GetObjectType(obj) == ObjectType.Waypoint && GetTag(obj) == DerelictEntranceTag)
                    return GetLocation(obj);
            }

            return Location(interior, Vector3(5f, 5f, 0f), 0f);
        }

        /// <summary>The player ships flying within the defense radius of the derelict, by anyone.</summary>
        private static List<uint> GetDerelictDefenders(DerelictBoarding boarding)
        {
            var defenders = new List<uint>();
            var anchor = GetDerelictAnchor(boarding);
            var area = GetAreaFromLocation(anchor);
            if (!GetIsObjectValid(area))
                return defenders;

            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (GetArea(player) == area && !GetIsDead(player) && IsPlayerInSpaceMode(player) &&
                    DerelictRules.IsDefending(GetDistanceBetweenLocations(GetLocation(player), anchor)))
                {
                    defenders.Add(player);
                }
            }

            return defenders;
        }

        /// <summary>Tells everyone working the derelict: the boarders aboard, the party's ships nearby and the salvage pilot.</summary>
        private static void AnnounceToDerelict(DerelictBoarding boarding, string message)
        {
            var recipients = new HashSet<uint>();
            if (GetIsObjectValid(boarding.Interior))
                recipients.UnionWith(GetPlayersInArea(boarding.Interior));

            recipients.UnionWith(GetDerelictDefenders(boarding).Where(x => boarding.PartyIds.Contains(GetObjectUUID(x))));
            if (boarding.Mode == DerelictMode.SalvageTeam)
            {
                var pilot = GetShipPilot(boarding.SalvageShipId);
                if (GetIsObjectValid(pilot))
                    recipients.Add(pilot);
            }

            foreach (var player in recipients)
                SendMessageToPC(player, message);
        }

        private static bool TryGetTargetDerelict(uint player, out uint wreck, out ScanSignature signature)
        {
            var (target, _) = GetCurrentTarget(player);
            wreck = target;
            signature = GetIsObjectValid(target) && GetObjectType(target) == ObjectType.Placeable
                ? FindSignature(GetLocalString(target, SiteSignatureVariable))
                : null;
            return signature is { Type: SignatureType.Derelict };
        }

        private static DerelictRequest BuildDerelictRequest(uint player, PlayerShip ship, uint wreck, ScanSignature signature)
        {
            if (signature == null)
                return new DerelictRequest();

            var existing = FindDerelict(signature.Id);
            var party = GetScanPartyIds(player);
            return new DerelictRequest
            {
                IsDerelict = true,
                Boardable = signature.Boardable,
                Looted = signature.DerelictLooted || existing?.Looted == true,
                PartyLocated = ScanningRules.PartyPercent(signature, party) >= ScanningTuning.InfoLocatedPercent,
                ShipSize = GetShipSize(ship),
                Distance = GetArea(wreck) == GetArea(player) ? GetDistanceBetween(player, wreck) : float.MaxValue,
                InCombat = IsShipInCombat(player),
                DefensesLeft = signature.Objectives.Any(x => GetIsObjectValid(x) && GetObjectType(x) == ObjectType.Creature && !GetIsDead(x)),
                Underway = existing?.Mode ?? DerelictMode.None,
                SameParty = existing != null && existing.PartyIds.Overlaps(party),
                DockedCount = existing?.DockedShipIds.Count ?? 0,
                PostedDroids = GetPostedDroids(ship).Count,
            };
        }

        // Boarding

        /// <summary>
        /// Docks the pilot's ship at the derelict they target and takes them and everyone aboard into its interior.
        /// Returns false and tells them why when they cannot (<see cref="DerelictRules.GetBoardRefusal"/>).
        /// </summary>
        public static bool BoardDerelict(uint player)
        {
            if (!IsPlayerInSpaceMode(player) || !TryGetFlownShip(player, out var ship))
            {
                SendMessageToPC(player, ColorToken.Red("You must be piloting a starship to board a derelict."));
                return false;
            }

            if (GetIsDead(player) || _pendingShipLaunches.ContainsKey(player))
                return false;

            TryGetTargetDerelict(player, out var wreck, out var signature);
            var refusal = DerelictRules.GetBoardRefusal(BuildDerelictRequest(player, ship, wreck, signature));
            if (refusal != DerelictRefusal.None)
            {
                SendMessageToPC(player, ColorToken.Red(DerelictRules.DescribeRefusal(refusal)));
                return false;
            }

            if (!Property.TryGetLoadedInstance(ship.PropertyId, out var shipInstance))
            {
                Property.RequestInstanceLoad(ship.PropertyId);
                SendMessageToPC(player, ColorToken.Red("Your ship's interior is not ready. Please try again shortly."));
                return false;
            }

            var boarding = FindDerelict(signature.Id) ?? StartDerelictBoarding(player, signature);
            if (boarding == null)
            {
                SendMessageToPC(player, ColorToken.Red("The derelict's interior cannot be reached right now."));
                return false;
            }

            // Viewport watchers, station crew and anyone in the ship's interior come aboard with it.
            var watchers = _crewAssignments.Where(x => x.Value.Pilot == player).Select(x => x.Key).ToList();
            var passengers = watchers.ToList();
            passengers.AddRange(GetPlayersInArea(shipInstance.Area));
            passengers = passengers.Where(x => x != player && GetIsObjectValid(x)).Distinct().ToList();
            foreach (var watcher in watchers)
                LeaveCrewStation(watcher, returnToConsole: false);

            ship.DockedDerelictId = signature.Id;
            DB.Set(ship);
            boarding.DockedShipIds.Add(ship.Id);
            boarding.CrewByShipId[ship.Id] = passengers.Prepend(player).Select(GetObjectUUID).Distinct().ToList();

            // Without a current position the ship leaves space when its pilot steps out of it, so nothing can attack it.
            var property = DB.Get<WorldProperty>(ship.PropertyId);
            property.Positions.Remove(PropertyLocationType.CurrentPosition);
            DB.Set(property);

            DeleteLocalLocation(player, "SPACE_INSTANCE_LOCATION");
            DeleteLocalBool(player, "SPACE_INSTANCE_LOCATION_SET");
            ExitSpaceMode(player);

            var entrance = GetDerelictEntrance(boarding.Interior);
            foreach (var traveller in passengers.Prepend(player))
            {
                _derelictBoarders[GetObjectUUID(traveller)] = boarding.SignatureId;
                JumpPlayer(traveller, entrance);
            }

            var shipName = GetShipName(ship);
            var warning = boarding.Contested
                ? " Enemy ships are closing in: pilots who stay outside must hold them off, or enemy boarders will cut their way in."
                : string.Empty;
            SendMessageToPC(player, $"You dock {shipName} at the {boarding.Name} and board it. Its airlock leads back to your ship; " +
                                    $"undock from the ship's computer or with /undock.{warning}");
            foreach (var passenger in passengers)
                SendMessageToPC(passenger, $"{shipName} docks at the {boarding.Name}, and you board it.{warning}");

            Log.Write(LogGroup.Space, $"DERELICT BOARDED - {PlayerName.GetAuditName(player)} - ship {ship.Id} at {boarding.Name} " +
                                      $"T{boarding.TierIndex} in {boarding.AreaResref} ({passengers.Count} passengers, contested {boarding.Contested}).");
            return true;
        }

        private static DerelictBoarding StartDerelictBoarding(uint player, ScanSignature signature)
        {
            var tierIndex = DerelictRules.TierIndex(signature.Tier, signature.WildSpace);
            var plan = DerelictRules.PlanInterior(tierIndex, max => Random.Next(max));
            var boarding = new DerelictBoarding
            {
                SignatureId = signature.Id,
                Mode = DerelictMode.Boarding,
                TierIndex = tierIndex,
                WildSpace = signature.WildSpace,
                SpaceTier = signature.Tier,
                Contested = signature.Contested,
                AreaResref = signature.AreaResref,
                X = signature.X,
                Y = signature.Y,
                Z = signature.Z,
                PartyIds = GetScanPartyIds(player),
                TemplateResref = plan.TemplateResref,
                Name = plan.Name,
            };

            boarding.Interior = CreateDerelictInterior(boarding, plan);
            if (!GetIsObjectValid(boarding.Interior))
            {
                Log.Write(LogGroup.Error, $"Derelict template '{plan.TemplateResref}' could not be instanced for signature {signature.Id}.");
                return null;
            }

            var now = DateTime.UtcNow;
            DerelictRules.Begin(boarding, now);
            signature.HeldUntil = DerelictRules.HoldUntil(now);
            _derelicts[signature.Id] = boarding;
            return boarding;
        }

        /// <summary>
        /// Instances a derelict interior from its plan: the template's ship computer goes dead, its exit becomes the
        /// airlock, and debris, the tier's ground enemies, slicing terminals and the strongbox are placed.
        /// </summary>
        public static uint CreateDerelictInterior(DerelictBoarding boarding, DerelictInteriorPlan plan)
        {
            var interior = Area.CreateInstance(plan.TemplateResref, DerelictInteriorTag, plan.Name);
            if (!GetIsObjectValid(interior))
                return OBJECT_INVALID;

            SetLocalString(interior, DerelictInteriorVariable, boarding.SignatureId);
            var anchors = new List<Vector3>();
            for (var obj = GetFirstObjectInArea(interior); GetIsObjectValid(obj); obj = GetNextObjectInArea(interior))
            {
                if (GetObjectType(obj) != ObjectType.Placeable)
                    continue;

                anchors.Add(GetPosition(obj));
                switch (GetTag(obj))
                {
                    case DerelictAirlockTag:
                        SetName(obj, "Airlock");
                        DeleteLocalString(obj, "CONVERSATION");
                        SetEventScript(obj, EventScript.Placeable_OnUsed, ScriptName.OnDerelictAirlockUsed);
                        break;
                    case DerelictComputerTag:
                        SetName(obj, "Dead Ship Computer");
                        SetEventScript(obj, EventScript.Placeable_OnUsed, string.Empty);
                        SetUseableFlag(obj, false);
                        break;
                }
            }

            var entrance = GetPositionFromLocation(GetDerelictEntrance(interior));
            foreach (var resref in plan.Dressing)
                CreateObject(ObjectType.Placeable, resref, Location(interior, PickDerelictSpot(interior, entrance, anchors, 2f), Random.Next(360)));

            foreach (var resref in plan.Enemies)
                SpawnDerelictEnemy(boarding, interior, resref, PickDerelictSpot(interior, entrance, anchors, DerelictTuning.EntranceClearMeters));

            foreach (var resref in plan.Terminals)
                CreateObject(ObjectType.Placeable, resref,
                    Location(interior, PickDerelictSpot(interior, entrance, anchors, DerelictTuning.EntranceClearMeters), Random.Next(360)));

            var strongboxSpot = Enumerable.Range(0, DerelictPlacementTries)
                .Select(_ => PickDerelictSpot(interior, entrance, anchors, DerelictTuning.EntranceClearMeters))
                .OrderByDescending(x => System.Numerics.Vector3.Distance(x, entrance))
                .First();
            var strongbox = CreateObject(ObjectType.Placeable, plan.StrongboxResref, Location(interior, strongboxSpot, Random.Next(360)),
                false, DerelictStrongboxTag);
            if (GetIsObjectValid(strongbox))
            {
                SetName(strongbox, "Derelict Strongbox");
                SetLocalString(strongbox, DerelictInteriorVariable, boarding.SignatureId);
                SetEventScript(strongbox, EventScript.Placeable_OnUsed, ScriptName.OnDerelictStrongboxUsed);
                boarding.Strongbox = strongbox;
            }

            return interior;
        }

        /// <summary>
        /// A random walkable spot of the interior at least <paramref name="clearance"/> from the entrance; with no baked
        /// walkmesh point, beside one of the template's placeables.
        /// </summary>
        private static Vector3 PickDerelictSpot(uint interior, Vector3 entrance, IReadOnlyList<Vector3> anchors, float clearance)
        {
            var fallback = entrance;
            for (var attempt = 0; attempt < DerelictPlacementTries; attempt++)
            {
                var position = GetPositionFromLocation(Walkmesh.GetRandomLocation(interior));
                if (position.X == 0f && position.Y == 0f)
                {
                    if (anchors.Count == 0)
                        break;
                    position = anchors[Random.Next(anchors.Count)];
                }

                fallback = position;
                if (System.Numerics.Vector3.Distance(position, entrance) >= clearance)
                    return position;
            }

            return fallback;
        }

        /// <summary>A ground enemy of the derelict: the spawn system's scripts and stats, without dungeon quest credit.</summary>
        private static void SpawnDerelictEnemy(DerelictBoarding boarding, uint interior, string resref, Vector3 position)
        {
            var enemy = CreateObject(ObjectType.Creature, resref, Location(interior, position, Random.Next(360)));
            if (!GetIsObjectValid(enemy))
                return;

            // The roster borrows other dungeons' creatures; their kills here do not count toward those dungeons' quests.
            DeleteLocalInt(enemy, QuestNpcGroupVariable);
            Spawn.PrepareDMSpawnedCreature(enemy);
            Stat.LoadNPCStats(enemy);
            SetLocalString(enemy, DerelictInteriorVariable, boarding.SignatureId);
            boarding.Enemies.Add(enemy);
        }

        private static int PruneDerelictEnemies(DerelictBoarding boarding)
        {
            boarding.Enemies.RemoveAll(x => !GetIsObjectValid(x) || GetIsDead(x));
            return boarding.Enemies.Count;
        }

        // Moving between the ship and the derelict

        /// <summary>The airlock: a boarder crosses back to their ship.</summary>
        [NWNEventHandler(ScriptName.OnDerelictAirlockUsed)]
        public static void UseDerelictAirlock()
        {
            var player = GetLastUsedBy();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var boarding = FindDerelict(GetLocalString(GetArea(OBJECT_SELF), DerelictInteriorVariable));
            if (boarding == null)
            {
                SendBoarderHome(player, null);
                return;
            }

            var shipId = DerelictRules.ChooseShip(boarding, GetObjectUUID(player), GetDockedShipOwners(boarding));
            var ship = string.IsNullOrEmpty(shipId) ? null : DB.Get<PlayerShip>(shipId);
            if (ship == null)
            {
                SendBoarderHome(player, boarding);
                return;
            }

            if (!Property.TryResolveEnterableInstance(player, ship.PropertyId, out var instance))
                return;

            _derelictBoarders.Remove(GetObjectUUID(player));
            SendMessageToPC(player, $"You cross back aboard {GetShipName(ship)}.");
            JumpPlayer(player, GetShipEntrance(ship, instance));
        }

        /// <summary>The hatch of a ship docked at a derelict: the player crosses into the derelict.</summary>
        public static void CrossToDerelict(uint player, string propertyId)
        {
            var ship = DB.Search(new DBQuery<PlayerShip>()
                .AddFieldSearch(nameof(PlayerShip.PropertyId), propertyId, false)).FirstOrDefault();
            if (!IsDockedAtDerelict(ship, out _))
                return;

            var boarding = FindDerelict(ship.DockedDerelictId);
            if (boarding == null || !GetIsObjectValid(boarding.Interior))
                return;

            var playerId = GetObjectUUID(player);
            if (!boarding.CrewByShipId.TryGetValue(ship.Id, out var crew))
                boarding.CrewByShipId[ship.Id] = crew = new List<string>();
            if (!crew.Contains(playerId))
                crew.Add(playerId);

            _derelictBoarders[playerId] = boarding.SignatureId;
            JumpPlayer(player, GetDerelictEntrance(boarding.Interior));
        }

        private static Dictionary<string, string> GetDockedShipOwners(DerelictBoarding boarding) =>
            boarding.DockedShipIds
                .Select(DB.Get<PlayerShip>)
                .Where(x => x != null)
                .ToDictionary(x => x.Id, x => x.OwnerPlayerId);

        /// <summary>
        /// Sends a boarder off a derelict that is ending or has lost its ships: to their ship's interior when it is ready,
        /// otherwise to the dock it is towed to (with no penalty), otherwise to the starting location.
        /// </summary>
        private static void SendBoarderHome(uint player, DerelictBoarding boarding)
        {
            _derelictBoarders.Remove(GetObjectUUID(player));

            var shipId = boarding == null ? null : DerelictRules.ChooseShip(boarding, GetObjectUUID(player), GetDockedShipOwners(boarding));
            var ship = string.IsNullOrEmpty(shipId) ? DB.Get<PlayerShip>(DB.Get<Player>(GetObjectUUID(player))?.ActiveShipId ?? string.Empty) : DB.Get<PlayerShip>(shipId);
            PropertyInstance instance = null;
            var interiorReady = ship != null && Property.TryGetLoadedInstance(ship.PropertyId, out instance);
            var property = ship == null ? null : DB.Get<WorldProperty>(ship.PropertyId);
            Location dock = default;
            var dockReachable = property != null && TryResolveEscapePodDestination(property, out dock, out _);

            switch (DerelictRules.ChooseReturn(interiorReady, dockReachable))
            {
                case DerelictReturn.ShipInterior:
                    SendMessageToPC(player, $"You make your way back aboard {GetShipName(ship)}.");
                    JumpPlayer(player, GetShipEntrance(ship, instance));
                    break;
                case DerelictReturn.ShipDock:
                    SendMessageToPC(player, ColorToken.Red("Your ship cannot take you aboard, so you catch a ride back to its dock."));
                    JumpPlayer(player, dock);
                    break;
                default:
                    SendMessageToPC(player, ColorToken.Red("Your ship cannot be reached, so you catch a ride back to safety."));
                    JumpPlayer(player, GetStartingLocation());
                    break;
            }
        }

        /// <summary>
        /// Arriving anywhere but the derelict's interior (or the entry area at login) means the player left it.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void ForgetDerelictBoarder()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || !_derelictBoarders.TryGetValue(GetObjectUUID(player), out var signatureId))
                return;

            var area = OBJECT_SELF;
            if (GetTag(area) == EntryAreaTag || GetLocalString(area, DerelictInteriorVariable) == signatureId)
                return;

            _derelictBoarders.Remove(GetObjectUUID(player));
        }

        // Undocking

        /// <summary>
        /// Undocks the ship the player stands aboard from its derelict: they take its controls beside the derelict, and
        /// the ship leaves the derelict when they arrive, bringing back its crew still aboard.
        /// </summary>
        public static bool UndockFromDerelict(uint player, PlayerShip ship)
        {
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDead(player) || _pendingShipLaunches.ContainsKey(player))
                return false;

            if (IsPlayerInSpaceMode(player) || IsAtCrewStation(player))
            {
                SendMessageToPC(player, ColorToken.Red("Leave the controls or your station before undocking."));
                return false;
            }

            var boarding = FindDerelict(ship?.DockedDerelictId);
            if (boarding == null || !boarding.DockedShipIds.Contains(ship.Id))
            {
                SendMessageToPC(player, ColorToken.Red("This ship is not docked at a derelict."));
                return false;
            }

            if (!HasShipPermission(player, ship.PropertyId, PropertyPermissionType.PilotShip))
            {
                SendMessageToPC(player, ColorToken.Red("You do not have permission to pilot this starship."));
                return false;
            }

            if (_pendingShipLaunches.Values.Any(x => x.ShipId == ship.Id))
            {
                SendMessageToPC(player, ColorToken.Red("This ship's controls are in use."));
                return false;
            }

            var anchor = GetDerelictAnchor(boarding);
            var area = GetAreaFromLocation(anchor);
            if (!GetIsObjectValid(area))
            {
                SendMessageToPC(player, ColorToken.Red("The derelict's system cannot be reached right now. Please try again shortly."));
                return false;
            }

            if (!CanShipEnterArea(player, ship, area))
                return false;

            if (!CanPlayerUseShip(player, ship.Status))
            {
                var skillError = GetShipSkillRequirementError(player, ship.Status);
                SendMessageToPC(player, ColorToken.Red(string.IsNullOrEmpty(skillError)
                    ? "You do not have the ability to pilot this ship."
                    : skillError));
                return false;
            }

            if (!Property.TryResolveEnterableInstance(player, ship.PropertyId, out var instance))
                return false;

            var pilotLocation = GetArea(player) == instance.Area ? GetLocation(player) : GetShipEntrance(ship, instance);
            var index = Math.Max(0, boarding.DockedShipIds.IndexOf(ship.Id));
            BeginShipLaunch(player, ship.Id, OffsetBesideShip(anchor, index), pilotLocation, undockFromDerelictId: boarding.SignatureId);
            SendMessageToPC(player, $"You undock {GetShipName(ship)} from the {boarding.Name}.");
            return true;
        }

        /// <summary>
        /// The undocking pilot reached space: the ship leaves the derelict, and the rest of its crew still aboard come
        /// back to its interior. Nothing changes if the boarding ended meanwhile (the ship already left).
        /// </summary>
        private static void CompleteDerelictUndock(uint pilot, PendingShipLaunch launch)
        {
            var ship = DB.Get<PlayerShip>(launch.ShipId);
            if (ship == null || ship.DockedDerelictId != launch.UndockFromDerelictId)
                return;

            ship.DockedDerelictId = string.Empty;
            DB.Set(ship);
            _derelictBoarders.Remove(GetObjectUUID(pilot));

            var boarding = FindDerelict(launch.UndockFromDerelictId);
            if (boarding == null)
                return;

            boarding.DockedShipIds.Remove(ship.Id);
            boarding.CrewByShipId.TryGetValue(ship.Id, out var crew);
            boarding.CrewByShipId.Remove(ship.Id);
            if (crew == null || !GetIsObjectValid(boarding.Interior) || !Property.TryGetLoadedInstance(ship.PropertyId, out var instance))
                return;

            var entrance = GetShipEntrance(ship, instance);
            var shipName = GetShipName(ship);
            foreach (var member in GetPlayersInArea(boarding.Interior).Where(x => crew.Contains(GetObjectUUID(x))))
            {
                _derelictBoarders.Remove(GetObjectUUID(member));
                SendMessageToPC(member, $"{shipName} undocks from the {boarding.Name}, and you return aboard it.");
                JumpPlayer(member, entrance);
            }

            Log.Write(LogGroup.Space, $"Ship {ship.Id} undocked from derelict {boarding.SignatureId} (pilot {launch.PlayerId}).");
        }

        // The strongbox

        [NWNEventHandler(ScriptName.OnDerelictStrongboxUsed)]
        public static void UseDerelictStrongbox()
        {
            OpenDerelictStrongbox(OBJECT_SELF, GetLastUsedBy());
        }

        /// <summary>
        /// Opens a derelict's strongbox once every enemy aboard is down: each boarder aboard gets its credits (stepped up by
        /// the contested waves cleared) and its ground lockboxes, and each party ship defending nearby a share of the credits.
        /// </summary>
        public static void OpenDerelictStrongbox(uint strongbox, uint user)
        {
            if (!GetIsPC(user) || GetIsDM(user) || !GetIsObjectValid(strongbox))
                return;

            var boarding = FindDerelict(GetLocalString(strongbox, DerelictInteriorVariable));
            if (boarding == null || boarding.Looted)
            {
                DestroyObject(strongbox);
                return;
            }

            var left = PruneDerelictEnemies(boarding);
            if (!DerelictRules.CanOpenStrongbox(left))
            {
                SendMessageToPC(user, ColorToken.Red($"The strongbox's maglock holds while hostiles remain aboard ({left} left)."));
                return;
            }

            boarding.Looted = true;
            var signature = FindSignature(boarding.SignatureId);
            if (signature != null)
                signature.DerelictLooted = true;

            var credits = DerelictRules.StrongboxCredits(boarding.TierIndex, boarding.WavesCleared);
            var lockboxes = DerelictRules.StrongboxLockboxes(boarding.WavesCleared);
            var lockbox = DerelictRules.LockboxResref(boarding.TierIndex);
            var boarders = GetPlayersInArea(boarding.Interior);
            foreach (var boarder in boarders)
            {
                GiveGoldToCreature(boarder, credits);
                for (var count = 0; count < lockboxes; count++)
                    CreateItemOnObject(lockbox, boarder);
                SendMessageToPC(boarder, ColorToken.Green($"You crack the {boarding.Name}'s strongbox: {credits} credits and {lockboxes} sealed lockbox{(lockboxes == 1 ? string.Empty : "es")}."));
            }

            var party = GetScanPartyIds(user);
            party.UnionWith(boarding.PartyIds);
            var defenderCredits = DerelictRules.DefenderCredits(boarding.TierIndex, boarding.WavesCleared);
            var defenders = GetDerelictDefenders(boarding).Where(x => party.Contains(GetObjectUUID(x))).ToList();
            foreach (var defender in defenders)
            {
                GiveGoldToCreature(defender, defenderCredits);
                SendMessageToPC(defender, ColorToken.Green($"Your boarders crack the {boarding.Name}'s strongbox: your share for holding the line is {defenderCredits} credits."));
            }

            Log.Write(LogGroup.Space, $"DERELICT STRONGBOX - {PlayerName.GetAuditName(user)} - {boarding.Name} T{boarding.TierIndex} in {boarding.AreaResref} - " +
                                      $"{boarders.Count} boarders x {credits} credits and {lockboxes} {lockbox}, {defenders.Count} defenders x {defenderCredits}, {boarding.WavesCleared} waves cleared");
            boarding.Strongbox = OBJECT_INVALID;
            DestroyObject(strongbox);
        }

        // Droid salvage teams

        /// <summary>
        /// Sends the droids posted aboard the pilot's ship to strip the derelict they target. The ship stays in space and
        /// must hold within <see cref="DerelictTuning.SalvageTetherMeters"/>; the droids leave their posts until the team
        /// is back. Returns false and tells them why when they cannot (<see cref="DerelictRules.GetSalvageRefusal"/>).
        /// </summary>
        public static bool SendSalvageTeam(uint player)
        {
            if (!IsPlayerInSpaceMode(player) || !TryGetFlownShip(player, out var ship))
            {
                SendMessageToPC(player, ColorToken.Red("You must be piloting a starship to send a salvage team."));
                return false;
            }

            if (GetIsDead(player) || _pendingShipLaunches.ContainsKey(player))
                return false;

            TryGetTargetDerelict(player, out var wreck, out var signature);
            var refusal = DerelictRules.GetSalvageRefusal(BuildDerelictRequest(player, ship, wreck, signature));
            if (refusal != DerelictRefusal.None)
            {
                SendMessageToPC(player, ColorToken.Red(DerelictRules.DescribeRefusal(refusal)));
                return false;
            }

            var droids = GetPostedDroids(ship);
            var industry = DerelictRules.TeamIndustry(droids.Select(x => x.Industry));
            var seconds = DerelictRules.SalvageSeconds(industry);
            var boarding = new DerelictBoarding
            {
                SignatureId = signature.Id,
                Mode = DerelictMode.SalvageTeam,
                TierIndex = DerelictRules.TierIndex(signature.Tier, signature.WildSpace),
                WildSpace = signature.WildSpace,
                SpaceTier = signature.Tier,
                Contested = signature.Contested,
                AreaResref = signature.AreaResref,
                X = signature.X,
                Y = signature.Y,
                Z = signature.Z,
                PartyIds = GetScanPartyIds(player),
                Name = "derelict",
                SalvageShipId = ship.Id,
                SalvagePilotId = GetObjectUUID(player),
                SalvageIndustry = industry,
                SalvageSecondsNeeded = seconds,
            };

            var now = DateTime.UtcNow;
            DerelictRules.Begin(boarding, now);
            signature.HeldUntil = DerelictRules.HoldUntil(now);
            _derelicts[signature.Id] = boarding;

            foreach (var posting in droids)
                UnseatDroid(player, posting.Seat);

            var names = string.Join(", ", droids.Select(x => x.Name));
            var warning = boarding.Contested
                ? " Enemy ships will come: the droids take cover while a wave is out, so clear each one."
                : string.Empty;
            SendMessageToPC(player, $"Your salvage team ({names}, Industry {industry}) crosses to the derelict. They need " +
                                    $"{seconds / 60.0:0.#} minutes of work; stay within {DerelictTuning.SalvageTetherMeters:0} meters.{warning}");
            Log.Write(LogGroup.Space, $"DERELICT SALVAGE TEAM - {PlayerName.GetAuditName(player)} - ship {ship.Id}, Industry {industry}, {seconds} s, " +
                                      $"T{boarding.TierIndex} in {boarding.AreaResref} (contested {boarding.Contested}).");
            return true;
        }

        private static void ProcessSalvageTeam(DerelictBoarding boarding, double seconds)
        {
            var pilot = GetShipPilot(boarding.SalvageShipId);
            var anchor = GetDerelictAnchor(boarding);
            if (!GetIsObjectValid(pilot) || GetIsDead(pilot) || GetArea(pilot) != GetAreaFromLocation(anchor) ||
                !DerelictRules.IsTethered(GetDistanceBetweenLocations(GetLocation(pilot), anchor)))
            {
                if (GetIsObjectValid(pilot))
                    SendMessageToPC(pilot, ColorToken.Red("Your ship pulled away from the derelict, so your salvage team comes back empty-handed."));
                EndDerelict(boarding);
                return;
            }

            DerelictRules.AdvanceSalvage(boarding, seconds);
            if (!DerelictRules.IsSalvageDone(boarding))
                return;

            boarding.Looted = true;
            var signature = FindSignature(boarding.SignatureId);
            if (signature != null)
                signature.DerelictLooted = true;

            var credits = DerelictRules.SalvageCredits(boarding.TierIndex, boarding.SalvageIndustry);
            var scrap = DerelictRules.SalvageScrap(boarding.SalvageIndustry);
            var items = Enumerable.Repeat(DerelictRules.ScrapResref(boarding.TierIndex), scrap).ToList();
            var table = Loot.GetLootTableByName(SignatureSiteRules.CacheLootTable(boarding.SpaceTier, boarding.WildSpace));
            items.AddRange(Enumerable.Range(0, DerelictTuning.SalvageItemPulls).Select(_ => table.GetRandomItem().Resref));

            GiveGoldToCreature(pilot, credits);
            ShipHold.DeliverYield(pilot, items);
            SendMessageToPC(pilot, ColorToken.Green($"Your salvage team strips the derelict and comes back with {credits} credits, {scrap} Ship Scrap and a find."));
            Log.Write(LogGroup.Space, $"DERELICT SALVAGED - {PlayerName.GetAuditName(pilot)} - T{boarding.TierIndex} in {boarding.AreaResref} - " +
                                      $"{credits} credits, {string.Join(", ", items)} (Industry {boarding.SalvageIndustry}, {boarding.WavesCleared} waves)");
            EndDerelict(boarding);
        }

        /// <summary>The salvage team is back (or cut off): the droids take their posts again while the ship flies.</summary>
        private static void ReturnSalvageTeam(DerelictBoarding boarding)
        {
            var pilot = GetShipPilot(boarding.SalvageShipId);
            var ship = DB.Get<PlayerShip>(boarding.SalvageShipId);
            if (!GetIsObjectValid(pilot) || ship == null)
                return;

            foreach (var posting in GetPostedDroids(ship))
                SpaceCrew.AssignDroidSeat(pilot, posting);
            RecalculateCrewedShip(pilot);
        }

        // Contested waves and reinforcements

        private static void ProcessDerelictWaves(DerelictBoarding boarding, DateTime now)
        {
            if (!boarding.Contested)
                return;

            if (boarding.WaveOut)
            {
                boarding.WaveShips.RemoveAll(x => !GetIsObjectValid(x) || GetIsDead(x));
                if (boarding.WaveShips.Count > 0)
                    return;

                var counted = !boarding.Looted;
                DerelictRules.RecordWaveCleared(boarding, now);
                if (boarding.Mode == DerelictMode.SalvageTeam)
                    AnnounceToDerelict(boarding, ColorToken.Green($"Wave {boarding.WavesSpawned} is broken. Your droids get back to work."));
                else if (counted)
                    AnnounceToDerelict(boarding, ColorToken.Green($"Wave {boarding.WavesSpawned} is broken: the strongbox now holds " +
                                                                  $"{DerelictRules.StrongboxCredits(boarding.TierIndex, boarding.WavesCleared)} credits for each boarder."));
                return;
            }

            // Waves come only while someone works the derelict.
            if (boarding.Mode == DerelictMode.Boarding && boarding.DockedShipIds.Count == 0)
                return;
            if (!DerelictRules.WaveDue(boarding, now))
                return;

            var anchor = GetDerelictAnchor(boarding);
            var area = GetAreaFromLocation(anchor);
            if (!GetIsObjectValid(area))
                return;

            var squad = SignatureSiteRules.CombatSquad(boarding.SpaceTier, boarding.WildSpace);
            for (var index = 0; index < squad.Count; index++)
            {
                var (x, y) = SignatureSiteRules.RingOffset(index, squad.Count, DerelictTuning.WaveRingMeters);
                var ship = CreateObject(ObjectType.Creature, squad[index], Location(area, Vector3(boarding.X + x, boarding.Y + y, boarding.Z), Random.Next(360)));
                if (!GetIsObjectValid(ship))
                    continue;

                SetLocalString(ship, DerelictWaveVariable, boarding.SignatureId);
                boarding.WaveShips.Add(ship);
            }

            if (boarding.WaveShips.Count == 0)
                return;

            DerelictRules.RecordWaveSpawned(boarding);
            AnnounceToDerelict(boarding, ColorToken.Red($"Enemy ships close on the derelict (wave {boarding.WavesSpawned} of {DerelictTuning.MaximumWaves})."));
        }

        private static void ReinforceDerelict(DerelictBoarding boarding, DateTime now)
        {
            var count = DerelictRules.ReinforcementSize(boarding);
            if (count <= 0 || !GetIsObjectValid(boarding.Interior))
                return;

            var roster = DerelictRules.Roster(boarding.TierIndex);
            var entrance = GetPositionFromLocation(GetDerelictEntrance(boarding.Interior));
            for (var index = 0; index < count; index++)
            {
                var (x, y) = SignatureSiteRules.RingOffset(index, count, 2f);
                SpawnDerelictEnemy(boarding, boarding.Interior, roster[Random.Next(roster.Count)], Vector3(entrance.X + x, entrance.Y + y, entrance.Z));
            }

            boarding.Reinforcements += count;
            boarding.LastReinforcementAt = now;
            foreach (var player in GetPlayersInArea(boarding.Interior))
                SendMessageToPC(player, ColorToken.Red("With no ship defending the derelict, enemy boarders cut their way in at the airlock!"));
        }

        // The loop

        private static void ProcessDerelicts()
        {
            var now = DateTime.UtcNow;
            var seconds = Math.Clamp((now - _lastDerelictTick).TotalSeconds, 0, 10);
            _lastDerelictTick = now;

            foreach (var boarding in _derelicts.Values.ToList())
            {
                try
                {
                    ProcessDerelictWaves(boarding, now);
                    if (boarding.Mode == DerelictMode.SalvageTeam)
                        ProcessSalvageTeam(boarding, seconds);
                    else
                        ProcessBoarding(boarding, now);

                    if (_derelicts.ContainsKey(boarding.SignatureId) && DerelictRules.IsExpired(boarding, now))
                        EndDerelict(boarding);
                }
                catch (Exception ex)
                {
                    Log.Write(LogGroup.Error, $"Derelict {boarding.SignatureId} failed: {ex}");
                }
            }
        }

        private static void ProcessBoarding(DerelictBoarding boarding, DateTime now)
        {
            // A ship whose record no longer says it is docked here left (its pilot undocked it).
            boarding.DockedShipIds.RemoveAll(id => DB.Get<PlayerShip>(id)?.DockedDerelictId != boarding.SignatureId);

            var aboard = GetIsObjectValid(boarding.Interior) ? GetPlayersInArea(boarding.Interior) : new List<uint>();
            if (DerelictRules.IsBoardingAbandoned(boarding, aboard.Count))
            {
                EndDerelict(boarding);
                return;
            }

            if (DerelictRules.ShouldReinforce(boarding, GetDerelictDefenders(boarding).Count, aboard.Count, now))
                ReinforceDerelict(boarding, now);
        }

        /// <summary>
        /// Ends a boarding or salvage team. Each ship still docked goes back into space beside the derelict when anyone is
        /// aboard it or the derelict (they go back to it and can fly it), otherwise it returns to its last dock; anyone
        /// still aboard the derelict goes back to their ship. Wave ships still flying leave once nobody fights them.
        /// </summary>
        private static void EndDerelict(DerelictBoarding boarding)
        {
            _derelicts.Remove(boarding.SignatureId);
            var signature = FindSignature(boarding.SignatureId);
            if (signature != null)
                signature.HeldUntil = null;

            foreach (var ship in boarding.WaveShips.Where(x => GetIsObjectValid(x) && !GetIsDead(x)))
                _lingeringSiteShips.Add(ship);
            boarding.WaveShips.Clear();

            if (boarding.Mode == DerelictMode.SalvageTeam)
            {
                ReturnSalvageTeam(boarding);
                return;
            }

            var anchor = GetDerelictAnchor(boarding);
            var aboardDerelict = GetIsObjectValid(boarding.Interior) ? GetPlayersInArea(boarding.Interior) : new List<uint>();
            for (var index = 0; index < boarding.DockedShipIds.Count; index++)
            {
                var ship = DB.Get<PlayerShip>(boarding.DockedShipIds[index]);
                if (ship == null || ship.DockedDerelictId != boarding.SignatureId ||
                    _pendingShipLaunches.Values.Any(x => x.ShipId == ship.Id))
                {
                    continue;
                }

                ship.DockedDerelictId = string.Empty;
                DB.Set(ship);

                var property = DB.Get<WorldProperty>(ship.PropertyId);
                boarding.CrewByShipId.TryGetValue(ship.Id, out var crew);
                var crewAboard = aboardDerelict.Any(x => crew?.Contains(GetObjectUUID(x)) == true) ||
                                 (Property.TryGetLoadedInstance(ship.PropertyId, out var shipInstance) && GetPlayersInArea(shipInstance.Area).Count > 0);
                if (property != null && crewAboard && GetIsObjectValid(GetAreaFromLocation(anchor)))
                {
                    PlaceShipInSpace(ship, property, OffsetBesideShip(anchor, index));
                    Log.Write(LogGroup.Space, $"Ship {ship.Id} left derelict {boarding.SignatureId} unpiloted beside it.");
                }
                else
                {
                    var owner = DMPlayerDirectory.GetOnlinePlayer(ship.OwnerPlayerId);
                    if (GetIsObjectValid(owner))
                        SendMessageToPC(owner, $"{GetShipName(ship)} was docked at a derelict that broke apart, and it was towed back to its last dock.");
                    Log.Write(LogGroup.Space, $"Ship {ship.Id} left derelict {boarding.SignatureId} for its last dock.");
                }
            }

            foreach (var player in aboardDerelict)
            {
                SendMessageToPC(player, ColorToken.Red($"The {boarding.Name} is breaking apart!"));
                SendBoarderHome(player, boarding);
            }

            foreach (var playerId in _derelictBoarders.Where(x => x.Value == boarding.SignatureId).Select(x => x.Key).ToList())
                _derelictBoarders.Remove(playerId);

            if (GetIsObjectValid(boarding.Interior))
            {
                var interior = boarding.Interior;
                Scheduler.Schedule(() => DestroyDerelictInterior(interior), TimeSpan.FromSeconds(10));
            }

            Log.Write(LogGroup.Space, $"DERELICT ENDED - {boarding.Name} T{boarding.TierIndex} in {boarding.AreaResref} - looted {boarding.Looted}, " +
                                      $"{boarding.WavesCleared} waves cleared, {boarding.Reinforcements} reinforcements");
        }

        private static void DestroyDerelictInterior(uint interior)
        {
            if (!GetIsObjectValid(interior))
                return;

            if (GetPlayersInArea(interior).Count > 0 || DestroyArea(interior) != 1)
                Scheduler.Schedule(() => DestroyDerelictInterior(interior), TimeSpan.FromSeconds(30));
        }
    }
}
