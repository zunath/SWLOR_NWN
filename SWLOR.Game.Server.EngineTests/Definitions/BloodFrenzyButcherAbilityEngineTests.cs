using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// The Blood Frenzy Butcher uses its whole kit in a fight. Reported in the Viscara Sewers
    /// Depths: the Butcher never used Rending Carve. The engine dropped Rending Carve, Stim
    /// Canister and Blood Frenzy Flurry from the spawned creature because their feat.2da rows
    /// had no name strref, so only Brutal Bash was left.
    /// </summary>
    public static class BloodFrenzyButcherAbilityEngineTests
    {
        private const string ButcherResref = "bf_butcher";

        private static readonly FeatType[] Kit =
        {
            FeatType.BloodFrenzyFlurry,
            FeatType.StimCanister,
            FeatType.BrutalBash,
            FeatType.RendingCarve,
        };

        [EngineTest("The Blood Frenzy Butcher uses Rending Carve and the rest of its kit on a lone player",
            Category = "BloodFrenzyButcherAbility", TimeoutSeconds = 120f)]
        public static async Task Butcher_UsesItsWholeKitOnALonePlayer(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx);
            // Both stay alive for the whole fight.
            SetPlotFlag(player.Creature, true);
            var butcher = ctx.SpawnCreature(ButcherResref, 2f);
            SetAILevel(butcher, AILevel.High);
            SetIsTemporaryEnemy(player.Creature, butcher);
            await ctx.WaitFrameAsync();
            SetPlotFlag(butcher, true);

            foreach (var feat in Kit)
                ctx.Assert(GetHasFeat(feat, butcher), $"the spawned Butcher knows {feat}");

            Enmity.ModifyEnmity(player.Creature, butcher, 10);
            AssignCommand(butcher, () => ActionAttack(player.Creature));

            var used = new HashSet<FeatType>();
            var started = DateTime.UtcNow;
            await ctx.WaitUntilAsync(() =>
            {
                foreach (var feat in Kit.Where(feat => !used.Contains(feat) && IsOnRecast(butcher, feat)))
                {
                    used.Add(feat);
                    ctx.Log($"{(DateTime.UtcNow - started).TotalSeconds:0.0}s: the Butcher used {feat}.");
                }

                return used.Count == Kit.Length;
            }, 90f, "the Butcher to use Rending Carve, Stim Canister, Blood Frenzy Flurry and Brutal Bash");
        }

        private static bool IsOnRecast(uint creature, FeatType feat)
        {
            return Recast.IsOnRecastDelay(creature, Ability.GetAbilityDetail(feat).RecastGroup).Item1;
        }
    }
}
