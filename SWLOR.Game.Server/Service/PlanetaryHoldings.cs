using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.HoldingService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.TradeService;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Planetary Holdings (plan section 26): a character buys up to three holdings at NPC planet docks. Each makes one
    /// batch of its planet's good a day into that dock's warehouse, owes credit upkeep, and is upgraded with credits and
    /// goods hauled from other planets. A Processing Plant refines the output with a delivered input; an industrial droid
    /// posted as staff raises the output; events roll every few days. The owner's ship docked there collects the
    /// warehouse into its cargo bay, and each holding has a small ground site reached from the dock. Holdings catch up
    /// lazily, whenever the owner logs in or opens the Holdings window.
    /// </summary>
    public static class PlanetaryHoldings
    {
        public const string SiteHoldingIdVariable = "HOLDING_SITE_ID";
        private const string SiteReturnVariable = "HOLDING_SITE_RETURN";
        private const string SiteConsoleTag = "building_exit";
        private const string SiteEntranceTag = "PROPERTY_ENTRANCE";
        private const string SiteConversation = "HoldingSiteDialog";

        private static readonly Dictionary<string, uint> _sites = new();

        // Records

        /// <summary>A player's holding records as saved, oldest first.</summary>
        public static List<PlanetaryHolding> GetHoldingRecords(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return new List<PlanetaryHolding>();

            var query = new DBQuery<PlanetaryHolding>()
                .AddFieldSearch(nameof(PlanetaryHolding.OwnerPlayerId), playerId, false)
                .AddPaging(HoldingTuning.MaximumHoldings * 4, 0);
            return DB.Search(query).OrderBy(x => x.DateCreated).ToList();
        }

        /// <summary>The player's holdings brought up to now. Foreclosed holdings are gone from the list.</summary>
        public static List<PlanetaryHolding> LoadHoldings(uint player)
        {
            return GetHoldingRecords(GetObjectUUID(player)).Where(x => Process(x, player)).ToList();
        }

        /// <summary>One of the player's holdings brought up to now, or null when it is not theirs or was lost.</summary>
        public static PlanetaryHolding LoadHolding(uint player, string holdingId)
        {
            var holding = string.IsNullOrWhiteSpace(holdingId) ? null : DB.Get<PlanetaryHolding>(holdingId);
            if (holding == null || holding.OwnerPlayerId != GetObjectUUID(player))
                return null;

            return Process(holding, player) ? holding : null;
        }

        public static string GetPlanetName(HoldingDetail detail) => TradeLookup.GetExchange(detail.Exchange)?.Name ?? "an unknown planet";

        public static string GetHoldingName(PlanetaryHolding holding)
        {
            var detail = HoldingRules.GetDetail(holding.Type);
            return detail == null ? "Holding" : $"{detail.Name} ({GetPlanetName(detail)})";
        }

        /// <summary>
        /// Catches the holding up to now and saves it. Returns false when the upkeep owed lost it (the record is deleted).
        /// New events go to the owner when they are online.
        /// </summary>
        private static bool Process(PlanetaryHolding holding, uint owner)
        {
            var detail = HoldingRules.GetDetail(holding.Type);
            if (detail == null)
                return false;

            var result = HoldingRules.Advance(holding, detail, DateTime.UtcNow, new System.Random(Random.Next()));
            if (result.Foreclosed)
            {
                Foreclose(holding, detail, owner);
                return false;
            }

            if (result.Days > 0)
                DB.Set(holding);

            if (GetIsObjectValid(owner))
            {
                foreach (var text in result.Events)
                    SendMessageToPC(owner, $"{GetHoldingName(holding)}: {text}");
            }

            return true;
        }

        private static void Foreclose(PlanetaryHolding holding, HoldingDetail detail, uint owner)
        {
            ReleaseDroid(holding, owner);
            DB.Delete<PlanetaryHolding>(holding.Id);
            Log.Write(LogGroup.Space, $"HOLDING FORECLOSED - {holding.OwnerPlayerId} - {holding.Type} {holding.Id} - upkeep owed {holding.UpkeepOwed}");

            if (GetIsObjectValid(owner))
            {
                SendMessageToPC(owner, ColorToken.Red(
                    $"Your {detail.Name} at {GetPlanetName(detail)} was foreclosed: {holding.UpkeepOwed} credits of upkeep went unpaid."));
            }
        }

        // Buying and abandoning

        /// <summary>The holding name with "a" or "an" ("an Assembly Line").</summary>
        public static string WithArticle(string name) =>
            (!string.IsNullOrEmpty(name) && "AEIOU".IndexOf(char.ToUpperInvariant(name[0])) >= 0 ? "an " : "a ") + name;

        public static bool Buy(uint player, HoldingType type, TradeExchangeType exchange, string dockId)
        {
            var detail = HoldingRules.GetDetail(type);
            var owned = LoadHoldings(player).Count;
            var refusal = HoldingRules.GetPurchaseRefusal(detail, exchange, owned, GetGold(player));
            if (refusal == HoldingRefusal.None && string.IsNullOrWhiteSpace(dockId))
                refusal = HoldingRefusal.NotSoldHere;
            if (refusal != HoldingRefusal.None)
            {
                SendMessageToPC(player, ColorToken.Red(HoldingRules.GetRefusalMessage(refusal)));
                return false;
            }

            AssignCommand(player, () => TakeGoldFromCreature(detail.PurchasePrice, player, true));
            var holding = HoldingRules.Create(GetObjectUUID(player), detail, dockId, DateTime.UtcNow);
            HoldingRules.AddLog(holding, DateTime.UtcNow, $"Bought for {detail.PurchasePrice} credits.");
            DB.Set(holding);

            SendMessageToPC(player, ColorToken.Green(
                $"You bought {WithArticle(detail.Name)} at {GetPlanetName(detail)} for {detail.PurchasePrice} credits. It makes " +
                $"{HoldingRules.GetDailyUnits(holding, detail)} {TradeLookup.GetCommodity(detail.Output)?.Name} a day for your ship " +
                $"to collect here, and owes {HoldingRules.GetDailyUpkeep(holding, detail)} credits of upkeep a day."));
            Log.Write(LogGroup.Space, $"HOLDING BUY - {PlayerName.GetAuditName(player)} - {type} {holding.Id} at {dockId} for {detail.PurchasePrice}");
            return true;
        }

        /// <summary>Gives the holding up: a share of its price back, less the upkeep owed; its stock and supplies are lost.</summary>
        public static void Abandon(uint player, string holdingId)
        {
            var holding = LoadHolding(player, holdingId);
            if (holding == null)
                return;

            var detail = HoldingRules.GetDetail(holding.Type);
            var refund = HoldingRules.GetSellBackRefund(holding);
            ReleaseDroid(holding, player);
            DB.Delete<PlanetaryHolding>(holding.Id);
            if (refund > 0)
                GiveGoldToCreature(player, refund);

            SendMessageToPC(player, $"You gave up your {detail.Name} at {GetPlanetName(detail)} and received {refund} credits.");
            Log.Write(LogGroup.Space, $"HOLDING ABANDON - {PlayerName.GetAuditName(player)} - {holding.Type} {holding.Id} - refund {refund}, upkeep owed {holding.UpkeepOwed}");
        }

        // Upkeep and fines

        public static void PayUpkeep(uint player, string holdingId)
        {
            var holding = LoadHolding(player, holdingId);
            if (holding == null || holding.UpkeepOwed <= 0)
                return;

            if (GetGold(player) < holding.UpkeepOwed)
            {
                SendMessageToPC(player, ColorToken.Red(HoldingRules.GetRefusalMessage(HoldingRefusal.NotEnoughCredits)));
                return;
            }

            var paid = holding.UpkeepOwed;
            AssignCommand(player, () => TakeGoldFromCreature(paid, player, true));
            holding.UpkeepOwed = 0;
            DB.Set(holding);
            SendMessageToPC(player, ColorToken.Green($"You paid {paid} credits of upkeep for your {GetHoldingName(holding)}."));
            Log.Write(LogGroup.Space, $"HOLDING UPKEEP - {PlayerName.GetAuditName(player)} - {holding.Id} - {paid}");
        }

        public static void PayFine(uint player, string holdingId)
        {
            var holding = LoadHolding(player, holdingId);
            if (holding == null)
                return;

            var refusal = holding.CustomsFine <= 0
                ? HoldingRefusal.NoFine
                : GetGold(player) < holding.CustomsFine ? HoldingRefusal.NotEnoughCredits : HoldingRefusal.None;
            if (refusal != HoldingRefusal.None)
            {
                SendMessageToPC(player, ColorToken.Red(HoldingRules.GetRefusalMessage(refusal)));
                return;
            }

            var fine = holding.CustomsFine;
            AssignCommand(player, () => TakeGoldFromCreature(fine, player, true));
            holding.CustomsFine = 0;
            HoldingRules.AddLog(holding, DateTime.UtcNow, $"Paid the {fine} credit customs fine.");
            DB.Set(holding);
            SendMessageToPC(player, ColorToken.Green($"You paid the {fine} credit customs fine. The inspectors leave your stock alone."));
            Log.Write(LogGroup.Space, $"HOLDING FINE - {PlayerName.GetAuditName(player)} - {holding.Id} - {fine}");
        }

        // The warehouse

        /// <summary>
        /// Loads the warehouse into the cargo bay of the player's ship docked at the holding's dock, as much as fits. Any
        /// upkeep owed is paid first. Collected goods count as costing their base price.
        /// </summary>
        public static void Collect(uint player, string holdingId, string shipId)
        {
            var holding = LoadHolding(player, holdingId);
            if (holding == null)
                return;

            var ship = GetDockedShip(player, holding, shipId);
            var plan = ship == null ? new Dictionary<CommodityType, int>() : HoldingRules.PlanCollection(holding.Warehouse, Trade.GetFreeVolume(ship));
            var refusal = ship == null
                ? HoldingRefusal.ShipNotDocked
                : holding.Warehouse.Values.Sum() <= 0
                    ? HoldingRefusal.NothingToCollect
                    : plan.Count == 0
                        ? HoldingRefusal.CargoBayFull
                        : GetGold(player) < holding.UpkeepOwed ? HoldingRefusal.UpkeepOwed : HoldingRefusal.None;
            if (refusal != HoldingRefusal.None)
            {
                SendMessageToPC(player, ColorToken.Red(HoldingRules.GetRefusalMessage(refusal)));
                return;
            }

            var upkeep = holding.UpkeepOwed;
            if (upkeep > 0)
            {
                AssignCommand(player, () => TakeGoldFromCreature(upkeep, player, true));
                holding.UpkeepOwed = 0;
            }

            foreach (var (commodity, units) in plan)
            {
                CargoHoldRules.AddUnits(ship.Cargo, commodity, units, HoldingRules.GetCostBasis(commodity, units));
                HoldingRules.RemoveStock(holding.Warehouse, commodity, units);
            }

            DB.Set(ship);
            DB.Set(holding);

            var loaded = string.Join(", ", plan.Select(x => $"{x.Value} {TradeLookup.GetCommodity(x.Key)?.Name}"));
            var left = holding.Warehouse.Values.Sum();
            SendMessageToPC(player, ColorToken.Green(
                $"Loaded {loaded} from your {GetHoldingName(holding)}" +
                (upkeep > 0 ? $", paying {upkeep} credits of upkeep" : string.Empty) +
                (left > 0 ? $". {left} units stay in the warehouse." : ".")));
            Log.Write(LogGroup.Space, $"HOLDING COLLECT - {PlayerName.GetAuditName(player)} - {holding.Id} - {loaded} - upkeep {upkeep} - ship {ship.Id}");
        }

        /// <summary>Unloads a good the holding needs from the ship's cargo bay into its supplies, up to what it still takes.</summary>
        public static void Deliver(uint player, string holdingId, string shipId, CommodityType commodity)
        {
            var holding = LoadHolding(player, holdingId);
            if (holding == null)
                return;

            var detail = HoldingRules.GetDetail(holding.Type);
            var ship = GetDockedShip(player, holding, shipId);
            var wanted = HoldingRules.GetDeliverableUnits(holding, detail, commodity);
            var aboard = ship == null ? 0 : CargoHoldRules.GetUnits(ship.Cargo, commodity);
            var refusal = ship == null
                ? HoldingRefusal.ShipNotDocked
                : wanted <= 0 ? HoldingRefusal.NothingNeeded : aboard <= 0 ? HoldingRefusal.NoneAboard : HoldingRefusal.None;
            if (refusal != HoldingRefusal.None)
            {
                SendMessageToPC(player, ColorToken.Red(HoldingRules.GetRefusalMessage(refusal)));
                return;
            }

            var units = System.Math.Min(wanted, aboard);
            CargoHoldRules.RemoveUnits(ship.Cargo, commodity, units);
            var result = new HoldingAdvanceResult();
            HoldingRules.Deliver(holding, detail, commodity, units, DateTime.UtcNow, result);
            DB.Set(ship);
            DB.Set(holding);

            SendMessageToPC(player, ColorToken.Green($"Delivered {units} {TradeLookup.GetCommodity(commodity)?.Name} to your {GetHoldingName(holding)}."));
            foreach (var text in result.Events)
                SendMessageToPC(player, $"{GetHoldingName(holding)}: {text}");
            Log.Write(LogGroup.Space, $"HOLDING DELIVER - {PlayerName.GetAuditName(player)} - {holding.Id} - {units} {commodity} - ship {ship.Id}");
        }

        /// <summary>The player's own ship docked at the holding's dock, or null.</summary>
        private static PlayerShip GetDockedShip(uint player, PlanetaryHolding holding, string shipId)
        {
            if (string.IsNullOrWhiteSpace(shipId))
                return null;

            return Trade.GetShipsDockedAt(player, holding.DockId).FirstOrDefault(x => x.Id == shipId);
        }

        // Upgrades

        public static void Upgrade(uint player, string holdingId)
        {
            var holding = LoadHolding(player, holdingId);
            if (holding == null)
                return;

            var detail = HoldingRules.GetDetail(holding.Type);
            var refusal = HoldingRules.GetUpgradeRefusal(holding, detail, GetGold(player));
            if (refusal != HoldingRefusal.None)
            {
                SendMessageToPC(player, ColorToken.Red(HoldingRules.GetRefusalMessage(refusal)));
                return;
            }

            var upgrade = HoldingRules.GetNextUpgrade(holding, detail);
            AssignCommand(player, () => TakeGoldFromCreature(upgrade.Credits, player, true));
            HoldingRules.ApplyUpgrade(holding, detail);
            HoldingRules.AddLog(holding, DateTime.UtcNow, $"Upgraded to level {holding.Level}.");
            DB.Set(holding);

            SendMessageToPC(player, ColorToken.Green(
                $"Your {GetHoldingName(holding)} is now level {holding.Level} and makes {HoldingRules.GetDailyUnits(holding, detail)} units a day."));
            Log.Write(LogGroup.Space, $"HOLDING UPGRADE - {PlayerName.GetAuditName(player)} - {holding.Id} - level {holding.Level} - {upgrade.Credits} credits");
        }

        public static void BuildPlant(uint player, string holdingId)
        {
            var holding = LoadHolding(player, holdingId);
            if (holding == null)
                return;

            var detail = HoldingRules.GetDetail(holding.Type);
            var refusal = HoldingRules.GetPlantRefusal(holding, detail, GetGold(player));
            if (refusal != HoldingRefusal.None)
            {
                SendMessageToPC(player, ColorToken.Red(HoldingRules.GetRefusalMessage(refusal)));
                return;
            }

            AssignCommand(player, () => TakeGoldFromCreature(detail.Plant.Credits, player, true));
            HoldingRules.ApplyPlant(holding, detail);
            HoldingRules.AddLog(holding, DateTime.UtcNow, "Built a Processing Plant.");
            DB.Set(holding);

            SendMessageToPC(player, ColorToken.Green(
                $"Your {GetHoldingName(holding)} has a Processing Plant. Deliver {TradeLookup.GetCommodity(detail.Chain.Input)?.Name} and it turns " +
                $"its output into {TradeLookup.GetCommodity(detail.Chain.Refined)?.Name}."));
            Log.Write(LogGroup.Space, $"HOLDING PLANT - {PlayerName.GetAuditName(player)} - {holding.Id} - {detail.Plant.Credits} credits");
        }

        public static void SetRefining(uint player, string holdingId, bool refine)
        {
            var holding = LoadHolding(player, holdingId);
            if (holding == null || !holding.HasProcessingPlant || holding.RefineOutput == refine)
                return;

            holding.RefineOutput = refine;
            DB.Set(holding);
        }

        // Droid staff

        /// <summary>
        /// Posts one of the player's droids as the holding's staff (plan section 27). Like ship droid crew, the controller
        /// stays with its owner, cursed, and the droid is never spawned, so it does not take the follow slot.
        /// </summary>
        public static void AssignDroid(uint player, string holdingId, uint controller)
        {
            var holding = LoadHolding(player, holdingId);
            if (holding == null)
                return;

            if (!string.IsNullOrWhiteSpace(holding.DroidItemId))
            {
                SendMessageToPC(player, ColorToken.Red(HoldingRules.GetRefusalMessage(HoldingRefusal.DroidAlreadyPosted)));
                return;
            }

            if (!GetIsObjectValid(controller) || !Space.GetPostableDroids(player).Contains(controller))
            {
                SendMessageToPC(player, ColorToken.Red("You no longer carry that droid, or it is busy."));
                return;
            }

            var details = Droid.LoadDroidItemPropertyDetails(controller);
            var protocols = DroidIndustryRules.SelectProtocols(details.Perks);
            holding.DroidItemId = Droid.GetDroidId(controller);
            holding.DroidName = Droid.GetDroidName(controller);
            holding.DroidIndustry = System.Math.Max(0, details.Industry);
            holding.DroidEfficiencyPercent = DroidIndustryRules.GetProtocolValue(protocols, PerkType.EfficiencyProtocol);
            HoldingRules.AddLog(holding, DateTime.UtcNow, $"{holding.DroidName} joined the staff.");
            DB.Set(holding);
            Droid.MarkPosted(controller, HoldingRules.GetDroidPostingId(holding.Id));

            SendMessageToPC(player, ColorToken.Green(
                $"{holding.DroidName} now works your {GetHoldingName(holding)}. With Industry {holding.DroidIndustry} it adds " +
                $"{HoldingRules.GetDroidPercent(holding)}% to the output. It stays at its post until recalled."));
            Log.Write(LogGroup.Space, $"HOLDING DROID - {PlayerName.GetAuditName(player)} - {holding.Id} - posted '{holding.DroidName}' (Industry {holding.DroidIndustry})");
        }

        /// <summary>
        /// A staff droid's rating changed (its instructions were edited, or a login found its controller changed): the
        /// holding catches up at the old rating, then works at the new one.
        /// </summary>
        public static void RefreshStaffDroid(uint owner, string holdingId, uint controller)
        {
            var holding = LoadHolding(owner, holdingId);
            if (holding == null || holding.DroidItemId != Droid.GetDroidId(controller))
                return;

            var (industry, efficiency) = StationBuildings.GetDroidIndustry(controller);
            if (HoldingRules.RefreshDroid(holding, industry, efficiency))
                DB.Set(holding);
        }

        public static void RecallDroid(uint player, string holdingId)
        {
            var holding = LoadHolding(player, holdingId);
            if (holding == null)
                return;

            if (string.IsNullOrWhiteSpace(holding.DroidItemId))
            {
                SendMessageToPC(player, ColorToken.Red(HoldingRules.GetRefusalMessage(HoldingRefusal.NoDroid)));
                return;
            }

            var name = holding.DroidName;
            ReleaseDroid(holding, player);
            HoldingRules.AddLog(holding, DateTime.UtcNow, $"{name} left the staff.");
            DB.Set(holding);
            SendMessageToPC(player, $"{name} leaves your {GetHoldingName(holding)} and is back with you.");
        }

        /// <summary>Clears the holding's staff droid and uncurses its controller when its owner is online.</summary>
        private static void ReleaseDroid(PlanetaryHolding holding, uint owner)
        {
            if (string.IsNullOrWhiteSpace(holding.DroidItemId))
                return;

            if (GetIsObjectValid(owner))
            {
                var controller = Droid.FindController(owner, holding.DroidItemId);
                if (GetIsObjectValid(controller))
                    Droid.ClearPosted(controller);
            }

            holding.DroidItemId = string.Empty;
            holding.DroidName = string.Empty;
            holding.DroidIndustry = 0;
            holding.DroidEfficiencyPercent = 0;
        }

        // Ground sites

        /// <summary>
        /// Takes the owner to the holding's ground site: an instance of its type's template, made on the first visit and
        /// removed once empty. Its office terminal manages the holding and leads back to the dock.
        /// </summary>
        public static void VisitSite(uint player, string holdingId, string dockId)
        {
            var holding = LoadHolding(player, holdingId);
            if (holding == null)
                return;

            if (holding.DockId != dockId)
            {
                SendMessageToPC(player, ColorToken.Red(HoldingRules.GetRefusalMessage(HoldingRefusal.NotAtDock)));
                return;
            }

            var detail = HoldingRules.GetDetail(holding.Type);
            var site = GetOrCreateSite(holding, detail);
            if (!GetIsObjectValid(site))
            {
                SendMessageToPC(player, ColorToken.Red("The holding's site cannot be reached right now."));
                Log.Write(LogGroup.Error, $"Holding site template '{detail.SiteAreaResref}' could not be instanced for holding {holding.Id}.");
                return;
            }

            var entrance = GetSiteEntrance(site);
            SetLocalLocation(player, SiteReturnVariable, GetLocation(player));
            AssignCommand(player, () =>
            {
                ClearAllActions();
                ActionJumpToLocation(entrance);
            });
        }

        /// <summary>Sends the player from a holding site back to the dock they came from.</summary>
        public static void LeaveSite(uint player)
        {
            var destination = GetLocalLocation(player, SiteReturnVariable);
            if (!GetIsObjectValid(GetAreaFromLocation(destination)))
                destination = GetStartingLocation();

            DeleteLocalLocation(player, SiteReturnVariable);
            AssignCommand(player, () =>
            {
                ClearAllActions();
                ActionJumpToLocation(destination);
            });
        }

        private static uint GetOrCreateSite(PlanetaryHolding holding, HoldingDetail detail)
        {
            if (_sites.TryGetValue(holding.Id, out var existing) && GetIsObjectValid(existing))
                return existing;

            var site = Area.CreateInstance(detail.SiteAreaResref, "holding_site", $"{detail.Name} - {GetPlanetName(detail)}");
            if (!GetIsObjectValid(site))
                return OBJECT_INVALID;

            SetLocalString(site, SiteHoldingIdVariable, holding.Id);
            for (var obj = GetFirstObjectInArea(site); GetIsObjectValid(obj); obj = GetNextObjectInArea(site))
            {
                if (GetTag(obj) != SiteConsoleTag)
                    continue;

                SetName(obj, "Holding Office Terminal");
                SetLocalString(obj, "CONVERSATION", SiteConversation);
                SetLocalString(obj, SiteHoldingIdVariable, holding.Id);
            }

            _sites[holding.Id] = site;
            return site;
        }

        private static Location GetSiteEntrance(uint site)
        {
            for (var obj = GetFirstObjectInArea(site); GetIsObjectValid(obj); obj = GetNextObjectInArea(site))
            {
                if (GetTag(obj) == SiteEntranceTag)
                    return GetLocation(obj);
            }

            return Location(site, Vector3(5f, 5f, 0f), 0f);
        }

        /// <summary>A holding site with no players left is removed a little later.</summary>
        [NWNEventHandler(ScriptName.OnAreaExit)]
        public static void CleanUpSite()
        {
            var site = OBJECT_SELF;
            var holdingId = GetLocalString(site, SiteHoldingIdVariable);
            if (string.IsNullOrWhiteSpace(holdingId) || !GetIsPC(GetExitingObject()))
                return;

            DelayCommand(10f, () => TryDestroySite(site, holdingId));
        }

        private static void TryDestroySite(uint site, string holdingId)
        {
            if (!GetIsObjectValid(site))
                return;

            for (var obj = GetFirstObjectInArea(site); GetIsObjectValid(obj); obj = GetNextObjectInArea(site))
            {
                if (GetIsPC(obj))
                    return;
            }

            if (DestroyArea(site) == 1)
            {
                if (_sites.TryGetValue(holdingId, out var current) && current == site)
                    _sites.Remove(holdingId);
            }
            else
            {
                DelayCommand(30f, () => TryDestroySite(site, holdingId));
            }
        }

        // Login and droid controllers

        /// <summary>
        /// At login: holdings catch up (their news goes to the owner) and a staff droid whose holding is gone, or no longer
        /// lists it, is the owner's again.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void CheckHoldingsAtLogin()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var holdings = LoadHoldings(player);
            foreach (var controller in Droid.GetControllers(player))
            {
                var postedId = Droid.GetPostedShipId(controller);
                if (!HoldingRules.IsDroidPosting(postedId))
                    continue;

                var holdingId = HoldingRules.GetHoldingIdFromPosting(postedId);
                var droidId = Droid.GetDroidId(controller);
                if (holdings.Any(x => x.Id == holdingId && x.DroidItemId == droidId))
                {
                    SetItemCursedFlag(controller, true);
                    continue;
                }

                Droid.ClearPosted(controller);
                SendMessageToPC(player, $"{Droid.GetDroidName(controller)} no longer works a holding and is back with you.");
            }

            var waiting = holdings.Where(x => x.Warehouse.Values.Sum() > 0).ToList();
            if (waiting.Count > 0)
            {
                SendMessageToPC(player, "Your holdings have goods waiting: " +
                                        string.Join(", ", waiting.Select(x => $"{GetHoldingName(x)} {x.Warehouse.Values.Sum()} units")) + ".");
            }
        }

        /// <summary>A staff droid's controller that leaves its owner (destroyed or taken) ends its posting.</summary>
        [NWNEventHandler(ScriptName.OnModuleUnacquire)]
        public static void EndStaffPostingOnControllerLoss()
        {
            var owner = GetModuleItemLostBy();
            var controller = GetModuleItemLost();
            if (!GetIsPC(owner) || GetResRef(controller) != Droid.DroidControlItemResref)
                return;

            var postedId = Droid.GetPostedShipId(controller);
            if (!HoldingRules.IsDroidPosting(postedId))
                return;

            var holdingId = HoldingRules.GetHoldingIdFromPosting(postedId);
            var droidId = Droid.GetDroidId(controller);

            // The item may only have moved inside the owner's inventory; check once the move is done.
            DelayCommand(0.1f, () =>
            {
                if (GetIsObjectValid(owner) && GetIsObjectValid(Droid.FindController(owner, droidId)))
                    return;

                var holding = DB.Get<PlanetaryHolding>(holdingId);
                if (holding == null || holding.DroidItemId != droidId)
                    return;

                var name = holding.DroidName;
                ReleaseDroid(holding, OBJECT_INVALID);
                HoldingRules.AddLog(holding, DateTime.UtcNow, $"{name} left the staff: its controller is gone.");
                DB.Set(holding);
                if (GetIsObjectValid(owner))
                    SendMessageToPC(owner, $"{name} no longer works your {GetHoldingName(holding)}: its controller is gone.");
            });
        }
    }
}
