using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Droid crew (plan section 10, "Droid crew"). The captain posts one of their own player-built droids at an
    /// empty crew station from the crew console or the ship's computer. The posting is saved on the ship's record;
    /// the droid's controller stays with its owner, cursed, and the droid is never spawned, so it does not count
    /// toward the one-droid follow limit. While someone flies the ship, its droids work their stations every
    /// second through the NPC ship routines: weapons at the pilot's hostile target, repairers and shield modules
    /// once a quarter of the pool is missing. They work at their tier's effectiveness (<see cref="DroidCrewRules"/>)
    /// and switch on modules up to their tier's rank. A player taking a droid's station, the ship's destruction,
    /// a recall, or the controller leaving its owner ends the posting.
    /// </summary>
    public static partial class Space
    {
        private const string ShipComputerVariable = "SPACE_SHIP_COMPUTER";

        private static int _droidControllerCheckTick;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void ScheduleDroidCrew()
        {
            Scheduler.ScheduleRepeating(ProcessDroidCrew, TimeSpan.FromSeconds(1));
        }

        // Queries

        /// <summary>The droids posted aboard a ship, by its record.</summary>
        public static List<ShipDroidCrew> GetPostedDroids(PlayerShip dbShip) =>
            dbShip?.DroidCrew ?? new List<ShipDroidCrew>();

        /// <summary>The droid controllers a player can post: carried, not posted and not following them.</summary>
        public static List<uint> GetPostableDroids(uint player)
        {
            var active = Droid.GetActiveController(player);
            return Droid.GetControllers(player)
                .Where(controller => controller != active && !Droid.IsPosted(controller))
                .ToList();
        }

        /// <summary>
        /// Whether a docked ship's computer opens the crew menu first: the hull has crew stations and the player
        /// may crew it and carries a droid or has droids posted aboard. Everyone else takes the controls at once.
        /// </summary>
        private static bool ShouldOfferDroidCrewMenu(uint player, PlayerShip dbShip, string propertyId)
        {
            if (dbShip?.Status == null || ShipCrewSeats.GetStationSeats(dbShip.Status.Size).Count == 0)
                return false;

            if (!HasCrewStationPermission(player, propertyId))
                return false;

            return GetPostedDroids(dbShip).Count > 0 || Droid.GetControllers(player).Count > 0;
        }

        /// <summary>Whether the player may post and recall droids aboard the ship whose interior they stand in.</summary>
        public static bool CanManageDroidCrew(uint player)
        {
            var dbShip = GetInteriorShip(player, out var propertyId);
            return dbShip?.Status != null &&
                   ShipCrewSeats.GetStationSeats(dbShip.Status.Size).Count > 0 &&
                   GetIsPC(player) && !GetIsDM(player) && !GetIsDMPossessed(player) &&
                   HasCrewStationPermission(player, propertyId);
        }

        /// <summary>The ship record of the interior the player stands in, or null.</summary>
        public static PlayerShip GetInteriorShipRecord(uint player) => GetInteriorShip(player, out _);

        private static string GetShipName(PlayerShip dbShip)
        {
            var name = dbShip == null ? null : DB.Get<WorldProperty>(dbShip.PropertyId)?.CustomName;
            return string.IsNullOrWhiteSpace(name) ? "the ship" : name;
        }

        // Posting and recalling

        /// <summary>
        /// Why the player cannot post the droid at the seat of the ship whose interior they stand in, or an empty
        /// string.
        /// </summary>
        public static string GetDroidPostingError(uint player, ShipCrewSeat seat, uint controller)
        {
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return "Only players can post droids.";

            var dbShip = GetInteriorShip(player, out var propertyId);
            if (dbShip?.Status == null)
                return "This console is not aboard a starship.";

            var pilot = GetShipPilot(dbShip.Id);
            return DroidCrewRules.GetPostingError(
                HasCrewStationPermission(player, propertyId),
                seat != ShipCrewSeat.Pilot && seat != ShipCrewSeat.Invalid && ShipCrewSeats.HasSeat(dbShip.Status.Size, seat),
                GetIsObjectValid(pilot) && GetIsObjectValid(SpaceCrew.GetSeatOccupant(pilot, seat)),
                GetPostedDroids(dbShip).Any(x => x.Seat == seat),
                GetIsObjectValid(controller) && GetResRef(controller) == Droid.DroidControlItemResref &&
                Droid.GetControllers(player).Contains(controller),
                GetIsObjectValid(controller) && Droid.IsPosted(controller),
                GetIsObjectValid(controller) && controller == Droid.GetActiveController(player),
                ShipCrewSeats.GetSeatName(seat));
        }

        /// <summary>
        /// Posts the droid at the seat of the ship whose interior the player stands in. While the ship flies, the
        /// droid takes the station at once. Returns false and tells the player why when it cannot.
        /// </summary>
        public static bool PostDroid(uint player, ShipCrewSeat seat, uint controller)
        {
            var error = GetDroidPostingError(player, seat, controller);
            if (!string.IsNullOrEmpty(error))
            {
                SendMessageToPC(player, ColorToken.Red(error));
                return false;
            }

            var dbShip = GetInteriorShip(player, out _);
            var details = Droid.LoadDroidItemPropertyDetails(controller);
            var posting = new ShipDroidCrew
            {
                Seat = seat,
                OwnerPlayerId = GetObjectUUID(player),
                DroidItemId = Droid.GetDroidId(controller),
                Name = Droid.GetDroidName(controller),
                Tier = DroidCrewRules.ClampTier(details.Tier),
                PostedAt = DateTime.UtcNow,
            };

            dbShip.DroidCrew ??= new List<ShipDroidCrew>();
            dbShip.DroidCrew.Add(posting);
            DB.Set(dbShip);
            Droid.MarkPosted(controller, dbShip.Id);

            var seatName = ShipCrewSeats.GetSeatName(seat);
            var pilot = GetShipPilot(dbShip.Id);
            if (GetIsObjectValid(pilot) && SpaceCrew.AssignDroidSeat(pilot, posting))
            {
                RecalculateCrewedShip(pilot);
                if (pilot != player)
                    SendMessageToPC(pilot, $"{posting.Name} takes the {seatName} station.");
            }

            var effectiveness = DroidCrewRules.BaseEffectiveness(seat, posting.Tier);
            SendMessageToPC(player,
                $"{posting.Name} takes the {seatName} station aboard {GetShipName(dbShip)}. A tier {posting.Tier} droid works it at " +
                $"{effectiveness:P0} and switches on modules up to rank {DroidCrewRules.MaximumModuleRank(posting.Tier)}. " +
                "It stays at its post until recalled.");
            return true;
        }

        /// <summary>
        /// Recalls the droid at a seat of the ship whose interior the player stands in. The captain or the droid's
        /// owner may recall it.
        /// </summary>
        public static bool RecallDroid(uint player, ShipCrewSeat seat)
        {
            var dbShip = GetInteriorShip(player, out var propertyId);
            var posting = GetPostedDroids(dbShip).FirstOrDefault(x => x.Seat == seat);
            if (posting == null)
            {
                SendMessageToPC(player, ColorToken.Red("No droid works that station."));
                return false;
            }

            if (posting.OwnerPlayerId != GetObjectUUID(player) && !HasCrewStationPermission(player, propertyId))
            {
                SendMessageToPC(player, ColorToken.Red("You do not have permission to crew this starship."));
                return false;
            }

            var recaller = player;
            EndDroidPosting(dbShip.Id, posting.DroidItemId,
                owner => owner == recaller ? "you recalled it" : $"{PlayerName.GetDisplayName(owner, recaller)} recalled it");
            SendMessageToPC(player, $"{posting.Name} leaves the {ShipCrewSeats.GetSeatName(seat)} station.");
            return true;
        }

        /// <summary>A player takes the seat a droid holds: the droid goes back to its owner.</summary>
        private static void BumpDroidCrew(uint player, string shipId, ShipCrewSeat seat)
        {
            var pilot = GetShipPilot(shipId);
            var droid = SpaceCrew.GetSeatDroid(pilot, seat);
            if (droid == null)
                return;

            var taker = player;
            EndDroidPosting(shipId, droid.DroidItemId,
                owner => $"{PlayerName.GetDisplayName(owner, taker)} took the {ShipCrewSeats.GetSeatName(seat)} station");
            SendMessageToPC(player, $"{droid.Name} steps aside and goes back to its owner.");
        }

        /// <summary>
        /// Ends a droid's posting: removes it from the ship's record, takes it off its seat while the ship flies, and
        /// gives the controller back to its owner when they are online (otherwise at their next login).
        /// </summary>
        private static void EndDroidPosting(string shipId, string droidItemId, Func<uint, string> reasonForOwner)
        {
            var dbShip = DB.Get<PlayerShip>(shipId);
            var posting = GetPostedDroids(dbShip).FirstOrDefault(x => x.DroidItemId == droidItemId);
            if (posting == null)
                return;

            dbShip.DroidCrew.Remove(posting);
            DB.Set(dbShip);

            var pilot = GetShipPilot(shipId);
            if (GetIsObjectValid(pilot) && SpaceCrew.GetSeatDroid(pilot, posting.Seat)?.DroidItemId == posting.DroidItemId)
            {
                UnseatDroid(pilot, posting.Seat);
                SendMessageToPC(pilot, $"{posting.Name} leaves the {ShipCrewSeats.GetSeatName(posting.Seat)} station.");
            }

            ReturnDroidToOwner(posting, dbShip, reasonForOwner);
        }

        /// <summary>Uncurses the controller of a droid whose posting ended and tells its owner, when they are online.</summary>
        private static void ReturnDroidToOwner(ShipDroidCrew posting, PlayerShip dbShip, Func<uint, string> reasonForOwner)
        {
            var owner = DMPlayerDirectory.GetOnlinePlayer(posting.OwnerPlayerId);
            if (!GetIsObjectValid(owner))
                return;

            var controller = Droid.FindController(owner, posting.DroidItemId);
            if (GetIsObjectValid(controller))
                Droid.ClearPosted(controller);

            var reason = reasonForOwner?.Invoke(owner);
            SendMessageToPC(owner,
                $"{posting.Name} is no longer posted at the {ShipCrewSeats.GetSeatName(posting.Seat)} station aboard {GetShipName(dbShip)}" +
                (string.IsNullOrWhiteSpace(reason) ? "." : $": {reason}."));
        }

        /// <summary>
        /// The ship was destroyed: every droid posted aboard goes back to its owner, who keeps it (escape pods are
        /// W2d). Changes the record; the caller saves it.
        /// </summary>
        private static void ReturnDroidCrew(PlayerShip dbShip, string reason)
        {
            var postings = GetPostedDroids(dbShip).ToList();
            if (postings.Count == 0)
                return;

            dbShip.DroidCrew = new List<ShipDroidCrew>();
            foreach (var posting in postings)
            {
                ReturnDroidToOwner(posting, dbShip, _ => $"the ship {reason}, and it made its way back to you");
            }
        }

        // Flight

        /// <summary>
        /// The pilot is launching: the ship's droids take their seats. A posting whose seat the hull no longer has,
        /// whose owner is gone, or whose controller its online owner no longer carries ends here; it is removed from
        /// <paramref name="dbShip"/>, which the caller saves.
        /// </summary>
        private static (List<ShipDroidCrew> Seated, List<ShipDroidCrew> Ended) SeatDroidCrew(uint pilot, PlayerShip dbShip)
        {
            SpaceCrew.ClearDroidCrew(pilot);

            var seated = new List<ShipDroidCrew>();
            var ended = new List<ShipDroidCrew>();
            foreach (var posting in GetPostedDroids(dbShip).ToList())
            {
                var dbOwner = DB.Get<Player>(posting.OwnerPlayerId);
                var owner = DMPlayerDirectory.GetOnlinePlayer(posting.OwnerPlayerId);
                var valid = dbOwner != null && !dbOwner.IsDeleted &&
                            ShipCrewSeats.GetStationSeats(dbShip.Status.Size).Contains(posting.Seat) &&
                            (!GetIsObjectValid(owner) || GetIsObjectValid(Droid.FindController(owner, posting.DroidItemId)));

                if (valid && SpaceCrew.AssignDroidSeat(pilot, posting))
                {
                    seated.Add(posting);
                    continue;
                }

                dbShip.DroidCrew.Remove(posting);
                ended.Add(posting);
            }

            return (seated, ended);
        }

        /// <summary>Tells the pilot which droids took their stations, and the owners of those that could not.</summary>
        private static void AnnounceDroidCrew(uint pilot, PlayerShip dbShip, (List<ShipDroidCrew> Seated, List<ShipDroidCrew> Ended) crew)
        {
            foreach (var posting in crew.Ended)
            {
                ReturnDroidToOwner(posting, dbShip, _ => "it could not take its post");
            }

            if (crew.Seated.Count == 0)
                return;

            var names = string.Join(", ", crew.Seated.Select(x => $"{x.Name} ({ShipCrewSeats.GetSeatName(x.Seat)})"));
            SendMessageToPC(pilot, $"Your droid crew takes their stations: {names}.");
        }

        /// <summary>The ship docked, lost its pilot or was destroyed: its droids stop working. Postings are kept.</summary>
        private static void UnloadDroidCrew(uint pilot)
        {
            SpaceCrew.ClearDroidCrew(pilot);
        }

        /// <summary>Takes a droid off its seat while the ship flies: its modules switch off and the ship loses its passives.</summary>
        private static void UnseatDroid(uint pilot, ShipCrewSeat seat)
        {
            if (!SpaceCrew.ReleaseDroidSeat(pilot, seat))
                return;

            var status = GetShipStatus(pilot);
            if (status != null)
            {
                foreach (var slot in ShipCrewSeats.GetSlotsForSeat(status.Size, status.SlotTags, seat))
                {
                    if (IsShipToggleOn(status, slot))
                        TurnOffToggle(pilot, status, slot);
                }
            }

            RecalculateCrewedShip(pilot);
        }

        // The loop

        /// <summary>
        /// Every second: each flying ship's droids work their stations. Every few seconds the droids of online
        /// owners are checked against the controllers those owners carry.
        /// </summary>
        private static void ProcessDroidCrew()
        {
            var ships = SpaceCrew.GetShipsWithDroidCrew();
            if (ships.Count == 0)
                return;

            var checkControllers = ++_droidControllerCheckTick >= DroidCrewRules.ControllerCheckSeconds;
            if (checkControllers)
                _droidControllerCheckTick = 0;

            foreach (var pilot in ships)
            {
                if (!GetIsObjectValid(pilot) || !IsPlayerInSpaceMode(pilot))
                {
                    SpaceCrew.ClearDroidCrew(pilot);
                    continue;
                }

                if (checkControllers)
                    CheckDroidControllers(pilot);

                if (GetIsDead(pilot) || IsShipIncapacitated(pilot))
                    continue;

                RunDroidStations(pilot);
            }
        }

        /// <summary>Ends the posting of every droid whose online owner no longer carries its controller.</summary>
        private static void CheckDroidControllers(uint pilot)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(pilot));
            if (dbPlayer == null)
                return;

            foreach (var droid in SpaceCrew.GetDroidCrew(pilot))
            {
                var owner = DMPlayerDirectory.GetOnlinePlayer(droid.OwnerPlayerId);
                if (GetIsObjectValid(owner) && !GetIsObjectValid(Droid.FindController(owner, droid.DroidItemId)))
                    EndDroidPosting(dbPlayer.ActiveShipId, droid.DroidItemId, _ => "its controller is gone");
            }
        }

        /// <summary>
        /// The droid behavior, through the routines NPC ships use (<see cref="UpdateRepairToggles"/> and
        /// <see cref="TurnOnWeaponToggles"/>): shield and hull support at <see cref="DroidCrewRules.RepairThreshold"/>,
        /// and weapons at the pilot's hostile target, each engaged once per pick of target.
        /// </summary>
        private static void RunDroidStations(uint pilot)
        {
            var status = GetShipStatus(pilot);
            if (status == null)
                return;

            var (target, targetStatus) = GetCurrentTarget(pilot);
            var hostile = GetIsObjectValid(target) && targetStatus != null && !GetIsDead(target) && GetIsEnemy(target, pilot);
            var distance = hostile ? GetDistanceBetween(pilot, target) : 0f;

            foreach (var droid in SpaceCrew.GetDroidCrew(pilot))
            {
                var posted = droid;
                var slots = new List<(FeatType Feat, ShipStatus.ShipStatusModule Module)>();
                foreach (var slot in ShipCrewSeats.GetSlotsForSeat(status.Size, status.SlotTags, droid.Seat))
                {
                    if (TryGetFittedModule(status, slot, out var module) &&
                        _shipModules.ContainsKey(module.ItemTag) &&
                        DroidCrewRules.MeetsRank(module.RequiredSkills, droid.Tier))
                    {
                        slots.Add((SlotNumberToFeat(slot), module));
                    }
                }

                if (slots.Count == 0)
                    continue;

                UpdateRepairToggles(pilot, status, slots, true, DroidCrewRules.RepairThreshold,
                    DroidCrewRules.RunsWhenShieldLow, DroidCrewRules.RunsWhenHullLow,
                    feat => AssignCommand(pilot, () => ActivateShipModuleCore(pilot, feat, true, droid: posted)));

                if (!hostile)
                    continue;

                TurnOnWeaponToggles(pilot, status, slots, target, targetStatus, distance,
                    slot => DroidCrewRules.MayEngageAgain(target, SpaceCrew.GetDroidEngagement(pilot, slot)),
                    (feat, slot) =>
                    {
                        SpaceCrew.RecordDroidEngagement(pilot, slot, target);
                        AssignCommand(pilot, () => ActivateShipModuleCore(pilot, feat, true, droid: posted));
                    });
            }
        }

        // Controllers

        /// <summary>
        /// A player's posted droid controllers are checked at login: one whose posting ended while they were away
        /// (a recall, a player taking the seat, the ship destroyed) is theirs again.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void ReturnEndedDroidPostings()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var playerId = GetObjectUUID(player);
            foreach (var controller in Droid.GetControllers(player))
            {
                var shipId = Droid.GetPostedShipId(controller);
                if (string.IsNullOrWhiteSpace(shipId))
                    continue;

                var droidId = Droid.GetDroidId(controller);
                var stillPosted = GetPostedDroids(DB.Get<PlayerShip>(shipId))
                    .Any(x => x.DroidItemId == droidId && x.OwnerPlayerId == playerId);
                if (stillPosted)
                {
                    SetItemCursedFlag(controller, true);
                    continue;
                }

                Droid.ClearPosted(controller);
                SendMessageToPC(player, $"{Droid.GetDroidName(controller)} is no longer posted aboard a ship and is back with you.");
            }
        }

        /// <summary>A posted droid's controller that leaves its owner (destroyed or taken) frees the droid's seat.</summary>
        [NWNEventHandler(ScriptName.OnModuleUnacquire)]
        public static void FreePostedDroidSeat()
        {
            var owner = GetModuleItemLostBy();
            var controller = GetModuleItemLost();
            if (!GetIsPC(owner) || GetResRef(controller) != Droid.DroidControlItemResref || !Droid.IsPosted(controller))
                return;

            var shipId = Droid.GetPostedShipId(controller);
            var droidId = Droid.GetDroidId(controller);

            // The item may only have moved inside the owner's inventory; check once the move is done.
            DelayCommand(0.1f, () =>
            {
                if (GetIsObjectValid(owner) && GetIsObjectValid(Droid.FindController(owner, droidId)))
                    return;

                EndDroidPosting(shipId, droidId, _ => "its controller is gone");
            });
        }
    }
}
