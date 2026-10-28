using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.HoldingDefinition;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service.HoldingService
{
    /// <summary>
    /// Planetary Holdings rules (plan section 26), mirrored in tools/SpaceRevampBalance/holdings.py: buying and its
    /// limit, daily output and upkeep, the warehouse, upgrades and deliveries, Processing Plant chains, the staff droid's
    /// bonus and the events rolled every few days.
    /// </summary>
    public static class HoldingRules
    {
        /// <summary>The prefix of a staff droid's posted id on its controller, beside ship postings' ship ids.</summary>
        public const string DroidPostingPrefix = "HOLDING:";

        private static readonly Dictionary<HoldingType, HoldingDetail> _details = HoldingCatalog.Holdings.ToDictionary(x => x.Type);

        public static HoldingDetail GetDetail(HoldingType type) => _details.TryGetValue(type, out var detail) ? detail : null;

        /// <summary>The holding types sold at a dock exchange.</summary>
        public static List<HoldingDetail> GetHoldingsAt(TradeExchangeType exchange) =>
            HoldingCatalog.Holdings.Where(x => x.Exchange == exchange).ToList();

        public static string GetDroidPostingId(string holdingId) => DroidPostingPrefix + holdingId;

        public static bool IsDroidPosting(string postedId) =>
            !string.IsNullOrEmpty(postedId) && postedId.StartsWith(DroidPostingPrefix, StringComparison.Ordinal);

        public static string GetHoldingIdFromPosting(string postedId) =>
            IsDroidPosting(postedId) ? postedId.Substring(DroidPostingPrefix.Length) : string.Empty;

        // Buying

        public static HoldingRefusal GetPurchaseRefusal(HoldingDetail detail, TradeExchangeType dockExchange, int owned, int credits)
        {
            if (detail == null || detail.Exchange != dockExchange)
                return HoldingRefusal.NotSoldHere;
            if (owned >= HoldingTuning.MaximumHoldings)
                return HoldingRefusal.AtLimit;
            if (credits < detail.PurchasePrice)
                return HoldingRefusal.NotEnoughCredits;

            return HoldingRefusal.None;
        }

        public static PlanetaryHolding Create(string ownerPlayerId, HoldingDetail detail, string dockId, DateTime now)
        {
            return new PlanetaryHolding
            {
                OwnerPlayerId = ownerPlayerId,
                Type = detail.Type,
                DockId = dockId ?? string.Empty,
                Level = 1,
                PricePaid = detail.PurchasePrice,
                LastProcessedAt = now,
                DaysUntilEvent = HoldingTuning.EventIntervalDays,
            };
        }

        /// <summary>What abandoning a holding refunds: a share of its price, less the upkeep owed.</summary>
        public static int GetSellBackRefund(PlanetaryHolding holding) =>
            Math.Max(0, Round(holding.PricePaid * HoldingTuning.SellBackShare) - holding.UpkeepOwed);

        // Output

        /// <summary>The staff droid's output bonus in percent (plan section 27), or 0 without a droid.</summary>
        public static int GetDroidPercent(PlanetaryHolding holding) =>
            string.IsNullOrEmpty(holding?.DroidItemId)
                ? 0
                : DroidIndustryRules.HoldingOutputPercent(holding.DroidIndustry, holding.DroidEfficiencyPercent);

        public static int GetDailyUnits(HoldingDetail detail, int level, int droidPercent) =>
            DroidIndustryRules.ApplyPercent(Round(detail.BatchUnits * HoldingTuning.LevelOutput[ClampLevel(level)]), droidPercent);

        public static int GetDailyUnits(PlanetaryHolding holding, HoldingDetail detail) =>
            GetDailyUnits(detail, holding.Level, GetDroidPercent(holding));

        public static int GetDailyUpkeep(HoldingDetail detail, int level) => detail.UpkeepByLevel[ClampLevel(level) - 1];

        /// <summary>The upkeep owed that loses the holding.</summary>
        public static int GetForeclosureLimit(HoldingDetail detail, int level) =>
            HoldingTuning.ForeclosureDays * GetDailyUpkeep(detail, level);

        /// <summary>The warehouse's volume: some days of output. A full warehouse stops output.</summary>
        public static int GetStorageVolume(PlanetaryHolding holding, HoldingDetail detail) =>
            HoldingTuning.StorageDays * GetDailyUnits(holding, detail) * TradeLookup.GetVolume(detail.Output);

        public static int GetStoredVolume(IReadOnlyDictionary<CommodityType, int> stock) =>
            stock?.Sum(x => Math.Max(0, x.Value) * TradeLookup.GetVolume(x.Key)) ?? 0;

        public static int GetStockValue(IReadOnlyDictionary<CommodityType, int> stock) =>
            stock?.Sum(x => Math.Max(0, x.Value) * (TradeLookup.GetCommodity(x.Key)?.BasePrice ?? 0)) ?? 0;

        /// <summary>
        /// The Processing Plant's recipes on a day's output: as many as the output and the delivered input allow. The
        /// rest of the output stays raw.
        /// </summary>
        public static (int Refined, int OwnUsed, int InputUsed) Refine(HoldingChainDetail chain, int ownUnits, int inputUnits)
        {
            if (chain == null || chain.OwnUnits <= 0 || chain.InputUnits <= 0)
                return (0, 0, 0);

            var recipes = Math.Max(0, Math.Min(ownUnits / chain.OwnUnits, inputUnits / chain.InputUnits));
            return (recipes * chain.RefinedUnits, recipes * chain.OwnUnits, recipes * chain.InputUnits);
        }

        // Each day

        /// <summary>
        /// Brings the holding up to now one whole day at a time: upkeep, an event roll every few days, an unanswered
        /// customs fine, repairs from delivered goods, and the day's output into the warehouse. Stops when the upkeep owed
        /// reaches the foreclosure limit.
        /// </summary>
        public static HoldingAdvanceResult Advance(PlanetaryHolding holding, HoldingDetail detail, DateTime now, System.Random random)
        {
            var result = new HoldingAdvanceResult();
            while (holding.LastProcessedAt.AddDays(1) <= now)
            {
                var day = holding.LastProcessedAt.AddDays(1);
                holding.LastProcessedAt = day;
                result.Days++;
                holding.UpkeepOwed += GetDailyUpkeep(detail, holding.Level);

                if (holding.CustomsFine > 0 && day >= holding.CustomsDeadline)
                    Confiscate(holding, day, result);

                holding.DaysUntilEvent--;
                if (holding.DaysUntilEvent <= 0)
                {
                    holding.DaysUntilEvent = HoldingTuning.EventIntervalDays;
                    if (holding.Setback == HoldingSetbackType.Invalid && holding.CustomsFine <= 0)
                        ApplyEvent(holding, detail, RollEvent(detail, random.NextDouble()), day, result);
                }

                TryRepair(holding, detail, day, result);
                if (holding.Setback == HoldingSetbackType.Invalid)
                    Produce(holding, detail);

                if (holding.UpkeepOwed >= GetForeclosureLimit(detail, holding.Level))
                {
                    result.Foreclosed = true;
                    break;
                }
            }

            return result;
        }

        /// <summary>The event a roll in [0, 1) gives: boom, setback, raid, then (contraband holdings) customs.</summary>
        public static HoldingEventType RollEvent(HoldingDetail detail, double roll)
        {
            var threshold = HoldingTuning.BoomChance;
            if (roll < threshold)
                return HoldingEventType.Boom;
            threshold += HoldingTuning.SetbackChance;
            if (roll < threshold)
                return HoldingEventType.Setback;
            threshold += HoldingTuning.RaidChance;
            if (roll < threshold)
                return HoldingEventType.Raid;
            threshold += HoldingTuning.CustomsChance;
            if (detail.CustomsInspections && roll < threshold)
                return HoldingEventType.Customs;

            return HoldingEventType.None;
        }

        public static void ApplyEvent(PlanetaryHolding holding, HoldingDetail detail, HoldingEventType type, DateTime day, HoldingAdvanceResult result)
        {
            switch (type)
            {
                case HoldingEventType.Boom:
                    holding.BoomDays++;
                    AddLog(holding, day, $"Boom: the {detail.Name} doubles its output today.", result);
                    break;
                case HoldingEventType.Setback:
                    holding.Setback = detail.Setback;
                    holding.RepairUnitsNeeded = detail.RepairUnits;
                    AddLog(holding, day,
                        $"{GetSetbackName(detail.Setback)}: {GetSetbackText(detail.Setback)} Output stops until " +
                        $"{detail.RepairUnits} {TradeLookup.GetCommodity(detail.RepairGood)?.Name} are delivered.", result);
                    break;
                case HoldingEventType.Raid:
                    var stolen = Raid(holding.Warehouse);
                    AddLog(holding, day, stolen > 0
                        ? $"Bandit raid: raiders carried off {stolen} units from the warehouse."
                        : "Bandit raid: raiders found the warehouse empty.", result);
                    break;
                case HoldingEventType.Customs:
                    var value = GetBannedValue(holding.Warehouse);
                    if (value <= 0)
                    {
                        AddLog(holding, day, "Customs inspection: the inspectors found nothing.", result);
                        break;
                    }

                    holding.CustomsFine = Round(value * HoldingTuning.CustomsFineRate);
                    holding.CustomsDeadline = day.AddDays(HoldingTuning.CustomsAnswerDays);
                    AddLog(holding, day,
                        $"Customs inspection: pay a {holding.CustomsFine} credit fine within {HoldingTuning.CustomsAnswerDays} days " +
                        "or the banned stock is confiscated.", result);
                    break;
            }
        }

        /// <summary>Takes the raid's share of every good in the warehouse (rounded half up); returns the units taken.</summary>
        public static int Raid(Dictionary<CommodityType, int> warehouse)
        {
            var taken = 0;
            foreach (var commodity in warehouse.Keys.ToList())
            {
                var lost = Math.Min(warehouse[commodity], Round(warehouse[commodity] * HoldingTuning.RaidLossShare));
                warehouse[commodity] -= lost;
                taken += lost;
                if (warehouse[commodity] <= 0)
                    warehouse.Remove(commodity);
            }

            return taken;
        }

        /// <summary>The base value of the warehouse's restricted and contraband goods.</summary>
        public static int GetBannedValue(IReadOnlyDictionary<CommodityType, int> warehouse) =>
            warehouse.Where(x => IsBanned(x.Key)).Sum(x => Math.Max(0, x.Value) * (TradeLookup.GetCommodity(x.Key)?.BasePrice ?? 0));

        private static bool IsBanned(CommodityType commodity) =>
            (TradeLookup.GetCommodity(commodity)?.Legality ?? CommodityLegality.Legal) != CommodityLegality.Legal;

        private static void Confiscate(PlanetaryHolding holding, DateTime day, HoldingAdvanceResult result)
        {
            var units = 0;
            foreach (var commodity in holding.Warehouse.Keys.Where(IsBanned).ToList())
            {
                units += holding.Warehouse[commodity];
                holding.Warehouse.Remove(commodity);
            }

            holding.CustomsFine = 0;
            AddLog(holding, day, $"Customs confiscated {units} units of banned stock: the fine went unpaid.", result);
        }

        /// <summary>A setback ends once its repair goods are in the supplies; they are used up.</summary>
        public static bool TryRepair(PlanetaryHolding holding, HoldingDetail detail, DateTime day, HoldingAdvanceResult result)
        {
            if (holding.Setback == HoldingSetbackType.Invalid || GetSupply(holding, detail.RepairGood) < holding.RepairUnitsNeeded)
                return false;

            TakeSupply(holding, detail.RepairGood, holding.RepairUnitsNeeded);
            AddLog(holding, day, $"Repaired: the {detail.Name} is working again.", result);
            holding.Setback = HoldingSetbackType.Invalid;
            holding.RepairUnitsNeeded = 0;
            return true;
        }

        /// <summary>The day's output (doubled by a boom) into the warehouse, refined when the Processing Plant runs.</summary>
        public static void Produce(PlanetaryHolding holding, HoldingDetail detail)
        {
            var units = GetDailyUnits(holding, detail);
            if (holding.BoomDays > 0)
            {
                units *= HoldingTuning.BoomMultiplier;
                holding.BoomDays--;
            }

            var free = GetStorageVolume(holding, detail) - GetStoredVolume(holding.Warehouse);
            units = Math.Min(units, Math.Max(0, free) / Math.Max(1, TradeLookup.GetVolume(detail.Output)));
            if (units <= 0)
                return;

            if (holding.HasProcessingPlant && holding.RefineOutput)
            {
                var (refined, ownUsed, inputUsed) = Refine(detail.Chain, units, GetSupply(holding, detail.Chain.Input));
                TakeSupply(holding, detail.Chain.Input, inputUsed);
                AddStock(holding.Warehouse, detail.Chain.Refined, refined);
                units -= ownUsed;
            }

            AddStock(holding.Warehouse, detail.Output, units);
        }

        // Deliveries, upgrades and the Processing Plant

        /// <summary>
        /// Every good the holding takes deliveries of, merged per good: repair goods during a setback, the next level's
        /// goods, the Processing Plant's build goods, and once it stands, its input up to some days of recipes.
        /// </summary>
        public static List<HoldingNeed> GetNeeds(PlanetaryHolding holding, HoldingDetail detail)
        {
            var wanted = new List<(CommodityType Commodity, int Units, string Purpose)>();
            if (holding.Setback != HoldingSetbackType.Invalid)
                wanted.Add((detail.RepairGood, holding.RepairUnitsNeeded, "Repairs"));

            var upgrade = GetNextUpgrade(holding, detail);
            if (upgrade != null)
                wanted.AddRange(upgrade.Goods.Select(x => (x.Key, x.Value, $"Level {upgrade.Level}")));

            if (!holding.HasProcessingPlant)
                wanted.AddRange(detail.Plant.Goods.Select(x => (x.Key, x.Value, "Processing Plant")));
            else
                wanted.Add((detail.Chain.Input, GetInputStock(holding, detail), "Plant input"));

            return wanted
                .GroupBy(x => x.Commodity)
                .Select(x => new HoldingNeed
                {
                    Commodity = x.Key,
                    Wanted = x.Sum(y => y.Units),
                    Have = GetSupply(holding, x.Key),
                    Purpose = string.Join(", ", x.Select(y => y.Purpose).Distinct()),
                })
                .ToList();
        }

        /// <summary>The Processing Plant's input stock the holding accepts: some days of recipes at its output.</summary>
        public static int GetInputStock(PlanetaryHolding holding, HoldingDetail detail) =>
            HoldingTuning.InputStockDays * (GetDailyUnits(holding, detail) / Math.Max(1, detail.Chain.OwnUnits)) * detail.Chain.InputUnits;

        /// <summary>How many units of a good the holding still takes.</summary>
        public static int GetDeliverableUnits(PlanetaryHolding holding, HoldingDetail detail, CommodityType commodity) =>
            GetNeeds(holding, detail).FirstOrDefault(x => x.Commodity == commodity)?.Missing ?? 0;

        /// <summary>Adds delivered units to the supplies; a setback whose repair goods are now there ends.</summary>
        public static void Deliver(PlanetaryHolding holding, HoldingDetail detail, CommodityType commodity, int units, DateTime now, HoldingAdvanceResult result)
        {
            if (units <= 0)
                return;

            AddStock(holding.Supplies, commodity, units);
            TryRepair(holding, detail, now, result);
        }

        public static HoldingUpgradeDetail GetNextUpgrade(PlanetaryHolding holding, HoldingDetail detail) =>
            detail.Upgrades.FirstOrDefault(x => x.Level == holding.Level + 1);

        public static HoldingRefusal GetUpgradeRefusal(PlanetaryHolding holding, HoldingDetail detail, int credits)
        {
            var upgrade = GetNextUpgrade(holding, detail);
            if (upgrade == null)
                return HoldingRefusal.MaximumLevel;

            return GetCostRefusal(holding, upgrade, credits);
        }

        public static HoldingRefusal GetPlantRefusal(PlanetaryHolding holding, HoldingDetail detail, int credits)
        {
            if (holding.HasProcessingPlant)
                return HoldingRefusal.PlantBuilt;

            return GetCostRefusal(holding, detail.Plant, credits);
        }

        private static HoldingRefusal GetCostRefusal(PlanetaryHolding holding, HoldingUpgradeDetail cost, int credits)
        {
            if (cost.Goods.Any(x => GetSupply(holding, x.Key) < x.Value))
                return HoldingRefusal.MissingGoods;
            if (credits < cost.Credits)
                return HoldingRefusal.NotEnoughCredits;

            return HoldingRefusal.None;
        }

        /// <summary>Raises the holding a level, using up the level's goods (the caller takes the credits).</summary>
        public static void ApplyUpgrade(PlanetaryHolding holding, HoldingDetail detail)
        {
            var upgrade = GetNextUpgrade(holding, detail);
            foreach (var (commodity, units) in upgrade.Goods)
                TakeSupply(holding, commodity, units);

            holding.Level = upgrade.Level;
        }

        /// <summary>Builds the Processing Plant, using up its goods (the caller takes the credits).</summary>
        public static void ApplyPlant(PlanetaryHolding holding, HoldingDetail detail)
        {
            foreach (var (commodity, units) in detail.Plant.Goods)
                TakeSupply(holding, commodity, units);

            holding.HasProcessingPlant = true;
            holding.RefineOutput = true;
        }

        // Collection

        /// <summary>
        /// The warehouse goods that fit in a cargo bay with this much free volume, the most valuable per unit of volume
        /// first.
        /// </summary>
        public static Dictionary<CommodityType, int> PlanCollection(IReadOnlyDictionary<CommodityType, int> warehouse, int freeVolume)
        {
            var plan = new Dictionary<CommodityType, int>();
            var free = Math.Max(0, freeVolume);
            foreach (var (commodity, units) in warehouse
                         .Where(x => x.Value > 0)
                         .OrderByDescending(x => (double)(TradeLookup.GetCommodity(x.Key)?.BasePrice ?? 0) / TradeLookup.GetVolume(x.Key))
                         .ThenBy(x => x.Key))
            {
                var fits = Math.Min(units, free / Math.Max(1, TradeLookup.GetVolume(commodity)));
                if (fits <= 0)
                    continue;

                plan[commodity] = fits;
                free -= fits * TradeLookup.GetVolume(commodity);
            }

            return plan;
        }

        /// <summary>
        /// What collected units count as having cost, for a sale's profit and trade XP: their base price, as scooped cargo
        /// does, so a holding's goods give trade XP only for the haul.
        /// </summary>
        public static int GetCostBasis(CommodityType commodity, int units) =>
            Math.Max(0, units) * (TradeLookup.GetCommodity(commodity)?.BasePrice ?? 0);

        public static void RemoveStock(Dictionary<CommodityType, int> stock, CommodityType commodity, int units)
        {
            if (!stock.TryGetValue(commodity, out var have))
                return;

            if (have - units <= 0)
                stock.Remove(commodity);
            else
                stock[commodity] = have - units;
        }

        // Log and text

        public static void AddLog(PlanetaryHolding holding, DateTime when, string text, HoldingAdvanceResult result = null)
        {
            holding.Log ??= new List<string>();
            holding.Log.Add($"{when:yyyy-MM-dd}: {text}");
            while (holding.Log.Count > HoldingTuning.LogEntries)
                holding.Log.RemoveAt(0);

            result?.Events.Add(text);
        }

        public static string GetSetbackName(HoldingSetbackType setback) =>
            HoldingTuning.SetbackNames.TryGetValue(setback, out var name) ? name : "Setback";

        public static string GetSetbackText(HoldingSetbackType setback) =>
            HoldingTuning.SetbackTexts.TryGetValue(setback, out var text) ? text : string.Empty;

        public static string GetRefusalMessage(HoldingRefusal refusal)
        {
            return refusal switch
            {
                HoldingRefusal.NotSoldHere => "That holding is not sold at this dock.",
                HoldingRefusal.AtLimit => $"You already own {HoldingTuning.MaximumHoldings} holdings, the most a character may own.",
                HoldingRefusal.NotEnoughCredits => "You do not have enough credits.",
                HoldingRefusal.MaximumLevel => "This holding is already at its highest level.",
                HoldingRefusal.MissingGoods => "Deliver the goods it needs first.",
                HoldingRefusal.PlantBuilt => "This holding already has a Processing Plant.",
                HoldingRefusal.NotAtDock => "Manage this holding from its own planet's dock.",
                HoldingRefusal.ShipNotDocked => "Pick one of your ships docked at this dock.",
                HoldingRefusal.NothingToCollect => "The warehouse is empty.",
                HoldingRefusal.CargoBayFull => "The ship's cargo bay has no room for any of it.",
                HoldingRefusal.UpkeepOwed => "Pay the upkeep owed first.",
                HoldingRefusal.NothingNeeded => "The holding needs none of that good.",
                HoldingRefusal.NoneAboard => "The ship carries none of that good.",
                HoldingRefusal.DroidAlreadyPosted => "A droid already works this holding.",
                HoldingRefusal.NoDroid => "No droid works this holding.",
                HoldingRefusal.NoFine => "There is no fine to pay.",
                HoldingRefusal.NotOwner => "That holding is not yours.",
                _ => string.Empty,
            };
        }

        // Helpers

        public static int GetSupply(PlanetaryHolding holding, CommodityType commodity) =>
            holding.Supplies != null && holding.Supplies.TryGetValue(commodity, out var units) ? units : 0;

        private static void TakeSupply(PlanetaryHolding holding, CommodityType commodity, int units)
        {
            if (units > 0)
                RemoveStock(holding.Supplies, commodity, units);
        }

        private static void AddStock(Dictionary<CommodityType, int> stock, CommodityType commodity, int units)
        {
            if (units <= 0)
                return;

            stock[commodity] = (stock.TryGetValue(commodity, out var have) ? have : 0) + units;
        }

        private static int ClampLevel(int level) => Math.Clamp(level, 1, HoldingTuning.MaximumLevel);

        private static int Round(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }
}
