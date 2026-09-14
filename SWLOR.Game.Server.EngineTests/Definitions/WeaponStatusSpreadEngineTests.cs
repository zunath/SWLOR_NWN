using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber;
using SWLOR.Game.Server.Feature.AbilityDefinition.TwinBlade;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
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

        [EngineTest("Sundering Sweep cannot spread Sunder procced by its own hit", Category = "StatusEffect", TimeoutSeconds = 30f)]
        public static Task SunderingSweepRequiresPreexistingSunder(EngineTestContext ctx)
            => VerifySpreadCount(ctx, new SunderingSweepAbilityDefinition().BuildAbilities(),
                new[] { FeatType.SunderingSweep1, FeatType.SunderingSweep2, FeatType.SunderingSweep3 },
                typeof(SunderStatusEffect), 0, 2, StatType.DamageDealtSunderChance);

        [EngineTest("Serrated Arc cannot spread Bleed procced by its own hit", Category = "StatusEffect", TimeoutSeconds = 30f)]
        public static Task SerratedArcRequiresPreexistingBleed(EngineTestContext ctx)
            => VerifySpreadCount(ctx, new SerratedArcAbilityDefinition().BuildAbilities(),
                new[] { FeatType.SerratedArc1, FeatType.SerratedArc2, FeatType.SerratedArc3 },
                typeof(BleedStatusEffect), 0, 2, StatType.DamageDealtBleedChance);

        private static async Task VerifySpreadCount(EngineTestContext ctx,
            Dictionary<FeatType, AbilityDetail> abilities, FeatType[] feats, Type statusEffect,
            int expectedRecipients, int expectedImpacts, StatType procStat = StatType.Invalid)
        {
            var testsHitProc = procStat != StatType.Invalid;
            var sourceOffset = testsHitProc ? 1.5f : 1f;
            var recipientOffset = testsHitProc ? 6f : 3.5f;
            var caster = ctx.SpawnCreature("nw_bandit001");
            var sources = new[] { ctx.SpawnCreature("nw_rat001", -sourceOffset), ctx.SpawnCreature("nw_rat001", sourceOffset) };
            var recipients = new[] { ctx.SpawnCreature("nw_rat001", -recipientOffset), ctx.SpawnCreature("nw_rat001", recipientOffset) };
            var enemies = sources.Concat(recipients).ToArray();
            await ctx.WaitFrameAsync();
            SWLOR.NWN.API.NWNX.ObjectPlugin.SetPosition(caster, GetPositionFromLocation(ctx.GetArenaLocation()));
            for (var index = 0; index < 2; index++)
            {
                var direction = index == 0 ? -1 : 1;
                SWLOR.NWN.API.NWNX.ObjectPlugin.SetPosition(sources[index], GetPositionFromLocation(ctx.GetArenaLocation(direction * sourceOffset)));
                SWLOR.NWN.API.NWNX.ObjectPlugin.SetPosition(recipients[index], GetPositionFromLocation(ctx.GetArenaLocation(direction * recipientOffset)));
            }
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), caster, 120f);
            if (testsHitProc)
                TemporaryStatModifier.Add(caster, procStat, 100, 120f);
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
                    if (!testsHitProc)
                    {
                        foreach (var source in sources)
                            ctx.Assert(StatusEffect.ApplyStatusEffect(caster, source, statusEffect, 60f), "setup status must apply");
                        ctx.Assert(GetDistanceBetween(sources[0], sources[1]) < GetDistanceBetween(sources[0], recipients[0]),
                            "the nearest neighbor must already carry the status, ahead of the clean recipient");
                    }
                    var originalEffects = sources.Select(source => StatusEffect.GetStatusEffect(source, statusEffect)).ToArray();

                    var feat = feats[rank - 1];
                    var ability = abilities[feat];
                    var impactedTargetCount = 0;
                    Ability.BeginAbilityImpact(caster, ability);
                    try
                    {
                        await ctx.ExecuteInCreatureContextAsync(caster, () => ability.ImpactAction(caster, sources[0], rank, GetLocation(sources[0])));
                    }
                    finally
                    {
                        impactedTargetCount = Ability.EndAbilityImpact(caster).ImpactedTargetCount;
                    }

                    ctx.AssertEqual(expectedImpacts, impactedTargetCount, $"{feat}: primary area targets");
                    ctx.AssertEqual(expectedRecipients, recipients.Count(target => StatusEffect.HasStatusEffect(target, statusEffect)),
                        $"{feat}: additional recipients across all initially affected targets");
                    for (var index = 0; index < sources.Length; index++)
                    {
                        if (testsHitProc)
                            ctx.Assert(StatusEffect.HasStatusEffect(sources[index], statusEffect),
                                $"{feat}: the hit must proc its status without spreading it");
                        else
                            ctx.Assert(StatusEffect.GetStatusEffect(sources[index], statusEffect) == originalEffects[index],
                                $"{feat}: spreading must preserve both source effects");
                    }
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
