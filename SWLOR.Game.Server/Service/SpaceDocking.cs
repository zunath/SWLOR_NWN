using System.Collections.Generic;
using System.Linq;
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
    /// Ship-to-ship docking (plan section 10). A Small or Medium ship's pilot targets a player's Large or Capital ship
    /// in flight and types <c>/dock</c>: the visiting ship leaves space, and its pilot and passengers walk into the
    /// host's interior. The ship is saved as docked aboard the host (<see cref="PlayerShip.DockedToShipId"/>) and
    /// travels with it. Its pilot undocks from the host's computer or with <c>/undock</c>: beside the host in space,
    /// or into the starport where the host is docked. When the host is destroyed, docked ships detach where it was
    /// and the players who belong to them go back aboard (<see cref="DetachDockedShips"/>).
    /// </summary>
    public static partial class Space
    {
        private const string DockedHostLocationVariable = "SPACE_DOCKED_HOST_LOCATION";
        private const string DockedHostLocationSetVariable = "SPACE_DOCKED_HOST_LOCATION_SET";
        private const string EntryAreaTag = "ooc_area";
        private const int DockedShipQueryLimit = 10;
        private const int ShipCombatPruneCount = 500;

        private static readonly Dictionary<uint, DateTime> _shipCombatAt = new();

        // Combat

        /// <summary>Both ships of a hit that removed shield or hull are in combat for <see cref="ShipDockingRules.CombatWindowSeconds"/>.</summary>
        private static void MarkShipCombat(uint attacker, uint target)
        {
            var now = DateTime.UtcNow;
            if (_shipCombatAt.Count > ShipCombatPruneCount)
            {
                foreach (var stale in _shipCombatAt.Where(x => !ShipDockingRules.IsInCombat(false, x.Value, now)).Select(x => x.Key).ToList())
                    _shipCombatAt.Remove(stale);
            }

            if (GetIsObjectValid(attacker))
                _shipCombatAt[attacker] = now;
            if (GetIsObjectValid(target))
                _shipCombatAt[target] = now;
        }

        /// <summary>Whether a ship is in combat: an enemy holds threat on it, or it dealt or took ship damage recently.</summary>
        public static bool IsShipInCombat(uint ship)
        {
            var last = _shipCombatAt.TryGetValue(ship, out var at) ? at : DateTime.MinValue;
            return ShipDockingRules.IsInCombat(Enmity.HasEnmity(ship), last, DateTime.UtcNow);
        }

        // Queries

        private static ShipSize GetShipSize(PlayerShip ship)
        {
            var detail = GetShipDetailByItemTag(ship.Status.ItemTag);
            return ShipDockingRules.ResolveSize(detail?.Size ?? ship.Status.Size, detail?.CapitalShip ?? ship.Status.CapitalShip);
        }

        private static bool HasShipPermission(uint player, string propertyId, PropertyPermissionType type)
        {
            if (GetIsDM(player) || GetIsDMPossessed(player))
                return true;

            var permissionQuery = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), propertyId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), GetObjectUUID(player), false);
            var permission = DB.Search(permissionQuery).FirstOrDefault();

            return permission != null && permission.Permissions.TryGetValue(type, out var allowed) && allowed;
        }

        /// <summary>The ship a player flies right now: their creature is the ship.</summary>
        private static bool TryGetFlownShip(uint creature, out PlayerShip ship)
        {
            ship = null;
            if (!GetIsObjectValid(creature) || !IsPlayerInSpaceMode(creature))
                return false;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(creature));
            ship = DB.Get<PlayerShip>(dbPlayer.ActiveShipId);
            return ship != null;
        }

        /// <summary>The ships docked aboard a host.</summary>
        public static List<PlayerShip> GetDockedShips(string hostShipId)
        {
            if (string.IsNullOrWhiteSpace(hostShipId))
                return new List<PlayerShip>();

            var query = new DBQuery<PlayerShip>()
                .AddFieldSearch(nameof(PlayerShip.DockedToShipId), hostShipId, false)
                .AddPaging(DockedShipQueryLimit, 0);
            return DB.Search(query).OrderBy(x => x.Id).ToList();
        }

        /// <summary>
        /// The host a ship is docked aboard, or null. A ship whose host no longer exists is undocked: it stays at
        /// the last dock it used itself.
        /// </summary>
        private static PlayerShip GetDockHost(PlayerShip ship)
        {
            if (ship == null || string.IsNullOrWhiteSpace(ship.DockedToShipId))
                return null;

            var host = DB.Get<PlayerShip>(ship.DockedToShipId);
            if (host != null)
                return host;

            Log.Write(LogGroup.Space, $"Ship {ship.Id} was docked aboard missing ship {ship.DockedToShipId}. It stays at its last dock.");
            ClearDockedState(ship);
            DB.Set(ship);
            return null;
        }

        /// <summary>Whether the ship is docked aboard another, and that ship's name.</summary>
        public static bool IsDockedAboardHost(PlayerShip ship, out string hostName)
        {
            var host = GetDockHost(ship);
            hostName = host == null ? string.Empty : GetShipName(host);
            return host != null;
        }

        /// <summary>The ships docked aboard the host that the player may undock (pilot), theirs first.</summary>
        public static List<PlayerShip> GetUndockableShips(uint player, PlayerShip host)
        {
            if (host == null || !GetIsPC(player))
                return new List<PlayerShip>();

            var playerId = GetObjectUUID(player);
            return GetDockedShips(host.Id)
                .Where(x => HasShipPermission(player, x.PropertyId, PropertyPermissionType.PilotShip))
                .OrderByDescending(x => x.DockedCrewPlayerIds?.FirstOrDefault() == playerId)
                .ThenByDescending(x => x.OwnerPlayerId == playerId)
                .ToList();
        }

        /// <summary>
        /// Whether the player may pilot a ship docked aboard the starship property, so they may board it to reach
        /// their ship (a host that returned to its dock after a restart).
        /// </summary>
        public static bool HasShipDockedAboard(uint player, string hostPropertyId)
        {
            var host = DB.Search(new DBQuery<PlayerShip>()
                .AddFieldSearch(nameof(PlayerShip.PropertyId), hostPropertyId, false)).FirstOrDefault();
            return GetUndockableShips(player, host).Count > 0;
        }

        private static ShipDockVisitor ToDockVisitor(PlayerShip ship) => new()
        {
            ShipId = ship.Id,
            OwnerPlayerId = ship.OwnerPlayerId,
            CrewPlayerIds = ship.DockedCrewPlayerIds?.ToList() ?? new List<string>(),
        };

        private static void ClearDockedState(PlayerShip ship)
        {
            ship.DockedToShipId = string.Empty;
            ship.DockedCrewPlayerIds = new List<string>();
        }

        private static Location GetShipEntrance(PlayerShip ship, PropertyInstance instance)
        {
            var detail = GetShipDetailByItemTag(ship.Status.ItemTag);
            var entrance = Property.GetEntrancePosition(detail.Layout);
            return Location(instance.Area, Vector3(entrance.X, entrance.Y, entrance.Z), entrance.W);
        }

        /// <summary>The players standing in an area (a ship interior).</summary>
        private static List<uint> GetPlayersInArea(uint area)
        {
            var players = new List<uint>();
            if (!GetIsObjectValid(area))
                return players;

            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (GetArea(player) == area && !GetIsDM(player))
                    players.Add(player);
            }

            return players;
        }

        /// <summary>Where a ship is in space: its pilot, its unpiloted copy, or its saved position. False while it is docked.</summary>
        private static bool TryGetShipSpaceLocation(PlayerShip ship, WorldProperty property, out Location location)
        {
            location = default;
            var pilot = GetShipPilot(ship.Id);
            if (GetIsObjectValid(pilot))
            {
                location = GetLocation(pilot);
                return true;
            }

            if (_shipClones.TryGetValue(ship.Id, out var clone) && GetIsObjectValid(clone))
            {
                location = GetLocation(clone);
                return true;
            }

            if (property == null ||
                !property.Positions.TryGetValue(PropertyLocationType.CurrentPosition, out var current) ||
                string.IsNullOrWhiteSpace(current.AreaResref))
            {
                return false;
            }

            var area = Area.GetAreaByResref(current.AreaResref);
            if (!GetIsObjectValid(area))
                return false;

            location = Location(area, Vector3(current.X, current.Y, current.Z), current.Orientation);
            return true;
        }

        private static Location OffsetBesideShip(Location hostLocation, int index)
        {
            var position = GetPositionFromLocation(hostLocation);
            var facing = GetFacingFromLocation(hostLocation);
            var (dx, dy) = ShipDockingRules.GetUndockOffset(facing, index);
            return Location(GetAreaFromLocation(hostLocation), Vector3(position.X + dx, position.Y + dy, position.Z), facing);
        }

        private static void JumpPlayer(uint player, Location location)
        {
            AssignCommand(player, () =>
            {
                ClearAllActions();
                ActionJumpToLocation(location);
            });
        }

        // Docking

        /// <summary>
        /// Docks the pilot's ship aboard the player ship they target. Returns false and tells them why when they
        /// cannot (<see cref="ShipDockingRules.GetDockRefusal"/>).
        /// </summary>
        public static bool DockWithTarget(uint player)
        {
            if (!IsPlayerInSpaceMode(player) || !TryGetFlownShip(player, out var visitor))
            {
                SendMessageToPC(player, ColorToken.Red("You must be piloting a starship to dock with another ship."));
                return false;
            }

            if (GetIsDead(player) || _pendingShipLaunches.ContainsKey(player))
                return false;

            var (target, _) = GetCurrentTarget(player);
            if (!TryGetFlownShip(target, out var host))
            {
                SendMessageToPC(player, ColorToken.Red("Target a player's Large or Capital ship in flight to dock with it."));
                return false;
            }

            var request = new ShipDockRequest
            {
                SameShip = target == player || host.Id == visitor.Id,
                HostSize = GetShipSize(host),
                VisitorSize = GetShipSize(visitor),
                // Owners hold Dock on their own ships; a ship of the host's owner may dock whoever flies it.
                HasDockPermission = HasShipPermission(player, host.PropertyId, PropertyPermissionType.Dock) ||
                                    visitor.OwnerPlayerId == host.OwnerPlayerId,
                Distance = GetArea(target) == GetArea(player) ? GetDistanceBetween(player, target) : float.MaxValue,
                HostInCombat = IsShipInCombat(target),
                VisitorInCombat = IsShipInCombat(player),
                HostDockedCount = GetDockedShips(host.Id).Count,
            };

            var refusal = ShipDockingRules.GetDockRefusal(request);
            if (refusal != ShipDockRefusal.None)
            {
                SendMessageToPC(player, ColorToken.Red(ShipDockingRules.DescribeRefusal(refusal, request.HostSize)));
                return false;
            }

            if (!Property.TryGetLoadedInstance(host.PropertyId, out var hostInstance))
            {
                SendMessageToPC(player, ColorToken.Red("That ship's interior is not ready. Please try again shortly."));
                return false;
            }

            var hostEntrance = GetShipEntrance(host, hostInstance);
            var hostName = GetShipName(host);
            var visitorName = GetShipName(visitor);

            // Viewport watchers and anyone in the visitor's interior come aboard with it.
            var watchers = _crewAssignments.Where(x => x.Value.Pilot == player).Select(x => x.Key).ToList();
            var passengers = watchers.ToList();
            if (Property.TryGetLoadedInstance(visitor.PropertyId, out var visitorInstance))
                passengers.AddRange(GetPlayersInArea(visitorInstance.Area));
            passengers = passengers.Where(x => x != player && GetIsObjectValid(x)).Distinct().ToList();

            foreach (var watcher in watchers)
                LeaveCrewStation(watcher, returnToConsole: false);

            visitor.DockedToShipId = host.Id;
            visitor.DockedCrewPlayerIds = passengers.Prepend(player).Select(GetObjectUUID).Distinct().ToList();
            DB.Set(visitor);

            // Without a current position the ship leaves space when its pilot steps out of it.
            var visitorProperty = DB.Get<WorldProperty>(visitor.PropertyId);
            visitorProperty.Positions.Remove(PropertyLocationType.CurrentPosition);
            DB.Set(visitorProperty);

            DeleteLocalLocation(player, "SPACE_INSTANCE_LOCATION");
            DeleteLocalBool(player, "SPACE_INSTANCE_LOCATION_SET");
            ExitSpaceMode(player);

            foreach (var traveller in passengers.Prepend(player))
            {
                SetLocalLocation(traveller, DockedHostLocationVariable, hostEntrance);
                SetLocalBool(traveller, DockedHostLocationSetVariable, true);
                JumpPlayer(traveller, hostEntrance);
            }

            SendMessageToPC(player, $"You dock {visitorName} aboard {hostName}. To undock, use {hostName}'s computer or type /undock aboard it.");
            SendMessageToPC(target, $"{PlayerName.GetDisplayName(target, player)} docks {visitorName} aboard your ship.");
            foreach (var passenger in passengers)
                SendMessageToPC(passenger, $"{visitorName} docks aboard {hostName}, and you go aboard with it.");

            Log.Write(LogGroup.Space, $"Ship {visitor.Id} docked aboard ship {host.Id} (pilot {GetObjectUUID(player)}, {passengers.Count} passengers).");
            return true;
        }

        // Undocking

        /// <summary>
        /// Undocks the first ship aboard the host whose interior the player stands in that they may pilot (theirs
        /// first). Returns false and tells them why when they cannot.
        /// </summary>
        public static bool UndockFromInterior(uint player)
        {
            var host = GetInteriorShip(player, out _);
            var ships = GetUndockableShips(player, host);
            if (ships.Count == 0)
            {
                SendMessageToPC(player, ColorToken.Red("No ship you may pilot is docked aboard this ship."));
                return false;
            }

            return UndockShip(player, ships[0].Id);
        }

        /// <summary>
        /// Undocks a ship from the host whose interior the player stands in. In space the player takes its controls
        /// beside the host, and the ship leaves the host when they arrive; at a starport it goes into the host's dock.
        /// </summary>
        public static bool UndockShip(uint player, string visitorShipId)
        {
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDead(player) || _pendingShipLaunches.ContainsKey(player))
                return false;

            if (IsPlayerInSpaceMode(player) || IsAtCrewStation(player))
            {
                SendMessageToPC(player, ColorToken.Red("Leave the controls or your station before undocking."));
                return false;
            }

            var host = GetInteriorShip(player, out _);
            var visitor = DB.Get<PlayerShip>(visitorShipId);
            if (host == null || visitor == null || visitor.DockedToShipId != host.Id)
            {
                SendMessageToPC(player, ColorToken.Red("That ship is not docked aboard this one."));
                return false;
            }

            if (!HasShipPermission(player, visitor.PropertyId, PropertyPermissionType.PilotShip))
            {
                SendMessageToPC(player, ColorToken.Red("You do not have permission to pilot this starship."));
                return false;
            }

            if (_pendingShipLaunches.Values.Any(x => x.ShipId == visitor.Id))
            {
                SendMessageToPC(player, ColorToken.Red("This ship's controls are in use."));
                return false;
            }

            var hostName = GetShipName(host);
            var visitorName = GetShipName(visitor);
            var hostProperty = DB.Get<WorldProperty>(host.PropertyId);
            var hostInSpace = TryGetShipSpaceLocation(host, hostProperty, out var hostLocation);
            var hostHasDock = hostProperty != null && hostProperty.Positions.ContainsKey(PropertyLocationType.DockPosition);

            switch (ShipDockingRules.ChooseUndockPlacement(hostInSpace, hostHasDock, GetShipSize(visitor)))
            {
                case ShipUndockPlacement.NoDock:
                    SendMessageToPC(player, ColorToken.Red($"{hostName} is not in space or at a dock, so {visitorName} cannot undock. Notify an admin."));
                    return false;
                case ShipUndockPlacement.StarportTooSmall:
                    SendMessageToPC(player, ColorToken.Red($"This starport cannot take {visitorName}. Undock once {hostName} is in space or at a larger starport."));
                    return false;
                case ShipUndockPlacement.Starport:
                    DockAtHostStarport(visitor, hostProperty);
                    SendMessageToPC(player, $"{visitorName} undocks from {hostName} and is now docked at this starport.");
                    Log.Write(LogGroup.Space, $"Ship {visitor.Id} undocked from ship {host.Id} into its starport (player {GetObjectUUID(player)}).");
                    return true;
            }

            // A host between areas (a starlane jump) has no position yet.
            if (!GetIsObjectValid(GetAreaFromLocation(hostLocation)))
            {
                SendMessageToPC(player, ColorToken.Red($"{hostName} is between systems. Please try again shortly."));
                return false;
            }

            if (!CanPlayerUseShip(player, visitor.Status))
            {
                var skillError = GetShipSkillRequirementError(player, visitor.Status);
                SendMessageToPC(player, ColorToken.Red(string.IsNullOrEmpty(skillError)
                    ? "You do not have the ability to pilot this ship."
                    : skillError));
                return false;
            }

            if (!Property.TryResolveEnterableInstance(player, visitor.PropertyId, out var visitorInstance))
                return false;

            var index = Math.Max(0, GetDockedShips(host.Id).FindIndex(x => x.Id == visitor.Id));
            BeginShipLaunch(player, visitor.Id, OffsetBesideShip(hostLocation, index), GetShipEntrance(visitor, visitorInstance), host.Id);
            SendMessageToPC(player, $"You undock {visitorName} from {hostName}.");
            return true;
        }

        /// <summary>
        /// The undocking pilot reached space: their ship leaves the host, and the rest of its crew still aboard the
        /// host go back to its interior. Nothing changes if the host was destroyed meanwhile (it already detached).
        /// </summary>
        private static void CompleteUndockLaunch(uint pilot, PendingShipLaunch launch)
        {
            var visitor = DB.Get<PlayerShip>(launch.ShipId);
            if (visitor == null || visitor.DockedToShipId != launch.UndockFromShipId)
                return;

            var host = DB.Get<PlayerShip>(launch.UndockFromShipId);
            var crew = visitor.DockedCrewPlayerIds?.ToList() ?? new List<string>();
            ClearDockedState(visitor);
            DB.Set(visitor);
            ClearDockedHostReturn(pilot);

            var hostName = GetShipName(host);
            var visitorName = GetShipName(visitor);
            var hostPilot = host == null ? OBJECT_INVALID : GetShipPilot(host.Id);
            if (GetIsObjectValid(hostPilot))
                SendMessageToPC(hostPilot, $"{visitorName} undocks from your ship.");

            if (host != null &&
                Property.TryGetLoadedInstance(host.PropertyId, out var hostInstance) &&
                Property.TryGetLoadedInstance(visitor.PropertyId, out var visitorInstance))
            {
                var entrance = GetShipEntrance(visitor, visitorInstance);
                foreach (var member in GetPlayersInArea(hostInstance.Area))
                {
                    if (member == pilot || !crew.Contains(GetObjectUUID(member)) || IsAtCrewStation(member) ||
                        _pendingShipLaunches.ContainsKey(member))
                    {
                        continue;
                    }

                    ClearDockedHostReturn(member);
                    SendMessageToPC(member, $"{visitorName} undocks from {hostName}, and you return aboard it.");
                    JumpPlayer(member, entrance);
                }
            }

            Log.Write(LogGroup.Space, $"Ship {visitor.Id} undocked from ship {launch.UndockFromShipId} into space (pilot {launch.PlayerId}).");
        }

        /// <summary>
        /// Puts a docked ship into the dock where its host is docked, registered there as the starport dock does,
        /// and undocks it.
        /// </summary>
        private static void DockAtHostStarport(PlayerShip visitor, WorldProperty hostProperty)
        {
            var visitorProperty = DB.Get<WorldProperty>(visitor.PropertyId);
            var dock = hostProperty.Positions[PropertyLocationType.DockPosition];
            hostProperty.Positions.TryGetValue(PropertyLocationType.SpacePosition, out var orbit);

            visitorProperty.Positions.Remove(PropertyLocationType.CurrentPosition);
            visitorProperty.Positions[PropertyLocationType.DockPosition] = CopyLocation(dock);
            if (orbit != null)
                visitorProperty.Positions[PropertyLocationType.SpacePosition] = CopyLocation(orbit);

            // An NPC dock outside a ship-only location becomes the ship's safe dock, as docking there does.
            var isNpcDock = !string.IsNullOrWhiteSpace(dock.AreaResref);
            if (isNpcDock && !StarshipDockReturn.IsShipOnlyArea(Area.GetAreaByResref(dock.AreaResref)))
            {
                visitorProperty.Positions[PropertyLocationType.LastNPCDockPosition] = CopyLocation(dock);
                if (orbit != null)
                    visitorProperty.Positions[PropertyLocationType.LastNPCSpacePosition] = CopyLocation(orbit);
            }

            if (!visitorProperty.ChildPropertyIds.TryGetValue(PropertyChildType.RegisteredStarport, out var registrations))
            {
                registrations = new List<string>();
                visitorProperty.ChildPropertyIds[PropertyChildType.RegisteredStarport] = registrations;
            }

            foreach (var oldStarportId in registrations.ToList())
            {
                var oldStarport = DB.Get<WorldProperty>(oldStarportId);
                if (oldStarport != null && oldStarport.ChildPropertyIds.TryGetValue(PropertyChildType.Starship, out var oldShips))
                {
                    oldShips.Remove(visitorProperty.Id);
                    DB.Set(oldStarport);
                }
            }

            registrations.Clear();

            var starportId = !isNpcDock &&
                             hostProperty.ChildPropertyIds.TryGetValue(PropertyChildType.RegisteredStarport, out var hostRegistrations)
                ? hostRegistrations.FirstOrDefault()
                : null;
            var starport = string.IsNullOrWhiteSpace(starportId) ? null : DB.Get<WorldProperty>(starportId);
            if (starport != null)
            {
                registrations.Add(starport.Id);
                if (!starport.ChildPropertyIds.TryGetValue(PropertyChildType.Starship, out var starportShips))
                {
                    starportShips = new List<string>();
                    starport.ChildPropertyIds[PropertyChildType.Starship] = starportShips;
                }

                if (!starportShips.Contains(visitorProperty.Id))
                    starportShips.Add(visitorProperty.Id);
                DB.Set(starport);
            }

            DB.Set(visitorProperty);
            ClearDockedState(visitor);
            DB.Set(visitor);
        }

        private static PropertyLocation CopyLocation(PropertyLocation location) => new()
        {
            AreaResref = location.AreaResref,
            InstancePropertyId = location.InstancePropertyId,
            X = location.X,
            Y = location.Y,
            Z = location.Z,
            Orientation = location.Orientation,
        };

        /// <summary>
        /// Undocks every ship aboard the host into the host's starport, or leaves it at its own last dock when the
        /// host has none or its dock does not take it. Ship management calls this before a host is unregistered;
        /// a future ship sale or transfer must call it too.
        /// </summary>
        public static void UndockAllToStarport(PlayerShip host, string reason)
        {
            if (host == null)
                return;

            var hostProperty = DB.Get<WorldProperty>(host.PropertyId);
            var hostName = GetShipName(host);
            foreach (var visitor in GetDockedShips(host.Id))
            {
                var visitorName = GetShipName(visitor);
                if (hostProperty != null &&
                    hostProperty.Positions.ContainsKey(PropertyLocationType.DockPosition) &&
                    ShipDockingRules.FitsStarport(GetShipSize(visitor)))
                {
                    DockAtHostStarport(visitor, hostProperty);
                }
                else
                {
                    ClearDockedState(visitor);
                    DB.Set(visitor);
                }

                var owner = DMPlayerDirectory.GetOnlinePlayer(visitor.OwnerPlayerId);
                if (GetIsObjectValid(owner))
                    SendMessageToPC(owner, $"{visitorName} was docked aboard {hostName}, which {reason}. It undocks and is now docked at a starport.");

                Log.Write(LogGroup.Space, $"Ship {visitor.Id} undocked from ship {host.Id} because the host {reason}.");
            }
        }

        // Host destroyed

        /// <summary>
        /// The host ship was destroyed. Every ship docked aboard detaches where it was: it is put in space beside the
        /// wreck, and the players aboard who belong to it (<see cref="ShipDockingRules.AssignReturningPlayers"/>)
        /// leave any station and go back to it, the first who can pilot it at its controls and the rest in its
        /// interior. A ship whose interior is not loaded returns to its last dock instead, and its players share the
        /// host's fate. Returns the players sent back to a docked ship, whom the destruction (and escape pods) skip.
        /// </summary>
        public static HashSet<uint> DetachDockedShips(uint hostPilot)
        {
            var returned = new HashSet<uint>();
            if (!GetIsPC(hostPilot) || !TryGetFlownShip(hostPilot, out var host))
                return returned;

            var visitors = GetDockedShips(host.Id);
            if (visitors.Count == 0)
                return returned;

            var hostLocation = GetLocation(hostPilot);
            var hostName = GetShipName(host);

            var aboard = _crewAssignments.Where(x => x.Value.Pilot == hostPilot).Select(x => x.Key).ToList();
            if (Property.TryGetLoadedInstance(host.PropertyId, out var hostInstance))
                aboard.AddRange(GetPlayersInArea(hostInstance.Area));
            var aboardById = aboard
                .Where(GetIsObjectValid)
                .GroupBy(GetObjectUUID)
                .ToDictionary(x => x.Key, x => x.First());

            var entries = visitors.Select(ToDockVisitor).ToList();
            var assignments = ShipDockingRules.AssignReturningPlayers(aboardById.Keys, entries);

            for (var index = 0; index < visitors.Count; index++)
            {
                var visitor = visitors[index];
                var visitorName = GetShipName(visitor);
                var visitorProperty = DB.Get<WorldProperty>(visitor.PropertyId);
                var returning = assignments.Where(x => x.Value == visitor.Id).Select(x => aboardById[x.Key]).ToList();
                ClearDockedState(visitor);
                DB.Set(visitor);

                if (visitorProperty == null ||
                    !GetIsObjectValid(GetAreaFromLocation(hostLocation)) ||
                    !Property.TryGetLoadedInstance(visitor.PropertyId, out var visitorInstance))
                {
                    if (visitorProperty != null)
                    {
                        visitorProperty.Positions.Remove(PropertyLocationType.CurrentPosition);
                        DB.Set(visitorProperty);
                    }

                    Log.Write(LogGroup.Space, $"Ship {visitor.Id} detached from destroyed ship {host.Id} without a loaded interior and returns to its last dock.");
                    continue;
                }

                var location = OffsetBesideShip(hostLocation, index);
                PlaceShipInSpace(visitor, visitorProperty, location);

                var entrance = GetShipEntrance(visitor, visitorInstance);
                var pilotId = ShipDockingRules.ChooseDetachPilot(entries[index], playerId =>
                    aboardById.TryGetValue(playerId, out var candidate) &&
                    returning.Contains(candidate) &&
                    !GetIsDead(candidate) &&
                    !_pendingShipLaunches.ContainsKey(candidate) &&
                    HasShipPermission(candidate, visitor.PropertyId, PropertyPermissionType.PilotShip) &&
                    CanPlayerUseShip(candidate, visitor.Status));

                foreach (var member in returning)
                {
                    returned.Add(member);
                    if (_pendingShipLaunches.ContainsKey(member))
                        continue;

                    ClearDockedHostReturn(member);
                    if (IsAtCrewStation(member))
                        LeaveCrewStation(member, returnToConsole: false);

                    if (GetObjectUUID(member) == pilotId)
                    {
                        SendMessageToPC(member, ColorToken.Red($"{hostName} is breaking apart! You get back to {visitorName} and take its controls."));
                        BeginShipLaunch(member, visitor.Id, location, entrance);
                    }
                    else
                    {
                        SendMessageToPC(member, ColorToken.Red($"{hostName} is breaking apart! You get back aboard {visitorName}."));
                        JumpPlayer(member, entrance);
                    }
                }

                Log.Write(LogGroup.Space, $"Ship {visitor.Id} detached from destroyed ship {host.Id} with {returning.Count} players aboard.");
            }

            return returned;
        }

        /// <summary>Puts an unpiloted ship in space at the location, as a pilot who logs out leaves theirs.</summary>
        private static void PlaceShipInSpace(PlayerShip ship, WorldProperty property, Location location)
        {
            var position = GetPositionFromLocation(location);
            property.Positions[PropertyLocationType.CurrentPosition] = new PropertyLocation
            {
                AreaResref = GetResRef(GetAreaFromLocation(location)),
                X = position.X,
                Y = position.Y,
                Z = position.Z,
                Orientation = GetFacingFromLocation(location)
            };
            DB.Set(property);

            var clone = CreateObject(ObjectType.Creature, "player_starship", location);
            SetCreatureAppearanceType(clone, GetShipDetailByItemTag(ship.Status.ItemTag).Appearance);
            SetName(clone, property.CustomName);
            _shipClones[ship.Id] = clone;
        }

        /// <summary>A host taking off loads its docked ships' interiors, so they can detach or undock in flight.</summary>
        private static void LoadDockedShipInteriors(string hostShipId)
        {
            foreach (var visitor in GetDockedShips(hostShipId))
                Property.RequestInstanceLoad(visitor.PropertyId);
        }

        // Logging in aboard a host

        private static void ClearDockedHostReturn(uint player)
        {
            DeleteLocalLocation(player, DockedHostLocationVariable);
            DeleteLocalBool(player, DockedHostLocationSetVariable);
        }

        /// <summary>
        /// A player who came aboard with a docked ship and logged out in the host's interior returns there, while
        /// that interior still exists (not after a restart).
        /// </summary>
        private static void RestoreDockedPassengerAfterLogin(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player) || !GetLocalBool(player, DockedHostLocationSetVariable))
                return;

            var location = GetLocalLocation(player, DockedHostLocationVariable);
            if (!GetIsObjectValid(GetAreaFromLocation(location)))
            {
                ClearDockedHostReturn(player);
                return;
            }

            JumpPlayer(player, location);
        }

        /// <summary>
        /// Arriving anywhere but the host's interior (or the entry area at login, or space at one of the host's
        /// stations) means the player left it.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void ForgetDockedHostReturn()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || !GetLocalBool(player, DockedHostLocationSetVariable) || IsAtCrewStation(player))
                return;

            var area = OBJECT_SELF;
            if (area == GetAreaFromLocation(GetLocalLocation(player, DockedHostLocationVariable)) || GetTag(area) == EntryAreaTag)
                return;

            ClearDockedHostReturn(player);
        }
    }
}
