using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class CombatPerkRegressionEngineTests
    {
        [EngineTest("Focus Stim accuracy applies to queued weapon abilities", Category = "CombatPerkRegression", TimeoutSeconds = 30f)]
        public static async Task FocusStimQueuedWeaponAccuracy(EngineTestContext ctx)
        {
            var attacker = ctx.SpawnCreature("nw_bandit001");
            await ctx.WaitFrameAsync();
            Prepare(ctx, attacker);

            foreach (var rank in new[] { 1, 2 })
            {
                var bonus = rank == 1 ? 5 : 8;
                IStatusEffect effect = rank == 1 ? new FocusStim1StatusEffect() : new FocusStim2StatusEffect();
                ctx.AssertEqual(0, Combat.GetQueuedWeaponAbilityActivationHitChanceAdjustment(attacker, SkillType.Vibroblade),
                    "Queued accuracy starts without a bonus or idle snapshot");
                var accuracy = Stat.GetStatAdjustment(attacker, StatType.AccuracyPercentAdjustment);
                ctx.Assert(StatusEffect.ApplyStatusEffect(attacker, attacker, effect, 120f), "Focus Stim applies");
                ctx.AssertEqual(bonus, Combat.GetQueuedWeaponAbilityActivationHitChanceAdjustment(attacker, SkillType.Vibroblade),
                    "Focus Stim affects the queued weapon roll without an idle snapshot");
                ctx.AssertEqual(76 + bonus, Combat.CalculateHitRate(2, 0,
                    Combat.GetQueuedWeaponAbilityActivationHitChanceAdjustment(attacker, SkillType.Vibroblade)),
                    "Focus Stim adds percentage points to the queued hit chance");
                ctx.AssertEqual(0, Combat.GetQueuedWeaponAbilityActivationHitChanceAdjustment(attacker, SkillType.Mimicry),
                    "Focus Stim excludes Mimicry");
                ctx.AssertEqual(accuracy, Stat.GetStatAdjustment(attacker, StatType.AccuracyPercentAdjustment),
                    "Focus Stim does not change ordinary attack accuracy");

                TemporaryStatModifier.Replace(attacker, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType,
                    (int)SkillType.Vibroblade, 30f, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
                TemporaryStatModifier.Replace(attacker, StatType.QueuedWeaponAbilityIdleHitChancePercentAdjustment,
                    3, 30f, StatType.QueuedWeaponAbilityActivationCriticalRateSkillType);
                ctx.AssertEqual(bonus + 3, Combat.GetQueuedWeaponAbilityActivationHitChanceAdjustment(attacker, SkillType.Vibroblade),
                    "Focus Stim stacks with the matching idle bonus");
                ctx.AssertEqual(bonus, Combat.GetQueuedWeaponAbilityActivationHitChanceAdjustment(attacker, SkillType.Rifle),
                    "Focus Stim remains when the idle bonus belongs to another skill");
                Combat.ClearQueuedWeaponAbilityActivationBonuses(attacker);
                StatusEffect.RemoveAllStatusEffects(attacker);
                ctx.AssertEqual(0, Combat.GetQueuedWeaponAbilityActivationHitChanceAdjustment(attacker, SkillType.Vibroblade),
                    "Removing Focus Stim removes its accuracy bonus");
            }
        }

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

        [EngineTest("Control application riders require an applied Control status", Category = "CombatPerkRegression", TimeoutSeconds = 30f)]
        public static async Task ControlApplicationRidersRequireAppliedControl(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001");
            var immuneTarget = ctx.SpawnCreature("nw_rat001", 2f);
            var controlTarget = ctx.SpawnCreature("nw_rat001", 3f);
            await ctx.WaitFrameAsync();
            Prepare(ctx, caster);
            foreach (var target in new[] { immuneTarget, controlTarget })
                Prepare(ctx, target);

            // NPC perks feed the same stat aggregation as player perks. Clear unrelated NPC-default
            // perks so the assertions isolate Charged Blows and Skull Rattle.
            foreach (var perk in Enum.GetValues<PerkType>().Distinct().Where(perk => perk != PerkType.Invalid))
                ctx.SetNPCPerkLevel(caster, perk, 0);
            ctx.SetNPCPerkLevel(caster, PerkType.ChargedBlows, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.SkullRattle, 1);
            ctx.AssertEqual((int)StatusEffectCategory.Control,
                Stat.GetStatAdjustment(caster, StatType.StatusAppliedRequiredCategory),
                "the selected NPC perks declare the Control rider category");
            ctx.AssertEqual(10, Stat.GetStatAdjustment(caster, StatType.StatusAppliedNextAttackDamageBonus),
                "Charged Blows supplies its rank-one next-attack bonus through perk stats");

            var epicenter = Ability.GetAbilityDetail(FeatType.Epicenter1);
            var immuneTargetAccuracy = Stat.GetStatAdjustment(immuneTarget, StatType.AccuracyPercentAdjustment);
            TemporaryStatModifier.Add(immuneTarget, StatType.KnockdownImmunity, 1, 120f, "ENGINE_TEST_KNOCKDOWN_IMMUNITY");
            Combat.SetAbilityHitResolutionOverride(true);
            try
            {
                async Task ApplyEpicenter(uint target)
                {
                    Ability.BeginAbilityImpact(caster, epicenter);
                    try
                    {
                        await ctx.ExecuteInCreatureContextAsync(caster,
                            () => epicenter.ImpactAction(caster, caster, 1, GetLocation(caster)));
                    }
                    finally
                    {
                        Ability.EndAbilityImpact(caster);
                    }
                }

                ctx.MakeHostile(immuneTarget);
                await ApplyEpicenter(immuneTarget);
                ctx.Assert(StatusEffect.HasStatusEffect<SunderStatusEffect>(immuneTarget),
                    "Epicenter's non-Control additional status applies despite Knockdown immunity");
                ctx.Assert(!StatusEffect.HasStatusEffect<KnockdownStatusEffect>(immuneTarget),
                    "the target's Knockdown immunity rejects Epicenter's Control status");
                ctx.AssertEqual(0, Combat.GetStatusAppliedNextAttackDamageBonus(caster),
                    "Sunder alone must not trigger Charged Blows");
                ctx.AssertEqual(immuneTargetAccuracy,
                    Stat.GetStatAdjustment(immuneTarget, StatType.AccuracyPercentAdjustment),
                    "Sunder alone must not trigger Skull Rattle");
                ChangeToStandardFaction(immuneTarget, StandardFaction.Defender);

                ctx.MakeHostile(controlTarget);
                var controlTargetAccuracy = Stat.GetStatAdjustment(controlTarget, StatType.AccuracyPercentAdjustment);
                await ApplyEpicenter(controlTarget);
                ctx.Assert(StatusEffect.HasStatusEffect<SunderStatusEffect>(controlTarget), "Epicenter applies Sunder");
                ctx.Assert(StatusEffect.HasStatusEffect<KnockdownStatusEffect>(controlTarget),
                    "a fresh target accepts Epicenter's Control status");
                ctx.AssertEqual(10, Combat.GetStatusAppliedNextAttackDamageBonus(caster),
                    "successfully applied Control triggers Charged Blows");
                ctx.AssertEqual(controlTargetAccuracy - 10,
                    Stat.GetStatAdjustment(controlTarget, StatType.AccuracyPercentAdjustment),
                    "successfully applied Control triggers Skull Rattle");
            }
            finally
            {
                Combat.SetAbilityHitResolutionOverride(null);
            }
        }

        [EngineTest("Beast areas select a single enemy and self-centered impacts select the beast", Category = "CombatPerkRegression", TimeoutSeconds = 30f)]
        public static async Task BeastAreaTargets(EngineTestContext ctx)
        {
            var beast = ctx.SpawnCreature("nw_bandit001");
            var target = ctx.SpawnCreature("nw_rat001", 2f);
            var edgeTarget = ctx.SpawnCreature("nw_rat001", 9.5f);
            var farTarget = ctx.SpawnCreature("nw_rat001", 11f);
            await ctx.WaitFrameAsync();
            Prepare(ctx, beast);
            Prepare(ctx, target);
            Prepare(ctx, edgeTarget);
            Prepare(ctx, farTarget);
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
                var action = NPCAI.Profiles[AIProfileType.BeastCompanion].Actions
                    .Single(candidate => candidate.Type == AIActionType.Ability && candidate.Feat == feat);
                var context = new AIContext(beast, AITriggerType.Heartbeat, target,
                    new AIProfile { Type = AIProfileType.BeastCompanion }, new AIState(), Array.Empty<uint>());
                ctx.Assert(ability.AITargetSelector != null, $"{feat} declares its AI target");
                var selected = action.TargetSelector(context);
                var aimed = feat is FeatType.PoisonBreath1 or FeatType.PoisonBreath2 or FeatType.PoisonBreath3
                    or FeatType.IceBreath1 or FeatType.IceBreath2 or FeatType.IceBreath3;
                ctx.AssertEqual(aimed ? target : beast, selected, $"{feat} target");
                context.SetEvaluatedTarget(selected);
                ctx.Assert(action.Score(context) > 0, $"{feat} scores with one enemy in the actual companion profile");
                if (aimed)
                {
                    ctx.MakeHostile(edgeTarget);
                    Enmity.ClearEnmityTable(beast);
                    Enmity.ModifyEnmity(edgeTarget, beast, 10);
                    var edgeContext = new AIContext(beast, AITriggerType.Heartbeat, edgeTarget,
                        context.Profile, new AIState(), Array.Empty<uint>());
                    ctx.AssertEqual(edgeTarget, ability.AITargetSelector(edgeContext), $"{feat} can reach beyond 6m within its 10m cone");
                    ChangeToStandardFaction(edgeTarget, StandardFaction.Defender);

                    ctx.MakeHostile(farTarget);
                    Enmity.ClearEnmityTable(beast);
                    Enmity.ModifyEnmity(farTarget, beast, 10);
                    var farContext = new AIContext(beast, AITriggerType.Heartbeat, farTarget,
                        context.Profile, new AIState(), Array.Empty<uint>());
                    ctx.AssertEqual(OBJECT_INVALID, ability.AITargetSelector(farContext), $"{feat} rejects enemies beyond cone reach");
                    ChangeToStandardFaction(farTarget, StandardFaction.Defender);
                    Enmity.ClearEnmityTable(beast);
                    Enmity.ModifyEnmity(target, beast, 10);
                }
                else
                {
                    ctx.MakeHostile(farTarget);
                    Enmity.ClearEnmityTable(beast);
                    Enmity.ModifyEnmity(farTarget, beast, 10);
                    var farContext = new AIContext(beast, AITriggerType.Heartbeat, farTarget,
                        context.Profile, new AIState(), Array.Empty<uint>());
                    var farSelected = action.TargetSelector(farContext);
                    ctx.AssertEqual(OBJECT_INVALID, farSelected, $"{feat} rejects an enemy outside its 5m radius");
                    farContext.SetEvaluatedTarget(farSelected);
                    ctx.AssertEqual(0, action.Score(farContext), $"{feat} does not spend stamina on an empty area");
                    ChangeToStandardFaction(farTarget, StandardFaction.Defender);
                    Enmity.ClearEnmityTable(beast);
                    Enmity.ModifyEnmity(target, beast, 10);
                }
            }
        }

        [EngineTest("Companion natural regeneration preserves resource caps and stamina delay", Category = "CombatPerkRegression", TimeoutSeconds = 30f)]
        public static async Task CompanionRegeneration(EngineTestContext ctx)
        {
            var beast = ctx.SpawnCreature("nw_bandit001");
            await ctx.WaitFrameAsync();
            Prepare(ctx, beast);
            ctx.SetNPCResources(beast, 10, 10);
            var maxFP = Stat.GetMaxFP(beast);
            var maxSTM = Stat.GetMaxStamina(beast);
            SetLocalInt(beast, "FP", maxFP - 1);
            SetLocalInt(beast, "STAMINA", maxSTM - 1);
            DeleteLocalInt(beast, Stat.SuppressNaturalRegenVariable);
            SetLocalString(beast, "BEAST_STAMINA_REGEN_AVAILABLE_AT", DateTime.UtcNow.AddMinutes(1).Ticks.ToString());
            await ctx.ExecuteInCreatureContextAsync(beast, Stat.RestoreBeastStats);
            ctx.AssertEqual(maxFP, Stat.GetCurrentFP(beast), "FP regenerates while stamina is delayed");
            ctx.AssertEqual(maxSTM - 1, Stat.GetCurrentStamina(beast), "Spending stamina delays its regeneration");
            DeleteLocalString(beast, "BEAST_STAMINA_REGEN_AVAILABLE_AT");
            await ctx.ExecuteInCreatureContextAsync(beast, Stat.RestoreBeastStats);
            ctx.AssertEqual(maxSTM, Stat.GetCurrentStamina(beast), "Stamina regenerates when its delay expires");
            await ctx.ExecuteInCreatureContextAsync(beast, Stat.RestoreBeastStats);
            ctx.AssertEqual(maxFP, Stat.GetCurrentFP(beast), "FP stays capped");
            ctx.AssertEqual(maxSTM, Stat.GetCurrentStamina(beast), "Stamina stays capped");
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

        [EngineTest("Flash preserves armed activation bonuses through its delayed impact", Category = "CombatPerkRegression", TimeoutSeconds = 15f)]
        public static async Task FlashPreservesActivationBonuses(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001");
            var target = ctx.SpawnCreature("nw_rat001", 2f);
            await ctx.WaitFrameAsync();
            Prepare(ctx, caster);
            Prepare(ctx, target);
            ctx.MakeHostile(target);
            Combat.GrantNextAbilityDamageBonus(caster, (int)PerkType.Flash, 31, 120);
            Combat.GrantNextSkillAbilityBonuses(caster, (int)SkillType.HeavyVibroblade, 37, 11, 120);
            TemporaryStatModifier.Add(caster, StatType.NextAttackGuardedHitDMGBonus, 41, 120,
                StatType.NextAttackGuardedHitDMGBonus);
            TemporaryStatModifier.Add(caster, StatType.NextAttackGuardedHitCriticalRatePercentAdjustment, 13, 120,
                StatType.NextAttackGuardedHitDMGBonus);
            TemporaryStatModifier.Add(caster, StatType.NextAttackGuardedHitEnmityBonus, 43, 120,
                StatType.NextAttackGuardedHitDMGBonus);
            var ability = Ability.GetAbilityDetail(FeatType.Flash1);
            var hp = GetCurrentHitPoints(target);
            Combat.SetAbilityHitResolutionOverride(true);
            try
            {
                Ability.BeginAbilityImpact(caster, ability);
                try
                {
                    await ctx.ExecuteInCreatureContextAsync(caster,
                        () => ability.ImpactAction(caster, target, 1, GetLocation(target)));
                }
                finally { Ability.EndAbilityImpact(caster); }
                await ctx.DelaySecondsAsync(0.75f);
                ctx.Assert(StatusEffect.HasStatusEffect(target, typeof(FlashStatusEffect)),
                    "Flash delayed impact resolves");
                ctx.AssertEqual(hp, GetCurrentHitPoints(target), "Flash causes no direct damage");
                ctx.AssertEqual(31, Combat.ConsumeNextAbilityDamageBonus(caster, PerkType.Flash), "Perk damage remains armed");
                var skill = Combat.ConsumeNextSkillAbilityBonuses(caster, SkillType.HeavyVibroblade);
                ctx.AssertEqual(37, skill.DamageBonus, "Skill damage remains armed");
                ctx.AssertEqual(11, skill.CriticalRatePercentAdjustment, "Skill critical rate remains armed");
                var guarded = Combat.ConsumeNextAttackGuardedHitBonuses(caster);
                ctx.AssertEqual(41, guarded.DMGBonus, "Guarded damage remains armed");
                ctx.AssertEqual(13, guarded.CriticalRatePercentAdjustment, "Guarded critical rate remains armed");
                ctx.AssertEqual(43, guarded.EnmityBonus, "Guarded enmity remains armed");
            }
            finally { Combat.SetAbilityHitResolutionOverride(null); }
        }

        [EngineTest("Repeated damage uses captured bonuses once and preserves newer bonuses", Category = "CombatPerkRegression", TimeoutSeconds = 15f)]
        public static async Task RepeatedDamageUsesCapturedBonuses(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001");
            var target = ctx.SpawnCreature("nw_rat001", 2f);
            await ctx.WaitFrameAsync();
            Prepare(ctx, caster);
            Prepare(ctx, target);
            ctx.MakeHostile(target);
            Stat.SetNPCMaxHitPoints(target, 1000, true);
            var ability = new AbilityDetail { SkillType = SkillType.Force, IsHostileAbility = true,
                ActivationType = AbilityActivationType.Casted };
            Combat.GrantNextSkillAbilityBonuses(caster, (int)SkillType.Force, 75, 0, 120);
            Ability.BeginAbilityImpact(caster, ability);
            var pulse = Ability.CaptureRepeatedAbilityImpact(caster, () => Ability.ApplyCombatImpact(
                caster, target, GetLocation(target), SkillType.Force, 10, 0, null, false,
                resolvesHit: false, canCritical: false, useUnscaledDamage: true), baseDamage: 10);
            Ability.EndAbilityImpact(caster);
            Combat.GrantNextSkillAbilityBonuses(caster, (int)SkillType.Force, 101, 0, 120);
            await ctx.ExecuteInCreatureContextAsync(caster, pulse);
            var first = Ability.GetLastCompletedAbilityImpactSummary(caster).AttributedDamage;
            await ctx.ExecuteInCreatureContextAsync(caster, pulse);
            var second = Ability.GetLastCompletedAbilityImpactSummary(caster).AttributedDamage;
            ctx.Assert(first > second && second > 0, "Only the first impacting pulse uses the captured damage");
            ctx.AssertEqual(101, Combat.ConsumeNextSkillAbilityBonuses(caster, SkillType.Force).DamageBonus,
                "Pulses preserve bonuses granted after the originating cast");
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

                TemporaryStatModifier.Add(caster, StatType.FirstHostileAbilityHitDamageBonus, 75, 120f);
                TemporaryStatModifier.Add(caster, StatType.FirstHostileAbilityHitMaximumCount, 1, 120f);
                TemporaryStatModifier.Add(caster, StatType.FirstHostileAbilityHitCooldownSeconds, 90, 120f);
                var choke = Ability.GetAbilityDetail(FeatType.ForceChoke1);
                ctx.Assert(choke.DealsDeferredDamage, "Choke declares its damaging status effect");
                ctx.AssertEqual(75, Combat.GetAbilityImpactBaseDamageBonus(caster, targets[0], choke, SkillType.Force),
                    "First Strike is initially available for Choke");
                Ability.BeginAbilityImpact(caster, choke);
                try
                {
                    await ctx.ExecuteInCreatureContextAsync(caster,
                        () => choke.ImpactAction(caster, targets[0], 1, GetLocation(targets[0])));
                }
                finally { Ability.EndAbilityImpact(caster); }
                ctx.Assert(Ability.GetLastCompletedAbilityImpactSummary(caster).AttributedDamage > 0,
                    "Choke applies the hostile-hit damage bonus despite having no immediate base damage");
                ctx.AssertEqual(0, Combat.GetAbilityImpactBaseDamageBonus(caster, targets[0], choke, SkillType.Force),
                    "Choke consumes the applied First Strike stack");
                Combat.GrantNextSkillAbilityBonuses(caster, (int)SkillType.Force, 75, 0, 120);
                Ability.BeginAbilityImpact(caster, choke);
                try
                {
                    await ctx.ExecuteInCreatureContextAsync(caster,
                        () => choke.ImpactAction(caster, targets[0], 1, GetLocation(targets[0])));
                }
                finally { Ability.EndAbilityImpact(caster); }
                ctx.Assert(Ability.GetLastCompletedAbilityImpactSummary(caster).AttributedDamage > 0,
                    "Choke applies captured flat damage even without a separate base-damage rider");
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
