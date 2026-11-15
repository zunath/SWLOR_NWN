using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// Abilities: the validation of an activation, the lifecycle of each impact while it resolves, and the combat impacts
    /// content applies. The catalog, the impact state, the auras, the temporary immunities and the activation state have
    /// interfaces of their own (Readmes/MaintainabilityRefactorAbilitySplit.md). Static callers reach the container's instance through <c>GameServices.Ability</c>; the event handlers are on
    /// <see cref="AbilityService"/>.
    /// </summary>
    public interface IAbilityService
    {

        /// <summary>
        /// Starts impact tracking and defers pending damage bonuses until a damaging payload,
        /// retaining activation-marker snapshots for the impact footprint and flash decision.
        /// </summary>
        void BeginAbilityImpact(
            uint activator,
            AbilityDetail ability,
            bool countsAsAttackAttempt = true,
            IReadOnlyList<TelegraphGeometry> activationAreaTelegraphs = null,
            AbilityImpactSequence sequence = null,
            uint triggeringWeapon = OBJECT_INVALID,
            int? triggeringWeaponDamage = null,
            CombatDamageType? triggeringWeaponDamageType = null,
            WeaponAbilityTraitActivation weaponTraits = null);

        /// <summary>
        /// Retains the originating ability and cast sequence for recurring impacts without
        /// consuming another activation's pending bonuses or replacing its impact tracker.
        /// </summary>
        Action CaptureRepeatedAbilityImpact(
            uint activator, Action impactAction, int baseDamage = 0, bool requiresLivingActivator = true);

        /// <summary>
        /// Captures one cast's recurring impact context while allowing each delayed impact
        /// to retain its own target or payload. Bonuses are consumed only when an impact lands.
        /// </summary>
        Action<T> CaptureRepeatedAbilityImpact<T>(
            uint activator, Action<T> impactAction, int baseDamage = 0, bool requiresLivingActivator = true,
            bool retainCapturedBonuses = false);

        Func<int, Action<T>> CaptureRepeatedAbilityImpactBatch<T>(
            uint activator, Action<T> impactAction, int baseDamage = 0);

        AbilityImpactSummary EndAbilityImpact(uint activator);

        /// <summary>
        /// Discards an incomplete tracked impact without applying its queued damage effects.
        /// </summary>
        void AbortAbilityImpact(uint activator);

        /// <summary>
        /// Plays the definition's finite receipt burst after a heal, buff, or other noncombat
        /// action succeeds. Call for the actual recipient while its ability impact is active.
        /// Failed actions must not call this method. Combat impacts report success internally.
        /// </summary>
        void PlaySuccessfulImpactVisualEffect(uint activator, uint recipient);

        /// <summary>
        /// Captures the originating visual for a delayed noncombat pulse without changing combat
        /// tracking or bonuses. Capture once per pulse, then report its successful recipients.
        /// </summary>
        Action<uint> CaptureSuccessfulImpactVisualEffect(uint activator);

        /// <summary>
        /// Shows an instant visual effect on <paramref name="target"/> to one player only, so feedback
        /// for a hidden creature never marks its position for anyone else.
        /// </summary>
        void ShowVisualEffectToPlayer(uint player, uint target, VisualEffect visualEffect);

        /// <summary>Returns the current impact's visual reference for a later independent combat event.</summary>
        VisualEffect GetSuccessfulImpactVisualEffect(uint activator);

        void ApplyHostileAbilityEnmity(uint activator, uint target, int bonus = 0);

        int CalculateHostileAbilityEnmity(int bonus, int volleyIndex = 0, int volleyCount = 1);

        /// <summary>
        /// The reason the most recent CanUseAbility call returned false. Player-facing feedback
        /// goes through SendMessageToPC, which is invisible for NPC activators - the in-engine
        /// test harness reads this to report WHY an activation was rejected. Cleared at the
        /// start of every check.
        /// </summary>
        string GetLastActivationDenialReason();

        /// <summary>
        /// Checks whether a creature can activate the perk feat.
        /// </summary>
        /// <param name="activator">The activator of the perk feat.</param>
        /// <param name="target">The target of the perk feat.</param>
        /// <param name="abilityType">The type of ability to use.</param>
        /// <param name="effectivePerkLevel">The activator's effective perk level.</param>
        /// <param name="targetLocation">The target location of the perk feat.</param>
        /// <returns>true if successful, false otherwise</returns>
        bool CanUseAbility(
            uint activator,
            uint target,
            FeatType abilityType,
            int effectivePerkLevel,
            Location targetLocation);

        /// <summary>
        /// Applies the standard hostile combat impact used by weapon and martial abilities.
        /// </summary>
        int ApplyCombatImpact(
            uint activator,
            uint target,
            Location targetLocation,
            SkillType skillType,
            int baseDamage,
            int duration,
            Type statusEffect,
            bool isArea,
            IEnumerable<Type> additionalStatusEffects = null,
            Func<IStatusEffect> statusEffectFactory = null,
            CombatDamageType damageType = CombatDamageType.Physical,
            ResistanceType statusResistanceType = ResistanceType.Invalid,
            VisualEffect targetVisualEffect = VisualEffect.None,
            VisualEffect areaVisualEffect = VisualEffect.None,
            Func<uint, int> damagePercentAdjustment = null,
            Func<uint, int> baseDamageAdjustment = null,
            Animation impactAnimation = Animation.Invalid,
            int enmityBonus = 0,
            Action<uint> afterSuccessfulHit = null,
            Func<uint, StatusEffectCategory?> beforeSuccessfulImpactRiders = null,
            int hitChancePercentAdjustment = 0,
            int criticalRatePercentAdjustment = 0,
            bool useNPCStatScaling = false,
            bool awardsCombatPoints = true,
            DamageType? effectDamageType = null,
            bool playImpactAnimation = true,
            AbilityType combatImpactDamageAbility = AbilityType.Invalid,
            bool resolvesHit = true,
            bool canCritical = true,
            bool useUnscaledDamage = false,
            Action<uint> beforeImpact = null,
            bool isAreaImpact = false);

        /// <summary>
        /// Applies a hostile combat impact after a visible telegraph resolves.
        /// </summary>
        int ApplyTelegraphedCombatImpact(
            uint activator,
            uint target,
            Location targetLocation,
            SkillType skillType,
            int baseDamage,
            int duration,
            Type statusEffect,
            CombatImpactAreaShape shape,
            float telegraphDuration,
            float lengthOrRadius,
            float width = 0f,
            IEnumerable<Type> additionalStatusEffects = null,
            bool centerOnActivator = false,
            Func<IStatusEffect> statusEffectFactory = null,
            CombatDamageType damageType = CombatDamageType.Physical,
            ResistanceType statusResistanceType = ResistanceType.Invalid,
            VisualEffect targetVisualEffect = VisualEffect.None,
            VisualEffect areaVisualEffect = VisualEffect.None,
            Func<uint, int> damagePercentAdjustment = null,
            Func<uint, int> baseDamageAdjustment = null,
            Action<AbilityImpactSummary> afterImpactAction = null,
            int maxTargets = 0,
            Animation impactAnimation = Animation.Invalid,
            int enmityBonus = 0,
            Action<uint> beforeImpact = null,
            Action<uint> afterSuccessfulHit = null,
            Func<uint, StatusEffectCategory?> beforeSuccessfulImpactRiders = null,
            int hitChancePercentAdjustment = 0,
            int criticalRatePercentAdjustment = 0,
            bool useNPCStatScaling = false,
            bool awardsCombatPoints = true,
            DamageType? effectDamageType = null,
            bool playImpactAnimation = true,
            bool alwaysApplyAreaVisualEffect = true,
            AbilityType combatImpactDamageAbility = AbilityType.Invalid,
            bool sendsNoTargetMessage = true,
            bool resolvesHit = true,
            bool canCritical = true,
            float impactFlashDuration = AbilityService.DefaultImpactFlashDuration,
            bool useUnscaledDamage = false,
            Action<TelegraphGeometry> onGeometryResolved = null);

        /// <summary>Plays the current ability's declared impact gesture independently of target outcomes.</summary>
        void PlayAbilityImpactAnimation(uint activator);

        int ApplyHostileCombatImpact(
            uint activator,
            uint target,
            SkillType skillType,
            int damage,
            CombatDamageType damageType,
            Type statusEffect = null,
            int duration = 0,
            IEnumerable<Type> additionalStatusEffects = null,
            Func<IStatusEffect> statusEffectFactory = null,
            ResistanceType statusResistanceType = ResistanceType.Invalid,
            VisualEffect targetVisualEffect = VisualEffect.None,
            int enmityBonus = 0,
            Action<uint> afterSuccessfulHit = null,
            Func<uint, StatusEffectCategory?> beforeSuccessfulImpactRiders = null,
            bool awardsCombatPoints = true,
            DamageType? effectDamageType = null,
            bool firstHostileAbilityHitDamageBonusApplied = false);

        /// <summary>
        /// An impact that adds weapon DMG also takes that weapon's element, matching auto-attacks.
        /// Like native weapon resolution, an element only replaces Physical; authored non-Physical types stay.
        /// </summary>
        CombatDamageType GetWeaponAbilityDamageType(uint activator, SkillType skillType, CombatDamageType damageType);

        /// <summary>
        /// Adds this impact's captured flat bonus to an already-scaled damage payload before
        /// target mitigation. Control-only payloads remain zero.
        /// </summary>
        int ApplyCapturedAbilityDamageBonus(uint activator, int damage);

        StatusEffectCategory GetCombatImpactStatusCategories(
            Type statusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory);

        void ApplyDarkForceDamageRestoration(uint activator, int damage);
    }
}
