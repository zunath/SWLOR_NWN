using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class ThrowingStanceEngineTests
    {
        [EngineTest("Ordnance Stance reduces single-target thrown damage by 10% and raises thrown area abilities", Category = "ThrowingStance", TimeoutSeconds = 30f)]
        public static async Task OrdnanceSingleTargetPenalty(EngineTestContext ctx)
        {
            var ordnance = ctx.SpawnCreature("nw_bandit001");
            var plain = ctx.SpawnCreature("nw_bandit001", 0f, 2f);
            var target = ctx.SpawnCreature("nw_rat001", 3f);
            await ctx.WaitFrameAsync();
            Prepare(ctx, ordnance, plain, target);
            ctx.Assert(StatusEffect.ApplyStatusEffect<OrdnanceStanceStatusEffect>(ordnance, ordnance, 60f),
                "Ordnance Stance applies");

            var singleTarget = Ability.GetAbilityDetail(FeatType.SeveringToss1);
            var area = Ability.GetAbilityDetail(FeatType.ExplosiveToss1);
            ctx.Assert(singleTarget.IsSingleTargetAbility && singleTarget.IsHostileAbility, "Severing Toss is a single-target hostile ability");
            ctx.Assert(area.IsAreaAbility, "Explosive Toss is an area ability");

            var reduced = CombatRules.ApplyPercentDamageAdjustment(100, -10);
            var raised = CombatRules.ApplyPercentDamageAdjustment(100, 10);
            ctx.AssertEqual(reduced, AutoAttack(ordnance, target, SkillType.Throwing),
                "thrown auto-attacks are single-target thrown damage");
            ctx.AssertEqual(reduced, AbilityDamage(ordnance, target, singleTarget),
                "single-target Throwing abilities lose 10%");
            ctx.AssertEqual(raised, AbilityDamage(ordnance, target, area),
                "thrown area abilities keep their +10%");
            ctx.AssertEqual(100, AutoAttack(ordnance, target, SkillType.Rifle),
                "other weapon skills are unaffected");

            ctx.AssertEqual(100, AutoAttack(plain, target, SkillType.Throwing), "no stance, no penalty");
            ctx.AssertEqual(100, AbilityDamage(plain, target, singleTarget), "no stance, no ability penalty");
        }

        [EngineTest("Flurry Stance Haste applies only to Throwing attacks against bleeding targets", Category = "ThrowingStance", TimeoutSeconds = 30f)]
        public static async Task FlurryBleedingHaste(EngineTestContext ctx)
        {
            var flurry = ctx.SpawnCreature("nw_bandit001");
            var bleeding = ctx.SpawnCreature("nw_bandit001", 3f);
            var healthy = ctx.SpawnCreature("nw_bandit001", 3f, 2f);
            await ctx.WaitFrameAsync();
            Prepare(ctx, flurry, bleeding, healthy);
            ctx.Assert(StatusEffect.ApplyStatusEffect<FlurryStanceStatusEffect>(flurry, flurry, 60f),
                "Flurry Stance applies");
            ctx.Assert(StatusEffect.ApplyStatusEffect(flurry, bleeding, new BleedStatusEffect(), 60f), "Bleed applies");

            ctx.AssertEqual(0, Stat.GetStatAdjustment(flurry, StatType.AttackDelayReductionPercent),
                "Flurry Stance no longer grants unconditional Haste");
            ctx.AssertEqual(-10, Stat.GetStatAdjustment(flurry, StatType.DefensePercentAdjustment),
                "Flurry Stance keeps its Defense penalty");
            ctx.AssertEqual(10, GameServices.CombatAttackTiming.GetTargetAttackDelayReduction(flurry, bleeding, SkillType.Throwing),
                "thrown attacks at a bleeding target gain +10% Haste");
            ctx.AssertEqual(0, GameServices.CombatAttackTiming.GetTargetAttackDelayReduction(flurry, healthy, SkillType.Throwing),
                "thrown attacks at a target that is not bleeding gain nothing");
            ctx.AssertEqual(0, GameServices.CombatAttackTiming.GetTargetAttackDelayReduction(flurry, bleeding, SkillType.Rifle),
                "other weapon skills gain nothing");
            ctx.AssertEqual(0, GameServices.CombatAttackTiming.GetTargetAttackDelayReduction(bleeding, flurry, SkillType.Throwing),
                "a creature without the stance gains nothing");
        }

        private static void Prepare(EngineTestContext ctx, params uint[] creatures)
        {
            foreach (var creature in creatures)
            {
                ctx.SuppressNPCNaturalRegen(creature);
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), creature, 60f);
            }
        }

        private static int AutoAttack(uint attacker, uint target, SkillType skill)
        {
            return GameServices.CombatDamageModifiers.ApplyDamageDealtModifiers(attacker, target, 100, skill,
                CombatDamageType.Physical, isAbilityDamage: false, canApplyRandomFlatBonuses: false);
        }

        private static int AbilityDamage(uint attacker, uint target, AbilityDetail ability)
        {
            return GameServices.CombatDamageModifiers.ApplyDamageDealtModifiers(attacker, target, 100, SkillType.Throwing,
                CombatDamageType.Physical, isAbilityDamage: true, canApplyRandomFlatBonuses: false,
                ability: ability);
        }
    }
}
