using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.TwinBlade;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class SerratedArcEngineTests
    {
        [EngineTest("Every Serrated Arc rank spreads Bleed to one other enemy", Category = "StatusEffect", TimeoutSeconds = 30f)]
        public static Task BleedSpreadsToOneAdditionalEnemy(EngineTestContext ctx)
            => VerifyBleedSpread(ctx, false);

        [EngineTest("Serrated Arc does not chain Bleed through later area targets", Category = "StatusEffect", TimeoutSeconds = 30f)]
        public static Task BleedDoesNotChainWithinAreaImpact(EngineTestContext ctx)
            => VerifyBleedSpread(ctx, true);

        private static async Task VerifyBleedSpread(EngineTestContext ctx, bool recipientInsideArea)
        {
            var caster = ctx.SpawnCreature("nw_bandit001");
            var target = ctx.SpawnCreature("nw_rat001", recipientInsideArea ? 1f : 2f);
            var nearby = ctx.SpawnCreature("nw_rat001", recipientInsideArea ? 4f : 6f);
            var otherNearby = ctx.SpawnCreature("nw_rat001", 6.5f);
            var outsideRange = ctx.SpawnCreature("nw_rat001", 8f);
            var enemies = new[] { target, nearby, otherNearby, outsideRange };
            await ctx.WaitFrameAsync();
            // Keep the primary area inside the entry room. Native spawning nudges crowded
            // creatures across the doorway, which otherwise changes the intended geometry.
            const float originOffset = -4.25f;
            ObjectPlugin.SetPosition(caster, GetPositionFromLocation(ctx.GetArenaLocation(originOffset)));
            ObjectPlugin.SetPosition(target, GetPositionFromLocation(ctx.GetArenaLocation(originOffset + (recipientInsideArea ? 1f : 2f))));
            ObjectPlugin.SetPosition(nearby, GetPositionFromLocation(ctx.GetArenaLocation(originOffset + (recipientInsideArea ? 4f : 6f))));
            ObjectPlugin.SetPosition(otherNearby, GetPositionFromLocation(ctx.GetArenaLocation(originOffset + 6.5f)));
            ObjectPlugin.SetPosition(outsideRange, GetPositionFromLocation(ctx.GetArenaLocation(originOffset + 8f)));
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), caster, 120f);
            ctx.SetNPCPerkLevel(caster, PerkType.ShrapnelCasing, 3);

            foreach (var enemy in enemies)
            {
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), enemy, 120f);
                ctx.SuppressNPCNaturalRegen(enemy);
                ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(1000), enemy, 120f);
                ctx.MakeHostile(enemy);
            }

            var abilities = new SerratedArcAbilityDefinition().BuildAbilities();
            Combat.SetAbilityHitResolutionOverride(true);
            Combat.SetAutoAttackHitResolutionOverride(false);
            try
            {
                foreach (var (feat, rank) in new[]
                         {
                             (FeatType.SerratedArc1, 1),
                             (FeatType.SerratedArc2, 2),
                             (FeatType.SerratedArc3, 3)
                         })
                {
                    foreach (var startsBleeding in new[] { false, true })
                    {
                        foreach (var enemy in enemies)
                            StatusEffect.RemoveStatusEffect<BleedStatusEffect>(enemy);

                        if (startsBleeding)
                            ctx.Assert(StatusEffect.ApplyStatusEffect<BleedStatusEffect>(caster, target, 60f), "setup Bleed must apply");
                        var originalBleed = StatusEffect.GetStatusEffect(target, typeof(BleedStatusEffect));

                        ctx.Assert((GetDistanceBetween(caster, nearby) < 5f) == recipientInsideArea &&
                                   GetDistanceBetween(target, nearby) < 5f,
                            $"spread geometry: caster-to-recipient {GetDistanceBetween(caster, nearby)}, source-to-recipient {GetDistanceBetween(target, nearby)}, expected inside area {recipientInsideArea}");
                        if (recipientInsideArea)
                        {
                            ctx.Assert(GetDistanceBetween(target, otherNearby) > 5f &&
                                       GetDistanceBetween(nearby, otherNearby) < GetDistanceBetween(nearby, target),
                                $"chain geometry: source-to-extra {GetDistanceBetween(target, otherNearby)}, recipient-to-extra {GetDistanceBetween(nearby, otherNearby)}, recipient-to-source {GetDistanceBetween(nearby, target)}");
                        }
                        var ability = abilities[feat];
                        var impactedTargetCount = 0;
                        Ability.BeginAbilityImpact(caster, ability);
                        try
                        {
                            await ctx.ExecuteInCreatureContextAsync(caster, () => ability.ImpactAction(caster, target, rank, GetLocation(target)));
                        }
                        finally
                        {
                            impactedTargetCount = Ability.EndAbilityImpact(caster).ImpactedTargetCount;
                        }

                        ctx.AssertEqual(recipientInsideArea ? 2 : 1, impactedTargetCount,
                            $"{feat}: the normal area impact must process every enemy inside 5m");
                        var expectedRecipients = startsBleeding ? 1 : 0;
                        ctx.AssertEqual(expectedRecipients,
                            new[] { nearby, otherNearby }.Count(StatusEffect.HasStatusEffect<BleedStatusEffect>),
                            $"{feat}: additional enemies receiving Bleed");
                        ctx.Assert(StatusEffect.GetStatusEffect(target, typeof(BleedStatusEffect)) == originalBleed,
                            $"{feat}: spreading must not replace the source target's Bleed");
                        ctx.Assert(!StatusEffect.HasStatusEffect<BleedStatusEffect>(outsideRange),
                            $"{feat}: Bleed must not spread beyond 5m");
                        if (recipientInsideArea)
                            ctx.Assert(!StatusEffect.HasStatusEffect<BleedStatusEffect>(otherNearby),
                                $"{feat}: a newly bleeding area target must not spread Bleed again");
                        ctx.Assert(enemies.All(enemy => !StatusEffect.HasStatusEffect<FragmentationStatusEffect>(enemy)),
                            $"{feat}: Shrapnel Casing must not add Fragmentation to Twin Blade abilities");
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
