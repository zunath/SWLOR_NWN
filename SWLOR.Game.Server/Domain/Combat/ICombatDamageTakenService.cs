using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The damage taken part of combat: the modifiers of an incoming hit, the effects of damage taken and of low HP, Guard and
    /// its riders, avoided attacks, deflection and Deflecting Return, and the incoming physical-to-Force conversion.
    /// </summary>
    public interface ICombatDamageTakenService
    {
        int ApplyDamageTakenModifiers(
            uint defender,
            int damage,
            uint attacker = OBJECT_INVALID,
            CombatDamageType damageType = CombatDamageType.Physical,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct,
            int targetStatusDamagePercentAdjustment = 0,
            bool isLandedAttack = true,
            bool typedLeadershipReductionAlreadyApplied = false);

        int ApplyTypedLeadershipDamageTakenModifier(
            uint defender,
            int damage,
            CombatDamageType damageType);

        void ApplyDamageReflectionEffects(
            uint attacker,
            uint defender,
            int damage,
            CombatDamageType damageType);

        int ApplyDamageOverTimeTakenModifiers(uint defender, int damage, CombatDamageType damageType);

        int ApplyDamageOverTimeTakenModifiers(
            uint defender,
            int damage,
            CombatDamageType damageType,
            out int targetStatusDamagePercentAdjustment);

        bool TryPreventFatalDamageAndGrantTemporaryHP(
            uint defender,
            int damage,
            bool restoreToOneHP);

        void ApplyDamageTakenEffects();

        void ApplyDamageTakenEffects(uint defender, uint attacker, int damage);

        void ApplyLowHPDamageTakenEffects(uint defender, int damage);

        void ApplyLowHPGuardEffectFromProtectedTarget(uint guardRecipient, uint protectedTarget, int damage);

        void TrackGuardedHit(uint creature);

        void TrackAvoidedAttack(uint creature, uint attacker);

        void ApplyMeleeDamageTakenEffects(uint defender, uint attacker);

        int ApplyGuardedHitModifiers(
            uint defender,
            uint attacker,
            int damage,
            CombatDamageType damageType,
            bool isLandedAttack);

        bool IsHostileAttackSource(uint defender, uint attacker);

        /// <summary>
        /// Retrieves the percentage of damage a successful Guard removes from an incoming hit,
        /// including stat adjustments and the effective minimum and maximum bounds.
        /// </summary>
        /// <param name="defender">The creature to check.</param>
        /// <returns>The damage reduction percentage applied on a guarded hit.</returns>
        int GetGuardDamageReductionPercent(uint defender);

        bool HasRecentGuardedHit(uint creature, float windowSeconds);

        void TrackDeflection(uint creature, DeflectionSource source);

        bool HasRecentDeflection(uint creature, DeflectionSource source, float windowSeconds);

        int ConsumeGuardedHitNextSkillAbilityExposedDamageBonus(uint creature, SkillType skillType);

        /// <summary>
        /// First half of Saber Ward / Aegis Eternal conversion: give the converted share its Force Defense
        /// mitigation. The physical hit's damage roll uses a defense value blended between the defender's
        /// Physical Defense and Force Defense by the conversion percent, so the converted share is mitigated
        /// as Force. The single damage roll is RNG-based and consumes stateful modifiers, so it cannot be run
        /// twice per hit; blending the defense is how the Force-Defense mitigation is applied without a second
        /// roll. The re-typing itself - removing the converted share from the physical hit and dealing it as a
        /// real Force instance (Force resistance, combat-log visibility) - is done afterward by
        /// <see cref="ApplyIncomingPhysicalToForceConversion"/>. The percentage is read from
        /// <see cref="StatType.IncomingPhysicalToForceConversionPercent"/> on the defender, and only
        /// physical-category damage is affected. The Force Defense value is resolved lazily because the
        /// auto-attack and ability damage paths look it up through different (native vs managed) helpers.
        /// </summary>
        int ApplyIncomingPhysicalToForceDefenseConversion(
            uint defender,
            CombatDamageType damageType,
            int physicalDefense,
            Func<int> forceDefenseProvider);

        /// <summary>
        /// Saber Ward (and Aegis Eternal) re-type a percentage of an incoming physical hit into a real
        /// Force damage instance. The converted share's Force Defense mitigation is already reflected in
        /// <paramref name="physicalDamage"/> because <see cref="ApplyIncomingPhysicalToForceDefenseConversion"/>
        /// blends the defense used for the damage roll toward Force Defense by the same percent. This removes
        /// the converted share from <paramref name="physicalDamage"/> (so it is not also dealt as physical) and
        /// deals it as Force damage, so it is reduced by the defender's Force resistance and shown as Force in
        /// the combat log. Runs before the physical-resistance stage; the Force portion is routed through
        /// <see cref="ApplyTriggeredDamage"/>, which applies Force resistance, the Force-specific Leadership
        /// channel, and the remaining damage-taken pipeline.
        /// Returns the pre-resistance Force amount split off (0 when nothing converts).
        /// </summary>
        int ApplyIncomingPhysicalToForceConversion(
            uint attacker,
            uint defender,
            CombatDamageType damageType,
            ref int physicalDamage,
            int targetStatusDamagePercentAdjustment = 0,
            Action<Action> convertedDamageScheduler = null);

        /// <summary>
        /// Deflecting Return: when the defender deflects a directly targeted ranged attack, reflect a capped
        /// share of weapon damage back to the attacker as Force damage. Fires at most once every
        /// <see cref="DeflectingReturnCooldownSeconds"/> seconds. Reflection amount is driven by the
        /// <see cref="StatType.RangedDeflectionReflectionPercent"/> / <see cref="StatType.RangedDeflectionReflectionCapPercent"/>
        /// stats the Deflecting Return perk grants. Embattled high-stack bonuses and finite capstone
        /// overrides are also stat-driven.
        /// </summary>
        int ApplyRangedDeflectionReflection(uint defender, uint attacker, SkillType attackerWeaponSkill);

        string BuildDeflectingReturnCombatLogMessage(
            uint observer,
            uint defender,
            uint attacker,
            int damage);

        void ApplyAbilityUsedAttackDeflection(
            uint activator,
            StatType attackDeflectionStatType,
            StatType durationStatType,
            StatType deflectionFPRestoreStatType = StatType.Invalid);

        void ApplyAbilityGrantedAttackDeflectionEffects(uint activator, DeflectionSource source);

        void ForgetDamageTakenState(uint creature);
    }
}
