using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class CombatPerkRegressionEngineTests
    {
        [EngineTest("Blood Frenzy restores stamina at both ranks on bleeding targets", Category = "CombatPerkRegression", TimeoutSeconds = 30f)]
        public static async Task BloodFrenzyStamina(EngineTestContext ctx)
        {
            var beast = ctx.SpawnCreature("nw_bandit001");
            var target = ctx.SpawnCreature("nw_rat001", 2f);
            await ctx.WaitFrameAsync();
            Prepare(ctx, beast);
            Prepare(ctx, target);
            ctx.MakeHostile(target);
            ctx.Assert(StatusEffect.ApplyStatusEffect(beast, target, new BleedStatusEffect(), 120f), "Bleed applies");
            foreach (var rank in new[] { 1, 2 })
            {
                ctx.SetNPCPerkLevel(beast, PerkType.BeastBloodFrenzy, rank);
                ctx.AssertEqual(rank == 1 ? 20 : 30,
                    Stat.GetStatAdjustment(beast, StatType.DamageDealtBleedingTargetStaminaRestoreChance), "Blood Frenzy chance");
                var procs = 0;
                ctx.SeedRandom(12345);
                for (var hit = 0; hit < 100; hit++)
                {
                    SetLocalInt(beast, "STAMINA", 0);
                    Combat.ApplyDamageDealtEffects(beast, target, 10, SkillType.BeastMastery);
                    var restored = Stat.GetCurrentStamina(beast);
                    ctx.Assert(restored is 0 or 1, "Each proc restores exactly one STM");
                    procs += restored;
                }
                ctx.Assert(procs > 0 && procs < 100, "The chance must produce both hits and misses");
                ctx.Log($"Blood Frenzy rank {rank}: {procs} STM restores in 100 seeded direct hits.");
            }
            StatusEffect.RemoveAllStatusEffects(target);
            SetLocalInt(beast, "STAMINA", 0);
            for (var hit = 0; hit < 100; hit++)
                Combat.ApplyDamageDealtEffects(beast, target, 10, SkillType.BeastMastery);
            ctx.AssertEqual(0, Stat.GetCurrentStamina(beast), "Non-bleeding targets never restore STM");
        }

        [EngineTest("Beast areas select a single enemy and self-centered impacts select the beast", Category = "CombatPerkRegression", TimeoutSeconds = 30f)]
        public static async Task BeastAreaTargets(EngineTestContext ctx)
        {
            var beast = ctx.SpawnCreature("nw_bandit001");
            var target = ctx.SpawnCreature("nw_rat001", 2f);
            await ctx.WaitFrameAsync();
            Prepare(ctx, beast);
            Prepare(ctx, target);
            ctx.MakeHostile(target);
            Enmity.ModifyEnmity(target, beast, 10);
            foreach (var feat in new[]
            {
                FeatType.PoisonBreath1, FeatType.PoisonBreath2, FeatType.PoisonBreath3,
                FeatType.IceBreath1, FeatType.IceBreath2, FeatType.IceBreath3,
                FeatType.CrushingSlam1, FeatType.CrushingSlam2, FeatType.CrushingSlam3,
                FeatType.Rampage1, FeatType.Rampage2, FeatType.PrimalOverrun1
            })
            {
                var ability = Ability.GetAbilityDetail(feat);
                var context = new AIContext(beast, AITriggerType.Heartbeat, target,
                    new AIProfile { Type = AIProfileType.BeastCompanion }, new AIState(), Array.Empty<uint>());
                ctx.Assert(ability.AITargetSelector != null, $"{feat} declares its AI target");
                var selected = ability.AITargetSelector(context);
                var aimed = feat is FeatType.PoisonBreath1 or FeatType.PoisonBreath2 or FeatType.PoisonBreath3
                    or FeatType.IceBreath1 or FeatType.IceBreath2 or FeatType.IceBreath3;
                ctx.AssertEqual(aimed ? target : beast, selected, $"{feat} target");
                context.SetEvaluatedTarget(selected);
                ctx.Assert(AIScore.Ability(ability)(context) > 0, $"{feat} scores with one enemy");
            }
        }

        [EngineTest("Beast defensive buffs retain their full reduction and Force Suppression reports the combined penalty", Category = "CombatPerkRegression", TimeoutSeconds = 30f)]
        public static async Task DefensiveAndAttackModifiers(EngineTestContext ctx)
        {
            var beast = ctx.SpawnCreature("nw_bandit001");
            await ctx.WaitFrameAsync();
            Prepare(ctx, beast);
            foreach (var entry in new[] { (FeatType.RampartHide1, -20), (FeatType.UnbreakableBeast1, -25) })
            {
                var ability = Ability.GetAbilityDetail(entry.Item1);
                await ctx.ExecuteInCreatureContextAsync(beast, () => ability.ImpactAction(beast, beast, 1, GetLocation(beast)));
                ctx.AssertEqual(entry.Item2, Stat.GetStatAdjustment(beast, StatType.DamageTakenPercentAdjustment), "Buff reduction");
                ctx.AssertEqual(100 + entry.Item2, Combat.ApplyDamageTakenModifiers(beast, 100), "Reduction of a 100-point hit");
                StatusEffect.RemoveAllStatusEffects(beast);
            }
            StatusEffect.ApplyStatusEffect(beast, beast, new ForceSuppressionStatusEffect(), 30f);
            ctx.AssertEqual(-10, Stat.GetAttackPercentAdjustment(beast, SkillType.Vibroblade), "Physical Attack penalty");
            ctx.AssertEqual(-25, Stat.GetAttackPercentAdjustment(beast, SkillType.Force), "Combined Force Attack penalty");
        }

        [EngineTest("Control-only Mimicry casts never gain direct damage from flat or potency bonuses", Category = "CombatPerkRegression", TimeoutSeconds = 30f)]
        public static async Task NonDamagingTechniques(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001");
            var target = ctx.SpawnCreature("nw_rat001", 2f);
            await ctx.WaitFrameAsync();
            Prepare(ctx, caster);
            Prepare(ctx, target);
            ctx.MakeHostile(target);
            TemporaryStatModifier.Add(caster, StatType.FirstHostileAbilityHitDamageBonus, 75, 120f);
            TemporaryStatModifier.Add(caster, StatType.FirstHostileAbilityHitMaximumCount, 3, 120f);
            TemporaryStatModifier.Add(caster, StatType.FirstHostileAbilityHitCooldownSeconds, 90, 120f);
            TemporaryStatModifier.Add(caster, StatType.MimicryPotencyPercent, 100, 120f);
            Combat.SetAbilityHitResolutionOverride(true);
            try
            {
                foreach (var feat in new[] { FeatType.PiercingQuillsTechnique, FeatType.DarkShockTechnique,
                    FeatType.NullShockTechnique, FeatType.InnerCircleBindTechnique })
                {
                    var ability = Ability.GetAbilityDetail(feat);
                    var hp = GetCurrentHitPoints(target);
                    Ability.BeginAbilityImpact(caster, ability);
                    try
                    {
                        await ctx.ExecuteInCreatureContextAsync(caster,
                            () => ability.ImpactAction(caster, target, 1, GetLocation(target)));
                    }
                    finally { Ability.EndAbilityImpact(caster); }
                    await ctx.DelaySecondsAsync(0.5f);
                    ctx.AssertEqual(hp, GetCurrentHitPoints(target), $"{feat} causes no direct damage");
                    ctx.Assert(StatusEffect.GetCreatureStatusEffects(target).GetAllEffects().Count > 0,
                        $"{feat} still applies its control effect");
                    StatusEffect.RemoveAllStatusEffects(target);
                }

                // All three damage bonuses must remain available after the four control-only casts.
                var attack = Ability.GetAbilityDetail(FeatType.ApexBite1);
                for (var stack = 0; stack < 3; stack++)
                {
                    ctx.AssertEqual(75, Combat.GetAbilityImpactBaseDamageBonus(caster, target, attack, SkillType.BeastMastery),
                        "Control-only casts preserve every First Strike stack without starting recharge");
                    Stat.SetNPCMaxHitPoints(target, 1000, true);
                    Ability.BeginAbilityImpact(caster, attack);
                    try
                    {
                        await ctx.ExecuteInCreatureContextAsync(caster,
                            () => attack.ImpactAction(caster, target, 1, GetLocation(target)));
                    }
                    finally { Ability.EndAbilityImpact(caster); }
                }
                ctx.AssertEqual(0, Combat.GetAbilityImpactBaseDamageBonus(caster, target, attack, SkillType.BeastMastery),
                    "Damaging attacks consume all three stacks and enter recharge normally");
            }
            finally { Combat.SetAbilityHitResolutionOverride(null); }
        }

        [EngineTest("Force Choke applies its full scaled damage budget over thirty seconds", Category = "CombatPerkRegression", TimeoutSeconds = 55f)]
        public static async Task ForceChokeDamageBudget(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001");
            var targets = new[] { ctx.SpawnCreature("nw_rat001", 1f), ctx.SpawnCreature("nw_rat001", 2f),
                ctx.SpawnCreature("nw_rat001", 3f), ctx.SpawnCreature("nw_rat001", 4f) };
            await ctx.WaitFrameAsync();
            Prepare(ctx, caster);
            SWLOR.NWN.API.NWNX.CreaturePlugin.SetRawAbilityScore(caster, AbilityType.Willpower, 26);
            var budgets = new[] { 10, 20, 30, 43 };
            var feats = new[] { FeatType.ForceChoke1, FeatType.ForceChoke2, FeatType.ForceChoke3, FeatType.ForceChoke4 };
            Combat.SetAbilityHitResolutionOverride(true);
            try
            {
                for (var i = 0; i < targets.Length; i++)
                {
                    var target = targets[i];
                    Prepare(ctx, target);
                    ctx.MakeHostile(target);
                    var ability = Ability.GetAbilityDetail(feats[i]);
                    Ability.BeginAbilityImpact(caster, ability);
                    try
                    {
                        await ctx.ExecuteInCreatureContextAsync(caster,
                            () => ability.ImpactAction(caster, target, 1, GetLocation(target)));
                    }
                    finally { Ability.EndAbilityImpact(caster); }
                    ctx.Assert(StatusEffect.HasStatusEffect(target, typeof(ImmobilizedStatusEffect)), "Choke immobilizes");
                }
                await ctx.DelaySecondsAsync(32f);
                for (var i = 0; i < targets.Length; i++)
                    ctx.AssertEqual(budgets[i], 1000 - GetCurrentHitPoints(targets[i]), $"{feats[i]} total WIL-scaled damage");
            }
            finally { Combat.SetAbilityHitResolutionOverride(null); }
        }

        [EngineTest("Apex Bite critical chance belongs to its impact and Overclocked Analyzer increases status procs", Category = "CombatPerkRegression", TimeoutSeconds = 30f)]
        public static async Task ConditionalCriticalAndProcBonuses(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001");
            var target = ctx.SpawnCreature("nw_rat001", 2f);
            await ctx.WaitFrameAsync();
            Prepare(ctx, caster);
            Prepare(ctx, target);
            ctx.MakeHostile(target);
            var passiveCritical = Stat.GetStatAdjustment(caster, StatType.CriticalRatePercentAdjustment);
            var baseRate = Combat.GetAbilityCriticalRate(caster, SkillType.BeastMastery, false);
            ctx.AssertEqual(baseRate + 25, Combat.GetAbilityCriticalRate(caster, SkillType.BeastMastery, false, 25), "Apex Bite impact critical chance");
            var apex = Ability.GetAbilityDetail(FeatType.ApexBite1);
            var criticals = 0;
            ctx.SeedRandom(54321);
            for (var i = 0; i < 40; i++)
            {
                Stat.SetNPCMaxHitPoints(target, 1000, true);
                Ability.BeginAbilityImpact(caster, apex);
                try
                {
                    await ctx.ExecuteInCreatureContextAsync(caster,
                        () => apex.ImpactAction(caster, target, 1, GetLocation(target)));
                }
                finally { Ability.EndAbilityImpact(caster); }
                criticals += Ability.GetLastCompletedAbilityImpactSummary(caster).CriticalHitCount;
            }
            ctx.Assert(criticals > 0 && criticals < 40, "Apex Bite actually rolls critical impacts");
            ctx.AssertEqual(passiveCritical, Stat.GetStatAdjustment(caster, StatType.CriticalRatePercentAdjustment), "Apex Bite does not change passive critical rate");
            var overload = Ability.GetAbilityDetail(FeatType.Overload);
            await ctx.ExecuteInCreatureContextAsync(caster, () => overload.ImpactAction(caster, caster, 1, GetLocation(caster)));
            foreach (var stat in new[] { StatType.DamageDealtBleedChance, StatType.DamageDealtFreezingChance,
                StatType.DamageDealtShockChance, StatType.DamageDealtSunderChance, StatType.DamageDealtHemorrhageChance })
                ctx.AssertEqual(15, Stat.GetStatAdjustment(caster, stat), "Overclocked on-hit proc bonus");
            ctx.AssertEqual(50, Stat.GetStatAdjustment(caster, StatType.MimicryPotencyPercent), "Overclocked potency");
            ctx.AssertEqual(0, Stat.GetStatAdjustment(caster, StatType.AccuracyPercentAdjustment), "Overclocked does not increase hit accuracy");
        }

        private static void Prepare(EngineTestContext ctx, uint creature)
        {
            ctx.SuppressNPCNaturalRegen(creature);
            Stat.SetNPCMaxHitPoints(creature, 1000, true);
            SetAILevel(creature, AILevel.VeryLow);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), creature, 120f);
        }
    }
}
