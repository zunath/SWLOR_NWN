using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>Enemy-bound melee sequences driven by ability metadata and source-scoped stats.</summary>
    public static class MeleeTechnique
    {
        private static readonly Dictionary<SkillType, MeleeTechniqueProfile> Profiles = new();
        private static readonly Dictionary<uint, OwnerState> Owners = new();
        private static readonly Dictionary<uint, Activation> Activations = new();
        private static readonly Dictionary<uint, OrdinaryAttempt> OrdinaryAttempts = new();
        private static readonly Dictionary<uint, DirectDamageSnapshot> DirectDamage = new();

        /// <summary>One notification per completed, landed technique which paid stamina.</summary>
        public static event Action<uint, uint, AbilityDetail, MeleeTechniqueStage> TechniqueLanded;

        /// <summary>Replaces this skill's content profile; invalid definitions fail closed.</summary>
        public static void RegisterProfile(SkillType skill, MeleeTechniqueProfile profile)
        {
            UnregisterProfile(skill);
            if (skill == SkillType.Invalid || profile?.IsValid != true)
                throw new ArgumentException("A technique profile requires a weapon skill, equipment rules and presentation factories.");
            Profiles[skill] = profile;
        }

        public static void UnregisterProfile(SkillType skill)
        {
            Profiles.Remove(skill);
            foreach (var (creature, owner) in Owners.Where(pair =>
                         pair.Value.ProfileSkill == skill || pair.Value.DiscountProfileSkill == skill || pair.Value.RetaliationProfileSkill == skill))
            {
                ClearBenefits(creature);
                owner.Profile = null;
                owner.ProfileSkill = SkillType.Invalid;
            }
        }

        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void ResetProfiles()
        {
            foreach (var (creature, owner) in Owners)
            {
                ClearBenefits(creature);
                owner.Profile = null;
                owner.ProfileSkill = SkillType.Invalid;
            }
            Profiles.Clear();
            DirectDamage.Clear();
        }

        public static int GetLineStat(uint creature, MeleeTechniqueLine line, StatType payload) =>
            Stat.GetStatSources(creature, payload)
                .Where(source => source[StatType.TechniqueLine] == (int)line)
                .Sum(source => source[payload]);

        public static string ValidateActivation(uint creature, AbilityDetail ability, uint target)
        {
            if (ability?.MeleeTechnique == null)
                return string.Empty;
            if (!Profiles.TryGetValue(ability.SkillType, out var profile) || !profile.IsValid)
                return "This technique's weapon profile is unavailable.";
            if (!TryGetEquippedProfile(creature, out var skill, out var equipped) ||
                skill != ability.SkillType || equipped != profile)
                return profile.EquipmentError;
            if (ability.IsHostileAbility && !GetOwner(creature).Resources.CanActivate(DateTime.UtcNow))
                return "Wait a moment before using another technique.";
            var required = ability.MeleeTechnique.RequiredStages;
            if (required != MeleeTechniqueStage.None && (GetRequiredSetupStage(creature, target, ability) & required) == 0)
                return $"{ability.Name} requires your {DescribeStages(required)} against this enemy.";
            return string.Empty;
        }

        /// <summary>
        /// Called when a cast starts. A technique which requires a setup keeps the one it started with
        /// until that same cast completes, so the setup cannot expire under its cast time.
        /// </summary>
        public static void HoldSetupForCast(uint creature, uint target, AbilityDetail ability, string activationId)
        {
            if (!Owners.TryGetValue(creature, out var owner))
                return;
            owner.HeldCast = null;
            var required = ability?.MeleeTechnique?.RequiredStages ?? MeleeTechniqueStage.None;
            if (required == MeleeTechniqueStage.None || string.IsNullOrEmpty(activationId))
                return;
            Refresh(creature, owner);
            if (owner.Line != ability.MeleeTechnique.Line || !owner.Sequence.Matches(target, DateTime.UtcNow) ||
                (owner.Sequence.Stage & required) == 0)
                return;
            owner.HeldCast = new HeldCast(activationId, ability, target, owner.Sequence.Stage, owner.Sequence.ExpiresAt);
        }

        private static MeleeTechniqueStage GetRequiredSetupStage(uint creature, uint target, AbilityDetail ability)
        {
            if (!Owners.TryGetValue(creature, out var owner))
                return MeleeTechniqueStage.None;
            Refresh(creature, owner);
            return owner.Line == ability.MeleeTechnique.Line && SetupMatches(creature, owner, target, ability, DateTime.UtcNow)
                ? owner.Sequence.Stage : MeleeTechniqueStage.None;
        }

        /// <summary>The live setup, or the one held for this ability's cast in progress.</summary>
        private static bool SetupMatches(uint creature, OwnerState owner, uint target, AbilityDetail ability, DateTime now) =>
            owner.Sequence.Matches(target, now) || IsSetupHeld(creature, owner, target, ability);

        private static bool IsSetupHeld(uint creature, OwnerState owner, uint target, AbilityDetail ability) =>
            owner.HeldCast is { } held && held.Ability == ability && held.Target == target &&
            owner.Sequence.Target == target && owner.Sequence.Stage == held.Stage && owner.Sequence.ExpiresAt == held.ExpiresAt &&
            UsePerkFeat.GetActiveAbilityActivationId(creature) == held.ActivationId;

        private static string DescribeStages(MeleeTechniqueStage stages) => string.Join(" or ",
            new[] { MeleeTechniqueStage.Opening, MeleeTechniqueStage.Finish, MeleeTechniqueStage.Distraction }
                .Where(stage => (stages & stage) != 0));

        /// <summary>Called after all paid requirements, before an impact is launched.</summary>
        public static void BeginPaidActivation(uint creature, uint target, AbilityDetail ability)
        {
            if (ability == null)
                return;
            var detail = ability.MeleeTechnique;
            if (detail?.ClearSetup == true)
                ClearSetup(creature);
            if (!ability.IsHostileAbility)
                return;
            // Only technique casts create state; other abilities only update an existing owner's benefits.
            if (detail == null && !Owners.ContainsKey(creature))
                return;

            var owner = GetOwner(creature);
            Refresh(creature, owner);
            var selectedEnemy = IsValidEnemy(creature, target);
            if (selectedEnemy && owner.Reactions.WindowTarget != 0 && owner.Reactions.WindowTarget != target)
                ClearReactiveWindows(creature, owner);
            if (selectedEnemy && owner.Reactions.RetaliationTarget != 0 && owner.Reactions.RetaliationTarget != target)
                ClearRetaliation(creature, owner);
            if (selectedEnemy && owner.Sequence.Stage != MeleeTechniqueStage.None && owner.Sequence.Target != target)
                ClearSetup(creature);
            OrdinaryAttempts.Remove(creature);
            Activations.Remove(creature);
            var now = DateTime.UtcNow;
            owner.LastHostileActivity = now;

            // Other skills can use the earned opening without replacing or cancelling its source.
            if (detail == null)
                return;
            if (!Profiles.TryGetValue(ability.SkillType, out var profile))
                return;
            var setupAccuracy = SetupMatches(creature, owner, target, ability, now) &&
                                owner.Sequence.Stage is MeleeTechniqueStage.Opening or MeleeTechniqueStage.Finish
                ? GetLineStat(creature, owner.Line, StatType.TechniqueSetupAbilityAccuracy) : 0;
            if (owner.Profile != profile || owner.ProfileSkill != ability.SkillType)
                ClearSetup(creature);
            owner.Profile = profile;
            owner.ProfileSkill = ability.SkillType;

            var matching = owner.Line == detail.Line && SetupMatches(creature, owner, target, ability, now);
            owner.HeldCast = null;
            var stage = matching && (detail.ConsumedStages & owner.Sequence.Stage) != 0
                ? owner.Sequence.Stage : MeleeTechniqueStage.None;
            var retaliation = detail.ConsumesRetaliation || stage == MeleeTechniqueStage.Distraction && detail.CreatedStage == MeleeTechniqueStage.None
                ? owner.Reactions.TakeRetaliation(target, now) : 0;
            if (retaliation > 0)
                ClearRetaliation(creature, owner);
            if (!detail.PreservesMatchingSetup || !matching)
                ClearSetup(creature);

            owner.Resources.LockAction(now);
            var main = GetItemInSlot(InventorySlot.RightHand, creature);
            var off = GetItemInSlot(InventorySlot.LeftHand, creature);
            var conditionalPower = (stage == MeleeTechniqueStage.None ? 0 :
                detail.GetStagePower(stage) + GetLineStat(creature, detail.Line, StatType.TechniqueConditionalPower)) + retaliation +
                (detail.UsesAbilityPower ? GetLineStat(creature, detail.Line, StatType.TechniqueAbilityPower) : 0);
            Activations[creature] = new Activation
            {
                Ability = ability,
                Profile = owner.Profile,
                Skill = owner.ProfileSkill,
                Target = target,
                SecondaryTarget = stage == MeleeTechniqueStage.Opening && detail.ImpactCount == 2 && detail.OpeningSweepRadiusMeters > 0f
                    ? Ability.GetHostileTargetsInSphere(creature, GetLocation(creature), detail.OpeningSweepRadiusMeters)
                        .FirstOrDefault(enemy => enemy != target && enemy != creature, OBJECT_INVALID)
                    : OBJECT_INVALID,
                Stage = stage,
                ConditionalPower = conditionalPower,
                ConditionalAccuracy = setupAccuracy,
                Weapon = main,
                OffHand = off,
                Area = GetArea(creature),
                WeaponDamage = WeaponDamage.GetAbilityDMG(creature, main, ability.SkillType),
                WeaponDamageType = WeaponDamage.GetDamageType(main)
            };
        }

        public static int GetConditionalPower(uint creature, uint target, int impactIndex = 0)
        {
            if (!TryGetImpactActivation(creature, target, out var activation))
                return 0;
            return TechniqueSequence.SplitPower(activation.ConditionalPower,
                activation.Ability.MeleeTechnique.ImpactCount, impactIndex);
        }

        public static uint GetImpactTarget(uint creature, uint primaryTarget, int impactIndex) =>
            Activations.TryGetValue(creature, out var activation) && activation.Target == primaryTarget &&
            impactIndex == 1 && activation.SecondaryTarget != OBJECT_INVALID
                ? activation.SecondaryTarget : primaryTarget;

        public static bool IsSweepActivation(uint creature, AbilityDetail ability) =>
            Activations.TryGetValue(creature, out var activation) && activation.Ability == ability &&
            activation.SecondaryTarget != OBJECT_INVALID;

        public static int GetConditionalAccuracy(uint creature, uint target) =>
            TryGetActivation(creature, target, out var activation) ? activation.ConditionalAccuracy : 0;

        /// <summary>Enemy-bound accuracy follows the setup, not the current weapon or ability skill.</summary>
        public static int GetAbilityAccuracy(uint creature, uint target, AbilityDetail ability)
        {
            if (ability == null || !ability.IsHostileAbility)
                return 0;
            if (TryGetActivation(creature, target, out var activation) && activation.Ability == ability)
                return activation.ConditionalAccuracy;
            var stage = GetSetupStage(creature, target);
            return stage is MeleeTechniqueStage.Opening or MeleeTechniqueStage.Finish
                ? GetLineStat(creature, Owners[creature].Line, StatType.TechniqueSetupAbilityAccuracy) : 0;
        }

        public static MeleeTechniqueStage GetSetupStage(uint creature, uint target)
        {
            if (!Owners.TryGetValue(creature, out var owner))
                return MeleeTechniqueStage.None;
            Refresh(creature, owner);
            return owner.Sequence.Matches(target, DateTime.UtcNow) ? owner.Sequence.Stage : MeleeTechniqueStage.None;
        }

        public static int GetPendingWeavePower(uint creature, uint target) =>
            GetSetupStage(creature, target) == MeleeTechniqueStage.Opening ? Owners[creature].Sequence.WeavePower : 0;

        public static int GetPendingRetaliationPower(uint creature, uint target)
        {
            if (!Owners.TryGetValue(creature, out var owner))
                return 0;
            Refresh(creature, owner);
            return owner.Reactions.GetRetaliation(target, DateTime.UtcNow);
        }

        public static int? GetCapturedWeaponDamage(uint creature, AbilityDetail ability) =>
            Activations.TryGetValue(creature, out var activation) && activation.Ability == ability
                ? activation.WeaponDamage : null;

        public static CombatDamageType? GetCapturedWeaponDamageType(uint creature, AbilityDetail ability) =>
            Activations.TryGetValue(creature, out var activation) && activation.Ability == ability
                ? activation.WeaponDamageType : null;

        public static bool IsImpactValid(uint creature, uint target, AbilityDetail ability) =>
            ability?.MeleeTechnique == null ||
            TryGetImpactActivation(creature, target, out var activation) && activation.Ability == ability;

        public static void CompleteActivation(uint creature, AbilityImpactSummary summary)
        {
            if (!Activations.TryGetValue(creature, out var activation) || activation.Ability != summary?.Ability)
                return;
            Activations.Remove(creature);
            var detail = activation.Ability.MeleeTechnique;
            var landed = summary.ImpactedTargetCount > 0;
            if (!landed || !IsGripSnapshotValid(creature, activation.Profile, activation.Skill, activation.Weapon, activation.OffHand, activation.Area) ||
                !GetIsObjectValid(activation.Target) || GetArea(activation.Target) != activation.Area ||
                !GetIsReactionTypeHostile(activation.Target, creature))
                return;

            var owner = GetOwner(creature);
            var paid = Combat.GetPaidAbilityStaminaCost(creature, activation.Ability);
            if (detail.RefundEligible && (!detail.RefundRequiresSetup || activation.Stage != MeleeTechniqueStage.None))
            {
                var requested = GetLineStat(creature, detail.Line, StatType.TechniqueRefundStamina);
                var cooldown = GetLineStat(creature, detail.Line, StatType.TechniqueRefundCooldownSeconds);
                using var recorderAttribution = CombatRecorder.BeginStatTriggerAttribution(creature, StatType.TechniqueRefundStamina);
                owner.Resources.TryRefund(paid, requested, cooldown, DateTime.UtcNow,
                    amount => Combat.RestoreAbilityHitStamina(creature, activation.Ability, amount, retainMinimumCost: false), out _);
            }
            if (paid > 0)
                TechniqueLanded?.Invoke(creature, activation.Target, activation.Ability, activation.Stage);
            if (activation.Stage != MeleeTechniqueStage.None &&
                detail.CreatedStage == MeleeTechniqueStage.None && IsValidEnemy(creature, activation.Target))
                TryOpenFinisherWindow(creature, activation.Target, detail.Line, activation.Stage, owner);

            if (!IsValidEnemy(creature, activation.Target) || detail.CreatedStage == MeleeTechniqueStage.None ||
                detail.CreatesStageOnlyWhenSequenced && activation.Stage == MeleeTechniqueStage.None)
                return;

            var duration = detail.SetupDurationSeconds + GetLineStat(creature, detail.Line, StatType.TechniqueWindowExtensionSeconds);
            var status = detail.SourceOwnedSetupStatusEffectFactory?.Invoke();
            if (status != null)
            {
                if (!StatusEffect.ApplyStatusEffect(creature, activation.Target, status, duration, activation.WeaponDamageType))
                    return;
                duration = (int)Math.Ceiling(status.GetRemainingDurationSeconds(DateTime.UtcNow));
            }
            else
            {
                owner.SetupMarkerType = ApplyOwnedEffect(creature,
                    activation.Profile.SetupMarkerFactory(detail.CreatedStage), duration);
            }
            owner.Line = detail.Line;
            owner.Area = activation.Area;
            owner.SourceOwnedStatusType = status?.GetType();
            owner.OrdinaryAccuracy = detail.OrdinaryAccuracyBonus +
                (status == null ? 0 : GetLineStat(creature, detail.Line, StatType.TechniqueDebuffTargetOrdinaryAccuracy));
            var weave = detail.CreatedStage == MeleeTechniqueStage.Opening &&
                        GetLineStat(creature, detail.Line, StatType.TechniqueWeaveEnabled) > 0
                ? GetLineStat(creature, detail.Line, StatType.TechniqueWeavePower) : 0;
            owner.Sequence.Begin(activation.Target, detail.CreatedStage, DateTime.UtcNow.AddSeconds(duration), weave);
        }

        private static void TryOpenFinisherWindow(uint creature, uint target, MeleeTechniqueLine line,
            MeleeTechniqueStage stage, OwnerState owner)
        {
            var evasion = GetLineStat(creature, line, StatType.TechniqueOpeningFinisherEvasionPercent);
            var movement = GetLineStat(creature, line, StatType.TechniqueOpeningFinisherMovementSpeedPercent);
            var haste = GetLineStat(creature, line, StatType.TechniqueFinishFinisherHastePercent);
            if (stage == MeleeTechniqueStage.Opening && evasion <= 0 && movement <= 0 ||
                stage == MeleeTechniqueStage.Finish && haste <= 0)
                return;
            var duration = GetLineStat(creature, line, StatType.TechniqueFinisherWindowDurationSeconds);
            if (!owner.Reactions.TryOpenWindow(target, stage, duration,
                    GetLineStat(creature, line, StatType.TechniqueFinisherWindowCooldownSeconds), DateTime.UtcNow))
                return;
            RemoveOwnedEffect(creature, owner.WindowStatusType);
            owner.WindowLine = line;
            owner.WindowStatusType = ApplyOwnedEffect(creature, stage == MeleeTechniqueStage.Opening
                ? owner.Profile.OpeningFinisherWindowFactory(evasion, movement)
                : owner.Profile.FinishFinisherWindowFactory(haste), duration);
        }

        public static void AbortActivation(uint creature) => Activations.Remove(creature);

        /// <summary>One sample per real native roll, including misses, excluding queued replacements.</summary>
        public static int PrepareOrdinaryAttempt(uint creature, uint target, SkillType skill, bool queuedReplacement, int attackIndex)
        {
            OrdinaryAttempts.Remove(creature);
            if (!Owners.TryGetValue(creature, out var owner))
                return 0;
            Refresh(creature, owner);
            var targetsEnemy = IsValidEnemy(creature, target);
            if (targetsEnemy && owner.Reactions.WindowTarget != 0 && owner.Reactions.WindowTarget != target)
                ClearReactiveWindows(creature, owner);
            if (targetsEnemy && owner.Reactions.RetaliationTarget != 0 && owner.Reactions.RetaliationTarget != target)
                ClearRetaliation(creature, owner);
            if (owner.Sequence.Stage == MeleeTechniqueStage.None)
                return 0;
            if (!owner.Sequence.Matches(target, DateTime.UtcNow))
            {
                ClearSetup(creature);
                return 0;
            }
            if (queuedReplacement)
                return 0;
            var power = owner.Sequence.TakeWeave(target, DateTime.UtcNow);
            OrdinaryAttempts[creature] = new OrdinaryAttempt(target, skill, attackIndex, power);
            return owner.OrdinaryAccuracy;
        }

        /// <summary>Duplicate/discarded damage queries cannot pay the sampled weave again.</summary>
        public static int TakeOrdinaryPower(uint creature, uint target, SkillType skill, int attackIndex, bool landed)
        {
            if (!landed || !OrdinaryAttempts.TryGetValue(creature, out var attempt) ||
                attempt.Target != target || attempt.Skill != skill || attempt.AttackIndex != attackIndex)
                return 0;
            OrdinaryAttempts.Remove(creature);
            return attempt.Power;
        }

        public static int ApplyOrdinaryDamageModifiers(uint creature, int damage, CombatDamageType damageType)
        {
            if (damageType.IsPhysicalDamageType())
                damage = Combat.ApplyPercentDamageAdjustment(damage,
                    -Stat.GetStatAdjustment(creature, StatType.OrdinaryPhysicalDamagePenaltyPercent));
            return damage;
        }

        public static int GetStaminaDiscount(uint creature, AbilityDetail ability, int cost, bool consume)
        {
            if (cost <= 0 || ability == null || !Owners.TryGetValue(creature, out var owner))
                return 0;
            Refresh(creature, owner);
            var amount = consume ? owner.Resources.TakeDiscount(cost, DateTime.UtcNow) :
                Math.Min(cost, owner.Resources.GetDiscount(DateTime.UtcNow));
            if (consume && amount > 0)
            {
                RemoveOwnedEffect(creature, owner.DiscountStatusType);
                owner.DiscountStatusType = null;
            }
            return amount;
        }

        /// <summary>Captures actual HP before a typed direct hit; periodic/triggered/reflected paths never enter.</summary>
        public static void PrepareDirectDamage(uint attacker, uint defender, CombatDamageDeliveryType deliveryType)
        {
            if (deliveryType != CombatDamageDeliveryType.Direct || attacker == defender ||
                !GetIsObjectValid(defender) || !GetIsObjectValid(attacker) ||
                !GetIsReactionTypeHostile(attacker, defender))
                return;
            if (Stat.GetStatAdjustment(defender, StatType.TechniqueRetaliationPower) <= 0 &&
                Stat.GetStatAdjustment(defender, StatType.DirectHostileDamageNextAbilityStaminaDiscount) <= 0)
                return;
            // Native GetCurrentHitPoints includes temporary HP; subtract its pool so absorbed hits cannot arm HP-damage reactions.
            DirectDamage[defender] = new DirectDamageSnapshot(attacker,
                Math.Max(0, GetCurrentHitPoints(defender) - TemporaryHitPointEffects.GetRemaining(defender)), DateTime.UtcNow);
        }

        [NWNEventHandler(ScriptName.OnCreatureDamagedAfter)]
        [NWNEventHandler(ScriptName.OnPlayerDamaged)]
        public static void OnDirectDamageApplied()
        {
            var creature = OBJECT_SELF;
            if (!DirectDamage.Remove(creature, out var snapshot) ||
                snapshot.Attacker != GetLastDamager(creature) ||
                (DateTime.UtcNow - snapshot.CapturedAt).TotalSeconds > 1 || GetIsDead(creature))
                return;
            var hp = Math.Max(0, GetCurrentHitPoints(creature) - TemporaryHitPointEffects.GetRemaining(creature));
            if (hp >= snapshot.HitPoints || !GetIsReactionTypeHostile(snapshot.Attacker, creature))
                return;

            var owner = GetOwner(creature);
            owner.LastHostileActivity = DateTime.UtcNow;
            Refresh(creature, owner);
            foreach (var source in Stat.GetStatSources(creature, StatType.TechniqueRetaliationPower))
            {
                var skill = (SkillType)source[StatType.TechniquePresentationSkillType];
                var line = (MeleeTechniqueLine)source[StatType.TechniqueLine];
                if (line == MeleeTechniqueLine.None || !Profiles.TryGetValue(skill, out var profile) ||
                    !IsValidEnemy(creature, snapshot.Attacker) || !owner.Reactions.TryGrantIndependentRetaliation(snapshot.Attacker,
                        source[StatType.TechniqueRetaliationPower], source[StatType.TechniqueRetaliationDurationSeconds],
                        source[StatType.TechniqueRetaliationCooldownSeconds], DateTime.UtcNow))
                    continue;
                owner.RetaliationLine = line;
                owner.RetaliationProfileSkill = skill;
                owner.RetaliationArea = GetArea(creature);
                RemoveOwnedEffect(creature, owner.RetaliationStatusType);
                owner.RetaliationStatusType = ApplyOwnedEffect(creature, profile.RetaliationMarkerFactory(),
                    source[StatType.TechniqueRetaliationDurationSeconds]);
                break;
            }
            foreach (var source in Stat.GetStatSources(creature, StatType.DirectHostileDamageNextAbilityStaminaDiscount))
            {
                var skill = (SkillType)source[StatType.TechniquePresentationSkillType];
                if (!Profiles.TryGetValue(skill, out var profile) || !owner.Resources.TryGrantDiscount(
                        source[StatType.DirectHostileDamageNextAbilityStaminaDiscount],
                        source[StatType.DirectHostileDamageNextAbilityDiscountDurationSeconds],
                        source[StatType.DirectHostileDamageNextAbilityDiscountCooldownSeconds], DateTime.UtcNow))
                    continue;
                owner.DiscountProfileSkill = skill;
                owner.DiscountStatusType = ApplyOwnedEffect(creature, profile.StaminaDiscountMarkerFactory(),
                    source[StatType.DirectHostileDamageNextAbilityDiscountDurationSeconds]);
                break;
            }
        }

        public static void ClearSetup(uint creature)
        {
            if (!Owners.TryGetValue(creature, out var owner))
                return;
            if (owner.SourceOwnedStatusType != null && GetIsObjectValid(owner.Sequence.Target))
                StatusEffect.RemoveStatusEffect(owner.Sequence.Target, owner.SourceOwnedStatusType, creature, false);
            owner.SourceOwnedStatusType = null;
            owner.Sequence.Clear();
            OrdinaryAttempts.Remove(creature);
            RemoveOwnedEffect(creature, owner.SetupMarkerType);
            owner.SetupMarkerType = null;
        }

        private static void ClearRetaliation(uint creature, OwnerState owner)
        {
            owner.Reactions.ClearRetaliation();
            RemoveOwnedEffect(creature, owner.RetaliationStatusType);
            owner.RetaliationStatusType = null;
            owner.RetaliationProfileSkill = SkillType.Invalid;
            owner.RetaliationLine = MeleeTechniqueLine.None;
            owner.RetaliationArea = OBJECT_INVALID;
        }

        public static void OnStanceChanged(uint creature)
        {
            ClearSetup(creature);
            if (Owners.TryGetValue(creature, out var owner))
            {
                ClearReactiveWindows(creature, owner);
                ClearRetaliation(creature, owner);
            }
        }

        private static void ClearReactiveWindows(uint creature, OwnerState owner)
        {
            owner.Reactions.ClearWindow();
            RemoveOwnedEffect(creature, owner.WindowStatusType);
            owner.WindowStatusType = null;
        }

        public static void ClearBenefits(uint creature)
        {
            ClearSetup(creature);
            Activations.Remove(creature);
            if (Owners.TryGetValue(creature, out var owner))
            {
                owner.Resources.ClearBenefits();
                ClearRetaliation(creature, owner);
                ClearReactiveWindows(creature, owner);
                RemoveOwnedEffect(creature, owner.DiscountStatusType);
                owner.DiscountStatusType = null;
                owner.DiscountProfileSkill = SkillType.Invalid;
            }
        }

        [NWNEventHandler(ScriptName.OnModuleEquip)]
        public static void OnEquip() => OnEquipmentChanged(GetPCItemLastEquippedBy());

        [NWNEventHandler(ScriptName.OnModuleUnequip)]
        public static void OnUnequip() => OnEquipmentChanged(GetPCItemLastUnequippedBy());

        private static void OnEquipmentChanged(uint creature)
        {
            // A cast still cannot change weapons mid-impact. Already earned benefits retain
            // their original target, expiry and cooldown when equipment changes.
            Activations.Remove(creature);
        }

        [NWNEventHandler(ScriptName.OnAreaExit)]
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void OnExit()
        {
            var creature = GetExitingObject();
            ClearBenefits(creature);
            ClearTargetReferences(creature);
        }

        [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
        public static void OnDeath()
        {
            ClearBenefits(OBJECT_SELF);
            ClearTargetReferences(OBJECT_SELF);
        }

        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void OnPlayerDeath()
        {
            var creature = GetLastPlayerDied();
            ClearBenefits(creature);
            ClearTargetReferences(creature);
        }

        [NWNEventHandler(ScriptName.OnModuleHeartbeat)]
        public static void RefreshAll()
        {
            foreach (var (creature, owner) in Owners.ToArray())
            {
                if (!GetIsObjectValid(creature))
                {
                    ClearSetup(creature);
                    Owners.Remove(creature);
                    Activations.Remove(creature);
                    DirectDamage.Remove(creature);
                    continue;
                }
                Refresh(creature, owner);
            }
            foreach (var creature in DirectDamage.Where(pair => (DateTime.UtcNow - pair.Value.CapturedAt).TotalSeconds > 1)
                         .Select(pair => pair.Key).ToArray())
                DirectDamage.Remove(creature);
        }

        private static void ClearTargetReferences(uint target)
        {
            foreach (var creature in Owners.Where(pair => pair.Value.Sequence.Target == target || pair.Value.Reactions.WindowTarget == target || pair.Value.Reactions.RetaliationTarget == target)
                         .Select(pair => pair.Key).ToArray())
            {
                ClearSetup(creature);
                ClearReactiveWindows(creature, Owners[creature]);
                ClearRetaliation(creature, Owners[creature]);
            }
        }

        private static OwnerState GetOwner(uint creature)
        {
            if (!Owners.TryGetValue(creature, out var owner))
                Owners[creature] = owner = new OwnerState();
            return owner;
        }

        private static void Refresh(uint creature, OwnerState owner)
        {
            var combatEnded = (DateTime.UtcNow - owner.LastHostileActivity).TotalSeconds > 30 && !Combat.HasRecentHostileCombatActivity(creature);
            if (!GetIsObjectValid(creature) || GetIsDead(creature) || combatEnded)
            {
                ClearBenefits(creature);
                return;
            }
            if (owner.Reactions.WindowTarget != 0 &&
                (owner.Reactions.GetWindowStage(DateTime.UtcNow) == MeleeTechniqueStage.None ||
                 !IsValidEnemy(creature, owner.Reactions.WindowTarget) ||
                 GetLineStat(creature, owner.WindowLine, StatType.TechniqueFinisherWindowDurationSeconds) <= 0))
                ClearReactiveWindows(creature, owner);
            if (owner.Reactions.RetaliationTarget != 0 &&
                (owner.Reactions.GetRetaliation(owner.Reactions.RetaliationTarget, DateTime.UtcNow) <= 0 ||
                 !IsValidEnemy(creature, owner.Reactions.RetaliationTarget) || GetArea(creature) != owner.RetaliationArea ||
                 !Profiles.ContainsKey(owner.RetaliationProfileSkill) ||
                 GetLineStat(creature, owner.RetaliationLine, StatType.TechniqueRetaliationPower) <= 0))
                ClearRetaliation(creature, owner);
            if (owner.Sequence.Stage == MeleeTechniqueStage.None)
                return;
            if (!SetupMatches(creature, owner, owner.Sequence.Target, owner.HeldCast?.Ability, DateTime.UtcNow) ||
                !IsValidEnemy(creature, owner.Sequence.Target) ||
                GetArea(creature) != owner.Area ||
                owner.SourceOwnedStatusType != null && !StatusEffect.HasStatusEffect(owner.Sequence.Target, owner.SourceOwnedStatusType, creature))
                ClearSetup(creature);
        }

        private static bool TryGetActivation(uint creature, uint target, out Activation activation)
        {
            return Activations.TryGetValue(creature, out activation) && activation.Target == target &&
                   IsGripSnapshotValid(creature, activation.Profile, activation.Skill, activation.Weapon, activation.OffHand, activation.Area) &&
                   IsValidEnemy(creature, target);
        }

        private static bool TryGetImpactActivation(uint creature, uint target, out Activation activation)
        {
            return Activations.TryGetValue(creature, out activation) &&
                   (activation.Target == target || activation.SecondaryTarget == target) &&
                   IsGripSnapshotValid(creature, activation.Profile, activation.Skill, activation.Weapon, activation.OffHand, activation.Area) &&
                   IsValidEnemy(creature, target);
        }

        private static bool IsGripSnapshotValid(uint creature, MeleeTechniqueProfile profile, SkillType skill,
            uint main, uint off, uint area) =>
            GetIsObjectValid(creature) && !GetIsDead(creature) &&
            TryGetEquippedProfile(creature, out var currentSkill, out var currentProfile) &&
            currentSkill == skill && currentProfile == profile &&
            GetArea(creature) == area && GetItemInSlot(InventorySlot.RightHand, creature) == main &&
            GetItemInSlot(InventorySlot.LeftHand, creature) == off;

        private static bool TryGetEquippedProfile(uint creature, out SkillType skill, out MeleeTechniqueProfile profile)
        {
            skill = SkillType.Invalid;
            profile = null;
            if (!GetIsObjectValid(creature))
                return false;
            var weapon = GetItemInSlot(InventorySlot.RightHand, creature);
            if (!GetIsObjectValid(weapon))
                return false;
            skill = Skill.GetSkillTypeByBaseItem(GetBaseItemType(weapon));
            return Profiles.TryGetValue(skill, out profile) && profile.IsValid && profile.HasValidEquipment(creature);
        }

        private static Type ApplyOwnedEffect(uint creature, IStatusEffect effect, float duration)
        {
            if (effect == null || !StatusEffect.ApplyStatusEffect(creature, creature, effect, duration))
                return null;
            return effect.GetType();
        }

        private static void RemoveOwnedEffect(uint creature, Type effectType)
        {
            if (effectType != null && GetIsObjectValid(creature))
                StatusEffect.RemoveStatusEffect(creature, effectType, creature, false);
        }

        private static bool IsValidEnemy(uint creature, uint target) =>
            GetIsObjectValid(target) && !GetIsDead(target) && GetArea(creature) == GetArea(target) &&
            GetIsReactionTypeHostile(target, creature);

        private sealed class OwnerState
        {
            public MeleeTechniqueProfile Profile { get; set; }
            public SkillType ProfileSkill { get; set; } = SkillType.Invalid;
            public Type SetupMarkerType { get; set; }
            public Type WindowStatusType { get; set; }
            public Type DiscountStatusType { get; set; }
            public Type RetaliationStatusType { get; set; }
            public MeleeTechniqueLine RetaliationLine { get; set; }
            public SkillType RetaliationProfileSkill { get; set; } = SkillType.Invalid;
            public uint RetaliationArea { get; set; }
            public TechniqueSequence Sequence { get; } = new();
            public TechniqueResourceState Resources { get; } = new();
            public TechniqueReactiveState Reactions { get; } = new();
            public MeleeTechniqueLine Line { get; set; }
            public MeleeTechniqueLine WindowLine { get; set; }
            public SkillType DiscountProfileSkill { get; set; } = SkillType.Invalid;
            public int OrdinaryAccuracy { get; set; }
            public Type SourceOwnedStatusType { get; set; }
            public uint Area { get; set; }
            public DateTime LastHostileActivity { get; set; }
            public HeldCast HeldCast { get; set; }
        }

        /// <summary>The setup a cast in progress started with; valid only while that activation is running.</summary>
        private sealed record HeldCast(string ActivationId, AbilityDetail Ability, uint Target, MeleeTechniqueStage Stage, DateTime ExpiresAt);

        private sealed class Activation
        {
            public AbilityDetail Ability { get; init; }
            public MeleeTechniqueProfile Profile { get; init; }
            public SkillType Skill { get; init; }
            public uint Target { get; init; }
            public uint SecondaryTarget { get; init; } = OBJECT_INVALID;
            public MeleeTechniqueStage Stage { get; init; }
            public int ConditionalPower { get; init; }
            public int ConditionalAccuracy { get; init; }
            public uint Weapon { get; init; }
            public uint OffHand { get; init; }
            public uint Area { get; init; }
            public int WeaponDamage { get; init; }
            public CombatDamageType WeaponDamageType { get; init; }
        }

        private sealed record OrdinaryAttempt(uint Target, SkillType Skill, int AttackIndex, int Power);
        private sealed record DirectDamageSnapshot(uint Attacker, int HitPoints, DateTime CapturedAt);
    }
}
