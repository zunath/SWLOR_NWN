using System;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class MeleeTechniqueEngineTests
    {
        [EngineTest("Opening sweep captures one secondary without transferring setup accuracy", Category = "MeleeTechnique", TimeoutSeconds = 25f)]
        public static async Task OpeningSweepSelection(EngineTestContext ctx)
        {
            var (caster, target) = await CreateFixture(ctx);
            var secondary = CreateSweepEnemy(ctx, 1f, 1f);
            var excluded = CreateSweepEnemy(ctx, 2f, 2f);
            ctx.SetNPCPerkLevel(caster, PerkType.Jab, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.MeasuredTechnique, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.PracticedForm, 3);
            Combat.SetAbilityHitResolutionOverride(true);
            Combat.SetAutoAttackHitResolutionOverride(false);
            try
            {
                await Cast(ctx, caster, target, FeatType.Jab1);
                var ability = Ability.GetAbilityDetail(FeatType.FinishingCombination2);
                ctx.AssertEqual(MeleeTechniqueStage.Opening, MeleeTechnique.GetSetupStage(caster, target),
                    "the landed Jab supplies the Opening used by this selection check");
                // Native LineOfSightVector requires a creature context in the tested area,
                // matching the real activation path rather than the test runner's context.
                await ctx.ExecuteInCreatureContextAsync(caster, () =>
                    MeleeTechnique.BeginPaidActivation(caster, target, ability));
                ctx.AssertEqual(target, MeleeTechnique.GetImpactTarget(caster, target, 0), "first impact keeps the selected enemy");
                ctx.AssertEqual(secondary, MeleeTechnique.GetImpactTarget(caster, target, 1), "only the nearest other enemy receives the second impact");
                ctx.AssertEqual(10, MeleeTechnique.GetConditionalPower(caster, target, 0), "half the Opening and Practiced Form budget belongs to the primary");
                ctx.AssertEqual(10, MeleeTechnique.GetConditionalPower(caster, secondary, 1), "the other half belongs to the secondary");
                ctx.AssertEqual(10, MeleeTechnique.GetAbilityAccuracy(caster, target, ability), "the primary retains Measured Technique accuracy");
                ctx.AssertEqual(0, MeleeTechnique.GetAbilityAccuracy(caster, secondary, ability), "the secondary has no earned setup accuracy");
                ctx.Assert(!MeleeTechnique.IsImpactValid(caster, excluded, ability), "a third enemy cannot join the captured sweep");
                Ability.BeginAbilityImpact(caster, ability);
                ctx.Assert(Ability.IsAreaImpact(caster, ability), "classification is area before either hit resolves");
                ctx.Assert(!Ability.IsSingleTargetImpact(caster, ability), "single-target bonuses cannot also apply");
                ctx.Assert(!Ability.IsAreaImpact(excluded, ability), "another actor cannot inherit this cast's area classification");
                ctx.Assert(!ability.IsAreaAbility && ability.IsSingleTargetAbility, "shared targeting metadata stays unchanged");
                Ability.EndAbilityImpact(caster);
            }
            finally
            {
                MeleeTechnique.AbortActivation(caster);
                ResetOverrides(caster);
            }
        }

        [EngineTest("Opening sweep hits two enemies and grants cross-skill area rewards once", Category = "MeleeTechnique", TimeoutSeconds = 30f)]
        public static Task OpeningSweepRewards(EngineTestContext ctx) => VerifyFinisherBranch(ctx, MeleeTechniqueStage.Opening, true);

        [EngineTest("Opening finisher without a second enemy retains focused damage and no area rewards", Category = "MeleeTechnique", TimeoutSeconds = 30f)]
        public static Task OpeningSweepFallback(EngineTestContext ctx) => VerifyFinisherBranch(ctx, MeleeTechniqueStage.Opening, false);

        [EngineTest("Full combo remains focused even with a nearby second enemy", Category = "MeleeTechnique", TimeoutSeconds = 30f)]
        public static Task FullComboDoesNotSweep(EngineTestContext ctx) => VerifyFinisherBranch(ctx, MeleeTechniqueStage.Finish, true);

        [EngineTest("Unsequenced finisher remains focused and cannot trigger area rewards", Category = "MeleeTechnique", TimeoutSeconds = 30f)]
        public static Task UnsequencedFinisherDoesNotSweep(EngineTestContext ctx) => VerifyFinisherBranch(ctx, MeleeTechniqueStage.None, true);

        private static async Task VerifyFinisherBranch(EngineTestContext ctx, MeleeTechniqueStage stage, bool hasSecondary)
        {
            var (caster, target) = await CreateFixture(ctx);
            var secondary = hasSecondary ? CreateSweepEnemy(ctx, 1f, 1f) : OBJECT_INVALID;
            var excluded = CreateSweepEnemy(ctx, 12f, 0f);
            foreach (var perk in new[] { PerkType.Jab, PerkType.PalmStrike, PerkType.FlowStance,
                         PerkType.ControlledBreathing, PerkType.Momentum, PerkType.FlowingFootwork, PerkType.SweepingAdvance })
                ctx.SetNPCPerkLevel(caster, perk, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.FinishingCombination, 2);
            ctx.SetNPCPerkLevel(caster, PerkType.SpinningRhythm, 3);
            Combat.SetAbilityHitResolutionOverride(true);
            Combat.SetAutoAttackHitResolutionOverride(false);
            try
            {
                await Cast(ctx, caster, caster, FeatType.FlowStance1);
                var evasion = Stat.GetStatAdjustment(caster, StatType.EvasionPercentAdjustment);
                var haste = Stat.GetStatAdjustment(caster, StatType.AttackDelayReductionPercent);
                if (stage != MeleeTechniqueStage.None)
                {
                    await Cast(ctx, caster, target, FeatType.Jab1);
                    await ctx.DelaySecondsAsync(1.05f);
                    if (stage == MeleeTechniqueStage.Finish)
                    {
                        await Cast(ctx, caster, target, FeatType.PalmStrike1);
                        await ctx.DelaySecondsAsync(1.05f);
                    }
                }
                var stamina = Stat.GetCurrentStamina(caster);
                var primaryHP = GetCurrentHitPoints(target);
                var secondaryHP = hasSecondary ? GetCurrentHitPoints(secondary) : 0;
                var excludedHP = GetCurrentHitPoints(excluded);
                await Cast(ctx, caster, target, FeatType.FinishingCombination2);
                var summary = Ability.GetLastCompletedAbilityImpactSummary(caster);
                var sweep = stage == MeleeTechniqueStage.Opening && hasSecondary;
                ctx.AssertEqual(sweep, summary.IsAreaAbility, "only a captured Opening sweep counts as area");
                ctx.AssertEqual(!sweep, summary.IsSingleTargetAbility, "the focused routes keep their original classification");
                ctx.AssertEqual(sweep ? 2 : 1, summary.ImpactedTargetCount, "two impacts never become three targets");
                ctx.Assert(GetCurrentHitPoints(target) < primaryHP, "the selected enemy receives damage");
                if (hasSecondary)
                    ctx.AssertEqual(sweep, GetCurrentHitPoints(secondary) < secondaryHP, "only the sweep damages the secondary");
                ctx.AssertEqual(excludedHP, GetCurrentHitPoints(excluded), "an enemy outside the sweep radius is untouched");
                ctx.AssertEqual(stamina - 9 + (stage == MeleeTechniqueStage.None ? 0 : 2) + (sweep ? 4 : 0),
                    Stat.GetCurrentStamina(caster), "Controlled Breathing refunds once and area recovery counts only the two hit targets");
                ctx.AssertEqual(evasion + (stage == MeleeTechniqueStage.Opening ? 20 : 0) + (sweep ? 10 : 0),
                    Stat.GetStatAdjustment(caster, StatType.EvasionPercentAdjustment), "Flowing Footwork applies only to the sweep");
                ctx.AssertEqual(haste + (stage == MeleeTechniqueStage.Finish ? 15 : 0) + (sweep ? 9 : 0),
                    Stat.GetStatAdjustment(caster, StatType.AttackDelayReductionPercent), "two landed targets grant one stack from each area haste source");
                if (stage == MeleeTechniqueStage.Opening)
                {
                    MeleeTechnique.PrepareOrdinaryAttempt(caster, target, SkillType.Unarmed, false, 0);
                    ctx.Assert(StatusEffect.HasStatusEffect(caster, typeof(SlipAwayStatusEffect)), "the selected primary still anchors Flow after the secondary impact");
                }
            }
            finally { ResetOverrides(caster); }
        }

        private static uint CreateSweepEnemy(EngineTestContext ctx, float x, float y)
        {
            var target = ctx.SpawnCreature("nw_rat001", x, y);
            ctx.SuppressNPCNaturalRegen(target);
            Stat.SetNPCMaxHitPoints(target, 20000, true);
            SetAILevel(target, AILevel.VeryLow);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 60f);
            TemporaryStatModifier.Add(target, StatType.MeleeDeflection, -100, 60f);
            ctx.MakeHostile(target);
            return target;
        }

        [EngineTest("Unarmed first purchases form a paid sequence without later traits", Category = "MeleeTechnique", TimeoutSeconds = 35f)]
        public static async Task FirstPurchaseSequence(EngineTestContext ctx)
        {
            var (caster, target) = await CreateFixture(ctx);
            ctx.SetNPCPerkLevel(caster, PerkType.Jab, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.PalmStrike, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.FinishingCombination, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.ControlledBreathing, 1);
            var spent = 0;
            void OnSpent(uint source, uint enemy, AbilityDetail ability, MeleeTechniqueStage stage)
            {
                if (source == caster && enemy == target && stage != MeleeTechniqueStage.None)
                    spent++;
            }
            MeleeTechnique.TechniqueLanded += OnSpent;
            Combat.SetAbilityHitResolutionOverride(true);
            Combat.SetAutoAttackHitResolutionOverride(false);
            try
            {
                var start = Stat.GetCurrentStamina(caster);
                await Cast(ctx, caster, target, FeatType.Jab1);
                ctx.AssertEqual(MeleeTechniqueStage.Opening, MeleeTechnique.GetSetupStage(caster, target), "first-rank Jab creates Opening");
                ctx.AssertEqual(0, MeleeTechnique.GetPendingWeavePower(caster, target), "no weave without Flowing Form");
                ctx.Assert(!Ability.CanUseAbility(caster, target, FeatType.PalmStrike1, 1, GetLocation(target)), "one-second action lock gates the next technique");
                ctx.Assert(!Ability.CanUseAbility(caster, target, FeatType.DirtyJab1, 1, GetLocation(target)), "the other opener shares the recast");
                await ctx.DelaySecondsAsync(1.05f);
                await Cast(ctx, caster, target, FeatType.PalmStrike1);
                ctx.AssertEqual(MeleeTechniqueStage.Finish, MeleeTechnique.GetSetupStage(caster, target), "first-rank Palm creates Finish");
                await ctx.DelaySecondsAsync(1.05f);
                await Cast(ctx, caster, target, FeatType.FinishingCombination1);
                ctx.AssertEqual(MeleeTechniqueStage.None, MeleeTechnique.GetSetupStage(caster, target), "finisher spends setup");
                ctx.AssertEqual(start - 3 - 4 - 6 + 2, Stat.GetCurrentStamina(caster), "two-hit finisher restores once from its actual payment");
                ctx.AssertEqual(2, spent, "bridge and finisher each emit one paid mastery event");
            }
            finally
            {
                MeleeTechnique.TechniqueLanded -= OnSpent;
                ResetOverrides(caster);
            }
        }

        [EngineTest("Missed ordinary Unarmed, melee and ranged rolls each consume one weave without spending Opening", Category = "MeleeTechnique", TimeoutSeconds = 60f)]
        public static async Task NativeMissedWeave(EngineTestContext ctx)
        {
            foreach (var weapon in new string[] { null, "b_longsword", "b_pistol" })
            {
                var (caster, target) = await CreateFixture(ctx);
                ctx.SetNPCPerkLevel(caster, PerkType.Jab, 4);
                ctx.SetNPCPerkLevel(caster, PerkType.FlowingForm, 1);
                ctx.SetNPCPerkLevel(caster, PerkType.PalmStrike, 1);
                Combat.SetAbilityHitResolutionOverride(true);
                Combat.SetAutoAttackHitResolutionOverride(false);
                try
                {
                    await Cast(ctx, caster, target, FeatType.Jab4);
                    ctx.AssertEqual(16, MeleeTechnique.GetPendingWeavePower(caster, target), "rank-four opener arms exactly one weave");
                    if (weapon != null)
                        await ctx.EquipItemAsync(caster, weapon, InventorySlot.RightHand);
                    ctx.AssertEqual(MeleeTechniqueStage.Opening, MeleeTechnique.GetSetupStage(caster, target), "the native roll begins with retained same-target setup");
                    var hitPoints = GetCurrentHitPoints(target);
                    await ctx.ExecuteInCreatureContextAsync(caster, () => ActionAttack(target));
                    await ctx.WaitUntilAsync(() => MeleeTechnique.GetPendingWeavePower(caster, target) == 0, 4.5f, "a real missed native ordinary roll to spend its weave");
                    ctx.AssertEqual(MeleeTechniqueStage.Opening, MeleeTechnique.GetSetupStage(caster, target), "miss does not consume Opening");
                    ctx.AssertEqual(hitPoints, GetCurrentHitPoints(target), "missed weaving does not create a free hit");
                    if (weapon == null)
                    {
                        await Cast(ctx, caster, target, FeatType.PalmStrike1);
                        ctx.AssertEqual(MeleeTechniqueStage.Finish, MeleeTechnique.GetSetupStage(caster, target), "bridge works after the missed weave");
                    }
                }
                finally { ResetOverrides(caster); }
            }
        }

        [EngineTest("Rattled is caster-owned and multiple brawlers apply the strongest ACC penalty", Category = "MeleeTechnique", TimeoutSeconds = 35f)]
        public static async Task CasterOwnedDistraction(EngineTestContext ctx)
        {
            var (first, target) = await CreateFixture(ctx);
            var second = ctx.SpawnCreature("nw_bandit001", -1f, 1f);
            await ctx.DelaySecondsAsync(1f);
            await ctx.EquipItemAsync(second, "t_pglove", InventorySlot.RightHand);
            ClearUnarmedPassiveTraits(ctx, second);
            ctx.SuppressNPCNaturalRegen(second);
            ctx.SetNPCResources(second, 100, 100);
            SetAILevel(second, AILevel.VeryLow);
            foreach (var caster in new[] { first, second })
            {
                ctx.SetNPCPerkLevel(caster, PerkType.DirtyJab, 1);
                ctx.SetNPCPerkLevel(caster, PerkType.DirtyFighting, 1);
                ctx.SetNPCPerkLevel(caster, PerkType.Uppercut, 1);
            }
            Combat.SetAbilityHitResolutionOverride(true);
            Combat.SetAutoAttackHitResolutionOverride(false);
            try
            {
                await Cast(ctx, first, target, FeatType.DirtyJab1);
                await Cast(ctx, second, target, FeatType.DirtyJab1);
                ctx.Assert(StatusEffect.HasStatusEffect(target, typeof(RattledStatusEffect), first), "first source owns Rattled");
                ctx.Assert(StatusEffect.HasStatusEffect(target, typeof(RattledStatusEffect), second), "second source owns Rattled");
                ctx.AssertEqual(10, Stat.GetStatAdjustment(target, StatType.OrdinaryWeaponAccuracyPenalty), "two sources do not stack to twenty");
                await ctx.DelaySecondsAsync(1.05f);
                await Cast(ctx, first, target, FeatType.Uppercut1);
                ctx.Assert(StatusEffect.HasStatusEffect(target, typeof(RattledStatusEffect), first), "Uppercut preserves its caster's independent pressure debuff");
                ctx.Assert(StatusEffect.HasStatusEffect(target, typeof(RattledStatusEffect), second), "other brawler's source remains");
                ctx.AssertEqual(MeleeTechniqueStage.Distraction, MeleeTechnique.GetSetupStage(second, target), "the other brawler retains its pressure target");
                ctx.AssertEqual(10, Stat.GetStatAdjustment(target, StatType.OrdinaryWeaponAccuracyPenalty), "remaining source preserves its penalty");
                await ctx.EquipItemAsync(second, "b_pistol", InventorySlot.RightHand);
                ctx.AssertEqual(MeleeTechniqueStage.Distraction, MeleeTechnique.GetSetupStage(second, target), "a ranged weapon swap retains the other owner's Rattled setup");
                ctx.Assert(StatusEffect.HasStatusEffect(target, typeof(RattledStatusEffect), second), "weapon swaps retain the caster-owned debuff");
                await ctx.WaitUntilAsync(() => !StatusEffect.HasStatusEffect(target, typeof(RattledStatusEffect), second), 9f, "the original Rattled duration to expire without refreshing on swap");
                ctx.AssertEqual(MeleeTechniqueStage.None, MeleeTechnique.GetSetupStage(second, target), "expired Rattled cannot remain banked after a swap");
            }
            finally
            {
                ResetOverrides(first);
                AssignCommand(second, () => ClearAllActions());
            }
        }

        [EngineTest("A missed heavy strike pays its cost without consuming independent pressure", Category = "MeleeTechnique", TimeoutSeconds = 30f)]
        public static async Task MissedSpenderConsumesSetup(EngineTestContext ctx)
        {
            var (caster, target) = await CreateFixture(ctx);
            ctx.SetNPCPerkLevel(caster, PerkType.DirtyJab, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.Uppercut, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.CatchYourBreath, 1);
            Combat.SetAbilityHitResolutionOverride(true);
            Combat.SetAutoAttackHitResolutionOverride(false);
            try
            {
                await Cast(ctx, caster, target, FeatType.DirtyJab1);
                await ctx.DelaySecondsAsync(1.05f);
                var stamina = Stat.GetCurrentStamina(caster);
                Combat.SetAbilityHitResolutionOverride(false);
                await Cast(ctx, caster, target, FeatType.Uppercut1);
                ctx.AssertEqual(MeleeTechniqueStage.Distraction, MeleeTechnique.GetSetupStage(caster, target), "a missed heavy strike preserves Rattled");
                ctx.Assert(StatusEffect.HasStatusEffect(target, typeof(RattledStatusEffect), caster), "the independent weakening remains after a missed heavy strike");
                ctx.AssertEqual(stamina - 4, Stat.GetCurrentStamina(caster), "miss pays full cost without a landed-hit refund");
            }
            finally { ResetOverrides(caster); }
        }

        [EngineTest("Payback works without gloves and discounts one nonhostile ability from another skill", Category = "MeleeTechnique", TimeoutSeconds = 35f)]
        public static async Task DirectDamageDiscount(EngineTestContext ctx)
        {
            var (caster, attacker) = await CreateFixture(ctx);
            ctx.SetNPCPerkLevel(caster, PerkType.Payback, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.BloodFrenzy, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.Provoke, 1);
            await ctx.EquipItemAsync(caster, "b_longsword", InventorySlot.RightHand);
            ctx.Assert(!EquipmentPredicates.HasUnarmedGrip(caster), "Payback begins without an Unarmed grip");
            var ability = Ability.GetAbilityDetail(FeatType.BloodFrenzyBurst);
            var cost = ability.Requirements.OfType<AbilityRequirementStamina>().Single().RequiredSTM;
            await ctx.ExecuteInCreatureContextAsync(attacker, () =>
            {
                StatusEffect.NotifyPreDamageStatusEffects(attacker, caster, 5, CombatDamageType.Physical, CombatDamageDeliveryType.DamageOverTime);
                ApplyEffectToObject(DurationType.Instant, EffectDamage(5), caster);
            });
            ctx.AssertEqual(0, MeleeTechnique.GetStaminaDiscount(caster, ability, 3, false), "periodic damage cannot charge Payback");
            await ctx.ExecuteInCreatureContextAsync(attacker, () => Combat.ApplyTriggeredDamage(attacker, caster, 5, CombatDamageType.Physical));
            ctx.AssertEqual(0, MeleeTechnique.GetStaminaDiscount(caster, ability, 3, false), "triggered and reflected damage cannot charge Payback");
            await ctx.ExecuteInCreatureContextAsync(caster, () =>
            {
                StatusEffect.NotifyPreDamageStatusEffects(caster, caster, 5, CombatDamageType.Physical);
                ApplyEffectToObject(DurationType.Instant, EffectDamage(5), caster);
            });
            ctx.AssertEqual(0, MeleeTechnique.GetStaminaDiscount(caster, ability, 3, false), "self damage cannot charge Payback");
            var baselineHP = GetCurrentHitPoints(caster);
            ctx.Log($"NATIVE_TEMP_HP Payback baseline HP={baselineHP}, temporary={TemporaryHitPointEffects.GetRemaining(caster)}");
            TemporaryHitPointEffects.ApplyFlat(caster, "ENGINE_TECHNIQUE_SHIELD", 20, 20f);
            ctx.Log($"NATIVE_TEMP_HP Payback after grant20 HP={GetCurrentHitPoints(caster)}, temporary={TemporaryHitPointEffects.GetRemaining(caster)}");
            ctx.AssertEqual(baselineHP + 20, GetCurrentHitPoints(caster), "native GetCurrentHitPoints includes the granted temporary pool");
            ctx.AssertEqual(20, TemporaryHitPointEffects.GetRemaining(caster), "native temporary pool starts at twenty");
            await ctx.ExecuteInCreatureContextAsync(attacker, () =>
            {
                StatusEffect.NotifyPreDamageStatusEffects(attacker, caster, 5, CombatDamageType.Physical);
                ApplyEffectToObject(DurationType.Instant, EffectDamage(5), caster);
            });
            ctx.Log($"NATIVE_TEMP_HP Payback after absorbed5 HP={GetCurrentHitPoints(caster)}, temporary={TemporaryHitPointEffects.GetRemaining(caster)}");
            ctx.AssertEqual(baselineHP + 15, GetCurrentHitPoints(caster), "absorbed damage reduces total native HP");
            ctx.AssertEqual(15, TemporaryHitPointEffects.GetRemaining(caster), "absorbed damage reduces the native temporary pool");
            ctx.AssertEqual(0, MeleeTechnique.GetStaminaDiscount(caster, ability, 3, false), "temporary HP absorption is not HP damage");
            await ctx.ExecuteInCreatureContextAsync(attacker, () =>
            {
                StatusEffect.NotifyPreDamageStatusEffects(attacker, caster, 25, CombatDamageType.Physical);
                ApplyEffectToObject(DurationType.Instant, EffectDamage(25), caster);
            });
            ctx.Log($"NATIVE_TEMP_HP Payback after spillover25 HP={GetCurrentHitPoints(caster)}, temporary={TemporaryHitPointEffects.GetRemaining(caster)}");
            ctx.AssertEqual(baselineHP - 10, GetCurrentHitPoints(caster), "twenty-five damage consumes the remaining fifteen temporary HP and ten real HP");
            ctx.AssertEqual(0, TemporaryHitPointEffects.GetRemaining(caster), "spillover consumes the temporary pool");
            await ctx.WaitUntilAsync(() => MeleeTechnique.GetStaminaDiscount(caster, ability, 3, false) == 2, 3f, "real direct hostile HP damage to charge Payback");
            await ctx.EquipItemAsync(caster, "b_pistol", InventorySlot.RightHand);
            ctx.AssertEqual(2, MeleeTechnique.GetStaminaDiscount(caster, ability, 3, false), "a melee-to-ranged swap preserves the independent Payback charge");
            var stamina = Stat.GetCurrentStamina(caster);
            Combat.SetAbilityHitResolutionOverride(true);
            Combat.SetAutoAttackHitResolutionOverride(false);
            try
            {
                await Cast(ctx, caster, attacker, FeatType.Provoke1);
                ctx.AssertEqual(stamina, Stat.GetCurrentStamina(caster), "a zero-stamina action still costs zero");
                ctx.AssertEqual(2, MeleeTechnique.GetStaminaDiscount(caster, ability, cost, false), "a zero-stamina action does not consume Payback");
                await Cast(ctx, caster, caster, FeatType.BloodFrenzyBurst);
                ctx.AssertEqual(stamina - cost + 2, Stat.GetCurrentStamina(caster), "the nonhostile Vibroblade activation receives exactly one two-stamina discount");
                ctx.AssertEqual(0, MeleeTechnique.GetStaminaDiscount(caster, ability, 3, false), "paid activation consumes charge");
                await ctx.ExecuteInCreatureContextAsync(attacker, () =>
                {
                    StatusEffect.NotifyPreDamageStatusEffects(attacker, caster, 5, CombatDamageType.Physical);
                    ApplyEffectToObject(DurationType.Instant, EffectDamage(5), caster);
                });
                ctx.AssertEqual(0, MeleeTechnique.GetStaminaDiscount(caster, ability, cost, false), "another hit cannot bypass the preserved eighteen-second trigger gate");
            }
            finally { ResetOverrides(caster); }
        }

        [EngineTest("Both character types use Unarmed and its stances and resistances work with other weapons", Category = "MeleeTechnique", TimeoutSeconds = 50f)]
        public static async Task CharacterTypesAndStanceEquipment(EngineTestContext ctx)
        {
            using var standard = await PlayerAbilityFixture.CreateAsync(ctx, -0.5f);
            using var sensitive = await PlayerAbilityFixture.CreateAsync(ctx, -1f);
            var target = ctx.SpawnCreature("nw_rat001", 1f);
            await ctx.DelaySecondsAsync(1f);
            ctx.MakeHostile(target);
            Stat.SetNPCMaxHitPoints(target, 20000, true);
            SetAILevel(target, AILevel.VeryLow);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 60f);
            TemporaryStatModifier.Add(target, StatType.MeleeDeflection, -100, 60f);
            standard.Update(record => record.CharacterType = CharacterType.Standard);
            sensitive.Update(record => record.CharacterType = CharacterType.ForceSensitive);
            foreach (var player in new[] { standard, sensitive })
            {
                player.Update(record =>
                {
                    record.Skills[SkillType.Unarmed].Rank = 50;
                    record.Skills[SkillType.Armor].Rank = 40;
                    record.Skills[SkillType.Vibroblade].Rank = 50;
                    record.Perks[PerkType.BruiserStance] = 1;
                    record.Perks[PerkType.RootedTechnique] = 1;
                    record.Perks[PerkType.HardKnocks] = 1;
                    record.Perks[PerkType.Jab] = 1;
                    record.Perks[PerkType.FlowStance] = 1;
                });
                await ctx.EquipItemAsync(player.Creature, "t_pglove", InventorySlot.RightHand);
            }
            Combat.SetAbilityHitResolutionOverride(true);
            Combat.SetAutoAttackHitResolutionOverride(false);
            try
            {
                foreach (var player in new[] { standard, sensitive })
                {
                    var fp = Stat.GetCurrentFP(player.Creature);
                    await Cast(ctx, player.Creature, target, FeatType.Jab1);
                    ctx.AssertEqual(MeleeTechniqueStage.Opening, MeleeTechnique.GetSetupStage(player.Creature, target), "character type can establish Opening");
                    ctx.AssertEqual(fp, Stat.GetCurrentFP(player.Creature), "Unarmed never charges FP");
                }
                await Cast(ctx, standard.Creature, standard.Creature, FeatType.FlowStance1);
                ctx.Assert(StatusEffect.HasStatusEffect(standard.Creature, typeof(FlowStanceStatusEffect)), "valid grip activates Flow Stance");
                await ctx.EquipItemAsync(standard.Creature, "ec_shield", InventorySlot.LeftHand);
                ctx.Assert(!EquipmentPredicates.HasUnarmedGrip(standard.Creature), "shield fails full Unarmed grip");
                ctx.Assert(StatusEffect.HasStatusEffect(standard.Creature, typeof(FlowStanceStatusEffect)), "the actual shield equip event retains Flow Stance");
                ctx.AssertEqual(MeleeTechniqueStage.None, MeleeTechnique.GetSetupStage(standard.Creature, target), "activating the stance resets the earlier setup");
                await ctx.EquipItemAsync(standard.Creature, "b_longsword", InventorySlot.RightHand);
                ctx.AssertEqual(15, Stat.GetStatAdjustment(standard.Creature, StatType.MobilityResistance), "Rooted Technique remains unconditional with a sword and shield");
                ctx.AssertEqual(15, Stat.GetStatAdjustment(standard.Creature, StatType.TraumaResistance), "Hard Knocks remains unconditional with a sword and shield");
                ctx.Assert(!string.IsNullOrEmpty(MeleeTechnique.ValidateActivation(standard.Creature, Ability.GetAbilityDetail(FeatType.Jab1), target)), "active fist strikes still require Power Gloves");
                await Cast(ctx, standard.Creature, standard.Creature, FeatType.BruiserStance1);
                ctx.Assert(StatusEffect.HasStatusEffect(standard.Creature, typeof(BruiserStanceStatusEffect)), "Bruiser Stance activates with another weapon class");
                ctx.Assert(!StatusEffect.HasStatusEffect(standard.Creature, typeof(FlowStanceStatusEffect)), "changing stances still removes the previous stance");
                Recast.ReduceRecastDelay(standard.Creature, RecastGroup.FlowStance, 100f);
                await Cast(ctx, standard.Creature, standard.Creature, FeatType.FlowStance1);
                ctx.Assert(StatusEffect.HasStatusEffect(standard.Creature, typeof(FlowStanceStatusEffect)), "Flow Stance activates without Power Gloves");
            }
            finally
            {
                ResetOverrides(standard.Creature);
                AssignCommand(sensitive.Creature, () => ClearAllActions());
            }
        }

        [EngineTest("Flow Stance rewards short and full finishers with mutually exclusive windows", Category = "MeleeTechnique", TimeoutSeconds = 45f)]
        public static async Task ReactiveFinisherWindows(EngineTestContext ctx)
        {
            Combat.SetAbilityHitResolutionOverride(true);
            Combat.SetAutoAttackHitResolutionOverride(false);
            try
            {
                foreach (var fullSequence in new[] { false, true })
                {
                    // Module equip events belong to players, so the swap regression needs native player identity.
                    using var player = await PlayerAbilityFixture.CreateAsync(ctx, -0.5f);
                    var caster = player.Creature;
                    var target = ctx.SpawnCreature("nw_rat001", 1f);
                    await ctx.DelaySecondsAsync(1f);
                    ctx.SuppressNPCNaturalRegen(target);
                    Stat.SetNPCMaxHitPoints(target, 20000, true);
                    SetAILevel(target, AILevel.VeryLow);
                    ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 60f);
                    TemporaryStatModifier.Add(target, StatType.MeleeDeflection, -100, 60f);
                    ctx.MakeHostile(target);
                    player.Update(record =>
                    {
                        record.Skills[SkillType.Unarmed].Rank = 50;
                        record.Perks[PerkType.Jab] = 1;
                        record.Perks[PerkType.PalmStrike] = 1;
                        record.Perks[PerkType.FinishingCombination] = 1;
                        record.Perks[PerkType.FlowStance] = 1;
                        record.Skills[SkillType.Pistol].Rank = 50;
                        record.Perks[PerkType.QuickDraw] = 1;
                    });
                    await ctx.EquipItemAsync(caster, "t_pglove", InventorySlot.RightHand);
                    await Cast(ctx, caster, caster, FeatType.FlowStance1);
                    var evasion = Stat.GetStatAdjustment(caster, StatType.EvasionPercentAdjustment);
                    var haste = Stat.GetStatAdjustment(caster, StatType.AttackDelayReductionPercent);
                    var movement = Stat.GetStatAdjustment(caster, StatType.MovementSpeedPercentAdjustment);
                    await Cast(ctx, caster, target, FeatType.Jab1);
                    await ctx.DelaySecondsAsync(1.05f);
                    if (fullSequence)
                    {
                        await Cast(ctx, caster, target, FeatType.PalmStrike1);
                        await ctx.DelaySecondsAsync(1.05f);
                    }
                    await Cast(ctx, caster, target, FeatType.FinishingCombination1);
                    ctx.AssertEqual(!fullSequence, StatusEffect.HasStatusEffect(caster, typeof(SlipAwayStatusEffect)), "short route alone grants Slip Away");
                    ctx.AssertEqual(fullSequence, StatusEffect.HasStatusEffect(caster, typeof(PressForwardStatusEffect)), "full route alone grants Press Forward");
                    ctx.AssertEqual(evasion + (fullSequence ? 0 : 20), Stat.GetStatAdjustment(caster, StatType.EvasionPercentAdjustment), "evasion window budget");
                    ctx.AssertEqual(movement + (fullSequence ? 0 : 15), Stat.GetStatAdjustment(caster, StatType.MovementSpeedPercentAdjustment), "movement window budget");
                    ctx.AssertEqual(haste + (fullSequence ? 15 : 0), Stat.GetStatAdjustment(caster, StatType.AttackDelayReductionPercent), "haste window budget");
                    var windowGrantedAt = DateTime.UtcNow;
                    await ctx.EquipItemAsync(caster, "b_pistol", InventorySlot.RightHand);
                    ctx.Assert(!EquipmentPredicates.HasUnarmedGrip(caster), "the window regression swaps to a ranged weapon");
                    ctx.Assert(StatusEffect.HasStatusEffect(caster, typeof(FlowStanceStatusEffect)), "weapon changes retain the stance itself");
                    ctx.AssertEqual(!fullSequence, StatusEffect.HasStatusEffect(caster, typeof(SlipAwayStatusEffect)), "swapping retains the earned evasion window");
                    ctx.AssertEqual(fullSequence, StatusEffect.HasStatusEffect(caster, typeof(PressForwardStatusEffect)), "swapping retains the earned haste window");
                    await Cast(ctx, caster, target, FeatType.QuickDraw1);
                    ctx.AssertEqual(!fullSequence, StatusEffect.HasStatusEffect(caster, typeof(SlipAwayStatusEffect)), "another skill's same-target cast retains Slip Away");
                    ctx.AssertEqual(fullSequence, StatusEffect.HasStatusEffect(caster, typeof(PressForwardStatusEffect)), "another skill's same-target cast retains Press Forward");
                    ctx.AssertEqual(evasion + (fullSequence ? 0 : 20), Stat.GetStatAdjustment(caster, StatType.EvasionPercentAdjustment), "the ranged weapon and its real skill retain the earned evasion magnitude");
                    ctx.AssertEqual(haste + (fullSequence ? 15 : 0), Stat.GetStatAdjustment(caster, StatType.AttackDelayReductionPercent), "the ranged weapon and its real skill retain the earned haste magnitude");
                    if (!fullSequence)
                    {
                        await ctx.WaitUntilAsync(() => !StatusEffect.HasStatusEffect(caster, typeof(SlipAwayStatusEffect)), 10f, "the original eight-second window to expire despite the swap and other skill's cast");
                        ctx.Assert(DateTime.UtcNow < windowGrantedAt.AddSeconds(10), "swaps and unrelated abilities cannot refresh the original window expiry");
                        await ctx.EquipItemAsync(caster, "t_pglove", InventorySlot.RightHand);
                        await RepeatShortFinisher();
                        ctx.Assert(!StatusEffect.HasStatusEffect(caster, typeof(SlipAwayStatusEffect)), "a fresh paid finisher cannot bypass the retained twelve-second trigger gate");
                        var remaining = (float)(windowGrantedAt.AddSeconds(12.1) - DateTime.UtcNow).TotalSeconds;
                        if (remaining > 0)
                            await ctx.DelaySecondsAsync(remaining);
                        await RepeatShortFinisher();
                        ctx.Assert(StatusEffect.HasStatusEffect(caster, typeof(SlipAwayStatusEffect)), "the same trigger reopens after its original twelve-second gate");
                    }
                    StatusEffect.RemoveStatusEffect(caster, typeof(FlowStanceStatusEffect), false);
                    ctx.Assert(!StatusEffect.HasStatusEffect(caster, typeof(SlipAwayStatusEffect)) &&
                               !StatusEffect.HasStatusEffect(caster, typeof(PressForwardStatusEffect)), "stance removal immediately clears either window");
                    AssignCommand(caster, () => ClearAllActions());

                    async Task RepeatShortFinisher()
                    {
                        // Remove only ability recasts so this regression exercises the independent reaction gate.
                        Recast.ReduceRecastDelay(caster, RecastGroup.UnarmedOpener, 100f);
                        Recast.ReduceRecastDelay(caster, RecastGroup.UnarmedTechnique, 100f);
                        await ctx.DelaySecondsAsync(1.05f);
                        await Cast(ctx, caster, target, FeatType.Jab1);
                        await ctx.DelaySecondsAsync(1.05f);
                        await Cast(ctx, caster, target, FeatType.FinishingCombination1);
                    }
                }
            }
            finally { Combat.SetAbilityHitResolutionOverride(null); Combat.SetAutoAttackHitResolutionOverride(null); }
        }

        [EngineTest("The queued technique hook clears another enemy's setup and preserves same-target weave", Category = "MeleeTechnique", TimeoutSeconds = 30f)]
        public static async Task QueuedAttemptTargetOwnership(EngineTestContext ctx)
        {
            Combat.SetAbilityHitResolutionOverride(true);
            Combat.SetAutoAttackHitResolutionOverride(false);
            try
            {
                foreach (var sameTarget in new[] { false, true })
                {
                    var (caster, target) = await CreateFixture(ctx);
                    ctx.SetNPCPerkLevel(caster, PerkType.Jab, 4);
                    ctx.SetNPCPerkLevel(caster, PerkType.FlowingForm, 1);
                    ctx.SetNPCPerkLevel(caster, PerkType.SoulStrike, 1);
                    ctx.SetNPCPerkLevel(caster, PerkType.MeasuredTechnique, 1);
                    CreaturePlugin.AddFeat(caster, FeatType.SoulStrike1);
                    var deliveryTarget = sameTarget ? target : ctx.SpawnCreature("nw_rat001", 1f, 1f);
                    ctx.MakeHostile(deliveryTarget);
                    ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), deliveryTarget, 30f);
                    await ctx.ExecuteInCreatureContextAsync(caster, () =>
                        ctx.Assert(UsePerkFeat.TryUseAbility(caster, caster, FeatType.SoulStrike1, GetLocation(caster)), "Soul Strike queues while idle"));
                    ctx.Assert(UsePerkFeat.IsWeaponAbilityQueued(caster, FeatType.SoulStrike1), "queued delivery is pending before the opener");
                    await Cast(ctx, caster, target, FeatType.Jab4);
                    AssignCommand(caster, () => ClearAllActions());
                    ctx.AssertEqual(16, MeleeTechnique.GetPendingWeavePower(caster, target), "the landed opener arms an ordinary-attempt weave");
                    ctx.Assert(UsePerkFeat.IsWeaponAbilityQueued(caster, FeatType.SoulStrike1), "the casted opener leaves the older queued delivery pending");
                    ctx.Assert(UsePerkFeat.TryGetQueuedWeaponAbility(caster, SkillType.Unarmed, out var queuedAbility) &&
                               queuedAbility == Ability.GetAbilityDetail(FeatType.SoulStrike1),
                        "the native resolver accepts the owned cross-skill queue before taking the attack roll");
                    ctx.Assert(!Combat.HasRecentAttackActivity(caster, 60f), "the fixture has not taken a native attack roll yet");
                    var hitPoints = GetCurrentHitPoints(deliveryTarget);
                    const int attackIndex = 17;
                    await ctx.ExecuteInCreatureContextAsync(caster, () =>
                    {
                        // ResolveAttackRoll calls this boundary before resolving hits or misses.
                        // NativeMissedWeave separately covers delivery through a real missed native roll.
                        ctx.Assert(UsePerkFeat.TryGetQueuedWeaponAbility(caster, SkillType.Unarmed,
                            out var delivery, attackIndex), "the native hook's queued resolver accepts this attempt");
                        ctx.AssertEqual(0, MeleeTechnique.PrepareOrdinaryAttempt(caster, deliveryTarget,
                            SkillType.Unarmed, delivery != null, attackIndex),
                            "the queued attempt never receives ordinary accuracy");
                        ctx.AssertEqual(sameTarget ? 10 : 0, MeleeTechnique.GetAbilityAccuracy(caster, deliveryTarget, delivery),
                            "the queued Force ability gets setup accuracy only against the caster's own enemy");
                        ctx.AssertEqual(0, MeleeTechnique.TakeOrdinaryPower(caster, deliveryTarget,
                            SkillType.Unarmed, attackIndex, true),
                            "the excluded queued attempt cannot receive ordinary weave power even if it lands");
                    });
                    ctx.Assert(UsePerkFeat.TryGetQueuedWeaponAbility(caster, SkillType.Unarmed, out _),
                        "attempt validation preserves the valid queued ability for its eventual landed delivery");
                    ctx.AssertEqual(hitPoints, GetCurrentHitPoints(deliveryTarget), "attempt validation cannot create a hit");
                    ctx.AssertEqual(sameTarget ? MeleeTechniqueStage.Opening : MeleeTechniqueStage.None,
                        MeleeTechnique.GetSetupStage(caster, target), "queued target validation follows the hook's defender");
                    ctx.AssertEqual(sameTarget ? 16 : 0, MeleeTechnique.GetPendingWeavePower(caster, target),
                        "same-target queued attempts preserve the ordinary weave; target transfer clears it");
                    UsePerkFeat.ClearQueuedAbility(caster);
                }
            }
            finally { Combat.SetAbilityHitResolutionOverride(null); Combat.SetAutoAttackHitResolutionOverride(null); }
        }

        [EngineTest("Independent Bruiser retaliation requires hostile direct HP damage and survives weapon swaps", Category = "MeleeTechnique", TimeoutSeconds = 40f)]
        public static async Task ReactiveRetaliation(EngineTestContext ctx)
        {
            var (caster, target) = await CreateFixture(ctx);
            var other = ctx.SpawnCreature("nw_rat001", 2f);
            ctx.MakeHostile(other);
            ctx.SetNPCPerkLevel(caster, PerkType.Uppercut, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.Payback, 1);
            StatusEffect.ApplyStatusEffect(caster, caster, new BruiserStanceStatusEffect(), -1);
            Combat.SetAbilityHitResolutionOverride(false);
            Combat.SetAutoAttackHitResolutionOverride(false);
            try
            {
                ctx.AssertEqual(MeleeTechniqueStage.None, MeleeTechnique.GetSetupStage(caster, target), "no opener or Rattled is active");
                await DirectHit(caster, 5);
                ctx.AssertEqual(0, MeleeTechnique.GetPendingRetaliationPower(caster, caster), "self damage cannot grant a counter");
                await ctx.ExecuteInCreatureContextAsync(target, () =>
                {
                    StatusEffect.NotifyPreDamageStatusEffects(target, caster, 5, CombatDamageType.Physical, CombatDamageDeliveryType.DamageOverTime);
                    ApplyEffectToObject(DurationType.Instant, EffectDamage(5), caster);
                });
                ctx.AssertEqual(0, MeleeTechnique.GetPendingRetaliationPower(caster, target), "periodic damage cannot grant a counter");
                await ctx.ExecuteInCreatureContextAsync(target, () => Combat.ApplyTriggeredDamage(target, caster, 5, CombatDamageType.Physical));
                ctx.AssertEqual(0, MeleeTechnique.GetPendingRetaliationPower(caster, target), "triggered or reflected damage cannot grant a counter");
                await ctx.EquipItemAsync(caster, "b_pistol", InventorySlot.RightHand);
                var baselineHP = GetCurrentHitPoints(caster);
                TemporaryHitPointEffects.ApplyFlat(caster, "ENGINE_TECHNIQUE_COUNTER_SHIELD", 10, 20f);
                await DirectHit(target, 5);
                ctx.AssertEqual(baselineHP + 5, GetCurrentHitPoints(caster), "absorbed damage preserves real HP");
                ctx.AssertEqual(0, MeleeTechnique.GetPendingRetaliationPower(caster, target), "temporary HP only damage cannot grant a counter");
                await DirectHit(target, 10);
                ctx.AssertEqual(baselineHP - 5, GetCurrentHitPoints(caster), "spillover costs five real HP");
                await ctx.WaitUntilAsync(() => MeleeTechnique.GetPendingRetaliationPower(caster, target) == 12, 2f, "direct HP loss arms a counter without gloves or Rattled");
                ctx.Assert(StatusEffect.HasStatusEffect(caster, typeof(RetaliationStatusEffect)), "the charge has visible feedback");
                await DirectHit(other, 5);
                ctx.AssertEqual(12, MeleeTechnique.GetPendingRetaliationPower(caster, target), "the grant cooldown prevents another attacker replacing the charge");
                ctx.AssertEqual(0, MeleeTechnique.GetPendingRetaliationPower(caster, other), "the counter remains bound to the damaging opponent");
                MeleeTechnique.BeginPaidActivation(caster, target, new AbilityDetail { IsHostileAbility = true, SkillType = SkillType.Force });
                ctx.AssertEqual(12, MeleeTechnique.GetPendingRetaliationPower(caster, target), "an unrelated same-target skill preserves the charge");
                await ctx.EquipItemAsync(caster, "t_pglove", InventorySlot.RightHand);
                ctx.AssertEqual(12, MeleeTechnique.GetPendingRetaliationPower(caster, target), "returning to gloves preserves the original charge");
                await Cast(ctx, caster, target, FeatType.Uppercut1);
                ctx.AssertEqual(0, MeleeTechnique.GetPendingRetaliationPower(caster, target), "a paid missed heavy strike consumes the single charge without setup");
                ctx.Assert(!StatusEffect.HasStatusEffect(caster, typeof(RetaliationStatusEffect)), "consumption removes the charge marker");
                await DirectHit(target, 5);
                ctx.AssertEqual(0, MeleeTechnique.GetPendingRetaliationPower(caster, target), "consumption does not reset the eighteen-second grant gate");
            }
            finally { ResetOverrides(caster); }

            async Task DirectHit(uint attacker, int damage) => await ctx.ExecuteInCreatureContextAsync(attacker, () =>
            {
                StatusEffect.NotifyPreDamageStatusEffects(attacker, caster, damage, CombatDamageType.Physical);
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), caster);
            });
        }

        [EngineTest("Independent heavy strikes refund paid stamina once under the shared gate without Rattled", Category = "MeleeTechnique", TimeoutSeconds = 25f)]
        public static async Task IndependentHeavyRefunds(EngineTestContext ctx)
        {
            var (caster, target) = await CreateFixture(ctx);
            ctx.SetNPCPerkLevel(caster, PerkType.Uppercut, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.Haymaker, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.CatchYourBreath, 1);
            Combat.SetAbilityHitResolutionOverride(true);
            Combat.SetAutoAttackHitResolutionOverride(false);
            try
            {
                var stamina = Stat.GetCurrentStamina(caster);
                await Cast(ctx, caster, target, FeatType.Uppercut1);
                ctx.AssertEqual(stamina - 4 + 2, Stat.GetCurrentStamina(caster), "a landed independent Uppercut earns the capped two-stamina refund");
                ctx.AssertEqual(MeleeTechniqueStage.None, MeleeTechnique.GetSetupStage(caster, target), "no fake setup is created or consumed");
                await ctx.DelaySecondsAsync(1.05f);
                stamina = Stat.GetCurrentStamina(caster);
                await Cast(ctx, caster, target, FeatType.Haymaker1);
                ctx.AssertEqual(stamina - 15, Stat.GetCurrentStamina(caster), "Haymaker cannot bypass the same twelve-second refund gate");
                ctx.AssertEqual(MeleeTechniqueStage.None, MeleeTechnique.GetSetupStage(caster, target), "Haymaker is usable without Rattled");
            }
            finally { ResetOverrides(caster); }
        }

        [EngineTest("Unarmed setup bonuses follow melee and ranged swaps and other skill abilities", Category = "MeleeTechnique", TimeoutSeconds = 60f)]
        public static async Task CrossWeaponSetupBonuses(EngineTestContext ctx)
        {
            Combat.SetAbilityHitResolutionOverride(true);
            Combat.SetAutoAttackHitResolutionOverride(false);
            try
            {
                foreach (var (weapon, skill, feat, perk) in new[]
                {
                    ("b_longsword", SkillType.Vibroblade, FeatType.RendingStrike1, PerkType.RendingStrike),
                    ("b_pistol", SkillType.Pistol, FeatType.QuickDraw1, PerkType.QuickDraw)
                })
                {
                    var (caster, target) = await CreateFixture(ctx);
                    var other = ctx.SpawnCreature("nw_rat001", 2f, 1f);
                    ctx.MakeHostile(other);
                    ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), other, 30f);
                    ctx.SetNPCPerkLevel(caster, PerkType.Jab, 4);
                    ctx.SetNPCPerkLevel(caster, PerkType.FlowingForm, 1);
                    ctx.SetNPCPerkLevel(caster, PerkType.MeasuredTechnique, 1);
                    ctx.SetNPCPerkLevel(caster, perk, 1);
                    await Cast(ctx, caster, target, FeatType.Jab4);
                    await ctx.EquipItemAsync(caster, weapon, InventorySlot.RightHand);
                    ctx.AssertEqual(MeleeTechniqueStage.Opening, MeleeTechnique.GetSetupStage(caster, target), "a different weapon class retains Opening");
                    ctx.AssertEqual(16, MeleeTechnique.GetPendingWeavePower(caster, target), "a swap cannot spend or remove the one ordinary weave");
                    var ability = Ability.GetAbilityDetail(feat);
                    ctx.AssertEqual(10, MeleeTechnique.GetAbilityAccuracy(caster, target, ability), "Measured Technique applies to another skill's hostile ability");
                    ctx.AssertEqual(0, MeleeTechnique.GetAbilityAccuracy(caster, other, ability), "another enemy receives no setup accuracy");
                    ctx.AssertEqual(0, MeleeTechnique.GetAbilityAccuracy(other, target, ability), "another caster cannot borrow the setup accuracy");
                    await Cast(ctx, caster, target, feat);
                    ctx.AssertEqual(MeleeTechniqueStage.Opening, MeleeTechnique.GetSetupStage(caster, target), "a real same-target cast from another skill preserves Opening");
                    ctx.AssertEqual(16, MeleeTechnique.GetPendingWeavePower(caster, target), "the other skill's cast cannot consume the ordinary weave");

                    var area = new AbilityDetail { IsHostileAbility = true, IsAreaAbility = true, SkillType = SkillType.Force };
                    MeleeTechnique.BeginPaidActivation(caster, OBJECT_INVALID, area);
                    MeleeTechnique.BeginPaidActivation(caster, caster, area);
                    ctx.AssertEqual(MeleeTechniqueStage.Opening, MeleeTechnique.GetSetupStage(caster, target), "ground and self-centered hostile casts do not select a different enemy");
                    ctx.AssertEqual(10, MeleeTechnique.GetAbilityAccuracy(caster, target, area), "the area impact on the setup enemy receives accuracy");
                    ctx.AssertEqual(0, MeleeTechnique.GetAbilityAccuracy(caster, other, area), "other area recipients receive no setup accuracy");
                    const int attackIndex = 23;
                    ctx.AssertEqual(3, MeleeTechnique.PrepareOrdinaryAttempt(caster, target, skill, false, attackIndex), "ordinary accuracy follows the source-owned enemy with either weapon");
                    ctx.AssertEqual(16, MeleeTechnique.TakeOrdinaryPower(caster, target, skill, attackIndex, true), "the new weapon receives the opener's single weave budget");
                    ctx.AssertEqual(0, MeleeTechnique.TakeOrdinaryPower(caster, target, skill, attackIndex, true), "duplicate or off-hand callbacks cannot pay the weave twice");
                    ctx.AssertEqual(3, MeleeTechnique.PrepareOrdinaryAttempt(caster, target, skill, false, attackIndex + 1), "ordinary accuracy persists until setup expires");
                    ctx.AssertEqual(0, MeleeTechnique.TakeOrdinaryPower(caster, target, skill, attackIndex + 1, true), "a second ordinary attack cannot earn another weave");

                    Recast.ReduceRecastDelay(caster, ability.RecastGroup, 100f);
                    await Cast(ctx, caster, other, feat);
                    ctx.AssertEqual(MeleeTechniqueStage.None, MeleeTechnique.GetSetupStage(caster, target), "an actual hostile cast selecting another enemy clears the original setup");
                    ctx.AssertEqual(0, MeleeTechnique.GetAbilityAccuracy(caster, target, ability), "target transfer removes the old accuracy benefit");
                    await ctx.EquipItemAsync(caster, "t_pglove", InventorySlot.RightHand);
                    Recast.ReduceRecastDelay(caster, RecastGroup.UnarmedOpener, 100f);
                    await Cast(ctx, caster, target, FeatType.Jab4);
                    var establishedAt = DateTime.UtcNow;
                    await ctx.EquipItemAsync(caster, weapon, InventorySlot.RightHand);
                    await ctx.WaitUntilAsync(() => MeleeTechnique.GetSetupStage(caster, target) == MeleeTechniqueStage.None, 7f, "the original six-second Opening to expire after a weapon swap");
                    ctx.Assert(DateTime.UtcNow < establishedAt.AddSeconds(7), "the swap cannot refresh Opening's original expiry");
                    ctx.AssertEqual(0, MeleeTechnique.GetAbilityAccuracy(caster, target, ability), "expired setup cannot grant another skill accuracy");
                    ResetOverrides(caster);
                    Combat.SetAbilityHitResolutionOverride(true);
                    Combat.SetAutoAttackHitResolutionOverride(false);
                }
            }
            finally { Combat.SetAbilityHitResolutionOverride(null); Combat.SetAutoAttackHitResolutionOverride(null); }
        }

        [EngineTest("Measured Technique captures ten accuracy for a spender against Opening and Perfect Form against Finish", Category = "MeleeTechnique", TimeoutSeconds = 30f)]
        public static async Task CapturedSpenderAccuracy(EngineTestContext ctx)
        {
            foreach (var (feat, perk, expectedAfter) in new[]
            {
                (FeatType.PalmStrike1, PerkType.PalmStrike, MeleeTechniqueStage.Finish),
                (FeatType.PerfectForm1, PerkType.PerfectForm, MeleeTechniqueStage.None)
            })
            {
                var (caster, target) = await CreateFixture(ctx);
                ctx.SetNPCPerkLevel(caster, PerkType.Jab, 1);
                ctx.SetNPCPerkLevel(caster, PerkType.PalmStrike, 1);
                ctx.SetNPCPerkLevel(caster, perk, 1);
                ctx.SetNPCPerkLevel(caster, PerkType.FlowingForm, 1);
                ctx.SetNPCPerkLevel(caster, PerkType.MeasuredTechnique, 1);
                var ability = Ability.GetAbilityDetail(feat);
                var originalImpact = ability.ImpactAction;
                var capturedAccuracy = -1;
                var consumedStage = MeleeTechniqueStage.Opening;
                Combat.SetAbilityHitResolutionOverride(true);
                Combat.SetAutoAttackHitResolutionOverride(false);
                try
                {
                    ability.ImpactAction = (source, enemy, level, location) =>
                    {
                        if (source == caster)
                        {
                            capturedAccuracy = MeleeTechnique.GetAbilityAccuracy(source, enemy, ability);
                            consumedStage = MeleeTechnique.GetSetupStage(source, enemy);
                        }
                        originalImpact(source, enemy, level, location);
                    };
                    await Cast(ctx, caster, target, FeatType.Jab1);
                    await ctx.DelaySecondsAsync(1.05f);
                    if (feat == FeatType.PerfectForm1)
                    {
                        await Cast(ctx, caster, target, FeatType.PalmStrike1);
                        await ctx.DelaySecondsAsync(1.05f);
                    }
                    await Cast(ctx, caster, target, feat);
                    ctx.AssertEqual(MeleeTechniqueStage.None, consumedStage, "the paid technique has cleared its setup before the captured impact");
                    ctx.AssertEqual(10, capturedAccuracy, "the captured impact keeps exactly ten accuracy even when its consumed stage is None");
                    ctx.AssertEqual(expectedAfter, MeleeTechnique.GetSetupStage(caster, target), "only the actual bridge creates Finish; Perfect Form spends it");
                }
                finally
                {
                    ability.ImpactAction = originalImpact;
                    ResetOverrides(caster);
                }
            }
        }

        [EngineTest("Payback retains its original eight-second expiry and eighteen-second trigger gate across swaps", Category = "MeleeTechnique", TimeoutSeconds = 35f)]
        public static async Task CrossWeaponPaybackExpiry(EngineTestContext ctx)
        {
            var (caster, attacker) = await CreateFixture(ctx);
            ctx.SetNPCPerkLevel(caster, PerkType.Payback, 1);
            await ctx.EquipItemAsync(caster, "b_longsword", InventorySlot.RightHand);
            var ability = Ability.GetAbilityDetail(FeatType.BloodFrenzyBurst);
            var triggeredAt = DateTime.UtcNow;
            await DirectHit();
            await ctx.WaitUntilAsync(() => MeleeTechnique.GetStaminaDiscount(caster, ability, 5, false) == 2, 3f, "direct HP loss without gloves grants Payback");
            await ctx.EquipItemAsync(caster, "b_pistol", InventorySlot.RightHand);
            ctx.AssertEqual(2, MeleeTechnique.GetStaminaDiscount(caster, ability, 5, false), "ranged equipment retains the unused charge");
            var remaining = (float)(triggeredAt.AddSeconds(8.2) - DateTime.UtcNow).TotalSeconds;
            if (remaining > 0)
                await ctx.DelaySecondsAsync(remaining);
            ctx.AssertEqual(0, MeleeTechnique.GetStaminaDiscount(caster, ability, 5, false), "the charge expires eight seconds from its original damage trigger");
            await DirectHit();
            ctx.AssertEqual(0, MeleeTechnique.GetStaminaDiscount(caster, ability, 5, false), "expiry and swaps cannot reset the eighteen-second cooldown");
            remaining = (float)(triggeredAt.AddSeconds(18.2) - DateTime.UtcNow).TotalSeconds;
            if (remaining > 0)
                await ctx.DelaySecondsAsync(remaining);
            await DirectHit();
            await ctx.WaitUntilAsync(() => MeleeTechnique.GetStaminaDiscount(caster, ability, 5, false) == 2, 3f, "a new real HP hit can grant the next charge after the original cooldown");

            async Task DirectHit() => await ctx.ExecuteInCreatureContextAsync(attacker, () =>
            {
                StatusEffect.NotifyPreDamageStatusEffects(attacker, caster, 5, CombatDamageType.Physical);
                ApplyEffectToObject(DurationType.Instant, EffectDamage(5), caster);
            });
        }

        [EngineTest("Technique profile hot reload cleans presentation and retains independent reaction gates", Category = "MeleeTechnique", TimeoutSeconds = 40f)]
        public static async Task ProfileOwnership(EngineTestContext ctx)
        {
            var (caster, target) = await CreateFixture(ctx);
            ctx.SetNPCPerkLevel(caster, PerkType.Jab, 4);
            ctx.SetNPCPerkLevel(caster, PerkType.FinishingCombination, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.FlowStance, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.FlowingForm, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.Payback, 1);
            var discountedAbility = Ability.GetAbilityDetail(FeatType.DirtyJab1);
            Combat.SetAbilityHitResolutionOverride(true);
            Combat.SetAutoAttackHitResolutionOverride(false);
            try
            {
                await Cast(ctx, caster, caster, FeatType.FlowStance1);
                await Cast(ctx, caster, target, FeatType.Jab4);
                await ctx.DelaySecondsAsync(1.05f);
                await Cast(ctx, caster, target, FeatType.FinishingCombination1);
                ctx.Assert(StatusEffect.HasStatusEffect(caster, typeof(SlipAwayStatusEffect)), "the original profile owns its earned window");
                await DirectHit();
                await ctx.WaitUntilAsync(() => MeleeTechnique.GetStaminaDiscount(caster, discountedAbility, 3, false) == 2,
                    3f, "the profile has a real unused discount before replacement");

                UnarmedTechniqueProfile.Register();
                ctx.Assert(!StatusEffect.HasStatusEffect(caster, typeof(SlipAwayStatusEffect)), "replacing a profile immediately removes its tracked presentation");
                ctx.AssertEqual(0, MeleeTechnique.GetStaminaDiscount(caster, discountedAbility, 3, false), "replacement removes the old profile's charge");
                ctx.Assert(StatusEffect.HasStatusEffect(caster, typeof(FlowStanceStatusEffect)), "replacement keeps the equipped stance's stat metadata");
                await ctx.DelaySecondsAsync(1.05f);
                Recast.ReduceRecastDelay(caster, RecastGroup.UnarmedOpener, 100f);
                Recast.ReduceRecastDelay(caster, RecastGroup.UnarmedTechnique, 100f);
                await Cast(ctx, caster, target, FeatType.Jab4);
                await ctx.DelaySecondsAsync(1.05f);
                await Cast(ctx, caster, target, FeatType.FinishingCombination1);
                ctx.Assert(!StatusEffect.HasStatusEffect(caster, typeof(SlipAwayStatusEffect)), "profile replacement preserves the shared twelve-second window gate");

                await ctx.DelaySecondsAsync(1.05f);
                Recast.ReduceRecastDelay(caster, RecastGroup.UnarmedOpener, 100f);
                await Cast(ctx, caster, target, FeatType.Jab4);
                ctx.AssertEqual(MeleeTechniqueStage.Opening, MeleeTechnique.GetSetupStage(caster, target), "the replacement profile can create fresh setup");
                await ctx.EquipItemAsync(caster, "b_longsword", InventorySlot.RightHand);
                ctx.AssertEqual(SkillType.Vibroblade, Skill.GetSkillTypeByBaseItem(GetBaseItemType(GetItemInSlot(InventorySlot.RightHand, caster))), "fixture equips another weapon skill");
                ctx.Assert(!string.IsNullOrEmpty(MeleeTechnique.ValidateActivation(caster, discountedAbility, target)), "fist activation fails its grip check with a sword");
                ctx.AssertEqual(MeleeTechniqueStage.Opening, MeleeTechnique.GetSetupStage(caster, target), "the weapon swap preserves the source-owned setup");
                ctx.Assert(StatusEffect.HasStatusEffect(caster, typeof(OpeningStatusEffect)), "the swap retains the tracked setup marker");
                await ctx.EquipItemAsync(caster, "t_pglove", InventorySlot.RightHand);
                await DirectHit();
                ctx.AssertEqual(0, MeleeTechnique.GetStaminaDiscount(caster, discountedAbility, 3, false), "profile and weapon changes preserve the eighteen-second damage-reaction gate");

                MeleeTechnique.UnregisterProfile(SkillType.Unarmed);
                ctx.Assert(!string.IsNullOrEmpty(MeleeTechnique.ValidateActivation(caster, discountedAbility, target)), "a missing profile fails activation closed");
                await DirectHit();
                ctx.AssertEqual(0, MeleeTechnique.GetStaminaDiscount(caster, discountedAbility, 3, false), "a missing profile cannot grant passive benefits");
            }
            finally
            {
                UnarmedTechniqueProfile.Register();
                ResetOverrides(caster);
            }

            async Task DirectHit() => await ctx.ExecuteInCreatureContextAsync(target, () =>
            {
                StatusEffect.NotifyPreDamageStatusEffects(target, caster, 5, CombatDamageType.Physical);
                ApplyEffectToObject(DurationType.Instant, EffectDamage(5), caster);
            });
        }

        [EngineTest("Perfect Form refuses to start without Finish and leaves Opening, STM and its recast untouched", Category = "MeleeTechnique", TimeoutSeconds = 25f)]
        public static async Task PerfectFormRequiresFinish(EngineTestContext ctx)
        {
            var (caster, target) = await CreateFixture(ctx);
            ctx.SetNPCPerkLevel(caster, PerkType.Jab, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.PerfectForm, 1);
            Combat.SetAbilityHitResolutionOverride(true);
            Combat.SetAutoAttackHitResolutionOverride(false);
            try
            {
                await AssertPerfectFormRefused(ctx, caster, target, "with no setup");
                await Cast(ctx, caster, target, FeatType.Jab1);
                await ctx.DelaySecondsAsync(1.05f);
                await AssertPerfectFormRefused(ctx, caster, target, "against Opening");
                ctx.AssertEqual(MeleeTechniqueStage.Opening, MeleeTechnique.GetSetupStage(caster, target), "a refused Perfect Form cannot spend or clear Opening");
            }
            finally { ResetOverrides(caster); }
        }

        [EngineTest("Perfect Form spends the Finish it started with when Finish runs out during its cast", Category = "MeleeTechnique", TimeoutSeconds = 30f)]
        public static async Task PerfectFormHoldsFinishThroughCast(EngineTestContext ctx)
        {
            var (caster, target) = await CreateFixture(ctx);
            ctx.SetNPCPerkLevel(caster, PerkType.Jab, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.PalmStrike, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.PerfectForm, 1);
            var perfect = Ability.GetAbilityDetail(FeatType.PerfectForm1);
            var landings = new System.Collections.Generic.List<MeleeTechniqueStage>();
            void OnLanded(uint source, uint enemy, AbilityDetail ability, MeleeTechniqueStage stage)
            {
                if (source == caster && enemy == target && ability == perfect)
                    landings.Add(stage);
            }
            MeleeTechnique.TechniqueLanded += OnLanded;
            Combat.SetAbilityHitResolutionOverride(true);
            Combat.SetAutoAttackHitResolutionOverride(false);
            try
            {
                var stamina = await StartPerfectFormNearFinishExpiry(ctx, caster, target);
                await ctx.DelaySecondsAsync(1.5f);
                ctx.Assert(!string.IsNullOrEmpty(UsePerkFeat.GetActiveAbilityActivationId(caster)), "the two-second cast is still in progress");
                ctx.AssertEqual(MeleeTechniqueStage.None, MeleeTechnique.GetSetupStage(caster, target), "the live four-second Finish has run out under the cast");
                await ctx.WaitUntilAsync(() => Ability.GetLastCompletedAbilityImpactSummary(caster)?.Ability == perfect,
                    4f, "Perfect Form to complete its actual activation");
                ctx.AssertEqual(1, landings.Count, "one landed Perfect Form");
                ctx.AssertEqual(MeleeTechniqueStage.Finish, landings.Single(), "the completed cast spends the Finish it started with");
                ctx.AssertEqual(stamina - 15, Stat.GetCurrentStamina(caster), "the completed cast pays its STM once");
                ctx.Assert(Recast.IsOnRecastDelay(caster, RecastGroup.Capstone).Item1, "the completed cast starts the Capstone recast");
                ctx.AssertEqual(MeleeTechniqueStage.None, MeleeTechnique.GetSetupStage(caster, target), "Perfect Form leaves no setup behind");
            }
            finally
            {
                MeleeTechnique.TechniqueLanded -= OnLanded;
                ResetOverrides(caster);
            }
        }

        [EngineTest("An interrupted Perfect Form releases its held Finish to its normal expiry", Category = "MeleeTechnique", TimeoutSeconds = 30f)]
        public static async Task InterruptedPerfectFormReleasesFinish(EngineTestContext ctx)
        {
            var (caster, target) = await CreateFixture(ctx);
            ctx.SetNPCPerkLevel(caster, PerkType.Jab, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.PalmStrike, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.PerfectForm, 1);
            Combat.SetAbilityHitResolutionOverride(true);
            Combat.SetAutoAttackHitResolutionOverride(false);
            try
            {
                var stamina = await StartPerfectFormNearFinishExpiry(ctx, caster, target);
                await ctx.DelaySecondsAsync(0.3f);
                ctx.Assert(UsePerkFeat.InterruptAbilityActivation(caster), "the cast in progress is interrupted");
                await ctx.DelaySecondsAsync(1.2f);
                ctx.AssertEqual(MeleeTechniqueStage.None, MeleeTechnique.GetSetupStage(caster, target), "Finish expires on its own schedule after the interruption");
                ctx.AssertEqual(stamina, Stat.GetCurrentStamina(caster), "the interrupted cast spends no STM");
                ctx.Assert(!Recast.IsOnRecastDelay(caster, RecastGroup.Capstone).Item1, "the interrupted cast starts no Capstone recast");
                await AssertPerfectFormRefused(ctx, caster, target, "after the interrupted cast's Finish expired");
            }
            finally { ResetOverrides(caster); }
        }

        /// <summary>Jab, Palm Strike, then Perfect Form with about one second of the four-second Finish left.</summary>
        private static async Task<int> StartPerfectFormNearFinishExpiry(EngineTestContext ctx, uint caster, uint target)
        {
            await Cast(ctx, caster, target, FeatType.Jab1);
            await ctx.DelaySecondsAsync(1.05f);
            await Cast(ctx, caster, target, FeatType.PalmStrike1);
            await ctx.DelaySecondsAsync(3f);
            ctx.AssertEqual(MeleeTechniqueStage.Finish, MeleeTechnique.GetSetupStage(caster, target), "Finish is still live when Perfect Form starts");
            var stamina = Stat.GetCurrentStamina(caster);
            Ability.ClearLastCompletedAbilityImpactSummary(caster);
            await ctx.ExecuteInCreatureContextAsync(caster, () =>
                ctx.Assert(UsePerkFeat.TryUseAbility(caster, target, FeatType.PerfectForm1, GetLocation(target)),
                    $"Perfect Form starts with Finish: {Ability.GetLastActivationDenialReason()}"));
            return stamina;
        }

        private static async Task AssertPerfectFormRefused(EngineTestContext ctx, uint caster, uint target, string setup)
        {
            var stamina = Stat.GetCurrentStamina(caster);
            var started = true;
            await ctx.ExecuteInCreatureContextAsync(caster, () =>
                started = UsePerkFeat.TryUseAbility(caster, target, FeatType.PerfectForm1, GetLocation(target)));
            ctx.Assert(!started, $"Perfect Form cannot start {setup}");
            ctx.AssertEqual("Perfect Form requires your Finish against this enemy.", Ability.GetLastActivationDenialReason(),
                $"the refusal {setup} names the missing Finish");
            ctx.AssertEqual(stamina, Stat.GetCurrentStamina(caster), $"a refusal {setup} spends no STM");
            ctx.Assert(!Recast.IsOnRecastDelay(caster, RecastGroup.Capstone).Item1, $"a refusal {setup} starts no Capstone recast");
        }

        private static async Task<(uint Caster, uint Target)> CreateFixture(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001", -0.5f);
            var target = ctx.SpawnCreature("nw_rat001", 1f);
            await ctx.DelaySecondsAsync(1f);
            await ctx.EquipItemAsync(caster, "t_pglove", InventorySlot.RightHand);
            ClearUnarmedPassiveTraits(ctx, caster);
            foreach (var creature in new[] { caster, target })
            {
                ctx.SuppressNPCNaturalRegen(creature);
                Stat.SetNPCMaxHitPoints(creature, 20000, true);
                SetAILevel(creature, AILevel.VeryLow);
            }
            ctx.SetNPCResources(caster, 100, 100);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 60f);
            TemporaryStatModifier.Add(target, StatType.MeleeDeflection, -100, 60f);
            ctx.MakeHostile(target);
            return (caster, target);
        }

        private static async Task Cast(EngineTestContext ctx, uint caster, uint target, FeatType feat)
        {
            var ability = Ability.GetAbilityDetail(feat);
            Ability.ClearLastCompletedAbilityImpactSummary(caster);
            await ctx.ExecuteInCreatureContextAsync(caster, () =>
                ctx.Assert(UsePerkFeat.TryUseAbility(caster, target, feat, GetLocation(target)),
                    $"{ability.Name} starts: {Ability.GetLastActivationDenialReason()}"));
            await ctx.WaitUntilAsync(() => Ability.GetLastCompletedAbilityImpactSummary(caster)?.Ability == ability,
                6f, $"{ability.Name} to complete its actual activation");
        }

        private static void ClearUnarmedPassiveTraits(EngineTestContext ctx, uint creature)
        {
            foreach (var perk in new[]
            {
                PerkType.FlowingForm, PerkType.PracticedForm, PerkType.MeasuredTechnique,
                PerkType.ControlledBreathing, PerkType.RootedTechnique, PerkType.DirtyFighting,
                PerkType.HeavyBlows, PerkType.HardKnocks, PerkType.CatchYourBreath, PerkType.Payback
            })
                ctx.SetNPCPerkLevel(creature, perk, 0);
        }

        private static void ResetOverrides(uint caster)
        {
            Combat.SetAbilityHitResolutionOverride(null);
            Combat.SetAutoAttackHitResolutionOverride(null);
            AssignCommand(caster, () => ClearAllActions());
        }
    }
}
