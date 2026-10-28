using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Scanning and exploration sites (plan section 24) in the engine: a hidden cache signature leaves nothing in the
    /// area until a Probe Launcher cycle takes it to 100%, then its cache spawns, refuses a ship outside the scanning
    /// party and pays the scanner. Headless players stand in for the ships.
    /// </summary>
    public static class ScanningEngineTests
    {
        private const string ShipTag = "sd_striker";

        [EngineTest("A cache signature stays hidden until 100%, then its cache pays only the scanning party", Category = "Scanning", TimeoutSeconds = 30f)]
        public static async Task CacheSignatureOpensForTheScanningParty(EngineTestContext ctx)
        {
            using var scanner = await PlayerAbilityFixture.CreateAsync(ctx);
            using var outsider = await PlayerAbilityFixture.CreateAsync(ctx, 2f);

            var shipId = "engine-scan-ship/" + scanner.Id;
            var outsiderShipId = "engine-scan-outsider/" + outsider.Id;
            scanner.Update(p => p.ActiveShipId = shipId);
            outsider.Update(p => p.ActiveShipId = outsiderShipId);
            var status = new ShipStatus { ItemTag = ShipTag, Size = ShipSize.Small, MaxHull = 100, MaxShield = 100 };
            DB.Set(new PlayerShip { Id = shipId, OwnerPlayerId = scanner.Id, Status = status });
            DB.Set(new PlayerShip { Id = outsiderShipId, OwnerPlayerId = outsider.Id, Status = new ShipStatus { ItemTag = ShipTag, Size = ShipSize.Small } });

            var area = GetArea(scanner.Creature);
            var position = GetPositionFromLocation(ctx.GetArenaLocation(6f));
            var signature = SignatureSiteRules.Create("engine-scan/" + scanner.Id, GetResRef(area), 1, SignatureType.CacheSite,
                position.X, position.Y, position.Z, System.DateTime.UtcNow);
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
                var weak = RevampModuleCatalog.Steps.First(x => x.Family == ShipModuleFamily.ProbeLauncher && x.Rank == 0);
                await ctx.ExecuteInCreatureContextAsync(scanner.Creature, () => Space.RunProbeCycle(scanner.Creature, status, weak));
                ctx.Assert(ScanningRules.Percent(signature, scanner.Id) > 0 && ScanningRules.Percent(signature, scanner.Id) < 100,
                    "one Mk I cycle gathers part of the signal");
                ctx.Assert(!signature.IsRevealed && !GetIsObjectValid(FindCache()), "nothing exists in the area before 100%");

                var strong = RevampModuleCatalog.Steps.First(x => x.Family == ShipModuleFamily.ProbeLauncher && x.Rank == 45);
                for (var cycle = 0; cycle < 5 && !signature.IsRevealed; cycle++)
                    await ctx.ExecuteInCreatureContextAsync(scanner.Creature, () => Space.RunProbeCycle(scanner.Creature, status, strong));

                ctx.Assert(signature.IsRevealed, "the signature resolves at 100%");
                var cache = FindCache();
                ctx.Assert(GetIsObjectValid(cache), "the site cache spawns");

                Space.OpenSiteCache(cache, outsider.Creature);
                ctx.Assert(!signature.Completed && GetIsObjectValid(cache), "a ship outside the scanning party is refused");

                var gold = GetGold(scanner.Creature);
                Space.OpenSiteCache(cache, scanner.Creature);
                ctx.Assert(signature.Completed, "the scanner completes the site");
                ctx.AssertEqual(gold + SignatureSiteRules.SiteCredits(SignatureType.CacheSite, 1), GetGold(scanner.Creature), "the cache pays its credits");
                await ctx.WaitUntilAsync(() => !GetIsObjectValid(cache), 5, "the opened cache is removed");
            }
            finally
            {
                Space.RemoveSignature(signature);
                var leftover = FindCache();
                if (GetIsObjectValid(leftover))
                    DestroyObject(leftover);
                DB.Delete<PlayerShip>(shipId);
                DB.Delete<PlayerShip>(outsiderShipId);
            }
        }
    }
}
