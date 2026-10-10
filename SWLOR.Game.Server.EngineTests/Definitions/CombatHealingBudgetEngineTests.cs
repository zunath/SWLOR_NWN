using System;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Force;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class CombatHealingBudgetEngineTests
    {
        [EngineTest("Lifesteal stacks share hit and rolling limits while active drains retain their allowance", Category = "CombatHealing", TimeoutSeconds = 20f)]
        public static async Task SharedBudgets(EngineTestContext ctx)
        {
            var source = ctx.SpawnCreature("civilian", 1f);
            var target = ctx.SpawnCreature("civilian", 3f);
            await ctx.DelaySecondsAsync(1f);
            Prepare(ctx, source, 1000, 1);
            Prepare(ctx, target, 1000, 1000);
            TemporaryStatModifier.Add(source, StatType.HealingReceivedPercentAdjustment, 35, 30f);

            using (Combat.BeginDamageDerivedHealing(source, target))
            {
                ctx.AssertEqual(60, Combat.ApplyDamageDerivedHealing(source, 684, 25), "critical passive is capped after healing bonuses");
                ctx.AssertEqual(0, Combat.ApplyDamageDerivedHealing(source, 684, 8), "another passive cannot refill the same hit");
                ctx.AssertEqual(150, Combat.ApplyDamageDerivedHealing(source, 684, 40, true), "active drain keeps its separate allowance");
                ctx.AssertEqual(0, Combat.ApplyDamageDerivedHealing(source, 684, 40, true), "one hit cannot repeat the active allowance");
            }
            ctx.AssertEqual(211, GetCurrentHitPoints(source), "combined spike heals 210 HP");

            for (var i = 0; i < 3; i++)
            {
                using (Combat.BeginDamageDerivedHealing(source, target))
                    ctx.AssertEqual(60, Combat.ApplyDamageDerivedHealing(source, 684, 25), "subsequent hits use the remaining rolling allowance");
            }
            using (Combat.BeginDamageDerivedHealing(source, target))
                ctx.AssertEqual(0, Combat.ApplyDamageDerivedHealing(source, 684, 25), "fifth hit cannot exceed 240 HP within six seconds");
        }

        [EngineTest("Soul Ascension preserves ordinary stacked lifesteal sustain", Category = "CombatHealing", TimeoutSeconds = 20f)]
        public static async Task StackedSustain(EngineTestContext ctx)
        {
            var baseline = ctx.SpawnCreature("civilian", 1f);
            var ascended = ctx.SpawnCreature("civilian", 2f);
            var target = ctx.SpawnCreature("civilian", 3f);
            await ctx.DelaySecondsAsync(1f);
            Prepare(ctx, baseline, 1000, 1);
            Prepare(ctx, ascended, 1000, 1);
            Prepare(ctx, target, 1000, 1000);
            foreach (var source in new[] { baseline, ascended })
            {
                TemporaryStatModifier.Add(source, StatType.LowHPDamageDealtHPRestoreThresholdPercent, 40, 30f);
                TemporaryStatModifier.Add(source, StatType.LowHPDamageDealtHPPercentRestore, 8, 30f);
            }
            ctx.Assert(StatusEffect.ApplyStatusEffect(ascended, ascended, typeof(SoulAscensionBurstStatusEffect), 45f),
                "Soul Ascension applies its real lifesteal buff");
            for (var i = 0; i < 6; i++)
            {
                foreach (var source in new[] { baseline, ascended })
                {
                    using (Combat.BeginDamageDerivedHealing(source, target))
                        Combat.ApplyDamageDealtEffects(source, target, 250, SkillType.HeavyVibroblade,
                            CombatDamageType.Physical, isAbilityDamage: true);
                }
            }
            ctx.AssertEqual(121, GetCurrentHitPoints(baseline), "six ordinary hits retain the full 120 HP of 8% lifesteal");
            ctx.AssertEqual(241, GetCurrentHitPoints(ascended), "Soul Ascension doubles recovery to 240 HP across the same hits");
        }

        [EngineTest("Lifesteal excludes overkill and overhealing does not spend recovery budgets", Category = "CombatHealing", TimeoutSeconds = 20f)]
        public static async Task EffectiveHealing(EngineTestContext ctx)
        {
            var source = ctx.SpawnCreature("civilian", 1f);
            var target = ctx.SpawnCreature("civilian", 3f);
            await ctx.DelaySecondsAsync(1f);
            Prepare(ctx, source, 1000, 1000);
            Prepare(ctx, target, 1000, 1000);
            using (Combat.BeginDamageDerivedHealing(source, target))
                ctx.AssertEqual(0, Combat.ApplyDamageDerivedHealing(source, 684, 25), "full-health healing spends nothing");

            ObjectPlugin.SetCurrentHitPoints(source, 999);
            using (Combat.BeginDamageDerivedHealing(source, target))
                ctx.AssertEqual(1, Combat.ApplyDamageDerivedHealing(source, 684, 25), "only the missing HP spends allowance");
            ObjectPlugin.SetCurrentHitPoints(source, 1);
            using (Combat.BeginDamageDerivedHealing(source, target))
                ctx.AssertEqual(60, Combat.ApplyDamageDerivedHealing(source, 684, 25), "next hit retains its per-hit allowance");

            ObjectPlugin.SetCurrentHitPoints(target, 20);
            using (Combat.BeginDamageDerivedHealing(source, target))
                ctx.AssertEqual(10, Combat.ApplyDamageDerivedHealing(source, 684, 50, true), "a 20-HP target supplies at most 10 HP of healing");

            SetPlotFlag(target, true);
            using (Combat.BeginDamageDerivedHealing(source, target))
                ctx.AssertEqual(0, Combat.ApplyDamageDerivedHealing(source, 684, 50, true), "an invulnerable plot target cannot supply healing");
        }

        [EngineTest("Kill recovery shares a six-second allowance without suppressing other kill rewards", Category = "CombatHealing", TimeoutSeconds = 20f)]
        public static async Task KillRecovery(EngineTestContext ctx)
        {
            var source = ctx.SpawnCreature("civilian", 1f);
            await ctx.DelaySecondsAsync(1f);
            Prepare(ctx, source, 1000, 1);
            TemporaryStatModifier.Add(source, StatType.DefeatedEnemyHPPercentRestore, 22, 30f);
            TemporaryStatModifier.Add(source, StatType.HealingReceivedPercentAdjustment, 35, 30f);
            TemporaryStatModifier.Add(source, StatType.DefeatedEnemyAttackPercentAdjustment, 15, 30f);
            TemporaryStatModifier.Add(source, StatType.DefeatedEnemyAttackDurationSeconds, 30, 30f);
            for (var i = 0; i < 5; i++)
                Combat.ApplyDefeatedEnemyEffects(source);
            ctx.AssertEqual(121, GetCurrentHitPoints(source), "five kills share a 120-HP allowance after bonuses");
            ctx.Assert(Stat.GetStatAdjustment(source, StatType.AttackPercentAdjustment) >= 15, "kill attack reward still applies");
            await ctx.DelaySecondsAsync(6.1f);
            Combat.ApplyDefeatedEnemyEffects(source);
            ctx.AssertEqual(241, GetCurrentHitPoints(source), "expired receipts release their allowance");
        }

        [EngineTest("Force Drain uses its shared healing cap and does not apply Combat Readiness twice", Category = "CombatHealing", TimeoutSeconds = 20f)]
        public static async Task ForceDrain(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var source = fixture.Creature;
            var target = ctx.SpawnCreature("civilian", 3f);
            await ctx.DelaySecondsAsync(1f);
            Prepare(ctx, target, 1000, 1000);
            ctx.MakeHostile(target);
            ctx.Assert(GetIsReactionTypeHostile(target, source), "drain target is hostile to the player fixture");
            fixture.Update(record =>
            {
                record.Perks[PerkType.ForceDrain] = 3;
                Stat.AdjustPlayerMaxHP(record, source, 1000 - GetMaxHitPoints(source));
            });
            ObjectPlugin.SetCurrentHitPoints(source, 1);
            TemporaryStatModifier.Add(source, StatType.CombatReadinessPercent, 15, 30f);
            TemporaryStatModifier.Add(source, StatType.HealingReceivedPercentAdjustment, 20, 30f);
            ctx.SetResources(source, 100, 100);
            Combat.SetAbilityHitResolutionOverride(true);
            try
            {
                var definition = new ForceDrainAbilityDefinition().BuildAbilities()[FeatType.ForceDrain3];
                Ability.BeginAbilityImpact(source, definition);
                try
                {
                    await ctx.ExecuteInCreatureContextAsync(source,
                        () => definition.ImpactAction(source, target, 3, GetLocation(target)));
                }
                finally
                {
                    Ability.EndAbilityImpact(source);
                }
                await ctx.DelaySecondsAsync(0.5f);
                var damage = 1000 - GetCurrentHitPoints(target);
                ctx.Assert(damage > 0, "Force Drain lands against the target");
                ctx.Assert(GetCurrentHitPoints(target) > 500, "target remains above the enhanced-drain threshold");
                var requested = Stat.ApplyHealingReceivedAdjustment(source, GameMath.PercentOf(damage, 40), false);
                var expected = Math.Min(GameMath.PercentOf(damage, 50), Math.Min(requested,
                    Combat.CalculateMaxHPHealingBudget(GetMaxHitPoints(source), Combat.MaximumActivatedDamageHealingMaxHPPercent)));
                ctx.AssertEqual(expected, GetCurrentHitPoints(source) - 1, "healing applies the damage and maximum-HP caps without another Readiness multiplier");
            }
            finally
            {
                Combat.SetAbilityHitResolutionOverride(null);
            }
        }

        [EngineTest("Area lifesteal shares one passive budget across all struck targets", Category = "CombatHealing", TimeoutSeconds = 20f)]
        public static async Task AreaHealing(EngineTestContext ctx)
        {
            var source = ctx.SpawnCreature("civilian", 1f);
            var targets = new uint[20];
            for (var i = 0; i < targets.Length; i++)
                targets[i] = ctx.SpawnCreature("civilian", 3f + i);
            await ctx.DelaySecondsAsync(1f);
            Prepare(ctx, source, 1000, 1);
            foreach (var target in targets)
                Prepare(ctx, target, 1000, 1000);
            TemporaryStatModifier.Add(source, StatType.DamageDealtHPPercentRestore, 12, 30f);
            var definition = Ability.GetAbilityDetail(FeatType.ForceDrain3);
            Ability.BeginAbilityImpact(source, definition);
            try
            {
                await ctx.ExecuteInCreatureContextAsync(source, () =>
                {
                    foreach (var target in targets)
                        Ability.ApplyHostileCombatImpact(source, target, SkillType.Force, 200, CombatDamageType.Force);
                });
            }
            finally { Ability.EndAbilityImpact(source); }
            ctx.AssertEqual(241, GetCurrentHitPoints(source), "twenty targets share 240 HP of passive healing");
        }

        [EngineTest("Repeated queued impacts cannot heal from the same overkill health twice", Category = "CombatHealing", TimeoutSeconds = 20f)]
        public static async Task QueuedOverkill(EngineTestContext ctx)
        {
            var source = ctx.SpawnCreature("civilian", 1f);
            var target = ctx.SpawnCreature("civilian", 3f);
            await ctx.DelaySecondsAsync(1f);
            Prepare(ctx, source, 1000, 1);
            Prepare(ctx, target, 1000, 20);
            TemporaryStatModifier.Add(source, StatType.DamageDealtHPPercentRestore, 8, 30f);
            var definition = Ability.GetAbilityDetail(FeatType.ForceDrain3);
            Ability.BeginAbilityImpact(source, definition);
            try
            {
                await ctx.ExecuteInCreatureContextAsync(source, () =>
                {
                    Ability.ApplyHostileCombatImpact(source, target, SkillType.Force, 200, CombatDamageType.Force);
                    Ability.ApplyHostileCombatImpact(source, target, SkillType.Force, 200, CombatDamageType.Force);
                });
            }
            finally { Ability.EndAbilityImpact(source); }
            ctx.AssertEqual(3, GetCurrentHitPoints(source), "only the first impact can draw 8% of the target's 20 remaining HP");
        }

        private static void Prepare(EngineTestContext ctx, uint creature, int maximumHP, int currentHP)
        {
            ctx.SuppressNPCNaturalRegen(creature);
            SetPlotFlag(creature, false);
            Stat.SetNPCMaxHitPoints(creature, maximumHP, true);
            ObjectPlugin.SetCurrentHitPoints(creature, currentHP);
        }
    }
}
