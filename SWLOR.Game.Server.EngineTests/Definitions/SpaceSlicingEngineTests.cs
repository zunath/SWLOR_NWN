using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Service.SlicingService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Slicing in space (plan section 24) in the engine: a data site reveals its data cans, which the slicing rank gate
    /// guards and whose completion spawns the site cache; a locked cache refuses to be used and opens when sliced.
    /// Headless players stand in for the ships.
    /// </summary>
    public static class SpaceSlicingEngineTests
    {
        private const string ShipTag = "sd_striker";

        [EngineTest("A data site's cans need the Slicing rank and spawn the site cache once sliced", Category = "SpaceSlicing", TimeoutSeconds = 30f)]
        public static async Task DataSiteCansGateOnRankAndLeadToTheCache(EngineTestContext ctx)
        {
            using var scanner = await PlayerAbilityFixture.CreateAsync(ctx);
            var shipId = "engine-slice-ship/" + scanner.Id;
            scanner.Update(p => p.ActiveShipId = shipId);
            var status = new ShipStatus { ItemTag = ShipTag, Size = ShipSize.Small, MaxHull = 100, MaxShield = 100 };
            DB.Set(new PlayerShip { Id = shipId, OwnerPlayerId = scanner.Id, Status = status });

            var area = GetArea(scanner.Creature);
            var position = GetPositionFromLocation(ctx.GetArenaLocation(6f));
            var signature = SignatureSiteRules.Create("engine-slice/" + scanner.Id, GetResRef(area), 1, SignatureType.DataSite,
                position.X, position.Y, position.Z, System.DateTime.UtcNow);
            Space.PlaceSignature(signature);
            string registryKey = null;
            var hadRegistered = false;
            uint registered = OBJECT_INVALID;

            List<uint> Find(string resref)
            {
                var found = new List<uint>();
                for (var o = GetFirstObjectInArea(area); GetIsObjectValid(o); o = GetNextObjectInArea(area))
                {
                    if (GetResRef(o) == resref)
                        found.Add(o);
                }

                return found;
            }

            try
            {
                var strong = RevampModuleCatalog.Steps.First(x => x.Family == ShipModuleFamily.ProbeLauncher && x.Rank == 45);
                for (var cycle = 0; cycle < 6 && !signature.IsRevealed; cycle++)
                    await ctx.ExecuteInCreatureContextAsync(scanner.Creature, () => Space.RunProbeCycle(scanner.Creature, status, strong));

                ctx.Assert(signature.IsRevealed, "the data site resolves at 100%");
                var cans = Find(SpaceSlicingTuning.DataCanResref);
                ctx.AssertEqual(SpaceSlicingTuning.DataSiteCans, cans.Count, "the data site holds its data cans");
                ctx.AssertEqual(1, GetLocalInt(cans[0], SlicingSession.TierVariable), "a tier 1 site's cans are tier 1");

                var error = SlicingSession.ValidateStart(scanner.Creature, cans[0], SlicingSourceType.DataCan, 1);
                ctx.Assert(!string.IsNullOrEmpty(error), "a slicer without Slicing 1 is refused");

                var gold = GetGold(scanner.Creature);
                foreach (var can in cans)
                    Space.CompleteSlicing(scanner.Creature, can, SlicingSourceType.DataCan, 1);
                ctx.Assert(GetGold(scanner.Creature) > gold, "each can pays its credits");

                await ctx.WaitUntilAsync(() => Find(SpaceSlicingTuning.DataCanResref).Count == 0, 5, "sliced cans are removed");
                // The periodic site check only runs over the configured scan areas, not this arena, so the test runs it, with the
                // arena standing in for its area resref in the area registry (the check spawns the cache in the registered area).
                registryKey = GetResRef(area);
                hadRegistered = Area.GetAreas().TryGetValue(registryKey, out registered);
                Area.GetAreas()[registryKey] = area;
                var updateSite = typeof(Space).GetMethod("UpdateSite", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                ctx.Assert(updateSite != null, "Space.UpdateSite exists");
                await ctx.WaitUntilAsync(() =>
                {
                    updateSite.Invoke(null, new object[] { signature });
                    return Find(ScanningTuning.CacheResref).Count == 1;
                }, 15, "the site cache appears after the cans");
            }
            finally
            {
                if (registryKey != null)
                {
                    if (hadRegistered)
                        Area.GetAreas()[registryKey] = registered;
                    else
                        Area.GetAreas().Remove(registryKey);
                }

                Space.RemoveSignature(signature);
                foreach (var leftover in Find(SpaceSlicingTuning.DataCanResref).Concat(Find(ScanningTuning.CacheResref)))
                    DestroyObject(leftover);
                DB.Delete<PlayerShip>(shipId);
            }
        }

        [EngineTest("A locked cache refuses to be used and opens when sliced", Category = "SpaceSlicing", TimeoutSeconds = 30f)]
        public static async Task LockedCacheOpensWhenSliced(EngineTestContext ctx)
        {
            using var scanner = await PlayerAbilityFixture.CreateAsync(ctx);
            var shipId = "engine-slice-lock/" + scanner.Id;
            scanner.Update(p => p.ActiveShipId = shipId);
            var status = new ShipStatus { ItemTag = ShipTag, Size = ShipSize.Small, MaxHull = 100, MaxShield = 100 };
            DB.Set(new PlayerShip { Id = shipId, OwnerPlayerId = scanner.Id, Status = status });

            var area = GetArea(scanner.Creature);
            var position = GetPositionFromLocation(ctx.GetArenaLocation(6f));
            var signature = SignatureSiteRules.Create("engine-slice-lock/" + scanner.Id, GetResRef(area), 1, SignatureType.CacheSite,
                position.X, position.Y, position.Z, System.DateTime.UtcNow, true);
            Space.PlaceSignature(signature);

            uint FindCache()
            {
                for (var o = GetFirstObjectInArea(area); GetIsObjectValid(o); o = GetNextObjectInArea(area))
                {
                    if (GetResRef(o) == ScanningTuning.CacheResref)
                        return o;
                }

                return OBJECT_INVALID;
            }

            try
            {
                var strong = RevampModuleCatalog.Steps.First(x => x.Family == ShipModuleFamily.ProbeLauncher && x.Rank == 45);
                for (var cycle = 0; cycle < 6 && !signature.IsRevealed; cycle++)
                    await ctx.ExecuteInCreatureContextAsync(scanner.Creature, () => Space.RunProbeCycle(scanner.Creature, status, strong));

                var cache = FindCache();
                ctx.Assert(GetIsObjectValid(cache), "the locked cache spawns");
                ctx.AssertEqual("Locked Cache", GetName(cache), "it is named a locked cache");

                Space.OpenSiteCache(cache, scanner.Creature);
                ctx.Assert(!signature.Completed && GetIsObjectValid(cache), "using a locked cache does not open it");

                var gold = GetGold(scanner.Creature);
                Space.CompleteSlicing(scanner.Creature, cache, SlicingSourceType.LockedCache, 1);
                ctx.Assert(signature.Completed, "slicing it open completes the site");
                ctx.Assert(GetGold(scanner.Creature) >= gold + SignatureSiteRules.LockedCacheCredits(1), "it pays the locked cache's credits");
                await ctx.WaitUntilAsync(() => !GetIsObjectValid(cache), 5, "the opened cache is removed");
            }
            finally
            {
                Space.RemoveSignature(signature);
                var leftover = FindCache();
                if (GetIsObjectValid(leftover))
                    DestroyObject(leftover);
                DB.Delete<PlayerShip>(shipId);
            }
        }
    }
}
