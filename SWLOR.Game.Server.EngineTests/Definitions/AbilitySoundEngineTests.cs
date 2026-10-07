using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Verifies abilities that declare an impact sound actually reach the sound call when
    /// activated through the real UsePerkFeat.TryUseAbility pipeline.
    /// </summary>
    public static class AbilitySoundEngineTests
    {
        [EngineTest("Force Push I plays its declared impact sound", Category = "AbilitySound", TimeoutSeconds = 40f)]
        public static async Task ForcePushPlaysImpactSound(EngineTestContext ctx)
        {
            await AssertAbilityPlaysSound(ctx, FeatType.ForcePush1, PerkType.ForcePush, "ksfx_frc_push");
        }

        [EngineTest("Force Leap I plays its declared impact sound", Category = "AbilitySound", TimeoutSeconds = 40f)]
        public static async Task ForceLeapPlaysImpactSound(EngineTestContext ctx)
        {
            await AssertAbilityPlaysSound(ctx, FeatType.ForceLeap1, PerkType.ForceLeap, "ksfx_frc_speed");
        }

        private static async Task AssertAbilityPlaysSound(
            EngineTestContext ctx,
            FeatType feat,
            PerkType perk,
            string expectedSound)
        {
            var caster = ctx.SpawnCreature("nw_bandit001", -0.5f, 0f);
            var target = ctx.SpawnCreature("nw_rat001", 4f, 0f);
            ctx.MakeHostile(target);
            await ctx.WaitFrameAsync();
            ctx.SetNPCResources(caster, 9999, 9999);
            ctx.SetNPCPerkLevel(caster, perk, 1);

            var used = false;
            var attempted = false;
            AssignCommand(caster, () =>
            {
                used = UsePerkFeat.TryUseAbility(caster, target, feat, GetLocation(target));
                attempted = true;
            });
            await ctx.WaitUntilAsync(() => attempted, 5f, "the assigned activation command to execute");
            ctx.Assert(used, $"TryUseAbility should start {feat}.");

            await ctx.WaitUntilAsync(
                () => GetLocalString(caster, UsePerkFeat.LastAbilitySoundName) != string.Empty,
                15f,
                $"{feat} to record its impact sound");
            ctx.AssertEqual(expectedSound, GetLocalString(caster, UsePerkFeat.LastAbilitySoundName), $"{feat} sound");
        }
    }
}
