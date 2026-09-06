using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class FinalLineEngineTests
    {
        [EngineTest("Final Line scales each line target and refreshes health on later casts", Category = "FinalLine", TimeoutSeconds = 60f)]
        public static async Task DamageScalesPerTarget(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001");
            var targets = new[] { ctx.SpawnCreature("nw_rat001", 2f), ctx.SpawnCreature("nw_rat001", 4f) };
            await ctx.WaitFrameAsync();
            ObjectPlugin.SetPosition(caster, GetPositionFromLocation(ctx.GetArenaLocation(-4f)));
            for (var i = 0; i < targets.Length; i++)
            {
                ObjectPlugin.SetPosition(targets[i], GetPositionFromLocation(ctx.GetArenaLocation(-2f + 2f * i)));
                ctx.MakeHostile(targets[i]);
                ctx.SuppressNPCNaturalRegen(targets[i]);
                Stat.SetNPCMaxHitPoints(targets[i], 30000);
                ctx.AssertEqual(30000, GetMaxHitPoints(targets[i]), "target HP budget");
            }
            foreach (var creature in targets.Append(caster))
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), creature, 120f);

            var ability = new FinalLineTechniqueAbilityDefinition().BuildAbilities()[FeatType.FinalLineTechnique];
            // Compare the actual technique against itself with only its bonus disabled. Identical
            // RNG seeds preserve the ordinary damage/critical rolls in both executions.
            var closure = ability.ImpactAction.Target;
            var field = closure.GetType().GetField("damagePercentAdjustment", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            ctx.Assert(field != null, "Final Line must forward its per-target bonus to the area impact");
            var ramp = (Func<uint, int>)field.GetValue(closure);
            ctx.Assert(ramp != null, "Final Line must have a damage bonus");
            ctx.AssertEqual(0, ramp(OBJECT_INVALID), "invalid target bonus");
            Combat.SetAbilityHitResolutionOverride(true);
            Combat.SetAutoAttackHitResolutionOverride(false);
            try
            {
                foreach (var percent in new[] { 100, 80, 75, 60, 50, 40, 25, 20, 10, 1, 100 })
                {
                    var startingHp = new[] { 300 * percent, 300 * (101 - percent) };
                    var baseline = new int[2];
                    for (var pass = 0; pass < 2; pass++)
                    {
                        for (var i = 0; i < targets.Length; i++)
                        {
                            StatusEffect.RemoveStatusEffect<ExposedStatusEffect>(targets[i]);
                            ObjectPlugin.SetCurrentHitPoints(targets[i], startingHp[i]);
                            ctx.AssertEqual(35 * (30000 - startingHp[i]) / 30000, ramp(targets[i]), "live HP bonus");
                        }
                        field.SetValue(closure, pass == 0 ? null : ramp);
                        ctx.SeedRandom(49123);
                        Ability.BeginAbilityImpact(caster, ability);
                        try
                        {
                            await ctx.ExecuteInCreatureContextAsync(caster, () =>
                                ability.ImpactAction(caster, targets[0], 1, GetLocation(targets[1])));
                        }
                        finally
                        {
                            ctx.AssertEqual(2, Ability.EndAbilityImpact(caster).ImpactedTargetCount, "both line targets hit");
                        }
                        await ctx.WaitFrameAsync();
                        for (var i = 0; i < targets.Length; i++)
                        {
                            var damage = startingHp[i] - GetCurrentHitPoints(targets[i]);
                            ctx.Assert(damage > 0, "Final Line must deal damage");
                            ctx.Assert(StatusEffect.HasStatusEffect<ExposedStatusEffect>(targets[i]), "Final Line must apply Exposed");
                            if (pass == 0)
                                baseline[i] = damage;
                            else
                            {
                                var bonus = 35 * (30000 - startingHp[i]) / 30000;
                                var expected = baseline[i] + (int)Math.Ceiling(baseline[i] * (bonus / 100f));
                                ctx.AssertEqual(expected, damage, $"target {i}, HP {startingHp[i]}/30000, bonus {bonus}%");
                                ctx.Log($"HP {startingHp[i]}/30000: baseline {baseline[i]}, bonus {bonus}%, damage {damage}");
                            }
                        }
                    }
                }
            }
            finally
            {
                field.SetValue(closure, ramp);
                Combat.SetAbilityHitResolutionOverride(null);
                Combat.SetAutoAttackHitResolutionOverride(null);
            }
        }
    }
}
