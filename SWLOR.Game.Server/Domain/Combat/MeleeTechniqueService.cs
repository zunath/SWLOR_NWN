using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>Enemy-bound melee sequences driven by ability metadata and source-scoped stats.</summary>
    public class MeleeTechniqueService : IMeleeTechniqueService
    {
        private readonly Dictionary<SkillType, MeleeTechniqueProfile> Profiles = new();
        private readonly Dictionary<uint, OwnerState> Owners = new();
        private readonly Dictionary<uint, Activation> Activations = new();
        private readonly Dictionary<uint, OrdinaryAttempt> OrdinaryAttempts = new();
        private readonly Dictionary<uint, DirectDamageSnapshot> DirectDamage = new();
        private readonly Lazy<IStatService> _stat;
        private readonly Lazy<IAbilityService> _ability;
        private readonly ICombatRecorderService _combatRecorder;
        private readonly Lazy<IStatusEffectService> _statusEffect;

        public MeleeTechniqueService(
            Lazy<IStatService> stat,
            Lazy<IAbilityService> ability,
            ICombatRecorderService combatRecorder,
            Lazy<IStatusEffectService> statusEffect)
        {
            _stat = stat;
            _ability = ability;
            _combatRecorder = combatRecorder;
            _statusEffect = statusEffect;
        }

        /// <summary>One notification per completed, landed technique which paid stamina.</summary>
        public event Action<uint, uint, AbilityDetail, MeleeTechniqueStage> TechniqueLanded;

        /// <summary>Replaces this skill's content profile; invalid definitions fail closed.</summary>
        public void RegisterProfile(SkillType skill, MeleeTechniqueProfile profile)
        {
            UnregisterProfile(skill);
            if (skill == SkillType.Invalid || profile?.IsValid != true)
                throw new ArgumentException("A technique profile requires a weapon skill and presentation factories.");
            Profiles[skill] = profile;
        }

        public void UnregisterProfile(SkillType skill)
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

        public void ResetProfiles()
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

        public int GetLineStat(uint creature, MeleeTechniqueLine line, StatType payload) =>
            GetStatSources(creature, payload)
                .Where(source => source[StatType.TechniqueLine] == (int)line)
                .Sum(source => source[payload]);

        public string ValidateActivation(uint creature, AbilityDetail ability, uint target)
        {
            if (ability?.MeleeTechnique == null)
                return string.Empty;
            // Techniques are cross-skill: any equipped weapon can perform them.
            if (!Profiles.TryGetValue(ability.SkillType, out var profile) || !profile.IsValid)
                return "This technique's weapon profile is unavailable.";
            if (ability.IsHostileAbility && !GetOwner(creature).Resources.CanActivate(Clock.UtcNow))
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
        public void HoldSetupForCast(uint creature, uint target, AbilityDetail ability, string activationId)
        {
            if (!Owners.TryGetValue(creature, out var owner))
                return;
            owner.HeldCast = null;
            var required = ability?.MeleeTechnique?.RequiredStages ?? MeleeTechniqueStage.None;
            if (required == MeleeTechniqueStage.None || string.IsNullOrEmpty(activationId))
                return;
            Refresh(creature, owner);
            if (owner.Line != ability.MeleeTechnique.Line || !owner.Sequence.Matches(target, Clock.UtcNow) ||
                (owner.Sequence.Stage & required) == 0)
                return;
            owner.HeldCast = new HeldCast(activationId, ability, target, owner.Sequence.Stage, owner.Sequence.ExpiresAt);
        }

        private MeleeTechniqueStage GetRequiredSetupStage(uint creature, uint target, AbilityDetail ability)
        {
            if (!Owners.TryGetValue(creature, out var owner))
                return MeleeTechniqueStage.None;
            Refresh(creature, owner);
            return owner.Line == ability.MeleeTechnique.Line && SetupMatches(creature, owner, target, ability, Clock.UtcNow)
                ? owner.Sequence.Stage : MeleeTechniqueStage.None;
        }

        /// <summary>The live setup, or the one held for this ability's cast in progress.</summary>
        private bool SetupMatches(uint creature, OwnerState owner, uint target, AbilityDetail ability, DateTime now) =>
            owner.Sequence.Matches(target, now) || IsSetupHeld(creature, owner, target, ability);

        private bool IsSetupHeld(uint creature, OwnerState owner, uint target, AbilityDetail ability) =>
            owner.HeldCast is { } held && held.Ability == ability && held.Target == target &&
            owner.Sequence.Target == target && owner.Sequence.Stage == held.Stage && owner.Sequence.ExpiresAt == held.ExpiresAt &&
            GetActiveAbilityActivationId(creature) == held.ActivationId;

        private string DescribeStages(MeleeTechniqueStage stages) => string.Join(" or ",
            new[] { MeleeTechniqueStage.Opening, MeleeTechniqueStage.Finish, MeleeTechniqueStage.Distraction }
                .Where(stage => (stages & stage) != 0));

        /// <summary>Called after all paid requirements, before an impact is launched.</summary>
        public void BeginPaidActivation(uint creature, uint target, AbilityDetail ability)
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
            var now = Clock.UtcNow;
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
                    ? GetHostileTargetsInSphere(creature, GetLocation(creature), detail.OpeningSweepRadiusMeters)
                        .FirstOrDefault(enemy => enemy != target && enemy != creature, OBJECT_INVALID)
                    : OBJECT_INVALID,
                Stage = stage,
                ConditionalPower = conditionalPower,
                ConditionalAccuracy = setupAccuracy,
                Weapon = main,
                OffHand = off,
                Area = GetArea(creature),
                WeaponDamage = GetAbilityDMG(creature, main, ability.SkillType),
                WeaponDamageType = GetWeaponDamageType(main)
            };
        }

        public int GetConditionalPower(uint creature, uint target, int impactIndex)
        {
            if (!TryGetImpactActivation(creature, target, out var activation))
                return 0;
            return TechniqueSequence.SplitPower(activation.ConditionalPower,
                activation.Ability.MeleeTechnique.ImpactCount, impactIndex);
        }

        public uint GetImpactTarget(uint creature, uint primaryTarget, int impactIndex) =>
            Activations.TryGetValue(creature, out var activation) && activation.Target == primaryTarget &&
            impactIndex == 1 && activation.SecondaryTarget != OBJECT_INVALID
                ? activation.SecondaryTarget : primaryTarget;

        public bool IsSweepActivation(uint creature, AbilityDetail ability) =>
            Activations.TryGetValue(creature, out var activation) && activation.Ability == ability &&
            activation.SecondaryTarget != OBJECT_INVALID;

        public int GetConditionalAccuracy(uint creature, uint target) =>
            TryGetActivation(creature, target, out var activation) ? activation.ConditionalAccuracy : 0;

        /// <summary>Enemy-bound accuracy follows the setup, not the current weapon or ability skill.</summary>
        public int GetAbilityAccuracy(uint creature, uint target, AbilityDetail ability)
        {
            if (ability == null || !ability.IsHostileAbility)
                return 0;
            if (TryGetActivation(creature, target, out var activation) && activation.Ability == ability)
                return activation.ConditionalAccuracy;
            var stage = GetSetupStage(creature, target);
            return stage is MeleeTechniqueStage.Opening or MeleeTechniqueStage.Finish
                ? GetLineStat(creature, Owners[creature].Line, StatType.TechniqueSetupAbilityAccuracy) : 0;
        }

        public MeleeTechniqueStage GetSetupStage(uint creature, uint target)
        {
            if (!Owners.TryGetValue(creature, out var owner))
                return MeleeTechniqueStage.None;
            Refresh(creature, owner);
            return owner.Sequence.Matches(target, Clock.UtcNow) ? owner.Sequence.Stage : MeleeTechniqueStage.None;
        }

        public int GetPendingWeavePower(uint creature, uint target) =>
            GetSetupStage(creature, target) == MeleeTechniqueStage.Opening ? Owners[creature].Sequence.WeavePower : 0;

        public int GetPendingRetaliationPower(uint creature, uint target)
        {
            if (!Owners.TryGetValue(creature, out var owner))
                return 0;
            Refresh(creature, owner);
            return owner.Reactions.GetRetaliation(target, Clock.UtcNow);
        }

        public int? GetCapturedWeaponDamage(uint creature, AbilityDetail ability) =>
            Activations.TryGetValue(creature, out var activation) && activation.Ability == ability
                ? activation.WeaponDamage : null;

        public CombatDamageType? GetCapturedWeaponDamageType(uint creature, AbilityDetail ability) =>
            Activations.TryGetValue(creature, out var activation) && activation.Ability == ability
                ? activation.WeaponDamageType : null;

        public bool IsImpactValid(uint creature, uint target, AbilityDetail ability) =>
            ability?.MeleeTechnique == null ||
            TryGetImpactActivation(creature, target, out var activation) && activation.Ability == ability;

        public void CompleteActivation(uint creature, AbilityImpactSummary summary)
        {
            if (!Activations.TryGetValue(creature, out var activation) || activation.Ability != summary?.Ability)
                return;
            Activations.Remove(creature);
            var detail = activation.Ability.MeleeTechnique;
            var landed = summary.ImpactedTargetCount > 0;
            if (!landed || !IsEquipmentSnapshotValid(creature, activation.Weapon, activation.OffHand, activation.Area) ||
                !GetIsObjectValid(activation.Target) || GetArea(activation.Target) != activation.Area ||
                !GetIsReactionTypeHostile(activation.Target, creature))
                return;

            var owner = GetOwner(creature);
            var paid = GetPaidAbilityStaminaCost(creature, activation.Ability);
            if (detail.RefundEligible && (!detail.RefundRequiresSetup || activation.Stage != MeleeTechniqueStage.None))
            {
                var requested = GetLineStat(creature, detail.Line, StatType.TechniqueRefundStamina);
                var cooldown = GetLineStat(creature, detail.Line, StatType.TechniqueRefundCooldownSeconds);
                using var recorderAttribution = BeginStatTriggerAttribution(creature, StatType.TechniqueRefundStamina);
                owner.Resources.TryRefund(paid, requested, cooldown, Clock.UtcNow,
                    amount => RestoreAbilityHitStamina(creature, activation.Ability, amount, retainMinimumCost: false), out _);
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
                if (!ApplyStatusEffect(creature, activation.Target, status, duration, activation.WeaponDamageType))
                    return;
                duration = (int)Math.Ceiling(status.GetRemainingDurationSeconds(Clock.UtcNow));
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
            owner.Sequence.Begin(activation.Target, detail.CreatedStage, Clock.UtcNow.AddSeconds(duration), weave);
        }

        private void TryOpenFinisherWindow(uint creature, uint target, MeleeTechniqueLine line,
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
                    GetLineStat(creature, line, StatType.TechniqueFinisherWindowCooldownSeconds), Clock.UtcNow))
                return;
            RemoveOwnedEffect(creature, owner.WindowStatusType);
            owner.WindowLine = line;
            owner.WindowStatusType = ApplyOwnedEffect(creature, stage == MeleeTechniqueStage.Opening
                ? owner.Profile.OpeningFinisherWindowFactory(evasion, movement)
                : owner.Profile.FinishFinisherWindowFactory(haste), duration);
        }

        public void AbortActivation(uint creature) => Activations.Remove(creature);

        /// <summary>One sample per real native roll, including misses, excluding queued replacements.</summary>
        public int PrepareOrdinaryAttempt(uint creature, uint target, SkillType skill, bool queuedReplacement, int attackIndex)
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
            if (!owner.Sequence.Matches(target, Clock.UtcNow))
            {
                ClearSetup(creature);
                return 0;
            }
            if (queuedReplacement)
                return 0;
            var power = owner.Sequence.TakeWeave(target, Clock.UtcNow);
            OrdinaryAttempts[creature] = new OrdinaryAttempt(target, skill, attackIndex, power);
            return owner.OrdinaryAccuracy;
        }

        /// <summary>Duplicate/discarded damage queries cannot pay the sampled weave again.</summary>
        public int TakeOrdinaryPower(uint creature, uint target, SkillType skill, int attackIndex, bool landed)
        {
            if (!landed || !OrdinaryAttempts.TryGetValue(creature, out var attempt) ||
                attempt.Target != target || attempt.Skill != skill || attempt.AttackIndex != attackIndex)
                return 0;
            OrdinaryAttempts.Remove(creature);
            return attempt.Power;
        }

        public int ApplyOrdinaryDamageModifiers(uint creature, int damage, CombatDamageType damageType)
        {
            if (damageType.IsPhysicalDamageType())
                damage = ApplyPercentDamageAdjustment(damage,
                    -GetStatAdjustment(creature, StatType.OrdinaryPhysicalDamagePenaltyPercent));
            return damage;
        }

        public int GetStaminaDiscount(uint creature, AbilityDetail ability, int cost, bool consume)
        {
            if (cost <= 0 || ability == null || !Owners.TryGetValue(creature, out var owner))
                return 0;
            Refresh(creature, owner);
            var amount = consume ? owner.Resources.TakeDiscount(cost, Clock.UtcNow) :
                Math.Min(cost, owner.Resources.GetDiscount(Clock.UtcNow));
            if (consume && amount > 0)
            {
                RemoveOwnedEffect(creature, owner.DiscountStatusType);
                owner.DiscountStatusType = null;
            }
            return amount;
        }

        /// <summary>Captures actual HP before a typed direct hit; periodic/triggered/reflected paths never enter.</summary>
        public void PrepareDirectDamage(uint attacker, uint defender, CombatDamageDeliveryType deliveryType)
        {
            if (deliveryType != CombatDamageDeliveryType.Direct || attacker == defender ||
                !GetIsObjectValid(defender) || !GetIsObjectValid(attacker) ||
                !GetIsReactionTypeHostile(attacker, defender))
                return;
            if (GetStatAdjustment(defender, StatType.TechniqueRetaliationPower) <= 0 &&
                GetStatAdjustment(defender, StatType.DirectHostileDamageNextAbilityStaminaDiscount) <= 0)
                return;
            // Native GetCurrentHitPoints includes temporary HP; subtract its pool so absorbed hits cannot arm HP-damage reactions.
            DirectDamage[defender] = new DirectDamageSnapshot(attacker,
                Math.Max(0, GetCurrentHitPoints(defender) - GetTemporaryHitPoints(defender)), Clock.UtcNow);
        }

        public void OnDirectDamageApplied()
        {
            var creature = OBJECT_SELF;
            if (!DirectDamage.Remove(creature, out var snapshot) ||
                snapshot.Attacker != GetLastDamager(creature) ||
                (Clock.UtcNow - snapshot.CapturedAt).TotalSeconds > 1 || GetIsDead(creature))
                return;
            var hp = Math.Max(0, GetCurrentHitPoints(creature) - GetTemporaryHitPoints(creature));
            if (hp >= snapshot.HitPoints || !GetIsReactionTypeHostile(snapshot.Attacker, creature))
                return;

            var owner = GetOwner(creature);
            owner.LastHostileActivity = Clock.UtcNow;
            Refresh(creature, owner);
            foreach (var source in GetStatSources(creature, StatType.TechniqueRetaliationPower))
            {
                var skill = (SkillType)source[StatType.TechniquePresentationSkillType];
                var line = (MeleeTechniqueLine)source[StatType.TechniqueLine];
                if (line == MeleeTechniqueLine.None || !Profiles.TryGetValue(skill, out var profile) ||
                    !IsValidEnemy(creature, snapshot.Attacker) || !owner.Reactions.TryGrantIndependentRetaliation(snapshot.Attacker,
                        source[StatType.TechniqueRetaliationPower], source[StatType.TechniqueRetaliationDurationSeconds],
                        source[StatType.TechniqueRetaliationCooldownSeconds], Clock.UtcNow))
                    continue;
                owner.RetaliationLine = line;
                owner.RetaliationProfileSkill = skill;
                owner.RetaliationArea = GetArea(creature);
                RemoveOwnedEffect(creature, owner.RetaliationStatusType);
                owner.RetaliationStatusType = ApplyOwnedEffect(creature, profile.RetaliationMarkerFactory(),
                    source[StatType.TechniqueRetaliationDurationSeconds]);
                break;
            }
            foreach (var source in GetStatSources(creature, StatType.DirectHostileDamageNextAbilityStaminaDiscount))
            {
                var skill = (SkillType)source[StatType.TechniquePresentationSkillType];
                if (!Profiles.TryGetValue(skill, out var profile) || !owner.Resources.TryGrantDiscount(
                        source[StatType.DirectHostileDamageNextAbilityStaminaDiscount],
                        source[StatType.DirectHostileDamageNextAbilityDiscountDurationSeconds],
                        source[StatType.DirectHostileDamageNextAbilityDiscountCooldownSeconds], Clock.UtcNow))
                    continue;
                owner.DiscountProfileSkill = skill;
                owner.DiscountStatusType = ApplyOwnedEffect(creature, profile.StaminaDiscountMarkerFactory(),
                    source[StatType.DirectHostileDamageNextAbilityDiscountDurationSeconds]);
                break;
            }
        }

        public void ClearSetup(uint creature)
        {
            if (!Owners.TryGetValue(creature, out var owner))
                return;
            if (owner.SourceOwnedStatusType != null && GetIsObjectValid(owner.Sequence.Target))
                RemoveStatusEffect(owner.Sequence.Target, owner.SourceOwnedStatusType, creature, false);
            owner.SourceOwnedStatusType = null;
            owner.Sequence.Clear();
            OrdinaryAttempts.Remove(creature);
            RemoveOwnedEffect(creature, owner.SetupMarkerType);
            owner.SetupMarkerType = null;
        }

        private void ClearRetaliation(uint creature, OwnerState owner)
        {
            owner.Reactions.ClearRetaliation();
            RemoveOwnedEffect(creature, owner.RetaliationStatusType);
            owner.RetaliationStatusType = null;
            owner.RetaliationProfileSkill = SkillType.Invalid;
            owner.RetaliationLine = MeleeTechniqueLine.None;
            owner.RetaliationArea = OBJECT_INVALID;
        }

        public void OnStanceChanged(uint creature)
        {
            ClearSetup(creature);
            if (Owners.TryGetValue(creature, out var owner))
            {
                ClearReactiveWindows(creature, owner);
                ClearRetaliation(creature, owner);
            }
        }

        private void ClearReactiveWindows(uint creature, OwnerState owner)
        {
            owner.Reactions.ClearWindow();
            RemoveOwnedEffect(creature, owner.WindowStatusType);
            owner.WindowStatusType = null;
        }

        public void ClearBenefits(uint creature)
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

        public void OnEquip() => OnEquipmentChanged(GetPCItemLastEquippedBy());

        public void OnUnequip() => OnEquipmentChanged(GetPCItemLastUnequippedBy());

        private void OnEquipmentChanged(uint creature)
        {
            // A cast still cannot change weapons mid-impact. Already earned benefits retain
            // their original target, expiry and cooldown when equipment changes.
            Activations.Remove(creature);
        }

        public void OnExit()
        {
            var creature = GetExitingObject();
            ClearBenefits(creature);
            ClearTargetReferences(creature);
        }

        public void OnDeath()
        {
            ClearBenefits(OBJECT_SELF);
            ClearTargetReferences(OBJECT_SELF);
        }

        public void OnPlayerDeath()
        {
            var creature = GetLastPlayerDied();
            ClearBenefits(creature);
            ClearTargetReferences(creature);
        }

        public void RefreshAll()
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
            foreach (var creature in DirectDamage.Where(pair => (Clock.UtcNow - pair.Value.CapturedAt).TotalSeconds > 1)
                         .Select(pair => pair.Key).ToArray())
                DirectDamage.Remove(creature);
        }

        private void ClearTargetReferences(uint target)
        {
            foreach (var creature in Owners.Where(pair => pair.Value.Sequence.Target == target || pair.Value.Reactions.WindowTarget == target || pair.Value.Reactions.RetaliationTarget == target)
                         .Select(pair => pair.Key).ToArray())
            {
                ClearSetup(creature);
                ClearReactiveWindows(creature, Owners[creature]);
                ClearRetaliation(creature, Owners[creature]);
            }
        }

        private OwnerState GetOwner(uint creature)
        {
            if (!Owners.TryGetValue(creature, out var owner))
                Owners[creature] = owner = new OwnerState();
            return owner;
        }

        private void Refresh(uint creature, OwnerState owner)
        {
            var combatEnded = (Clock.UtcNow - owner.LastHostileActivity).TotalSeconds > 30 && !HasRecentHostileCombatActivity(creature);
            if (!GetIsObjectValid(creature) || GetIsDead(creature) || combatEnded)
            {
                ClearBenefits(creature);
                return;
            }
            if (owner.Reactions.WindowTarget != 0 &&
                (owner.Reactions.GetWindowStage(Clock.UtcNow) == MeleeTechniqueStage.None ||
                 !IsValidEnemy(creature, owner.Reactions.WindowTarget) ||
                 GetLineStat(creature, owner.WindowLine, StatType.TechniqueFinisherWindowDurationSeconds) <= 0))
                ClearReactiveWindows(creature, owner);
            if (owner.Reactions.RetaliationTarget != 0 &&
                (owner.Reactions.GetRetaliation(owner.Reactions.RetaliationTarget, Clock.UtcNow) <= 0 ||
                 !IsValidEnemy(creature, owner.Reactions.RetaliationTarget) || GetArea(creature) != owner.RetaliationArea ||
                 !Profiles.ContainsKey(owner.RetaliationProfileSkill) ||
                 GetLineStat(creature, owner.RetaliationLine, StatType.TechniqueRetaliationPower) <= 0))
                ClearRetaliation(creature, owner);
            if (owner.Sequence.Stage == MeleeTechniqueStage.None)
                return;
            if (!SetupMatches(creature, owner, owner.Sequence.Target, owner.HeldCast?.Ability, Clock.UtcNow) ||
                !IsValidEnemy(creature, owner.Sequence.Target) ||
                GetArea(creature) != owner.Area ||
                owner.SourceOwnedStatusType != null && !HasStatusEffect(owner.Sequence.Target, owner.SourceOwnedStatusType, creature))
                ClearSetup(creature);
        }

        private bool TryGetActivation(uint creature, uint target, out Activation activation)
        {
            return Activations.TryGetValue(creature, out activation) && activation.Target == target &&
                   IsEquipmentSnapshotValid(creature, activation.Weapon, activation.OffHand, activation.Area) &&
                   IsValidEnemy(creature, target);
        }

        private bool TryGetImpactActivation(uint creature, uint target, out Activation activation)
        {
            return Activations.TryGetValue(creature, out activation) &&
                   (activation.Target == target || activation.SecondaryTarget == target) &&
                   IsEquipmentSnapshotValid(creature, activation.Weapon, activation.OffHand, activation.Area) &&
                   IsValidEnemy(creature, target);
        }

        /// <summary>A cast resolves only with the hands it started with, whatever those weapons are.</summary>
        private bool IsEquipmentSnapshotValid(uint creature, uint main, uint off, uint area) =>
            GetIsObjectValid(creature) && !GetIsDead(creature) &&
            GetArea(creature) == area && GetItemInSlot(InventorySlot.RightHand, creature) == main &&
            GetItemInSlot(InventorySlot.LeftHand, creature) == off;

        private Type ApplyOwnedEffect(uint creature, IStatusEffect effect, float duration)
        {
            if (effect == null || !ApplyStatusEffect(creature, creature, effect, duration))
                return null;
            return effect.GetType();
        }

        private void RemoveOwnedEffect(uint creature, Type effectType)
        {
            if (effectType != null && GetIsObjectValid(creature))
                RemoveStatusEffect(creature, effectType, creature, false);
        }

        private bool IsValidEnemy(uint creature, uint target) =>
            GetIsObjectValid(target) && !GetIsDead(target) && GetArea(creature) == GetArea(target) &&
            GetIsReactionTypeHostile(target, creature);

        protected virtual IReadOnlyList<StatAdjustmentSource> GetStatSources(uint creature, StatType payloadStat) =>
            _stat.Value.GetStatSources(creature, payloadStat);

        protected virtual int GetStatAdjustment(uint creature, StatType stat) =>
            _stat.Value.GetStatAdjustment(creature, stat);

        protected virtual IReadOnlyList<uint> GetHostileTargetsInSphere(uint activator, Location location, float radius) =>
            _ability.Value.GetHostileTargetsInSphere(activator, location, radius);

        protected virtual string GetActiveAbilityActivationId(uint activator) =>
            UsePerkFeat.GetActiveAbilityActivationId(activator);

        protected virtual int GetAbilityDMG(uint creature, uint weapon, SkillType abilitySkill) =>
            WeaponDamage.GetAbilityDMG(creature, weapon, abilitySkill);

        protected virtual CombatDamageType GetWeaponDamageType(uint weapon) =>
            WeaponDamage.GetDamageType(weapon);

        protected virtual int GetPaidAbilityStaminaCost(uint creature, AbilityDetail ability) =>
            Service.Combat.GetPaidAbilityStaminaCost(creature, ability);

        protected virtual int RestoreAbilityHitStamina(uint creature, AbilityDetail ability, int requested, bool retainMinimumCost) =>
            Service.Combat.RestoreAbilityHitStamina(creature, ability, requested, retainMinimumCost: retainMinimumCost);

        protected virtual int ApplyPercentDamageAdjustment(int damage, int adjustment) =>
            Service.Combat.ApplyPercentDamageAdjustment(damage, adjustment);

        protected virtual bool HasRecentHostileCombatActivity(uint creature) =>
            Service.Combat.HasRecentHostileCombatActivity(creature);

        protected virtual CombatRecordAttributionScope BeginStatTriggerAttribution(uint creature, StatType stat) =>
            _combatRecorder.BeginStatTriggerAttribution(creature, stat);

        protected virtual bool ApplyStatusEffect(uint source, uint creature, IStatusEffect statusEffect, float durationSeconds) =>
            _statusEffect.Value.ApplyStatusEffect(source, creature, statusEffect, durationSeconds);

        protected virtual bool ApplyStatusEffect(uint source, uint creature, IStatusEffect statusEffect, float durationSeconds, CombatDamageType sourceDamageType) =>
            _statusEffect.Value.ApplyStatusEffect(source, creature, statusEffect, durationSeconds, sourceDamageType);

        protected virtual void RemoveStatusEffect(uint creature, Type statusEffectClass, uint source, bool sendsWornOffMessage) =>
            _statusEffect.Value.RemoveStatusEffect(creature, statusEffectClass, source, sendsWornOffMessage);

        protected virtual bool HasStatusEffect(uint creature, Type statusEffectClass, uint source) =>
            _statusEffect.Value.HasStatusEffect(creature, statusEffectClass, source);

        protected virtual int GetTemporaryHitPoints(uint target) =>
            TemporaryHitPointEffects.GetRemaining(target);

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
