using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Crew stations and viewports (plan section 10). A player at a console in the interior of a Large or Capital
    /// ship in flight takes a seat: their character moves into the space area beside the ship, hidden, ghosted and
    /// plot, their camera attaches to the ship and a copy of them stands at the console. Their hotbar shows their
    /// seat's slots. Every second the server keeps them beside the ship; a camera dropped by an area transition is
    /// attached again when they arrive. They leave with <c>/exit</c>, or when the pilot leaves the controls, the
    /// ship docks or is destroyed, or they leave the captain's party or the ship's area. A viewport watcher uses the
    /// same camera and hidden character with no controls.
    /// </summary>
    public static partial class Space
    {
        private const string CrewPresenceEffectTag = "SPACE_CREW_PRESENCE";
        private const string CrewChairTag = "crew_chair";
        private const float CrewFollowDistance = 1.5f;
        private const float CrewChairReach = 3f;

        private static readonly Dictionary<string, uint> _pilotsByShipId = new();
        private static readonly Dictionary<uint, CrewStationAssignment> _crewAssignments = new();

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void ScheduleCrewStations()
        {
            Scheduler.ScheduleRepeating(ProcessCrewStations, TimeSpan.FromSeconds(1));
        }

        // Pilots

        private static void RegisterShipPilot(string shipId, uint pilot)
        {
            if (!string.IsNullOrWhiteSpace(shipId))
                _pilotsByShipId[shipId] = pilot;
        }

        private static void UnregisterShipPilot(uint pilot)
        {
            foreach (var shipId in _pilotsByShipId.Where(x => x.Value == pilot).Select(x => x.Key).ToList())
                _pilotsByShipId.Remove(shipId);
        }

        /// <summary>The player flying a ship, or OBJECT_INVALID when it is docked or nobody is at its controls.</summary>
        public static uint GetShipPilot(string shipId)
        {
            if (string.IsNullOrWhiteSpace(shipId) || !_pilotsByShipId.TryGetValue(shipId, out var pilot))
                return OBJECT_INVALID;

            return GetIsObjectValid(pilot) && IsPlayerInSpaceMode(pilot) && !GetIsDead(pilot) ? pilot : OBJECT_INVALID;
        }

        // Queries

        /// <summary>Whether the player is at a crew station or a viewport. Such a character cannot act on its own.</summary>
        public static bool IsAtCrewStation(uint player) => _crewAssignments.ContainsKey(player);

        /// <summary>The ship (its pilot) and seat a player works. False for viewport watchers and everyone else.</summary>
        public static bool TryGetCrewStation(uint player, out uint ship, out ShipCrewSeat seat) =>
            SpaceCrew.TryGetSeat(player, out ship, out seat);

        /// <summary>The player who works a slot: the crew member at its station, the pilot for a Helm slot, or OBJECT_INVALID for an empty station.</summary>
        private static uint GetSlotOperator(uint ship, ShipStatus status, int slotNumber)
        {
            if (status == null)
                return ship;

            var seat = ShipCrewSeats.GetSeatForSlot(status.Size, status.SlotTags, slotNumber);
            return seat == ShipCrewSeat.Pilot ? ship : SpaceCrew.GetSeatOccupant(ship, seat);
        }

        /// <summary>Whether a hotbar slot (1-30) of the ship is a crew-station slot.</summary>
        public static bool IsStationSlot(ShipStatus status, int hotbarSlot) =>
            status != null && ShipCrewSeats.GetSeatForSlot(status.Size, status.SlotTags, hotbarSlot) != ShipCrewSeat.Pilot;

        private static int GetPlayerSkillRank(uint player, SkillType skill)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            return dbPlayer?.Skills != null && dbPlayer.Skills.TryGetValue(skill, out var playerSkill) ? playerSkill.Rank : 0;
        }

        /// <summary>
        /// Why a crew member cannot switch a station module on, or an empty string. The module's rank is checked
        /// against the seat's skill (plan section 10, "Gear rank on stations").
        /// </summary>
        private static string GetStationRankError(uint crewMember, ShipCrewSeat seat, ShipStatus.ShipStatusModule module)
        {
            var required = ShipCrewSeats.GetRequiredRank(module?.RequiredSkills);
            if (required <= 0)
                return string.Empty;

            var skill = ShipCrewSeats.GetSeatSkill(seat);
            if (GetPlayerSkillRank(crewMember, skill) >= required)
                return string.Empty;

            return $"You need {Skill.GetSkillDetails(skill).Name} rank {required} to work this module at the {ShipCrewSeats.GetSeatName(seat)} station.";
        }

        /// <summary>
        /// A station slot's effectiveness right now: the crew multiplier while a crew member who meets the module's
        /// rank works the seat, a droid's effectiveness while a droid whose tier meets it does, otherwise 0. Helm
        /// slots are always 1.
        /// </summary>
        public static double GetStationSlotEffectiveness(uint ship, ShipStatus status, int slotNumber)
        {
            var seat = ShipCrewSeats.GetSeatForSlot(status.Size, status.SlotTags, slotNumber);
            if (seat == ShipCrewSeat.Pilot)
                return 1.0;

            var crewMember = SpaceCrew.GetSeatOccupant(ship, seat);
            var crewed = crewMember != OBJECT_INVALID && GetIsObjectValid(crewMember);
            var droid = crewed ? null : SpaceCrew.GetSeatDroid(ship, seat);
            if (droid != null)
            {
                var droidMeetsRank = !TryGetFittedModule(status, slotNumber, out var droidModule) ||
                                     DroidCrewRules.MeetsRank(droidModule.RequiredSkills, droid.Tier);
                return droidMeetsRank ? SpaceCrew.GetDroidEffectiveness(ship, droid) : 0.0;
            }

            var meetsRank = crewed &&
                            (!TryGetFittedModule(status, slotNumber, out var module) ||
                             ShipCrewSeats.MeetsRank(module.RequiredSkills, GetPlayerSkillRank(crewMember, ShipCrewSeats.GetSeatSkill(seat))));

            return ShipCrewSeats.SeatEffectiveness(crewed, meetsRank, crewed ? SpaceCrew.GetCrewEffectiveness(ship) : 0.0);
        }

        /// <summary>The fire context of a crew member working a station slot, or null when the pilot fires.</summary>
        private static SpaceCrewFireContext BuildCrewFireContext(uint ship, uint crewMember, ShipCrewSeat seat)
        {
            if (crewMember == OBJECT_INVALID || crewMember == ship || seat == ShipCrewSeat.Pilot || seat == ShipCrewSeat.Invalid)
                return null;

            return new SpaceCrewFireContext
            {
                Ship = ship,
                CrewMember = crewMember,
                Seat = seat,
                Effectiveness = SpaceCrew.GetCrewEffectiveness(ship),
                Skill = ShipCrewSeats.GetSeatSkill(seat),
            };
        }

        /// <summary>
        /// The context in which a crew member uses one of their own Piloting abilities from their station (plan
        /// section 10): the ship activates it at the station's effectiveness and the crew member is its user. Null,
        /// with the reason, for a viewport watcher or an ability that cannot be used at a station.
        /// </summary>
        public static SpaceCrewFireContext GetCrewStationAbilityContext(uint crewMember, AbilityDetail ability, out string error)
        {
            var seated = TryGetCrewStation(crewMember, out var ship, out var seat);
            error = SpaceCrewAbilityRules.GetStationAbilityError(seated, ability?.CanBeUsedInSpace == true, ability?.SkillType ?? SkillType.Invalid);
            if (!string.IsNullOrEmpty(error))
                return null;

            var station = BuildCrewFireContext(ship, crewMember, seat);
            if (station == null || !IsShipInSpace(ship))
            {
                error = "Your ship is not in flight.";
                return null;
            }

            return new SpaceCrewFireContext
            {
                Ship = ship,
                CrewMember = crewMember,
                Seat = seat,
                Effectiveness = station.Effectiveness,
                Skill = ability.SkillType,
                IsAbility = true,
            };
        }

        /// <summary>The fire context of a droid working a station slot.</summary>
        private static SpaceCrewFireContext BuildDroidFireContext(uint ship, ShipDroidCrew droid)
        {
            return new SpaceCrewFireContext
            {
                Ship = ship,
                CrewMember = OBJECT_INVALID,
                Droid = droid,
                Seat = droid.Seat,
                Effectiveness = SpaceCrew.GetDroidEffectiveness(ship, droid),
                Skill = SkillType.Piloting,
            };
        }

        /// <summary>
        /// Works out the pilot's ship again with its stations as they are now crewed, so station passives count
        /// only while their seat is worked, and returns the power routing to Balanced once no player works the
        /// Engineer seat. Saves the ship.
        /// </summary>
        private static void RecalculateCrewedShip(uint pilot)
        {
            if (!GetIsObjectValid(pilot) || !IsPlayerInSpaceMode(pilot))
                return;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(pilot));
            var dbShip = DB.Get<PlayerShip>(dbPlayer.ActiveShipId);
            if (dbShip == null)
                return;

            var status = dbShip.Status;
            var routingReset = status.PowerRouting != ShipPowerRouting.Balanced &&
                               !GetIsObjectValid(SpaceCrew.GetSeatOccupant(pilot, ShipCrewSeat.Engineer));
            if (routingReset)
                status.PowerRouting = ShipPowerRouting.Balanced;

            var recalculated = RecalculateShipStats(status, GetAbilityScore(pilot, AbilityType.Willpower), false,
                slot => GetStationSlotEffectiveness(pilot, status, slot));
            if (!recalculated && !routingReset)
                return;

            DB.Set(dbShip);
            ApplyShipSpeed(pilot, status);
            if (routingReset)
                SendMessageToPC(pilot, "Nobody works the Engineer station, so the ship's power returns to Balanced.");

            ExecuteScript("pc_target_upd", pilot);
        }

        // Boarding

        private static PlayerShip GetInteriorShip(uint player, out string propertyId)
        {
            propertyId = Property.GetPropertyId(GetArea(player));
            if (string.IsNullOrWhiteSpace(propertyId))
                return null;

            var shipQuery = new DBQuery<PlayerShip>()
                .AddFieldSearch(nameof(PlayerShip.PropertyId), propertyId, false);
            return DB.Search(shipQuery).FirstOrDefault();
        }

        /// <summary>Whether the player may take a crew station aboard the property. DMs always may.</summary>
        public static bool HasCrewStationPermission(uint player, string propertyId)
        {
            if (GetIsDM(player) || GetIsDMPossessed(player))
                return true;

            var permissionQuery = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), propertyId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), GetObjectUUID(player), false);
            var permission = DB.Search(permissionQuery).FirstOrDefault();

            return permission != null &&
                   permission.Permissions.TryGetValue(PropertyPermissionType.CrewStation, out var allowed) &&
                   allowed;
        }

        /// <summary>
        /// Why the player cannot take the seat aboard the ship whose interior they stand in, or an empty string.
        /// <see cref="ShipCrewSeat.Invalid"/> asks for a viewport, which any passenger aboard may use.
        /// </summary>
        public static string GetCrewStationBoardingError(uint player, ShipCrewSeat seat)
        {
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return "Only players can take a crew station.";
            if (GetIsDead(player))
                return "You cannot take a station while dead.";
            if (IsPlayerInSpaceMode(player))
                return "You are already flying a ship.";
            if (IsAtCrewStation(player))
                return "You are already at a station.";

            var dbShip = GetInteriorShip(player, out var propertyId);
            if (dbShip == null)
                return "This console is not aboard a starship.";

            var pilot = GetShipPilot(dbShip.Id);
            if (!GetIsObjectValid(pilot))
                return "The ship must be in flight with a pilot at the controls.";

            if (seat == ShipCrewSeat.Invalid)
                return string.Empty;

            var status = GetShipStatus(pilot);
            if (status == null || seat == ShipCrewSeat.Pilot || !ShipCrewSeats.HasSeat(status.Size, seat))
                return "This ship has no such station.";
            if (!HasCrewStationPermission(player, propertyId))
                return "You do not have permission to crew this starship.";
            if (!Party.IsInParty(pilot, player))
                return "You must be in the captain's party to take a station.";
            if (SpaceCrew.GetSeatOccupant(pilot, seat) != OBJECT_INVALID)
                return $"The {ShipCrewSeats.GetSeatName(seat)} station is taken.";

            return string.Empty;
        }

        /// <summary>
        /// Puts the player at a station (or, with <see cref="ShipCrewSeat.Invalid"/>, a viewport) of the ship whose
        /// interior they stand in. Returns false and tells them why when they cannot.
        /// </summary>
        public static bool BoardCrewStation(uint player, ShipCrewSeat seat)
        {
            var error = GetCrewStationBoardingError(player, seat);
            if (!string.IsNullOrEmpty(error))
            {
                SendMessageToPC(player, ColorToken.Red(error));
                return false;
            }

            var dbShip = GetInteriorShip(player, out var propertyId);
            var pilot = GetShipPilot(dbShip.Id);
            var isViewer = seat == ShipCrewSeat.Invalid;

            // A seat holds a player or a droid, and players come first (DroidCrewRules.PlayerBumpsDroid).
            if (!isViewer && SpaceCrew.GetSeatDroid(pilot, seat) != null)
                BumpDroidCrew(player, dbShip.Id, seat);

            if (!isViewer && !SpaceCrew.AssignSeat(pilot, seat, player))
            {
                SendMessageToPC(player, ColorToken.Red($"The {ShipCrewSeats.GetSeatName(seat)} station is taken."));
                return false;
            }

            var consoleLocation = GetLocation(player);
            var assignment = new CrewStationAssignment
            {
                ShipId = dbShip.Id,
                PropertyId = propertyId,
                Pilot = pilot,
                Seat = seat,
                ConsoleLocation = consoleLocation,
            };
            _crewAssignments[player] = assignment;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            dbPlayer.ActiveCrewShipId = dbShip.Id;
            dbPlayer.SerializedHotBar = CreaturePlugin.SerializeQuickbar(player);

            if (!isViewer)
            {
                // Ground effects stay on the ground, as they do for a pilot: a crew member's stats set the strength of
                // what their station fires.
                StatusEffect.RemoveAllStatusEffects(player);
                for (var effect = GetFirstEffect(player); GetIsEffectValid(effect); effect = GetNextEffect(player))
                {
                    RemoveEffect(player, effect);
                }

                PlayerTemporaryEffects.ApplyCutsceneGhostToPlayer(player);
            }

            DB.Set(dbPlayer);

            if (!isViewer)
                LoadCrewStationHotBar(player, playerId, dbShip.Id, GetShipStatus(pilot), seat);

            assignment.Copy = CreateCrewCopy(player, consoleLocation, isViewer ? "Passenger" : ShipCrewSeats.GetSeatName(seat));
            SetLocalLocation(player, "SPACE_INSTANCE_LOCATION", consoleLocation);
            SetLocalBool(player, "SPACE_INSTANCE_LOCATION_SET", true);

            if (!isViewer)
                RecalculateCrewedShip(pilot);

            var shipName = DB.Get<WorldProperty>(propertyId)?.CustomName;
            if (string.IsNullOrWhiteSpace(shipName))
                shipName = "the ship";

            if (isViewer)
            {
                SendMessageToPC(player, $"You watch from a viewport aboard {shipName}. Type /exit to step away.");
            }
            else
            {
                var seatName = ShipCrewSeats.GetSeatName(seat);
                SendMessageToPC(player, $"You take the {seatName} station aboard {shipName}. Your hotbar shows your station's modules. Type /exit to leave the station.");
                SendMessageToPC(pilot, $"{PlayerName.GetDisplayName(pilot, player)} takes the {seatName} station.");
            }

            MoveCrewToShip(player, pilot);
            return true;
        }

        private static uint CreateCrewCopy(uint player, Location consoleLocation, string name)
        {
            var copy = CopyObject(player, consoleLocation, OBJECT_INVALID, "spaceship_crew_copy");
            SetName(copy, name);
            ChangeToStandardFaction(copy, StandardFaction.Defender);
            TakeGoldFromCreature(GetGold(copy), copy, true);
            ApplyEffectToObject(DurationType.Instant, EffectHeal(GetMaxHitPoints(copy)), copy);

            for (var item = GetFirstItemInInventory(copy); GetIsObjectValid(item); item = GetNextItemInInventory(copy))
            {
                SetDroppableFlag(item, false);
                DestroyObject(item);
            }

            AssignCommand(copy, () => ClearAllActions());

            // A console with a crew chair beside it seats the copy; otherwise it stands at the console.
            var chair = GetNearestObjectByTag(CrewChairTag, copy);
            if (GetIsObjectValid(chair) && GetDistanceBetween(copy, chair) <= CrewChairReach)
            {
                DelayCommand(1f, () => AssignCommand(copy, () => ActionSit(chair)));
            }

            SetPlotFlag(copy, true);
            return copy;
        }

        private static string GetCrewHotBarKey(string playerId, ShipCrewSeat seat) => $"{playerId}:{seat}";

        /// <summary>
        /// Gives the crew member the feats of their seat's active modules and loads their saved station hotbar, or
        /// a new one with those modules in slot order.
        /// </summary>
        private static void LoadCrewStationHotBar(uint crewMember, string playerId, string shipId, ShipStatus status, ShipCrewSeat seat)
        {
            foreach (var feat in ShipModuleFeats.Keys)
            {
                CreaturePlugin.RemoveFeat(crewMember, feat);
            }

            if (status == null)
                return;

            var slots = ShipCrewSeats.GetSlotsForSeat(status.Size, status.SlotTags, seat);
            var feats = new List<FeatType>();
            foreach (var slot in slots)
            {
                if (!TryGetFittedModule(status, slot, out var module) ||
                    !_shipModules.TryGetValue(module.ItemTag, out var detail) ||
                    detail.Type == ShipModuleType.Passive)
                {
                    continue;
                }

                var feat = SlotNumberToFeat(slot);
                CreaturePlugin.AddFeat(crewMember, feat);
                ApplyShipModuleFeat(crewMember, detail, feat);
                feats.Add(feat);
            }

            // The Engineer routes power. The feats are owned before the hotbar loads, so saved slots for them survive.
            if (seat == ShipCrewSeat.Engineer)
            {
                foreach (var feat in PowerRoutingRules.Feats)
                {
                    CreaturePlugin.AddFeat(crewMember, feat);
                    feats.Add(feat);
                }
            }

            var key = GetCrewHotBarKey(playerId, seat);
            var dbShip = DB.Get<PlayerShip>(shipId);
            if (dbShip == null ||
                !dbShip.PlayerHotBars.TryGetValue(key, out var saved) ||
                !CreaturePlugin.DeserializeQuickbar(crewMember, saved))
            {
                const int MaxSlots = 35;
                for (var slot = 0; slot <= MaxSlots; slot++)
                {
                    PlayerPlugin.SetQuickBarSlot(crewMember, slot, PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty));
                }

                for (var index = 0; index < feats.Count && index <= MaxSlots; index++)
                {
                    PlayerPlugin.SetQuickBarSlot(crewMember, index, PlayerQuickBarSlot.UseFeat(feats[index]));
                }

                if (dbShip != null)
                {
                    dbShip.PlayerHotBars[key] = CreaturePlugin.SerializeQuickbar(crewMember);
                    DB.Set(dbShip);
                }
            }

            if (seat == ShipCrewSeat.Engineer)
                EnsurePowerRoutingOnHotBar(crewMember);

            RestoreCrewStationVisuals(crewMember, status, slots);
        }

        /// <summary>Shows a crew member their seat's module icons, lit states and running cycles.</summary>
        private static void RestoreCrewStationVisuals(uint crewMember, ShipStatus status, IEnumerable<int> slots)
        {
            ClearShipModuleRecasts(crewMember);
            foreach (var slot in slots)
            {
                RefreshShipModuleVisual(crewMember, status, slot);
            }
        }

        /// <summary>Jumps the crew member's hidden character to the ship.</summary>
        private static void MoveCrewToShip(uint crewMember, uint pilot)
        {
            var location = GetLocation(pilot);
            if (!GetIsObjectValid(GetAreaFromLocation(location)))
                return;

            AssignCommand(crewMember, () =>
            {
                ClearAllActions();
                ActionJumpToLocation(location);
            });
        }

        /// <summary>Hides the crew member's character beside the ship and attaches their camera to the ship.</summary>
        private static void ApplyCrewPresence(uint crewMember, uint pilot)
        {
            SetPlotFlag(crewMember, true);

            var hasPresence = false;
            for (var effect = GetFirstEffect(crewMember); GetIsEffectValid(effect); effect = GetNextEffect(crewMember))
            {
                if (GetEffectTag(effect) == CrewPresenceEffectTag)
                {
                    hasPresence = true;
                    break;
                }
            }

            // The crew member's own client still draws their character (it must stay visible to itself), so it
            // stood on top of the ship; cutscene invisibility keeps it from being drawn.
            if (!hasPresence)
            {
                ApplyEffectToObject(DurationType.Permanent,
                    TagEffect(SupernaturalEffect(EffectLinkEffects(EffectCutsceneGhost(),
                        EffectVisualEffect(VisualEffect.Vfx_Dur_Cutscene_Invisibility))), CrewPresenceEffectTag), crewMember);
            }

            VisibilityPlugin.SetVisibilityOverride(OBJECT_INVALID, crewMember, VisibilityType.Hidden);
            VisibilityPlugin.SetVisibilityOverride(crewMember, crewMember, VisibilityType.Visible);

            // The client must know the ship before the camera can attach to it.
            DelayCommand(0.5f, () =>
            {
                if (_crewAssignments.TryGetValue(crewMember, out var assignment) &&
                    assignment.Pilot == pilot &&
                    GetArea(crewMember) == GetArea(pilot))
                {
                    AttachCamera(crewMember, pilot);
                }
            });
        }

        /// <summary>Gives the character back its own visibility, targeting and camera.</summary>
        private static void RemoveCrewPresence(uint player)
        {
            AttachCamera(player, OBJECT_INVALID);
            RemoveEffectByTag(player, CrewPresenceEffectTag);
            VisibilityPlugin.SetVisibilityOverride(OBJECT_INVALID, player, VisibilityType.Default);
            VisibilityPlugin.SetVisibilityOverride(player, player, VisibilityType.Default);
            SetPlotFlag(player, false);
        }

        /// <summary>
        /// A crew member arriving in an area: in the ship's area they are hidden again and their camera, which the
        /// transition dropped, is attached to the ship. Arriving anywhere else means they left the ship.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void ReattachCrewCamera()
        {
            var player = GetEnteringObject();
            if (!_crewAssignments.TryGetValue(player, out var assignment))
                return;

            var area = GetArea(player);
            if (area == GetArea(assignment.Pilot))
            {
                ApplyCrewPresence(player, assignment.Pilot);
                return;
            }

            // Another space area means the ship jumped on ahead (a starlane) and the next tick brings the crew member
            // to it. Anywhere else except the console's interior means they left the ship.
            if (area != GetAreaFromLocation(assignment.ConsoleLocation) && !IsSpaceArea(area))
                LeaveCrewStation(player, "You left the ship, so you leave your station.");
        }

        /// <summary>Whether an area is a space area (flagged SPACE, or named "Space - ...").</summary>
        public static bool IsSpaceArea(uint area)
        {
            return GetIsObjectValid(area) && (GetLocalBool(area, "SPACE") || GetName(area).StartsWith("Space -"));
        }

        // Leaving

        /// <summary>
        /// Takes the player off their station or viewport and, unless <paramref name="returnToConsole"/> is false,
        /// back to the console. Their seat's modules switch off, the ship loses the station's passives, and their
        /// character, hotbar and camera are their own again.
        /// </summary>
        public static void LeaveCrewStation(uint player, string message = null, bool returnToConsole = true)
        {
            if (!_crewAssignments.Remove(player, out var assignment))
                return;

            var pilot = assignment.Pilot;
            var wasSeated = SpaceCrew.ReleaseSeat(player);
            if (wasSeated && GetIsObjectValid(pilot) && IsPlayerInSpaceMode(pilot))
            {
                var status = GetShipStatus(pilot);
                if (status != null)
                {
                    foreach (var slot in ShipCrewSeats.GetSlotsForSeat(status.Size, status.SlotTags, assignment.Seat))
                    {
                        if (IsShipToggleOn(status, slot))
                            TurnOffToggle(pilot, status, slot);
                    }
                }

                RecalculateCrewedShip(pilot);
                SendMessageToPC(pilot, $"{PlayerName.GetDisplayName(pilot, player)} leaves the {ShipCrewSeats.GetSeatName(assignment.Seat)} station.");
            }

            RestoreCrewMember(player, assignment);

            if (GetIsObjectValid(assignment.Copy))
                DestroyObject(assignment.Copy);

            DeleteLocalLocation(player, "SPACE_INSTANCE_LOCATION");
            DeleteLocalBool(player, "SPACE_INSTANCE_LOCATION_SET");

            if (!string.IsNullOrWhiteSpace(message))
                SendMessageToPC(player, message);

            if (!returnToConsole)
                return;

            var console = assignment.ConsoleLocation;
            if (GetIsObjectValid(GetAreaFromLocation(console)))
            {
                AssignCommand(player, () =>
                {
                    ClearAllActions();
                    ActionJumpToLocation(console);
                });
            }
            else
            {
                PersistentLocation.LoadLocation(player);
            }
        }

        /// <summary>Restores what boarding changed on the character and saves their station hotbar.</summary>
        private static void RestoreCrewMember(uint player, CrewStationAssignment assignment)
        {
            RemoveCrewPresence(player);
            ClearCurrentTarget(player);

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (!assignment.IsViewer)
            {
                // Station statuses (Command orders) end with the station, as a pilot's end with the flight.
                StatusEffect.RemoveAllStatusEffects(player);

                var dbShip = DB.Get<PlayerShip>(assignment.ShipId);
                if (dbShip != null)
                {
                    dbShip.PlayerHotBars[GetCrewHotBarKey(playerId, assignment.Seat)] = CreaturePlugin.SerializeQuickbar(player);
                    DB.Set(dbShip);
                }

                foreach (var feat in ShipModuleFeats.Keys)
                {
                    CreaturePlugin.RemoveFeat(player, feat);
                }

                RemovePowerRoutingFeats(player);
                ClearShipModuleRecasts(player, dbPlayer);
                RestoreGroundHotBar(player, dbPlayer);
            }

            dbPlayer.ActiveCrewShipId = string.Empty;
            DB.Set(dbPlayer);

            Enmity.RemoveCreatureEnmity(player);
            PlayerTemporaryEffects.ApplyCutsceneGhostToPlayer(player);
            Ability.ReapplyPlayerAuraAOE(player);
        }

        /// <summary>Loads the character's own hotbar, or an empty one when it cannot be read.</summary>
        private static void RestoreGroundHotBar(uint player, Player dbPlayer)
        {
            if (!string.IsNullOrWhiteSpace(dbPlayer.SerializedHotBar) &&
                CreaturePlugin.DeserializeQuickbar(player, dbPlayer.SerializedHotBar))
            {
                return;
            }

            for (var slot = 0; slot <= 35; slot++)
            {
                PlayerPlugin.SetQuickBarSlot(player, slot, PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty));
            }

            dbPlayer.SerializedHotBar = CreaturePlugin.SerializeQuickbar(player);
        }

        /// <summary>Returns every crew member and viewport watcher aboard the pilot's ship to their console.</summary>
        private static void ReleaseShipCrew(uint pilot, string message)
        {
            foreach (var crewMember in _crewAssignments.Where(x => x.Value.Pilot == pilot).Select(x => x.Key).ToList())
            {
                LeaveCrewStation(crewMember, message);
            }
        }


        /// <summary>
        /// A crew member disconnected. Their seat is freed now; their character is put back on its feet at the next
        /// login (<see cref="RestoreCrewMemberAfterLogin"/>), since changes made while a player leaves are not saved.
        /// </summary>
        private static void CrewMemberDisconnected(uint player)
        {
            if (!_crewAssignments.Remove(player, out var assignment))
                return;

            if (SpaceCrew.ReleaseSeat(player) && GetIsObjectValid(assignment.Pilot) && IsPlayerInSpaceMode(assignment.Pilot))
            {
                var status = GetShipStatus(assignment.Pilot);
                if (status != null)
                {
                    foreach (var slot in ShipCrewSeats.GetSlotsForSeat(status.Size, status.SlotTags, assignment.Seat))
                    {
                        if (IsShipToggleOn(status, slot))
                            TurnOffToggle(assignment.Pilot, status, slot);
                    }
                }

                RecalculateCrewedShip(assignment.Pilot);
                SendMessageToPC(assignment.Pilot, $"{PlayerName.GetDisplayName(assignment.Pilot, player)} leaves the {ShipCrewSeats.GetSeatName(assignment.Seat)} station.");
            }

            if (GetIsObjectValid(assignment.Copy))
                DestroyObject(assignment.Copy);
        }

        /// <summary>
        /// A character that disconnected at a station or viewport gets its own hotbar, feats, visibility and camera
        /// back. The space login flow then returns it to the console it left from when that interior still exists.
        /// </summary>
        private static void RestoreCrewMemberAfterLogin(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null || string.IsNullOrWhiteSpace(dbPlayer.ActiveCrewShipId))
                return;

            _crewAssignments.Remove(player);
            SpaceCrew.ReleaseSeat(player);
            RemoveCrewPresence(player);
            StatusEffect.RemoveAllStatusEffects(player);

            foreach (var feat in ShipModuleFeats.Keys)
            {
                CreaturePlugin.RemoveFeat(player, feat);
            }

            RemovePowerRoutingFeats(player);
            ClearShipModuleRecasts(player, dbPlayer);
            RestoreGroundHotBar(player, dbPlayer);
            dbPlayer.ActiveCrewShipId = string.Empty;
            DB.Set(dbPlayer);
            PlayerTemporaryEffects.ApplyCutsceneGhostToPlayer(player);

            // The interior it left from may be gone (a server restart); the saved location takes over then.
            if (GetLocalBool(player, "SPACE_INSTANCE_LOCATION_SET") &&
                !GetIsObjectValid(GetAreaFromLocation(GetLocalLocation(player, "SPACE_INSTANCE_LOCATION"))))
            {
                DeleteLocalLocation(player, "SPACE_INSTANCE_LOCATION");
                DeleteLocalBool(player, "SPACE_INSTANCE_LOCATION_SET");
                PersistentLocation.LoadLocation(player);
            }
        }

        // The loop

        /// <summary>
        /// Every second: keeps each crew member's hidden character beside the ship, and returns them to the console
        /// when the pilot has left the controls or they have left the captain's party.
        /// </summary>
        private static void ProcessCrewStations()
        {
            if (_crewAssignments.Count == 0)
                return;

            foreach (var (crewMember, assignment) in _crewAssignments.ToList())
            {
                if (!GetIsObjectValid(crewMember))
                {
                    _crewAssignments.Remove(crewMember);
                    if (SpaceCrew.ReleaseSeat(crewMember))
                        RecalculateCrewedShip(assignment.Pilot);
                    continue;
                }

                var pilot = assignment.Pilot;
                if (!GetIsObjectValid(pilot) || !IsPlayerInSpaceMode(pilot) || GetIsDead(pilot))
                {
                    LeaveCrewStation(crewMember, "The ship's pilot has left the controls, so you leave your station.");
                    continue;
                }

                if (!assignment.IsViewer && !Party.IsInParty(pilot, crewMember))
                {
                    LeaveCrewStation(crewMember, "You are no longer in the captain's party, so you leave your station.");
                    continue;
                }

                var pilotArea = GetArea(pilot);
                if (!GetIsObjectValid(pilotArea))
                    continue;

                if (GetArea(crewMember) != pilotArea || GetDistanceBetween(crewMember, pilot) > CrewFollowDistance)
                    MoveCrewToShip(crewMember, pilot);
            }
        }
    }
}
