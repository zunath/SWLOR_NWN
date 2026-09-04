using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.TwinBlade;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class SerratedArcEngineTests
    {
        [EngineTest("Every Serrated Arc rank spreads Bleed to one other enemy", Category = "StatusEffect", TimeoutSeconds = 30f)]
        public static async Task BleedSpreadsToOneAdditionalEnemy(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001");
            var target = ctx.SpawnCreature("nw_rat001", 2f);
            var nearby = ctx.SpawnCreature("nw_rat001", 6f);
            var otherNearby = ctx.SpawnCreature("nw_rat001", 6.5f);
            var outsideRange = ctx.SpawnCreature("nw_rat001", 8f);
            var enemies = new[] { target, nearby, otherNearby, outsideRange };
            await ctx.WaitFrameAsync();
            SetCommandable(false, caster);
            ctx.SetNPCPerkLevel(caster, PerkType.ShrapnelCasing, 3);

            foreach (var enemy in enemies)
            {
                SetCommandable(false, enemy);
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

                        // Only the source target is inside the caster's 5m area. The two
                        // spread candidates are outside it, but within 5m of that target.
                        ctx.Assert(GetDistanceBetween(caster, nearby) > 5f && GetDistanceBetween(target, nearby) < 5f,
                            "the spread recipient must be outside the primary area and inside the spread radius");
                        var ability = abilities[feat];
                        Ability.BeginAbilityImpact(caster, ability);
                        try
                        {
                            ability.ImpactAction(caster, target, rank, GetLocation(target));
                        }
                        finally
                        {
                            Ability.EndAbilityImpact(caster);
                        }

                        var expectedRecipients = startsBleeding ? 1 : 0;
                        ctx.AssertEqual(expectedRecipients,
                            new[] { nearby, otherNearby }.Count(StatusEffect.HasStatusEffect<BleedStatusEffect>),
                            $"{feat}: additional enemies receiving Bleed");
                        ctx.Assert(StatusEffect.GetStatusEffect(target, typeof(BleedStatusEffect)) == originalBleed,
                            $"{feat}: spreading must not replace the source target's Bleed");
                        ctx.Assert(!StatusEffect.HasStatusEffect<BleedStatusEffect>(outsideRange),
                            $"{feat}: Bleed must not spread beyond 5m");
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
