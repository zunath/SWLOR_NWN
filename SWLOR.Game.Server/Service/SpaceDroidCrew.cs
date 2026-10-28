using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.HoldingService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SlicingService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Droid crew (plan section 10, "Droid crew") and droid bays (section 27). The captain posts one of their own
    /// player-built droids at an empty crew station, or in the droid bay of a Mining Barge, Industrial or Logistics
    /// hull, from the crew console or the ship's computer. The posting is saved on the ship's record; the droid's
    /// controller stays with its owner, cursed, and the droid is never spawned, so it does not count toward the
    /// one-droid follow limit. While someone flies the ship, its station droids work their stations every second
    /// through the NPC ship routines: weapons at the pilot's hostile target, repairers and shield modules once a
    /// quarter of the pool is missing, and a Probe Launcher when they run Survey Protocol. They work at their
    /// Industry's effectiveness (<see cref="DroidCrewRules"/>) and switch on modules up to their tier's rank. A bay
    /// droid adds its Industry to the ship's yields or repairs. A player taking a droid's station, the ship's
    /// destruction, a recall, or the controller leaving its owner ends the posting.
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

        /// <summary>Whether a ship's hull has a droid bay (plan section 27: Mining Barge, Industrial and Logistics).</summary>
        public static bool HasDroidBay(ShipStatus status) =>
            status != null && FindShipDetail(status.ItemTag)?.HasDroidBay == true;

        /// <summary>Where a droid can work aboard a ship: its crew stations, then its droid bay.</summary>
        public static List<ShipCrewSeat> GetDroidPosts(ShipStatus status)
        {
            if (status == null)
                return new List<ShipCrewSeat>();

            var posts = ShipCrewSeats.GetStationSeats(status.Size).ToList();
            if (HasDroidBay(status))
                posts.Add(ShipCrewSeat.DroidBay);
            return posts;
        }

        /// <summary>The droid riding in a flying ship's droid bay, or null.</summary>
        public static ShipDroidCrew GetBayDroid(uint ship) => SpaceCrew.GetSeatDroid(ship, ShipCrewSeat.DroidBay);

        /// <summary>
        /// The percent a ship's bay droid adds to its mining, strip-mining, gas and salvage yield: its Industry (raised
        /// by Efficiency Protocol) on a Mining Barge or Industrial hull, otherwise 0.
        /// </summary>
        public static int GetDroidBayYieldPercent(uint ship)
        {
            var droid = GetBayDroid(ship);
            var role = droid == null ? ShipRole.Invalid : FindShipDetail(GetShipStatus(ship)?.ItemTag)?.Role ?? ShipRole.Invalid;
            return droid == null || !DroidIndustryRules.BayAddsYield(role)
                ? 0
                : DroidIndustryRules.BayYieldPercent(droid.Industry,
                    DroidIndustryRules.GetProtocolValue(droid.Protocols, PerkType.EfficiencyProtocol));
        }

        /// <summary>
        /// The rare-find points a ship's bay droid adds to its mining and salvage through Prospecting Protocol, or 0.
        /// </summary>
        public static int GetDroidBayRareFindBonus(uint ship) =>
            DroidIndustryRules.GetProtocolValue(GetBayDroid(ship)?.Protocols, PerkType.ProspectingProtocol);

        /// <summary>
        /// The percent droids add to a ship's repair right now: a Logistics hull's bay droid (its Industry and Damage
        /// Control Protocol) on every repair, and a droid with Damage Control Protocol on the repairs it runs at the
        /// Engineer station.
        /// </summary>
        public static int GetDroidRepairBonusPercent(uint ship)
        {
            var percent = SpaceCrew.GetDroidRepairBonusPercent(SpaceCrew.GetFireContext(ship)?.Droid);
            var droid = GetBayDroid(ship);
            if (droid != null && DroidIndustryRules.BayAddsRepairs(FindShipDetail(GetShipStatus(ship)?.ItemTag)?.Role ?? ShipRole.Invalid))
            {
                percent += DroidIndustryRules.BayRepairPercent(droid.Industry,
                    DroidIndustryRules.GetProtocolValue(droid.Protocols, PerkType.DamageControlProtocol));
            }

            return percent;
        }

        /// <summary>
        /// Whether a docked ship's computer opens the crew menu first: the hull has crew stations or a droid bay and
        /// the player may crew it and carries a droid or has droids posted aboard. Everyone else takes the controls
        /// at once.
        /// </summary>
        private static bool ShouldOfferDroidCrewMenu(uint player, PlayerShip dbShip, string propertyId)
        {
            if (dbShip?.Status == null || GetDroidPosts(dbShip.Status).Count == 0)
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
                   GetDroidPosts(dbShip.Status).Count > 0 &&
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
                GetDroidPosts(dbShip.Status).Contains(seat),
                GetIsObjectValid(pilot) && GetIsObjectValid(SpaceCrew.GetSeatOccupant(pilot, seat)),
                GetPostedDroids(dbShip).Any(x => x.Seat == seat),
                GetIsObjectValid(controller) && GetResRef(controller) == Droid.DroidControlItemResref &&
                Droid.GetControllers(player).Contains(controller),
                GetIsObjectValid(controller) && Droid.IsPosted(controller),
                GetIsObjectValid(controller) && controller == Droid.GetActiveController(player),
                ShipCrewSeats.GetPostName(seat));
        }

        /// <summary>
        /// Posts the droid at the seat or droid bay of the ship whose interior the player stands in. While the ship
        /// flies, the droid takes its post at once. Returns false and tells the player why when it cannot.
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
                Industry = Math.Max(0, details.Industry),
                Protocols = DroidIndustryRules.SelectProtocols(details.Perks),
                PostedAt = DateTime.UtcNow,
            };

            dbShip.DroidCrew ??= new List<ShipDroidCrew>();
            dbShip.DroidCrew.Add(posting);
            DB.Set(dbShip);
            Droid.MarkPosted(controller, dbShip.Id);

            var postName = ShipCrewSeats.GetPostName(seat);
            var pilot = GetShipPilot(dbShip.Id);
            if (GetIsObjectValid(pilot) && SpaceCrew.AssignDroidSeat(pilot, posting))
            {
                RecalculateCrewedShip(pilot);
                if (pilot != player)
                    SendMessageToPC(pilot, $"{posting.Name} takes {postName}.");
            }

            SendMessageToPC(player,
                $"{posting.Name} takes {postName} aboard {GetShipName(dbShip)}. {DescribeDroidWork(seat, posting, dbShip.Status)} " +
                "It stays at its post until recalled.");
            return true;
        }

        /// <summary>What a droid does at a post, for the captain: its effectiveness at a station, or its bay bonus.</summary>
        public static string DescribeDroidWork(ShipCrewSeat seat, ShipDroidCrew droid, ShipStatus status)
        {
            if (seat != ShipCrewSeat.DroidBay)
            {
                return $"With Industry {droid.Industry} it works the station at {DroidCrewRules.BaseEffectiveness(seat, droid.Industry):P0} " +
                       $"and switches on modules up to rank {DroidCrewRules.MaximumModuleRank(droid.Tier)}.";
            }

            var role = FindShipDetail(status?.ItemTag)?.Role ?? ShipRole.Invalid;
            if (DroidIndustryRules.BayAddsRepairs(role))
            {
                var repair = DroidIndustryRules.BayRepairPercent(droid.Industry,
                    DroidIndustryRules.GetProtocolValue(droid.Protocols, PerkType.DamageControlProtocol));
                return $"With Industry {droid.Industry} it adds {repair}% to the ship's repairs.";
            }

            var yield = DroidIndustryRules.BayYieldPercent(droid.Industry,
                DroidIndustryRules.GetProtocolValue(droid.Protocols, PerkType.EfficiencyProtocol));
            var rare = DroidIndustryRules.GetProtocolValue(droid.Protocols, PerkType.ProspectingProtocol);
            return $"With Industry {droid.Industry} it adds {yield}% to the ship's mining, gas and salvage yield" +
                   (rare > 0 ? $" and {rare} to their rare-find chance." : ".");
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
                SendMessageToPC(player, ColorToken.Red($"No droid works {ShipCrewSeats.GetPostName(seat)}."));
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
            SendMessageToPC(player, $"{posting.Name} leaves {ShipCrewSeats.GetPostName(seat)}.");
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
                SendMessageToPC(pilot, $"{posting.Name} leaves {ShipCrewSeats.GetPostName(posting.Seat)}.");
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
                $"{posting.Name} is no longer posted at {ShipCrewSeats.GetPostName(posting.Seat)} aboard {GetShipName(dbShip)}" +
                (string.IsNullOrWhiteSpace(reason) ? "." : $": {reason}."));
        }

        /// <summary>
        /// The ship was destroyed: every droid posted aboard goes back to its owner, who keeps it (escape pods are
        /// W2d). Changes the record; the caller saves it.
        /// </summary>
        private static void ReturnDroidCrew(PlayerShip dbShip, string reason)
        {
            ReturnShipDroidCrew(dbShip, reason);
        }

        /// <summary>
        /// Every droid posted aboard goes back to its owner, who keeps it: the ship was destroyed or changed owner
        /// (plan section 7). Changes the record; the caller saves it.
        /// </summary>
        public static void ReturnShipDroidCrew(PlayerShip dbShip, string reason)
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
        /// The pilot is launching: the ship's droids take their seats and droid bay. A posting whose post the hull no
        /// longer has, whose owner is gone, or whose controller its online owner no longer carries ends here; it is
        /// removed from <paramref name="dbShip"/>, which the caller saves.
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
                            GetDroidPosts(dbShip.Status).Contains(posting.Seat) &&
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
            SendMessageToPC(pilot, $"Your droid crew takes their posts: {names}.");
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
        /// weapons at the pilot's hostile target, each engaged once per pick of target, and a Probe Launcher whenever
        /// there is something to scan for a droid running Survey Protocol. A bay droid works no slots.
        /// </summary>
        private static void RunDroidStations(uint pilot)
        {
            var status = GetShipStatus(pilot);
            if (status == null)
                return;

            var (target, targetStatus) = GetCurrentTarget(pilot);
            var hostile = GetIsObjectValid(target) && targetStatus != null && !GetIsDead(target) && GetIsEnemy(target, pilot);
            var distance = hostile ? GetDistanceBetween(pilot, target) : 0f;
            bool? somethingToScan = null;

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

                foreach (var (feat, module) in slots)
                {
                    var slot = GetFeatSlotNumber(feat);
                    var detail = _shipModules[module.ItemTag];
                    if (DroidCrewRules.GetTask(detail.Type, detail.Activation) != DroidCrewTask.Survey)
                        continue;

                    somethingToScan ??= HasSomethingToScan(pilot);
                    if (DroidCrewRules.RunsProbeLauncher(posted.Protocols, IsShipToggleOn(status, slot), somethingToScan.Value))
                        AssignCommand(pilot, () => ActivateShipModuleCore(pilot, feat, true, droid: posted));
                }

                UpdateRepairToggles(pilot, status, slots, true, DroidCrewRules.RepairThreshold,
                    DroidCrewRules.RunsWhenShieldLow, DroidCrewRules.RunsWhenHullLow,
                    feat => AssignCommand(pilot, () => ActivateShipModuleCore(pilot, feat, true, droid: posted)));

                UpdateDroidElectronicWarfare(pilot, status, slots, posted);
                RunDroidDataAnalyzer(pilot, slots, posted, target);

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

        /// <summary>
        /// A droid's electronic warfare (plan section 10: the Sensors seat runs Mid electronic warfare): each array
        /// among <paramref name="slots"/> is on while an enemy ship is within the array's range and off once none is.
        /// It works at the droid's station effectiveness, like everything else it fires.
        /// </summary>
        private static void UpdateDroidElectronicWarfare(
            uint pilot,
            ShipStatus status,
            List<(FeatType Feat, ShipStatus.ShipStatusModule Module)> slots,
            ShipDroidCrew droid)
        {
            foreach (var (feat, module) in slots)
            {
                var detail = _shipModules[module.ItemTag];
                if (DroidCrewRules.GetTask(detail.Type, detail.Activation) != DroidCrewTask.ElectronicWarfare)
                    continue;

                var step = RevampModuleCatalog.Steps.FirstOrDefault(x =>
                    string.Equals(x.Tag, module.ItemTag, StringComparison.OrdinalIgnoreCase));
                if (step == null)
                    continue;

                var slot = GetFeatSlotNumber(feat);
                var isOn = IsShipToggleOn(status, slot);
                if (!DroidCrewRules.FlipsElectronicWarfare(isOn, HasEnemyShipWithin(pilot, step.Range)))
                    continue;

                if (isOn)
                    TurnOffToggle(pilot, status, slot);
                else
                    AssignCommand(pilot, () => ActivateShipModuleCore(pilot, feat, true, droid: droid));
            }
        }

        /// <summary>
        /// A droid's slicing (plan section 24): a Data Analyzer among <paramref name="slots"/> is pressed whenever the
        /// pilot has a data can, locked cache or wreck data core targeted. The press validates the rest (range,
        /// combat, the droid's tier, the site's keying) and reports nothing to a droid.
        /// </summary>
        private static void RunDroidDataAnalyzer(
            uint pilot,
            List<(FeatType Feat, ShipStatus.ShipStatusModule Module)> slots,
            ShipDroidCrew droid,
            uint target)
        {
            if (!GetIsObjectValid(target) || GetObjectType(target) != ObjectType.Placeable ||
                !SpaceSlicingRules.UsesDataAnalyzer(GetAnalyzerTarget(target).Source))
            {
                return;
            }

            foreach (var (feat, module) in slots)
            {
                var detail = _shipModules[module.ItemTag];
                if (DroidCrewRules.GetTask(detail.Type, detail.Activation) != DroidCrewTask.Slicing)
                    continue;

                AssignCommand(pilot, () => ActivateShipModuleCore(pilot, feat, true, droid: droid));
            }
        }

        // Controllers

        /// <summary>
        /// A posted droid's snapshot (tier, Industry, protocols) is brought up to its controller's current rating, for a
        /// ship's station or a holding's staff. Station building droids are not refreshed: their controller is stored
        /// with the building and cannot change while posted.
        /// </summary>
        public static void RefreshPostedDroid(uint controller)
        {
            var postedId = Droid.GetPostedShipId(controller);
            if (string.IsNullOrWhiteSpace(postedId))
                return;

            var owner = GetItemPossessor(controller);
            if (HoldingRules.IsDroidPosting(postedId))
            {
                if (GetIsPC(owner))
                    PlanetaryHoldings.RefreshStaffDroid(owner, HoldingRules.GetHoldingIdFromPosting(postedId), controller);
                return;
            }

            var dbShip = DB.Get<PlayerShip>(postedId);
            var droidId = Droid.GetDroidId(controller);
            var posting = dbShip?.DroidCrew?.FirstOrDefault(x => x.DroidItemId == droidId);
            if (posting == null)
                return;

            var details = Droid.LoadDroidItemPropertyDetails(controller);
            var protocols = DroidIndustryRules.SelectProtocols(details.Perks);
            var changed = DroidCrewRules.RefreshPosting(posting, details.Tier, details.Industry, protocols);
            if (changed)
                DB.Set(dbShip);

            // The ship in flight works from its own copy of the posting.
            var pilot = GetShipPilot(dbShip.Id);
            var live = GetIsObjectValid(pilot) ? SpaceCrew.GetSeatDroid(pilot, posting.Seat) : null;
            if (live != null && live.DroidItemId == droidId &&
                DroidCrewRules.RefreshPosting(live, details.Tier, details.Industry, protocols))
            {
                RecalculateCrewedShip(pilot);
            }
        }

        /// <summary>At login, the droids the player carries that are posted aboard a ship or at a holding catch up with their controllers.</summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void RefreshPostedDroidsAtLogin()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            foreach (var controller in Droid.GetControllers(player).Where(Droid.IsPosted))
            {
                RefreshPostedDroid(controller);
            }
        }

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
                if (string.IsNullOrWhiteSpace(shipId) || HoldingRules.IsDroidPosting(shipId))
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
            if (!GetIsPC(owner) || GetResRef(controller) != Droid.DroidControlItemResref || !Droid.IsPosted(controller) ||
                HoldingRules.IsDroidPosting(Droid.GetPostedShipId(controller)))
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
