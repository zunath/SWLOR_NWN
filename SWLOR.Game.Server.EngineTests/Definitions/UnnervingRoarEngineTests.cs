using System;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// The Great Arkanian Dragon's Unnerving Roar, through the real UsePerkFeat.TryUseAbility pipeline.
    /// </summary>
    public static class UnnervingRoarEngineTests
    {
        [EngineTest("Unnerving Roar unnerves targets every hit would miss and cuts their Evasion by 30%", Category = "UnnervingRoar", TimeoutSeconds = 30f)]
        public static async Task UnnervingRoar_LandsWithoutAHitRoll(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_rat001");
            var target = ctx.SpawnCreature("nw_rat001", 3f);
            SetAILevel(caster, AILevel.VeryLow);
            SetAILevel(target, AILevel.VeryLow);
            ctx.MakeHostile(target);

            // Spawn scripts reset the FP and STM locals a frame after creation.
            await ctx.WaitFrameAsync();
            ctx.SetNPCResources(caster, 50, 50);

            // A rat's own Evasion is too low to tell a 30% cut from a smaller one.
            TemporaryStatModifier.Add(target, StatType.Evasion, 200, 60f);
            var evasionBefore = Stat.GetEvasion(target, SkillType.Invalid);
            ctx.Assert(evasionBefore >= 150, $"The target starts with high Evasion: {evasionBefore}");

            // Every hit roll misses, so only an impact without one can apply the debuff.
            Combat.SetAbilityHitResolutionOverride(false);
            try
            {
                var attempted = false;
                var used = false;
                var denial = string.Empty;
                AssignCommand(caster, () =>
                {
                    used = UsePerkFeat.TryUseAbility(caster, target, FeatType.UnnervingRoar, GetLocation(caster));
                    if (!used)
                        denial = Ability.GetLastActivationDenialReason();
                    attempted = true;
                });
                await ctx.WaitUntilAsync(() => attempted, 3f, "Unnerving Roar to be attempted in the caster's script context");
                ctx.Assert(used, $"Unnerving Roar was refused: {denial}");

                await ctx.WaitUntilAsync(() => StatusEffect.HasStatusEffect(target, typeof(UnnervedStatusEffect)), 6f,
                    "Unnerved to land after the roar's 2-second activation");
            }
            finally
            {
                Combat.SetAbilityHitResolutionOverride(null);
            }

            var evasionAfter = Stat.GetEvasion(target, SkillType.Invalid);
            ctx.Log($"Target Evasion {evasionBefore} before, {evasionAfter} while Unnerved.");
            ctx.AssertEqual(-30, Stat.GetStatAdjustment(target, StatType.EvasionPercentAdjustment),
                "Unnerved is the target's only Evasion percent adjustment");
            var expected = (int)Math.Round(evasionBefore * 0.7, MidpointRounding.AwayFromZero);
            ctx.Assert(Math.Abs(evasionAfter - expected) <= 1,
                $"Unnerved should cut Evasion by 30%: {evasionBefore} became {evasionAfter}, expected about {expected}");
        }
    }
}
