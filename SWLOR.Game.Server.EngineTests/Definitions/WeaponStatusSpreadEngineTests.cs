using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber;
using SWLOR.Game.Server.Feature.AbilityDefinition.TwinBlade;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class WeaponStatusSpreadEngineTests
    {
        [EngineTest("Sundering Sweep spreads once when multiple targets have Sunder", Category = "StatusEffect", TimeoutSeconds = 30f)]
        public static Task SunderingSweepSpreadsOncePerCast(EngineTestContext ctx)
            => VerifySpreadCount(ctx, new SunderingSweepAbilityDefinition().BuildAbilities(),
                new[] { FeatType.SunderingSweep1, FeatType.SunderingSweep2, FeatType.SunderingSweep3 },
                typeof(SunderStatusEffect), 1, 2);

        [EngineTest("Serrated Arc spreads from each initially bleeding target", Category = "StatusEffect", TimeoutSeconds = 30f)]
        public static Task SerratedArcSpreadsFromEachSource(EngineTestContext ctx)
            => VerifySpreadCount(ctx, new SerratedArcAbilityDefinition().BuildAbilities(),
                new[] { FeatType.SerratedArc1, FeatType.SerratedArc2, FeatType.SerratedArc3 },
                typeof(BleedStatusEffect), 2, 4);

        private static async Task VerifySpreadCount(EngineTestContext ctx,
            Dictionary<FeatType, AbilityDetail> abilities, FeatType[] feats, Type statusEffect,
            int expectedRecipients, int expectedImpacts)
        {
            var caster = ctx.SpawnCreature("nw_bandit001");
            var sources = new[] { ctx.SpawnCreature("nw_rat001", -2f), ctx.SpawnCreature("nw_rat001", 2f) };
            var recipients = new[] { ctx.SpawnCreature("nw_rat001", -3.5f), ctx.SpawnCreature("nw_rat001", 3.5f) };
            var enemies = sources.Concat(recipients).ToArray();
            await ctx.WaitFrameAsync();
            SetCommandable(false, caster);
            foreach (var enemy in enemies)
            {
                SetCommandable(false, enemy);
                ctx.SuppressNPCNaturalRegen(enemy);
                ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(1000), enemy, 120f);
                ctx.MakeHostile(enemy);
            }

            Combat.SetAbilityHitResolutionOverride(true);
            Combat.SetAutoAttackHitResolutionOverride(false);
            try
            {
                for (var rank = 1; rank <= feats.Length; rank++)
                {
                    foreach (var enemy in enemies)
                        StatusEffect.RemoveStatusEffect(enemy, statusEffect);
                    foreach (var source in sources)
                        ctx.Assert(StatusEffect.ApplyStatusEffect(caster, source, statusEffect, 60f), "setup status must apply");
                    var originalEffects = sources.Select(source => StatusEffect.GetStatusEffect(source, statusEffect)).ToArray();

                    var feat = feats[rank - 1];
                    var ability = abilities[feat];
                    var impactedTargetCount = 0;
                    Ability.BeginAbilityImpact(caster, ability);
                    try
                    {
                        ability.ImpactAction(caster, sources[0], rank, GetLocation(sources[0]));
                    }
                    finally
                    {
                        impactedTargetCount = Ability.EndAbilityImpact(caster).ImpactedTargetCount;
                    }

                    ctx.AssertEqual(expectedImpacts, impactedTargetCount, $"{feat}: primary area targets");
                    ctx.AssertEqual(expectedRecipients, recipients.Count(target => StatusEffect.HasStatusEffect(target, statusEffect)),
                        $"{feat}: additional recipients across all initially affected targets");
                    for (var index = 0; index < sources.Length; index++)
                        ctx.Assert(StatusEffect.GetStatusEffect(sources[index], statusEffect) == originalEffects[index],
                            $"{feat}: spreading must preserve both source effects");
                }
            }
            finally
            {
                Combat.SetAbilityHitResolutionOverride(null);
                Combat.SetAutoAttackHitResolutionOverride(null);
            }
        }
    }
}
