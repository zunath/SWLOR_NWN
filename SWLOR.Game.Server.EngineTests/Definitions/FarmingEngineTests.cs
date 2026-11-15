using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.ClerkRecoveryService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.EnzymeCultureService;
using SWLOR.Game.Server.Service.FarmhandService;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.LivestockService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Farming on live players and real records (Player City Revamp, "Farming (Agriculture)"): garden plots, livestock, farmhands,
    /// fermentation vats, the City Hall clerk's interrupted harvests and the server-start processing of each. Every test builds its
    /// own city and structure records, calls the real service entry points in a creature's script context, asserts the records and
    /// the inventory, and deletes everything it created.
    /// </summary>
    public static class FarmingEngineTests
    {
        private const string Category = "Farming";
        private const string ContextCreatureResref = "nw_bandit001";

        // Garden plots

        [EngineTest("A crop is refused tending early, tended after a quarter of its grow time and harvested ripe for 3 + tended + fertilized + Bountiful Harvest", Category = Category, TimeoutSeconds = 60f)]
        public static async Task PlantTendAndHarvest(EngineTestContext ctx)
        {
            using var farmer = await PlayerAbilityFixture.CreateAsync(ctx);
            await RequireCharacterSaves(ctx, farmer);
            var farm = new FarmRecords();
            farm.TrackPlayer(farmer.Id);

            try
            {
                var city = farm.City(farmer.Id);
                farm.GrantGardens(city.Id, farmer.Id);
                var plot = farm.Structure(StructureType.GardenPlot, city.Id);
                // Agriculture 15 opens Crop Management II and Bountiful Harvest I; a tier 2 crop (level 15) pays XP at that rank.
                MakeFarmer(farmer, city.Id, agricultureRank: 15, cropManagement: 2, bountifulHarvest: 1);
                var crop = Farming.GetCrop(CropType.ViscaranApple);
                var fertilizer = FertilizerRules.GetResref(crop.Tier);
                await Give(ctx, farmer, crop.SeedResref, 1);
                await Give(ctx, farmer, fertilizer, 1);

                var planted = await Run(ctx, farmer.Creature, () => Farming.Plant(farmer.Creature, plot.Id, crop.SeedResref, fertilizer));
                ctx.Assert(planted.Succeeded, $"the farmer plants the seed with fertilizer: {planted.Message}");
                var record = Farming.GetPlot(plot.Id);
                ctx.Assert(record != null, "the plot records the crop");
                ctx.AssertEqual(crop.Type, record.Crop, "the plot grows the seed's crop");
                ctx.AssertEqual(farmer.Id, record.PlanterPlayerId, "the farmer is the planter");
                ctx.Assert(record.Fertilized, "the crop is fertilized");
                ctx.Assert(!record.Tended, "the crop is not tended yet");
                ctx.AssertEqual(0, await Count(ctx, farmer, crop.SeedResref), "the seed is used up");
                ctx.AssertEqual(0, await Count(ctx, farmer, fertilizer), "the fertilizer is used up");

                var early = await Run(ctx, farmer.Creature, () => Farming.Tend(farmer.Creature, plot.Id));
                ctx.AssertEqual(FarmingRefusal.TooYoungToTend, early.Refusal, "a crop just planted is too young to tend");
                ctx.Assert(!Farming.GetPlot(plot.Id).Tended, "the refused tending changes nothing");

                ShiftPlot(plot.Id, TimeSpan.FromHours(crop.GrowHours / (double)FarmingRules.TendDivisor));
                var beforeTend = AgricultureProgress(farmer.Id);
                var tended = await Run(ctx, farmer.Creature, () => Farming.Tend(farmer.Creature, plot.Id));
                ctx.Assert(tended.Succeeded, $"a quarter of the grow time in, the crop can be tended: {tended.Message}");
                ctx.Assert(Farming.GetPlot(plot.Id).Tended, "the crop is saved as tended");
                ctx.Assert(Gained(beforeTend, AgricultureProgress(farmer.Id)), "tending grants Agriculture XP");

                var again = await Run(ctx, farmer.Creature, () => Farming.Tend(farmer.Creature, plot.Id));
                ctx.AssertEqual(FarmingRefusal.AlreadyTended, again.Refusal, "a crop is tended only once");

                var unripe = await Run(ctx, farmer.Creature, () => Farming.Harvest(farmer.Creature, plot.Id));
                ctx.AssertEqual(FarmingRefusal.NotRipe, unripe.Refusal, "an unripe crop cannot be harvested");
                ctx.AssertEqual(0, await Count(ctx, farmer, crop.HarvestResref), "the refused harvest gives nothing");

                // Past ripe, well before it withers.
                ShiftPlot(plot.Id, TimeSpan.FromHours(crop.GrowHours));
                var beforeHarvest = AgricultureProgress(farmer.Id);
                var harvested = await Run(ctx, farmer.Creature, () => Farming.Harvest(farmer.Creature, plot.Id));
                ctx.Assert(harvested.Succeeded, $"the ripe crop is harvested: {harvested.Message}");

                var expected = FarmingRules.BaseYield + 1 + 1 + 1;
                ctx.AssertEqual(expected, await Count(ctx, farmer, crop.HarvestResref), "the harvest is 3, +1 tended, +1 fertilized, +1 Bountiful Harvest I");
                ctx.Assert(Farming.GetPlot(plot.Id) == null, "the harvested plot is empty");
                ctx.AssertEqual(0, OverflowOf(farmer.Id).Count, "nothing went to overflow storage");
                ctx.Assert(Gained(beforeHarvest, AgricultureProgress(farmer.Id)), "harvesting grants Agriculture XP");
            }
            finally
            {
                farm.DeleteAll();
            }
        }

        [EngineTest("A growing crop stands as its own bed over a hidden plot, and the plot shows again once the crop is harvested", Category = Category, TimeoutSeconds = 60f)]
        public static async Task GrowingCropReplacesThePlotsBed(EngineTestContext ctx)
        {
            using var farmer = await PlayerAbilityFixture.CreateAsync(ctx);
            await RequireCharacterSaves(ctx, farmer);
            var farm = new FarmRecords();
            farm.TrackPlayer(farmer.Id);
            var placeable = OBJECT_INVALID;

            try
            {
                var city = farm.City(farmer.Id);
                farm.GrantGardens(city.Id, farmer.Id);
                var plot = farm.Structure(StructureType.GardenPlot, city.Id);
                MakeFarmer(farmer, city.Id, agricultureRank: 15, cropManagement: 2, bountifulHarvest: 1);
                var crop = Farming.GetCrop(CropType.ViscaranApple);
                await Give(ctx, farmer, crop.SeedResref, 1);

                await Run(ctx, farmer.Creature, () =>
                {
                    placeable = CreateObject(ObjectType.Placeable, "growing_plant", GetLocation(farmer.Creature));
                    ctx.Assert(GetIsObjectValid(placeable), "the Garden Plot blueprint spawns");
                    Farming.OnPlotSpawned(plot, placeable);
                    ctx.Assert(!GetIsObjectValid(Farming.GetCropModel(placeable)), "an empty plot shows only its own bed");
                    ctx.AssertEqual(VisibilityType.Default, VisibilityPlugin.GetVisibilityOverride(OBJECT_INVALID, placeable), "an empty plot is shown");
                });

                var planted = await Run(ctx, farmer.Creature, () => Farming.Plant(farmer.Creature, plot.Id, crop.SeedResref, string.Empty));
                ctx.Assert(planted.Succeeded, $"the farmer plants the seed: {planted.Message}");
                await Run(ctx, farmer.Creature, () =>
                {
                    Farming.OnPlotSpawned(plot, placeable);
                    var model = Farming.GetCropModel(placeable);
                    ctx.Assert(GetIsObjectValid(model), "a growing crop stands as its own bed");
                    ctx.AssertEqual(FarmingRules.GrowingCropAppearance, GetLocalInt(placeable, "GARDEN_PLOT_CROP_APPEARANCE"), "the bed is the sprouting bed");
                    ctx.AssertEqual(VisibilityType.Hidden, VisibilityPlugin.GetVisibilityOverride(OBJECT_INVALID, placeable), "the plot's own bed hides behind it");

                    // Manage Structures nudges and turns only the plot; the bed standing in for it follows.
                    var moved = GetPosition(placeable);
                    moved.X += 0.5f;
                    ObjectPlugin.SetPosition(placeable, moved);
                    AssignCommand(placeable, () => SetFacing(135f));
                    AssignCommand(placeable, () => Farming.MatchCropModel(placeable));
                });
                await ctx.DelaySecondsAsync(1f);
                await Run(ctx, farmer.Creature, () =>
                {
                    var model = Farming.GetCropModel(placeable);
                    ctx.Assert(GetDistanceBetweenLocations(GetLocation(model), GetLocation(placeable)) < 0.01f, "the crop's bed moves with its plot");
                    ctx.Assert(System.Math.Abs(GetFacing(model) - GetFacing(placeable)) < 0.5f, $"the crop's bed turns with its plot ({GetFacing(model)} vs {GetFacing(placeable)})");
                });

                ShiftPlot(plot.Id, TimeSpan.FromHours(crop.GrowHours));
                var harvested = await Run(ctx, farmer.Creature, () => Farming.Harvest(farmer.Creature, plot.Id));
                ctx.Assert(harvested.Succeeded, $"the ripe crop is harvested: {harvested.Message}");
                await Run(ctx, farmer.Creature, () =>
                {
                    Farming.OnPlotSpawned(plot, placeable);
                    ctx.Assert(!GetIsObjectValid(Farming.GetCropModel(placeable)), "the harvested plot has no crop bed");
                    ctx.AssertEqual(VisibilityType.Default, VisibilityPlugin.GetVisibilityOverride(OBJECT_INVALID, placeable), "the harvested plot shows again");
                });
            }
            finally
            {
                if (GetIsObjectValid(placeable))
                {
                    var model = Farming.GetCropModel(placeable);
                    if (GetIsObjectValid(model))
                        DestroyObject(model);
                    DestroyObject(placeable);
                }
                farm.DeleteAll();
            }
        }

        [EngineTest("Planting is refused for a crop tier above Crop Management and at the 11th crop of a CD key", Category = Category, TimeoutSeconds = 60f)]
        public static async Task CropManagementAndCropCap(EngineTestContext ctx)
        {
            using var farmer = await PlayerAbilityFixture.CreateAsync(ctx);
            await RequireCharacterSaves(ctx, farmer);
            var farm = new FarmRecords();
            farm.TrackPlayer(farmer.Id);

            try
            {
                var city = farm.City(farmer.Id);
                farm.GrantGardens(city.Id, farmer.Id);
                MakeFarmer(farmer, city.Id, agricultureRank: 5, cropManagement: 1, bountifulHarvest: 0);
                var tierTwo = Farming.GetCrop(CropType.ViscaranApple);
                var tierOne = Farming.GetCrop(CropType.ViscaranOrange);

                var locked = farm.Structure(StructureType.GardenPlot, city.Id);
                await Give(ctx, farmer, tierTwo.SeedResref, 1);
                var refused = await Run(ctx, farmer.Creature, () => Farming.Plant(farmer.Creature, locked.Id, tierTwo.SeedResref));
                ctx.AssertEqual(FarmingRefusal.TierLocked, refused.Refusal, "Crop Management I does not open tier 2 crops");
                ctx.AssertEqual(1, await Count(ctx, farmer, tierTwo.SeedResref), "the refused seed stays in the inventory");
                ctx.Assert(Farming.GetPlot(locked.Id) == null, "the refused plot stays empty");

                // Ten crops growing for the farmer's CD key, each in a plot that stands.
                var cdKey = await Run(ctx, farmer.Creature, () => GetPCPublicCDKey(farmer.Creature));
                var now = DateTime.UtcNow;
                var growing = new List<WorldProperty>();
                for (var index = 0; index < FarmingRules.MaxCropsPerCdKey; index++)
                {
                    var plot = farm.Structure(StructureType.GardenPlot, city.Id);
                    growing.Add(plot);
                    DB.Set(GrowingCrop(plot.Id, farmer.Id, cdKey, tierOne, now));
                }

                var eleventh = farm.Structure(StructureType.GardenPlot, city.Id);
                await Give(ctx, farmer, tierOne.SeedResref, 1);
                var capped = await Run(ctx, farmer.Creature, () => Farming.Plant(farmer.Creature, eleventh.Id, tierOne.SeedResref));
                ctx.AssertEqual(FarmingRefusal.CropCap, capped.Refusal, "the 11th crop of a CD key is refused");
                ctx.AssertEqual(1, await Count(ctx, farmer, tierOne.SeedResref), "the capped seed stays in the inventory");
                ctx.Assert(Farming.GetPlot(eleventh.Id) == null, "the capped plot stays empty");

                var cleared = await Run(ctx, farmer.Creature, () => Farming.Clear(farmer.Creature, growing[0].Id));
                ctx.Assert(cleared.Succeeded, $"the planter clears one crop: {cleared.Message}");
                var planted = await Run(ctx, farmer.Creature, () => Farming.Plant(farmer.Creature, eleventh.Id, tierOne.SeedResref));
                ctx.Assert(planted.Succeeded, $"with one crop cleared the seed can be planted: {planted.Message}");
                ctx.AssertEqual(0, await Count(ctx, farmer, tierOne.SeedResref), "the seed is used up");
            }
            finally
            {
                farm.DeleteAll();
            }
        }

        [EngineTest("A withered crop is removed by server-start processing and by the next use of its plot, and a growing one is kept", Category = Category, TimeoutSeconds = 60f)]
        public static async Task WitheredCropsAreRemoved(EngineTestContext ctx)
        {
            using var farmer = await PlayerAbilityFixture.CreateAsync(ctx);
            await RequireCharacterSaves(ctx, farmer);
            var farm = new FarmRecords();
            farm.TrackPlayer(farmer.Id);

            try
            {
                var city = farm.City(farmer.Id);
                farm.GrantGardens(city.Id, farmer.Id);
                MakeFarmer(farmer, city.Id, agricultureRank: 5, cropManagement: 1, bountifulHarvest: 0);
                var crop = Farming.GetCrop(CropType.ViscaranOrange);
                var cdKey = await Run(ctx, farmer.Creature, () => GetPCPublicCDKey(farmer.Creature));
                var now = DateTime.UtcNow;

                var witheredAtStart = farm.Structure(StructureType.GardenPlot, city.Id);
                var growing = farm.Structure(StructureType.GardenPlot, city.Id);
                DB.Set(WitheredCrop(witheredAtStart.Id, farmer.Id, cdKey, crop, now));
                DB.Set(GrowingCrop(growing.Id, farmer.Id, cdKey, crop, now));

                await Run(ctx, farmer.Creature, () => Farming.ProcessPlotsAtServerStart());
                ctx.Assert(Farming.GetPlot(witheredAtStart.Id) == null, "server-start processing removes the crop that withered while the server was down");
                ctx.Assert(Farming.GetPlot(growing.Id) != null, "a growing crop survives server-start processing");

                var witheredInUse = farm.Structure(StructureType.GardenPlot, city.Id);
                DB.Set(WitheredCrop(witheredInUse.Id, farmer.Id, cdKey, crop, now));
                var harvest = await Run(ctx, farmer.Creature, () => Farming.Harvest(farmer.Creature, witheredInUse.Id));
                ctx.AssertEqual(FarmingRefusal.PlotEmpty, harvest.Refusal, "harvesting a withered crop finds the plot empty");
                ctx.Assert(Farming.GetPlot(witheredInUse.Id) == null, "using the plot removes the withered crop");
                ctx.AssertEqual(0, await Count(ctx, farmer, crop.HarvestResref), "a withered crop gives nothing");
                ctx.AssertEqual(0, await Count(ctx, farmer, crop.SeedResref), "a withered crop's seed is lost");

                await Give(ctx, farmer, crop.SeedResref, 1);
                var replanted = await Run(ctx, farmer.Creature, () => Farming.Plant(farmer.Creature, witheredInUse.Id, crop.SeedResref));
                ctx.Assert(replanted.Succeeded, $"the plot can be planted again: {replanted.Message}");
            }
            finally
            {
                farm.DeleteAll();
            }
        }

        [EngineTest("Server-start processing finishes an interrupted clerk harvest exactly once into overflow storage", Category = Category, TimeoutSeconds = 30f)]
        public static async Task InterruptedClerkHarvestFinishesOnce(EngineTestContext ctx)
        {
            var context = await SpawnContextCreature(ctx);
            var farm = new FarmRecords();
            var planterId = Guid.NewGuid().ToString();
            farm.TrackPlayer(planterId);

            try
            {
                var city = farm.City(planterId);
                var plot = farm.Structure(StructureType.GardenPlot, city.Id);
                var crop = Farming.GetCrop(CropType.ViscaranOrange);
                var now = DateTime.UtcNow;
                var markId = Guid.NewGuid().ToString();
                var record = GrowingCrop(plot.Id, planterId, string.Empty, crop, now.AddHours(-crop.GrowHours - 1));
                // A restart stopped the clerk after the mark was saved and before the overflow records and the deletion.
                record.Recovery = ClerkRecoveryRules.BuildHarvestMark(markId, planterId, now, crop.HarvestResref, 5, crop.SeedResref, true);
                DB.Set(record);

                await Run(ctx, context, () => Farming.ProcessPlotsAtServerStart());

                var overflow = OverflowOf(planterId);
                var prefix = $"clerk-recovery:{markId}:";
                ctx.Assert(overflow.Count > 0 && overflow.All(item => item.Id.StartsWith(prefix, StringComparison.Ordinal)),
                    "every overflow record carries the mark's ids");
                ctx.AssertEqual(5, overflow.Where(item => item.Resref == crop.HarvestResref).Sum(item => item.Quantity), "the marked harvest is in overflow storage");
                ctx.AssertEqual(1, overflow.Where(item => item.Resref == crop.SeedResref).Sum(item => item.Quantity), "the marked seed is in overflow storage");
                ctx.Assert(Farming.GetPlot(plot.Id) == null, "the harvested crop is gone");

                var firstIds = overflow.Select(item => item.Id).OrderBy(id => id, StringComparer.Ordinal).ToList();
                await Run(ctx, context, () => Farming.ProcessPlotsAtServerStart());
                var secondIds = OverflowOf(planterId).Select(item => item.Id).OrderBy(id => id, StringComparer.Ordinal).ToList();
                ctx.Assert(firstIds.SequenceEqual(secondIds), "a second processing creates nothing new");
            }
            finally
            {
                farm.DeleteAll();
            }
        }

        // Livestock

        [EngineTest("Livestock is placed, fed its tier, grows up after 72 hours and 3 feedings, gives products, is butchered, and crates with its timers", Category = Category, TimeoutSeconds = 90f)]
        public static async Task LivestockLifecycle(EngineTestContext ctx)
        {
            using var farmer = await PlayerAbilityFixture.CreateAsync(ctx);
            await RequireCharacterSaves(ctx, farmer);
            var farm = new FarmRecords();
            farm.TrackPlayer(farmer.Id);

            try
            {
                var city = farm.City(farmer.Id);
                var corral = farm.Structure(StructureType.Corral, city.Id);
                // Agriculture 50 opens Crop Management V (tier 5 animals) and Bountiful Harvest III.
                MakeFarmer(farmer, city.Id, agricultureRank: 50, cropManagement: 5, bountifulHarvest: 3);
                var warocas = Corrals.GetDefinition(LivestockType.Warocas);
                var thune = Corrals.GetDefinition(LivestockType.Thune);

                var warocasId = await PlaceYoung(ctx, farmer, corral.Id, warocas);
                var thuneId = await PlaceYoung(ctx, farmer, corral.Id, thune);
                ctx.AssertEqual(2, Corrals.GetAnimalsInCorral(corral.Id).Count, "both animals stand in the corral");

                var orange = Farming.GetCrop(CropType.ViscaranOrange);
                var apple = Farming.GetCrop(CropType.ViscaranApple);
                await Give(ctx, farmer, apple.HarvestResref, LivestockRules.FeedCrops);
                var tooHigh = await Run(ctx, farmer.Creature, () => Corrals.Feed(farmer.Creature, warocasId, apple.Type));
                ctx.AssertEqual(LivestockRefusal.CropTierTooHigh, tooHigh.Refusal, "a tier 1 animal does not eat a tier 2 crop");
                ctx.AssertEqual(LivestockRules.FeedCrops, await Count(ctx, farmer, apple.HarvestResref), "the refused crops stay in the inventory");

                await Give(ctx, farmer, orange.HarvestResref, LivestockRules.FeedCrops * 4);
                await Feed(ctx, farmer, warocasId, orange, 1);
                var tooSoon = await Run(ctx, farmer.Creature, () => Corrals.Feed(farmer.Creature, warocasId, orange.Type));
                ctx.AssertEqual(LivestockRefusal.FeedTooSoon, tooSoon.Refusal, "an animal fed less than 20 hours ago is refused");

                ShiftAnimal(warocasId, TimeSpan.FromHours(LivestockRules.FeedIntervalHours + 1));
                await Feed(ctx, farmer, warocasId, orange, 2);
                ShiftAnimal(warocasId, TimeSpan.FromHours(LivestockRules.FeedIntervalHours + 1));
                await Feed(ctx, farmer, warocasId, orange, 3);

                // Three feedings, but only 42 hours since it was placed.
                ctx.AssertEqual(LivestockStage.Young, LivestockRules.GetStage(Corrals.GetAnimal(warocasId), DateTime.UtcNow), "three feedings alone do not make an adult");
                var young = await Run(ctx, farmer.Creature, () => Corrals.Collect(farmer.Creature, warocasId));
                ctx.AssertEqual(LivestockRefusal.StillYoung, young.Refusal, "a young animal gives nothing");

                ShiftAnimal(warocasId, TimeSpan.FromHours(31));
                ctx.AssertEqual(LivestockStage.Adult, LivestockRules.GetStage(Corrals.GetAnimal(warocasId), DateTime.UtcNow), "72 hours after placing with 3 feedings it is an adult");
                await Feed(ctx, farmer, warocasId, orange, 4);
                ctx.AssertEqual(0, await Count(ctx, farmer, orange.HarvestResref), "each feeding used 2 crops");

                var collected = await Run(ctx, farmer.Creature, () => Corrals.Collect(farmer.Creature, warocasId));
                ctx.Assert(collected.Succeeded, $"the fed adult is collected from: {collected.Message}");
                ctx.AssertEqual(1, await Count(ctx, farmer, warocas.ManureResref), "a collection gives 1 manure of the animal's tier");
                ctx.Assert(Corrals.GetAnimal(warocasId).LastCollectedUtc.HasValue, "the collection is recorded");
                var again = await Run(ctx, farmer.Creature, () => Corrals.Collect(farmer.Creature, warocasId));
                ctx.AssertEqual(LivestockRefusal.CollectTooSoon, again.Refusal, "a second collection within 20 hours is refused");
                ctx.AssertEqual(1, await Count(ctx, farmer, warocas.ManureResref), "the refused collection gives nothing");

                var butchered = await Run(ctx, farmer.Creature, () => Corrals.Butcher(farmer.Creature, warocasId));
                ctx.Assert(butchered.Succeeded, $"the adult is butchered: {butchered.Message}");
                ctx.AssertEqual(LivestockRules.ButcherMeat + 3, await Count(ctx, farmer, warocas.MeatResref), "butchering gives 4 meat + Bountiful Harvest III");
                ctx.AssertEqual(1, await Count(ctx, farmer, warocas.SecondProductResref), "and 1 of the second product");
                ctx.Assert(Corrals.GetAnimal(warocasId) == null, "the butchered animal is gone");

                // The Thune: an adult fed 2 hours ago.
                var adult = Corrals.GetAnimal(thuneId);
                var placed = DateTime.UtcNow;
                adult.Feedings = LivestockRules.MaturityFeedings;
                adult.PlacedUtc = placed.AddHours(-80);
                adult.LastFedUtc = placed.AddHours(-2);
                DB.Set(adult);
                var milked = await Run(ctx, farmer.Creature, () => Corrals.Collect(farmer.Creature, thuneId));
                ctx.Assert(milked.Succeeded, $"the Thune is collected from: {milked.Message}");
                ctx.AssertEqual(1, await Count(ctx, farmer, thune.ManureResref), "a Thune gives its tier's manure");
                ctx.AssertEqual(1, await Count(ctx, farmer, thune.CollectExtraResref), "and 1 milk");

                var beforeCrate = Corrals.GetAnimal(thuneId);
                var crated = await Run(ctx, farmer.Creature, () => Corrals.Crate(farmer.Creature, thuneId));
                ctx.Assert(crated.Succeeded, $"the Thune is crated: {crated.Message}");
                ctx.Assert(Corrals.GetAnimal(thuneId) == null, "the crated animal leaves the corral");
                ctx.AssertEqual(1, await Count(ctx, farmer, LivestockRules.CrateResref), "the crate is in the inventory");

                var crate = await Run(ctx, farmer.Creature, () => FindItem(farmer.Creature, LivestockRules.CrateResref));
                var replaced = await Run(ctx, farmer.Creature, () => Corrals.Place(farmer.Creature, corral.Id, crate));
                ctx.Assert(replaced.Succeeded, $"the crate is placed again: {replaced.Message}");
                ctx.AssertEqual(0, await Count(ctx, farmer, LivestockRules.CrateResref), "the crate is used up");
                var restored = Corrals.GetAnimalsInCorral(corral.Id).Single();
                ctx.AssertEqual(LivestockType.Thune, restored.Species, "the crate holds the Thune");
                ctx.AssertEqual(beforeCrate.Feedings, restored.Feedings, "its feedings are kept");
                // A crate keeps the time left until maturity, which is none for an adult: it comes back an adult.
                ctx.Assert(LivestockRules.IsAdult(restored, DateTime.UtcNow), "the adult is still an adult");
                AssertClose(ctx, beforeCrate.LastFedUtc, restored.LastFedUtc, "its hunger timer resumes");
                AssertClose(ctx, beforeCrate.LastCollectedUtc, restored.LastCollectedUtc, "its collection timer resumes");
            }
            finally
            {
                farm.DeleteAll();
            }
        }

        [EngineTest("Server-start processing removes an animal unfed for 72 hours, and a crated animal's timers do not advance", Category = Category, TimeoutSeconds = 60f)]
        public static async Task LivestockRestart(EngineTestContext ctx)
        {
            using var farmer = await PlayerAbilityFixture.CreateAsync(ctx);
            await RequireCharacterSaves(ctx, farmer);
            var farm = new FarmRecords();
            farm.TrackPlayer(farmer.Id);
            var absentOwnerId = Guid.NewGuid().ToString();
            farm.TrackPlayer(absentOwnerId);

            try
            {
                var city = farm.City(farmer.Id);
                var corral = farm.Structure(StructureType.Corral, city.Id);
                MakeFarmer(farmer, city.Id, agricultureRank: 5, cropManagement: 1, bountifulHarvest: 0);
                var warocas = Corrals.GetDefinition(LivestockType.Warocas);
                var now = DateTime.UtcNow;

                // The farmer's animal, an hour from wandering off, is crated.
                var animalId = await PlaceYoung(ctx, farmer, corral.Id, warocas);
                var hungry = Corrals.GetAnimal(animalId);
                hungry.Feedings = 1;
                hungry.PlacedUtc = now.AddHours(-(LivestockRules.WanderOffHours - 1));
                hungry.LastFedUtc = hungry.PlacedUtc;
                DB.Set(hungry);
                var crated = await Run(ctx, farmer.Creature, () => Corrals.Crate(farmer.Creature, animalId));
                ctx.Assert(crated.Succeeded, $"the hungry animal is crated: {crated.Message}");
                var hungerSeconds = await Run(ctx, farmer.Creature, () => GetLocalInt(FindItem(farmer.Creature, LivestockRules.CrateResref), LivestockCrate.HungerSecondsLocal));
                ctx.Assert(hungerSeconds > 0 && hungerSeconds <= 3600, $"the crate holds the hour of hunger left, not a deadline ({hungerSeconds} s)");

                // Another owner's animal, never fed and placed 73 hours ago, is still on record when the server starts. It is written
                // after the farmer's placements, which clear wandered-off animals of the corral themselves.
                var unfed = new Livestock
                {
                    OwnerPlayerId = absentOwnerId,
                    CorralId = corral.Id,
                    Species = LivestockType.Warocas,
                    PlacedUtc = now.AddHours(-(LivestockRules.WanderOffHours + 1)),
                    LastProcessedUtc = now.AddHours(-(LivestockRules.WanderOffHours + 1)),
                };
                DB.Set(unfed);

                await Run(ctx, farmer.Creature, () => Corrals.ProcessAtServerStart());
                ctx.Assert(Corrals.GetAnimal(unfed.Id) == null, "the animal unfed for 72 hours wandered off at server start");
                ctx.Assert(Search(new DBQuery<LivestockNotice>().AddFieldSearch(nameof(LivestockNotice.PlayerId), absentOwnerId, false)).Count == 1,
                    "its absent owner is told at their next login");
                ctx.AssertEqual(0, Corrals.GetAnimalsInCorral(corral.Id).Count, "the crated animal is not in the corral");
                ctx.AssertEqual(hungerSeconds, await Run(ctx, farmer.Creature, () => GetLocalInt(FindItem(farmer.Creature, LivestockRules.CrateResref), LivestockCrate.HungerSecondsLocal)),
                    "server-start processing does not touch the crate");

                // Time passes while it is crated.
                await ctx.DelaySecondsAsync(3f);
                var crate = await Run(ctx, farmer.Creature, () => FindItem(farmer.Creature, LivestockRules.CrateResref));
                var placed = await Run(ctx, farmer.Creature, () => Corrals.Place(farmer.Creature, corral.Id, crate));
                ctx.Assert(placed.Succeeded, $"the crate is placed again: {placed.Message}");
                var restored = Corrals.GetAnimalsInCorral(corral.Id).Single();
                var hungerLeft = (LivestockRules.GetWanderOffUtc(restored) - restored.LastProcessedUtc).TotalSeconds;
                ctx.Assert(Math.Abs(hungerLeft - hungerSeconds) < 1.0, $"the hunger left resumes where the crate stopped it ({hungerLeft:F1} s of {hungerSeconds} s)");
                ctx.Assert(!LivestockRules.HasWanderedOff(restored, restored.LastProcessedUtc), "the animal has not wandered off");
            }
            finally
            {
                farm.DeleteAll();
            }
        }

        // Farmhands

        [EngineTest("A farmhand replays 48 hours of feeding and collecting in order, and a second catch-up changes nothing", Category = Category, TimeoutSeconds = 60f)]
        public static async Task FarmhandCatchUpReplays(EngineTestContext ctx)
        {
            using var farmer = await PlayerAbilityFixture.CreateAsync(ctx);
            await RequireCharacterSaves(ctx, farmer);
            var farm = new FarmRecords();
            farm.TrackPlayer(farmer.Id);
            const int droidTier = 2;

            try
            {
                var city = farm.City(farmer.Id);
                var corral = farm.Structure(StructureType.Corral, city.Id);
                MakeFarmer(farmer, city.Id, agricultureRank: 5, cropManagement: 1, bountifulHarvest: 0);
                var orange = Farming.GetCrop(CropType.ViscaranOrange);
                var warocas = Corrals.GetDefinition(LivestockType.Warocas);

                var controller = OBJECT_INVALID;
                await ctx.ExecuteInCreatureContextAsync(farmer.Creature, () =>
                {
                    controller = CreateItemOnObject(Droid.DroidControlItemResref, farmer.Creature);
                    ctx.Assert(GetIsObjectValid(controller), "the droid controller exists");
                    AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Tier, droidTier), controller);
                });
                await ctx.WaitFrameAsync();
                ctx.AssertEqual(droidTier, await Run(ctx, farmer.Creature, () => Farmhands.GetDroidTier(controller)), "the controller is a tier 2 droid");

                var assigned = await Run(ctx, farmer.Creature, () => Farmhands.AssignDroid(farmer.Creature, corral.Id, controller));
                ctx.AssertEqual(FarmhandRefusal.None, assigned, "the droid is assigned to the corral");
                var farmhand = Farmhands.Get(corral.Id);
                ctx.Assert(farmhand != null, "the corral has a farmhand");
                ctx.AssertEqual(droidTier, farmhand.DroidTier, "the farmhand keeps the droid's tier");
                ctx.AssertEqual(farmer.Id, farmhand.OwnerPlayerId, "the farmhand works for the farmer");

                await Give(ctx, farmer, orange.HarvestResref, 6);
                var loaded = 0;
                var load = await Run(ctx, farmer.Creature, () => Farmhands.LoadFeed(farmer.Creature, corral.Id, orange.Type, out loaded));
                ctx.AssertEqual(FarmhandRefusal.None, load, "the feed is loaded");
                ctx.AssertEqual(6, loaded, "all 6 crops go in");
                ctx.AssertEqual(0, await Count(ctx, farmer, orange.HarvestResref), "the loaded crops leave the inventory");
                ctx.AssertEqual(6, Farmhands.Get(corral.Id).FeedHopper.GetValueOrDefault(orange.Type), "the feed hopper holds them");

                // 48 hours ago (T0): the farmhand was last processed, and an adult fed an hour before that had never been collected from.
                var start = DateTime.UtcNow.AddHours(-48);
                farmhand = Farmhands.Get(corral.Id);
                farmhand.LastProcessedUtc = start;
                DB.Set(farmhand);
                var animal = new Livestock
                {
                    OwnerPlayerId = farmer.Id,
                    CorralId = corral.Id,
                    Species = LivestockType.Warocas,
                    PlacedUtc = start.AddHours(-100),
                    Feedings = LivestockRules.MaturityFeedings,
                    LastFedUtc = start.AddHours(-1),
                    LastProcessedUtc = start,
                };
                DB.Set(animal);

                await Run(ctx, farmer.Creature, () => Farmhands.CatchUp(corral.Id));

                // Collected at T0, fed at T0+19h, collected at T0+20h, fed at T0+39h, collected at T0+40h; the next meal (T0+59h) is still ahead.
                var replayed = Corrals.GetAnimal(animal.Id);
                farmhand = Farmhands.Get(corral.Id);
                ctx.AssertEqual(LivestockRules.MaturityFeedings + 2, replayed.Feedings, "the farmhand fed the animal twice");
                AssertClose(ctx, start.AddHours(39), replayed.LastFedUtc, "the last meal was 20 hours after the one before");
                AssertClose(ctx, start.AddHours(40), replayed.LastCollectedUtc, "the last collection was every 20 hours from T0");
                ctx.AssertEqual(2, farmhand.FeedHopper.GetValueOrDefault(orange.Type), "two meals used 4 of the 6 crops");
                ctx.AssertEqual(3, farmhand.OutputHopper.GetValueOrDefault(warocas.ManureResref), "three collections put 3 manure in the output hopper");
                ctx.Assert(DB.Get<FarmhandCatchUp>(FarmhandCatchUp.KeyFor(corral.Id)) == null, "the catch-up left no pending record");

                await Run(ctx, farmer.Creature, () => Farmhands.CatchUp(corral.Id));
                var unchanged = Corrals.GetAnimal(animal.Id);
                var secondFarmhand = Farmhands.Get(corral.Id);
                ctx.AssertEqual(replayed.Feedings, unchanged.Feedings, "a second catch-up feeds nothing");
                ctx.AssertEqual(replayed.LastFedUtc, unchanged.LastFedUtc, "a second catch-up keeps the last meal");
                ctx.AssertEqual(replayed.LastCollectedUtc, unchanged.LastCollectedUtc, "a second catch-up collects nothing");
                ctx.AssertEqual(2, secondFarmhand.FeedHopper.GetValueOrDefault(orange.Type), "the feed hopper is unchanged");
                ctx.AssertEqual(3, secondFarmhand.OutputHopper.GetValueOrDefault(warocas.ManureResref), "the output hopper is unchanged");
            }
            finally
            {
                farm.DeleteAll();
            }
        }

        // Fermentation

        [EngineTest("A culture needs a hunted starter for a new color, completes once to its owner, and a finished batch of a lost vat is delivered once at server start", Category = Category, TimeoutSeconds = 60f)]
        public static async Task FermentationBatch(EngineTestContext ctx)
        {
            using var farmer = await PlayerAbilityFixture.CreateAsync(ctx);
            await RequireCharacterSaves(ctx, farmer);
            var farm = new FarmRecords();
            farm.TrackPlayer(farmer.Id);
            var absentOwnerId = Guid.NewGuid().ToString();
            farm.TrackPlayer(absentOwnerId);
            var lostVatId = Guid.NewGuid().ToString();
            farm.TrackProperty(lostVatId);

            try
            {
                var city = farm.City(farmer.Id);
                var house = farm.House(city.Id, farmer.Id);
                var vat = farm.Structure(StructureType.FermentationVat, house.Id);
                farm.GrantGardens(house.Id, farmer.Id);
                MakeFarmer(farmer, city.Id, agricultureRank: EnzymeCultureRules.RequiredAgricultureRank, cropManagement: 0, bountifulHarvest: 0);
                const EnzymeType type = EnzymeType.Hydrolase;
                const EnzymeColorType color = EnzymeColorType.Blue;
                var enzyme = EnzymeCultureRules.GetResref(type, color);
                var orange = Farming.GetCrop(CropType.ViscaranOrange);
                var manure = LivestockRules.GetManureResref(1);

                await Give(ctx, farmer, orange.HarvestResref, EnzymeCultureRules.CropsPerBatch);
                await Give(ctx, farmer, manure, EnzymeCultureRules.ManurePerBatch);
                await Give(ctx, farmer, EnzymeCultureRules.WaterResref, EnzymeCultureRules.WaterPerBatch);

                var noStarter = await Run(ctx, farmer.Creature, () => FermentationVats.Start(farmer.Creature, vat.Id, type, color, orange.Type));
                ctx.AssertEqual(FermentationRefusal.NoStarter, noStarter.Refusal, "a color the vat has not copied needs a hunted starter");
                ctx.Assert(FermentationVats.GetJob(vat.Id) == null, "the refused batch is not started");
                ctx.AssertEqual(EnzymeCultureRules.CropsPerBatch, await Count(ctx, farmer, orange.HarvestResref), "the refused crops stay in the inventory");

                await Give(ctx, farmer, enzyme, 1);
                var started = await Run(ctx, farmer.Creature, () => FermentationVats.Start(farmer.Creature, vat.Id, type, color, orange.Type));
                ctx.Assert(started.Succeeded, $"the batch starts with the starter: {started.Message}");
                var job = FermentationVats.GetJob(vat.Id);
                ctx.Assert(job != null, "the vat holds the batch");
                ctx.AssertEqual(farmer.Id, job.OwnerPlayerId, "the farmer owns the batch");
                ctx.Assert(FermentationVats.GetRecord(vat.Id)?.Starters.Contains(enzyme) == true, "the vat remembers the color");
                ctx.AssertEqual(0, await Count(ctx, farmer, orange.HarvestResref), "the 2 crops are used");
                ctx.AssertEqual(0, await Count(ctx, farmer, manure), "the manure is used");
                ctx.AssertEqual(0, await Count(ctx, farmer, EnzymeCultureRules.WaterResref), "the distilled water is used");
                ctx.AssertEqual(0, await Count(ctx, farmer, enzyme), "the starter is used");

                var notDone = await Run(ctx, farmer.Creature, () => FermentationVats.Collect(farmer.Creature, vat.Id));
                ctx.AssertEqual(FermentationRefusal.NotDone, notDone.Refusal, "a batch cannot be collected before it completes");

                job.CompleteUtc = DateTime.UtcNow.AddMinutes(-1);
                DB.Set(job);
                var collected = await Run(ctx, farmer.Creature, () => FermentationVats.Collect(farmer.Creature, vat.Id));
                ctx.Assert(collected.Succeeded, $"the completed batch is collected: {collected.Message}");
                ctx.AssertEqual(1, await Count(ctx, farmer, enzyme), "the enzyme goes to its owner");
                var cultured = await Run(ctx, farmer.Creature, () => GetLocalInt(FindItem(farmer.Creature, enzyme), FermentationVats.CulturedVariable));
                ctx.AssertEqual(1, cultured, "the enzyme is marked cultured");
                ctx.Assert(FermentationVats.GetJob(vat.Id) == null, "the vat is empty");
                var twice = await Run(ctx, farmer.Creature, () => FermentationVats.Collect(farmer.Creature, vat.Id));
                ctx.AssertEqual(FermentationRefusal.NoBatch, twice.Refusal, "the batch completes only once");
                ctx.AssertEqual(1, await Count(ctx, farmer, enzyme), "a second collection gives nothing");

                // A finished batch whose vat is gone, found at server start.
                var started2 = DateTime.UtcNow.AddHours(-(EnzymeCultureRules.Hours + 1));
                var lost = new FermentationJob
                {
                    Id = FermentationJob.KeyFor(lostVatId),
                    VatId = lostVatId,
                    OwnerPlayerId = absentOwnerId,
                    Type = type,
                    Color = color,
                    StartedUtc = started2,
                    CompleteUtc = EnzymeCultureRules.GetCompleteUtc(started2),
                    CropResref = orange.HarvestResref,
                    ManureResref = manure,
                    WaterResref = EnzymeCultureRules.WaterResref,
                };
                DB.Set(lost);

                await Run(ctx, farmer.Creature, () => FermentationVats.ProcessAtServerStart());
                var delivered = OverflowOf(absentOwnerId);
                ctx.AssertEqual(1, delivered.Count, "the finished batch is delivered to its owner's overflow storage");
                ctx.AssertEqual(enzyme, delivered[0].Resref, "as the enzyme, not the ingredients");
                ctx.Assert(FermentationVats.GetJob(lostVatId) == null, "the delivered batch is gone");

                await Run(ctx, farmer.Creature, () => FermentationVats.ProcessAtServerStart());
                ctx.AssertEqual(1, OverflowOf(absentOwnerId).Count, "a second processing delivers nothing more");
            }
            finally
            {
                farm.DeleteAll();
            }
        }

        // Hunts

        [EngineTest("A livestock hunt that ran out while the server was down gets its full time again at server start", Category = Category, TimeoutSeconds = 30f)]
        public static async Task ExpiredHuntRestarts(EngineTestContext ctx)
        {
            var context = await SpawnContextCreature(ctx);
            var farm = new FarmRecords();

            try
            {
                var mayorId = Guid.NewGuid().ToString();
                var city = farm.City(mayorId);
                var otherCity = farm.City(mayorId);
                var started = DateTime.UtcNow.AddHours(-2);
                var expired = new LivestockHunt { CityId = city.Id, StartedUtc = started, EndsUtc = started.AddMinutes(LivestockRules.HuntMinutes) };
                var running = new LivestockHunt { CityId = otherCity.Id, StartedUtc = DateTime.UtcNow, EndsUtc = DateTime.UtcNow.AddMinutes(10) };
                DB.Set(expired);
                DB.Set(running);

                var before = DateTime.UtcNow;
                await Run(ctx, context, () => LivestockHunts.ProcessAtServerStart());
                var after = DateTime.UtcNow;

                var restarted = DB.Get<LivestockHunt>(expired.Id);
                ctx.Assert(restarted != null, "the expired hunt is not deleted");
                ctx.Assert(restarted.EndsUtc >= before.AddMinutes(LivestockRules.HuntMinutes) && restarted.EndsUtc <= after.AddMinutes(LivestockRules.HuntMinutes),
                    $"it gets the full {LivestockRules.HuntMinutes} minutes from the processing ({restarted.EndsUtc:O})");
                ctx.AssertEqual(started, restarted.StartedUtc, "its start time is kept");
                ctx.AssertEqual(running.EndsUtc, DB.Get<LivestockHunt>(running.Id).EndsUtc, "a hunt still running keeps its time");
            }
            finally
            {
                farm.DeleteAll();
            }
        }

        // Setup

        /// <summary>
        /// Every farming action saves the character file before its database write. A headless fixture has no client whose file can
        /// be saved, so its saves are reported as made for this test, and normal saving is restored when it ends.
        /// </summary>
        private static async Task RequireCharacterSaves(EngineTestContext ctx, PlayerAbilityFixture player)
        {
            var saved = await Run(ctx, player.Creature, () => CharacterCheckpoint.Save(player.Creature));
            if (saved)
                return;

            var fixture = player.Creature;
            CharacterCheckpoint.SetSaveOverride(save => save == fixture);
            ctx.AddCleanup(() => CharacterCheckpoint.SetSaveOverride(null));
        }

        private static async Task<uint> SpawnContextCreature(EngineTestContext ctx)
        {
            var creature = ctx.SpawnCreature(ContextCreatureResref);
            await ctx.WaitFrameAsync();
            return creature;
        }

        private static void MakeFarmer(PlayerAbilityFixture player, string cityId, int agricultureRank, int cropManagement, int bountifulHarvest)
        {
            player.Update(record =>
            {
                record.Skills[SkillType.Agriculture] = new PlayerSkill { Rank = agricultureRank };
                SetPerk(record, PerkType.CropManagement, cropManagement);
                SetPerk(record, PerkType.BountifulHarvest, bountifulHarvest);
                SetPerk(record, PerkType.SeedSaver, 0);
                record.CitizenPropertyId = cityId;
            });
        }

        private static void SetPerk(Player record, PerkType perk, int level)
        {
            if (level > 0)
                record.Perks[perk] = level;
            else
                record.Perks.Remove(perk);
        }

        private static (int Rank, int XP) AgricultureProgress(string playerId)
        {
            var skill = DB.Get<Player>(playerId).Skills[SkillType.Agriculture];
            return (skill.Rank, skill.XP);
        }

        private static bool Gained((int Rank, int XP) before, (int Rank, int XP) after) =>
            after.Rank > before.Rank || (after.Rank == before.Rank && after.XP > before.XP);

        private static async Task<T> Run<T>(EngineTestContext ctx, uint creature, Func<T> action)
        {
            var result = default(T);
            await ctx.ExecuteInCreatureContextAsync(creature, () => result = action());
            return result;
        }

        private static Task Run(EngineTestContext ctx, uint creature, Action action) =>
            ctx.ExecuteInCreatureContextAsync(creature, action);

        private static Task Give(EngineTestContext ctx, PlayerAbilityFixture player, string resref, int count)
        {
            return ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
            {
                var before = FarmItems.Count(player.Creature, resref);
                for (var unit = 0; unit < count; unit++)
                    ctx.Assert(GetIsObjectValid(CreateItemOnObject(resref, player.Creature)), $"the item '{resref}' exists");
                ctx.AssertEqual(before + count, FarmItems.Count(player.Creature, resref), $"the player carries the {count} {resref} given");
            });
        }

        private static Task<int> Count(EngineTestContext ctx, PlayerAbilityFixture player, string resref) =>
            Run(ctx, player.Creature, () => FarmItems.Count(player.Creature, resref));

        private static uint FindItem(uint player, string resref)
        {
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
            {
                if (GetResRef(item) == resref)
                    return item;
            }

            return OBJECT_INVALID;
        }

        /// <summary>Places a young animal from its item through the real placement and returns its record's id.</summary>
        private static async Task<string> PlaceYoung(EngineTestContext ctx, PlayerAbilityFixture player, string corralId, LivestockDefinition definition)
        {
            var before = Corrals.GetAnimalsInCorral(corralId).Select(animal => animal.Id).ToHashSet();
            await Give(ctx, player, definition.YoungResref, 1);
            var item = await Run(ctx, player.Creature, () => FindItem(player.Creature, definition.YoungResref));
            var result = await Run(ctx, player.Creature, () => Corrals.Place(player.Creature, corralId, item));
            ctx.Assert(result.Succeeded, $"the young {definition.Name} is placed: {result.Message}");
            ctx.AssertEqual(0, await Count(ctx, player, definition.YoungResref), $"the young {definition.Name} item is used up");

            var placed = Corrals.GetAnimalsInCorral(corralId).Single(animal => !before.Contains(animal.Id));
            ctx.AssertEqual(definition.Type, placed.Species, "the corral holds the placed species");
            ctx.AssertEqual(player.Id, placed.OwnerPlayerId, "the placer owns the animal");
            ctx.AssertEqual(0, placed.Feedings, "a young animal starts unfed");
            return placed.Id;
        }

        private static async Task Feed(EngineTestContext ctx, PlayerAbilityFixture player, string animalId, CropDefinition crop, int feedings)
        {
            var carried = await Count(ctx, player, crop.HarvestResref);
            var result = await Run(ctx, player.Creature, () => Corrals.Feed(player.Creature, animalId, crop.Type));
            ctx.Assert(result.Succeeded, $"feeding {feedings} is accepted: {result.Message}");
            ctx.AssertEqual(feedings, Corrals.GetAnimal(animalId).Feedings, $"the animal has had {feedings} feedings");
            ctx.AssertEqual(carried - LivestockRules.FeedCrops, await Count(ctx, player, crop.HarvestResref), "a feeding uses 2 crops");
        }

        /// <summary>Moves every timer of the crop back, as if <paramref name="span"/> had passed.</summary>
        private static void ShiftPlot(string plotId, TimeSpan span)
        {
            var record = DB.Get<GardenPlot>(GardenPlot.KeyFor(plotId));
            record.PlantedUtc -= span;
            record.TendReadyUtc -= span;
            record.RipeUtc -= span;
            record.WitherUtc -= span;
            record.LastProcessedUtc -= span;
            DB.Set(record);
        }

        /// <summary>Moves every timer of the animal back, as if <paramref name="span"/> had passed.</summary>
        private static void ShiftAnimal(string animalId, TimeSpan span)
        {
            var animal = DB.Get<Livestock>(animalId);
            animal.PlacedUtc -= span;
            animal.LastFedUtc -= span;
            animal.LastCollectedUtc -= span;
            animal.NoProductsUntilUtc -= span;
            animal.LastProcessedUtc -= span;
            DB.Set(animal);
        }

        private static GardenPlot GrowingCrop(string plotId, string planterId, string cdKey, CropDefinition crop, DateTime plantedUtc)
        {
            var ripe = FarmingRules.GetRipeUtc(plantedUtc, crop.GrowHours);
            return new GardenPlot
            {
                Id = GardenPlot.KeyFor(plotId),
                PlotId = plotId,
                PlanterPlayerId = planterId,
                PlanterCdKey = cdKey ?? string.Empty,
                Crop = crop.Type,
                PlantedUtc = plantedUtc,
                TendReadyUtc = FarmingRules.GetTendReadyUtc(plantedUtc, crop.GrowHours),
                RipeUtc = ripe,
                WitherUtc = FarmingRules.GetWitherUtc(ripe),
                LastProcessedUtc = plantedUtc,
            };
        }

        private static GardenPlot WitheredCrop(string plotId, string planterId, string cdKey, CropDefinition crop, DateTime now) =>
            GrowingCrop(plotId, planterId, cdKey, crop, now.AddHours(-(crop.GrowHours + FarmingRules.WitherHours + 1)));

        private static void AssertClose(EngineTestContext ctx, DateTime? expected, DateTime? actual, string label)
        {
            ctx.Assert(expected.HasValue == actual.HasValue, $"{label}: expected '{expected:O}' but was '{actual:O}'.");
            if (expected.HasValue)
                ctx.Assert(Math.Abs((expected.Value - actual.Value).TotalSeconds) < 10, $"{label}: expected about '{expected:O}' but was '{actual:O}'.");
        }

        private static List<OverflowItem> OverflowOf(string playerId) =>
            Search(new DBQuery<OverflowItem>().AddFieldSearch(nameof(OverflowItem.PlayerId), playerId, false));

        private static List<T> Search<T>(DBQuery<T> query) where T : EntityBase
        {
            var count = checked((int)DB.SearchCount(query));
            return count <= 0 ? new List<T>() : DB.Search(query.AddPaging(count, 0)).ToList();
        }

        /// <summary>The records one test creates, and everything the farming services write under them, deleted at the end.</summary>
        private sealed class FarmRecords
        {
            private readonly List<string> _propertyIds = new();
            private readonly HashSet<string> _playerIds = new();

            public void TrackPlayer(string playerId) => _playerIds.Add(playerId);

            public void TrackProperty(string propertyId) => _propertyIds.Add(propertyId);

            public WorldProperty City(string mayorId) => Save(new WorldProperty
            {
                PropertyType = PropertyType.City,
                CustomName = "Engine Farming City",
                OwnerPlayerId = mayorId,
            });

            public WorldProperty House(string parentId, string ownerId) => Save(new WorldProperty
            {
                PropertyType = PropertyType.House,
                CustomName = "Engine Farming House",
                OwnerPlayerId = ownerId,
                ParentPropertyId = parentId,
            });

            public WorldProperty Structure(StructureType type, string parentId) => Save(new WorldProperty
            {
                PropertyType = PropertyType.Structure,
                StructureType = type,
                CustomName = $"Engine Farming {type}",
                ParentPropertyId = parentId,
            });

            /// <summary>Grants Tend Gardens on the property, the permission that plants, tends and uses a vat there.</summary>
            public void GrantGardens(string propertyId, string playerId)
            {
                DB.Set(new WorldPropertyPermission
                {
                    PropertyId = propertyId,
                    PlayerId = playerId,
                    Permissions = new Dictionary<PropertyPermissionType, bool> { [PropertyPermissionType.TendGardens] = true },
                });
                _playerIds.Add(playerId);
            }

            private WorldProperty Save(WorldProperty property)
            {
                DB.Set(property);
                _propertyIds.Add(property.Id);
                return property;
            }

            public void DeleteAll()
            {
                foreach (var id in _propertyIds)
                {
                    DeleteIfExists<GardenPlot>(GardenPlot.KeyFor(id));
                    var farmhand = DB.Get<Farmhand>(Farmhand.KeyFor(id));
                    if (farmhand != null)
                    {
                        if (!string.IsNullOrEmpty(farmhand.DroidAssignmentId))
                        {
                            DeleteIfExists<StationDroidReceipt>(farmhand.DroidAssignmentId);
                            DeleteIfExists<OverflowItem>("farmhand-droid:" + farmhand.DroidAssignmentId);
                        }

                        DB.Delete<Farmhand>(farmhand.Id);
                    }

                    DeleteIfExists<FarmhandCatchUp>(FarmhandCatchUp.KeyFor(id));
                    DeleteIfExists<FermentationJob>(FermentationJob.KeyFor(id));
                    DeleteIfExists<FermentationVat>(FermentationVat.KeyFor(id));
                    DeleteWhere<Livestock>(nameof(Livestock.CorralId), id);
                    foreach (var hunt in Search(new DBQuery<LivestockHunt>().AddFieldSearch(nameof(LivestockHunt.CityId), id, false)))
                        LivestockHunts.Cancel(hunt);
                    DeleteWhere<WorldPropertyPermission>(nameof(WorldPropertyPermission.PropertyId), id);
                    DeleteIfExists<WorldProperty>(id);
                }

                foreach (var id in _playerIds)
                {
                    DeleteWhere<GardenPlot>(nameof(GardenPlot.PlanterPlayerId), id);
                    DeleteWhere<Livestock>(nameof(Livestock.OwnerPlayerId), id);
                    DeleteWhere<FermentationJob>(nameof(FermentationJob.OwnerPlayerId), id);
                    DeleteWhere<OverflowItem>(nameof(OverflowItem.PlayerId), id);
                    DeleteWhere<TransferEntry>(nameof(TransferEntry.PlayerId), id);
                    DeleteWhere<LivestockNotice>(nameof(LivestockNotice.PlayerId), id);
                    DeleteWhere<StationDroidReceipt>(nameof(StationDroidReceipt.OwnerPlayerId), id);
                    DeleteWhere<WorldPropertyPermission>(nameof(WorldPropertyPermission.PlayerId), id);
                }
            }

            private static void DeleteIfExists<T>(string id) where T : EntityBase
            {
                if (!string.IsNullOrWhiteSpace(id) && DB.Exists<T>(id))
                    DB.Delete<T>(id);
            }

            private static void DeleteWhere<T>(string field, string value) where T : EntityBase
            {
                if (string.IsNullOrWhiteSpace(value))
                    return;

                foreach (var record in Search(new DBQuery<T>().AddFieldSearch(field, value, false)))
                    DB.Delete<T>(record.Id);
            }
        }
    }
}
