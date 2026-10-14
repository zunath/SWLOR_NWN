using System;
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
        [EngineTest("Unarmed first purchases form a paid sequence without later traits", Category = "MeleeTechnique", TimeoutSeconds = 35f)]
        public static async Task FirstPurchaseSequence(EngineTestContext ctx)
        {
            var (caster, target) = await CreateFixture(ctx);
            ctx.SetNPCPerkLevel(caster, PerkType.Jab, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.PalmStrike, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.FinishingCombination, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.ControlledBreathing, 1);
            var spent = 0;
            void OnSpent(uint source, uint enemy, MeleeTechniqueDetail detail, MeleeTechniqueStage stage)
            {
                if (source == caster && enemy == target)
                    spent++;
            }
            MeleeTechnique.TechniqueSpent += OnSpent;
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
                MeleeTechnique.TechniqueSpent -= OnSpent;
                ResetOverrides(caster);
            }
        }

        [EngineTest("A missed ordinary Unarmed roll consumes its weave and preserves Opening", Category = "MeleeTechnique", TimeoutSeconds = 30f)]
        public static async Task NativeMissedWeave(EngineTestContext ctx)
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
                var hitPoints = GetCurrentHitPoints(target);
                await ctx.ExecuteInCreatureContextAsync(caster, () => ActionAttack(target));
                await ctx.WaitUntilAsync(() => MeleeTechnique.GetPendingWeavePower(caster, target) == 0, 4.5f, "a real missed native ordinary roll to spend its weave");
                ctx.AssertEqual(MeleeTechniqueStage.Opening, MeleeTechnique.GetSetupStage(caster, target), "miss does not consume Opening");
                ctx.AssertEqual(hitPoints, GetCurrentHitPoints(target), "missed weaving does not create a free hit");
                await Cast(ctx, caster, target, FeatType.PalmStrike1);
                ctx.AssertEqual(MeleeTechniqueStage.Finish, MeleeTechnique.GetSetupStage(caster, target), "bridge works after the missed weave");
            }
            finally { ResetOverrides(caster); }
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
                ctx.Assert(!StatusEffect.HasStatusEffect(target, typeof(RattledStatusEffect), first), "Uppercut consumes its caster's source");
                ctx.Assert(StatusEffect.HasStatusEffect(target, typeof(RattledStatusEffect), second), "other brawler's source remains");
                ctx.AssertEqual(MeleeTechniqueStage.Distraction, MeleeTechnique.GetSetupStage(second, target), "other brawler can still spend setup");
                ctx.AssertEqual(10, Stat.GetStatAdjustment(target, StatType.OrdinaryWeaponAccuracyPenalty), "remaining source preserves its penalty");
                await ctx.EquipItemAsync(second, "b_pistol", InventorySlot.RightHand);
                ctx.AssertEqual(MeleeTechniqueStage.None, MeleeTechnique.GetSetupStage(second, target), "equipment change clears the other owner's setup");
                ctx.Assert(!StatusEffect.HasStatusEffect(target, typeof(RattledStatusEffect), second), "invalid grip removes only that owner's remaining source");
            }
            finally
            {
                ResetOverrides(first);
                AssignCommand(second, () => ClearAllActions());
            }
        }

        [EngineTest("A missed spender pays its cost and loses setup", Category = "MeleeTechnique", TimeoutSeconds = 30f)]
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
                ctx.AssertEqual(MeleeTechniqueStage.None, MeleeTechnique.GetSetupStage(caster, target), "miss spent Rattled");
                ctx.Assert(!StatusEffect.HasStatusEffect(target, typeof(RattledStatusEffect), caster), "miss does not leave the debuff behind");
                ctx.AssertEqual(stamina - 4, Stat.GetCurrentStamina(caster), "miss pays full cost without a landed-hit refund");
            }
            finally { ResetOverrides(caster); }
        }

        [EngineTest("Payback ignores periodic and temporary-HP-only damage then discounts one paid Brawler activation", Category = "MeleeTechnique", TimeoutSeconds = 35f)]
        public static async Task DirectDamageDiscount(EngineTestContext ctx)
        {
            var (caster, attacker) = await CreateFixture(ctx);
            ctx.SetNPCPerkLevel(caster, PerkType.Payback, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.DirtyJab, 1);
            var ability = Ability.GetAbilityDetail(FeatType.DirtyJab1);
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
            await ctx.EquipItemAsync(caster, "t_pglove", InventorySlot.LeftHand);
            ctx.AssertEqual(2, MeleeTechnique.GetStaminaDiscount(caster, ability, 3, false), "valid-grip equipment changes preserve the independent Payback charge");
            var stamina = Stat.GetCurrentStamina(caster);
            Combat.SetAbilityHitResolutionOverride(true);
            Combat.SetAutoAttackHitResolutionOverride(false);
            try
            {
                await Cast(ctx, caster, attacker, FeatType.DirtyJab1);
                ctx.AssertEqual(stamina - 1, Stat.GetCurrentStamina(caster), "three-stamina attack pays one with its single discount");
                ctx.AssertEqual(0, MeleeTechnique.GetStaminaDiscount(caster, ability, 3, false), "paid activation consumes charge");
            }
            finally { ResetOverrides(caster); }
        }

        [EngineTest("Both Standard and Force-sensitive players use Unarmed, and weapon swaps remove its stance", Category = "MeleeTechnique", TimeoutSeconds = 50f)]
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
                ctx.Assert(!StatusEffect.HasStatusEffect(standard.Creature, typeof(FlowStanceStatusEffect)), "actual equip event removes equipment-bound stance benefits");
                ctx.AssertEqual(MeleeTechniqueStage.None, MeleeTechnique.GetSetupStage(standard.Creature, target), "stance and equipment changes leave no banked setup");
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
                    await ctx.EquipItemAsync(caster, "t_pglove", InventorySlot.LeftHand);
                    ctx.Assert(EquipmentPredicates.HasUnarmedGrip(caster), "adding an off-hand Power Glove retains a valid grip");
                    ctx.Assert(StatusEffect.HasStatusEffect(caster, typeof(FlowStanceStatusEffect)), "valid equipment changes retain the stance itself");
                    ctx.Assert(!StatusEffect.HasStatusEffect(caster, typeof(SlipAwayStatusEffect)) &&
                               !StatusEffect.HasStatusEffect(caster, typeof(PressForwardStatusEffect)), "valid-grip equipment changes immediately clear either earned window");
                    ctx.AssertEqual(evasion, Stat.GetStatAdjustment(caster, StatType.EvasionPercentAdjustment), "equipment change removes earned evasion");
                    ctx.AssertEqual(movement, Stat.GetStatAdjustment(caster, StatType.MovementSpeedPercentAdjustment), "equipment change removes earned movement");
                    ctx.AssertEqual(haste, Stat.GetStatAdjustment(caster, StatType.AttackDelayReductionPercent), "equipment change removes earned haste");
                    if (!fullSequence)
                    {
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

        [EngineTest("Bruiser brace requires direct HP damage from its Rattled enemy and spends once on a missed response", Category = "MeleeTechnique", TimeoutSeconds = 35f)]
        public static async Task ReactiveRetaliation(EngineTestContext ctx)
        {
            var (caster, target) = await CreateFixture(ctx);
            var other = ctx.SpawnCreature("nw_rat001", 2f);
            ctx.MakeHostile(other);
            ctx.SetNPCPerkLevel(caster, PerkType.DirtyJab, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.Uppercut, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.Payback, 1);
            StatusEffect.ApplyStatusEffect(caster, caster, new BruiserStanceStatusEffect(), -1);
            Combat.SetAbilityHitResolutionOverride(true);
            Combat.SetAutoAttackHitResolutionOverride(false);
            try
            {
                await Cast(ctx, caster, target, FeatType.DirtyJab1);
                await DirectHit(other, 5);
                ctx.AssertEqual(0, MeleeTechnique.GetPendingRetaliationPower(caster, target), "another enemy cannot arm the brace");
                await ctx.ExecuteInCreatureContextAsync(target, () =>
                {
                    StatusEffect.NotifyPreDamageStatusEffects(target, caster, 5, CombatDamageType.Physical, CombatDamageDeliveryType.DamageOverTime);
                    ApplyEffectToObject(DurationType.Instant, EffectDamage(5), caster);
                });
                ctx.AssertEqual(0, MeleeTechnique.GetPendingRetaliationPower(caster, target), "periodic damage cannot arm the brace");
                var baselineHP = GetCurrentHitPoints(caster);
                TemporaryHitPointEffects.ApplyFlat(caster, "ENGINE_TECHNIQUE_BRACE_SHIELD", 10, 20f);
                ctx.Log($"NATIVE_TEMP_HP Retaliation baseline HP={baselineHP}; after grant10 HP={GetCurrentHitPoints(caster)}, temporary={TemporaryHitPointEffects.GetRemaining(caster)}");
                ctx.AssertEqual(baselineHP + 10, GetCurrentHitPoints(caster), "native brace HP includes its temporary pool");
                await DirectHit(target, 5);
                ctx.Log($"NATIVE_TEMP_HP Retaliation after absorbed5 HP={GetCurrentHitPoints(caster)}, temporary={TemporaryHitPointEffects.GetRemaining(caster)}");
                ctx.AssertEqual(baselineHP + 5, GetCurrentHitPoints(caster), "brace absorption preserves real HP");
                ctx.AssertEqual(5, TemporaryHitPointEffects.GetRemaining(caster), "brace temporary pool falls to five");
                ctx.AssertEqual(0, MeleeTechnique.GetPendingRetaliationPower(caster, target), "temporary HP only damage cannot arm the brace");
                await DirectHit(target, 10);
                ctx.Log($"NATIVE_TEMP_HP Retaliation after spillover10 HP={GetCurrentHitPoints(caster)}, temporary={TemporaryHitPointEffects.GetRemaining(caster)}");
                ctx.AssertEqual(baselineHP - 5, GetCurrentHitPoints(caster), "brace spillover costs five real HP");
                ctx.AssertEqual(0, TemporaryHitPointEffects.GetRemaining(caster), "brace spillover exhausts the temporary pool");
                await ctx.WaitUntilAsync(() => MeleeTechnique.GetPendingRetaliationPower(caster, target) == 8, 2f, "same enemy's actual direct HP damage arms Retaliation");
                ctx.Assert(StatusEffect.HasStatusEffect(caster, typeof(RetaliationStatusEffect)), "armed retaliation has visible feedback");
                ctx.AssertEqual(2, MeleeTechnique.GetStaminaDiscount(caster, Ability.GetAbilityDetail(FeatType.Uppercut1), 4, false), "Payback remains an independent discount");
                await ctx.DelaySecondsAsync(1.05f);
                Combat.SetAbilityHitResolutionOverride(false);
                await Cast(ctx, caster, target, FeatType.Uppercut1);
                ctx.AssertEqual(0, MeleeTechnique.GetPendingRetaliationPower(caster, target), "paid missed response consumes its single power budget");
                ctx.Assert(!StatusEffect.HasStatusEffect(caster, typeof(RetaliationStatusEffect)), "spent charge feedback is removed");
                ctx.AssertEqual(MeleeTechniqueStage.None, MeleeTechnique.GetSetupStage(caster, target), "the same paid response spends its caster-owned Rattled");
            }
            finally { ResetOverrides(caster); }

            async Task DirectHit(uint attacker, int damage) => await ctx.ExecuteInCreatureContextAsync(attacker, () =>
            {
                StatusEffect.NotifyPreDamageStatusEffects(attacker, caster, damage, CombatDamageType.Physical);
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), caster);
            });
        }

        [EngineTest("Technique profiles replace deterministically and cannot retain another weapon skill's benefits", Category = "MeleeTechnique", TimeoutSeconds = 40f)]
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
                // Passive damage reactions must discover the profile before any technique is cast.
                await DirectHit();
                await ctx.WaitUntilAsync(() => MeleeTechnique.GetStaminaDiscount(caster, discountedAbility, 3, false) == 2,
                    3f, "the equipped weapon profile to grant its passive discount before an active cast");
                await Cast(ctx, caster, caster, FeatType.FlowStance1);
                await Cast(ctx, caster, target, FeatType.Jab4);
                await ctx.DelaySecondsAsync(1.05f);
                await Cast(ctx, caster, target, FeatType.FinishingCombination1);
                ctx.Assert(StatusEffect.HasStatusEffect(caster, typeof(SlipAwayStatusEffect)), "the original profile owns its earned window");

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
                MeleeTechnique.RegisterProfile(SkillType.Vibroblade, new MeleeTechniqueProfile
                {
                    HasValidEquipment = _ => true,
                    EquipmentError = "Equip a vibroblade.",
                    SetupMarkerFactory = _ => new OpeningStatusEffect(),
                    OpeningFinisherWindowFactory = (evasion, movement) => new SlipAwayStatusEffect(evasion, movement),
                    FinishFinisherWindowFactory = haste => new PressForwardStatusEffect(haste),
                    StaminaDiscountMarkerFactory = () => new PaybackStatusEffect(),
                    RetaliationMarkerFactory = () => new RetaliationStatusEffect()
                });
                await ctx.EquipItemAsync(caster, "b_longsword", InventorySlot.RightHand);
                ctx.AssertEqual(SkillType.Vibroblade, Skill.GetSkillTypeByBaseItem(GetBaseItemType(GetItemInSlot(InventorySlot.RightHand, caster))), "fixture equips the other registered weapon skill");
                ctx.AssertEqual(string.Empty, MeleeTechnique.ValidateActivation(caster,
                    new AbilityDetail { SkillType = SkillType.Vibroblade, MeleeTechnique = new MeleeTechniqueDetail() }),
                    "the new weapon has its own valid technique profile");
                ctx.AssertEqual(MeleeTechniqueStage.None, MeleeTechnique.GetSetupStage(caster, target), "a valid different profile cannot keep the old enemy's setup");
                ctx.Assert(!StatusEffect.HasStatusEffect(caster, typeof(OpeningStatusEffect)), "the other weapon skill removes the tracked setup marker");
                await ctx.EquipItemAsync(caster, "t_pglove", InventorySlot.RightHand);
                await DirectHit();
                ctx.AssertEqual(0, MeleeTechnique.GetStaminaDiscount(caster, discountedAbility, 3, false), "profile and weapon changes preserve the eighteen-second damage-reaction gate");

                MeleeTechnique.UnregisterProfile(SkillType.Unarmed);
                ctx.Assert(!string.IsNullOrEmpty(MeleeTechnique.ValidateActivation(caster, discountedAbility)), "a missing profile fails activation closed");
                await DirectHit();
                ctx.AssertEqual(0, MeleeTechnique.GetStaminaDiscount(caster, discountedAbility, 3, false), "a missing profile cannot grant passive benefits");
            }
            finally
            {
                MeleeTechnique.UnregisterProfile(SkillType.Vibroblade);
                UnarmedTechniqueProfile.Register();
                ResetOverrides(caster);
            }

            async Task DirectHit() => await ctx.ExecuteInCreatureContextAsync(target, () =>
            {
                StatusEffect.NotifyPreDamageStatusEffects(target, caster, 5, CombatDamageType.Physical);
                ApplyEffectToObject(DurationType.Instant, EffectDamage(5), caster);
            });
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
