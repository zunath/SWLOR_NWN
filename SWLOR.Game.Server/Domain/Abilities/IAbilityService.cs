using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.TelegraphService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// Abilities: the catalog of ability definitions, the validation of an activation, the tracker of each impact while it
    /// resolves, player auras, temporary immunities, and the activation and queued-attack state <c>UsePerkFeat</c> runs on.
    /// Static callers reach the container's instance through <c>GameServices.Ability</c>; the event handlers are on
    /// <see cref="AbilityService"/>.
    /// </summary>
    public interface IAbilityService
    {
        /// <summary>
        /// When the module caches, abilities will be cached and events will be scheduled.
        /// </summary>
        void CacheData();

        IReadOnlyDictionary<FeatType, AbilityDetail> GetAllAbilityDetails();

        /// <summary>
        /// Returns true if a feat is registered to an ability.
        /// Returns false otherwise.
        /// </summary>
        /// <param name="featType">The type of feat to check.</param>
        /// <returns>true if feat is registered to an ability. false otherwise.</returns>
        bool IsFeatRegistered(FeatType featType);

        /// <summary>
        /// Retrieves an ability's details by the specified feat type.
        /// If feat does not have an ability, an exception will be thrown.
        /// </summary>
        /// <param name="featType">The type of feat</param>
        /// <returns>The ability detail</returns>
        AbilityDetail GetAbilityDetail(FeatType featType);

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

        AbilityImpactSequence GetAbilityImpactSequence(uint activator);

        int GetWeaponTraitAccuracy(uint activator);

        void RecordSuccessfulWeaponAbilityHit(uint activator, uint target, SkillType skill);

        /// <summary>
        /// Native current HP includes temporary HP. Subtract queued damage so another hit
        /// cannot draw healing from health already claimed by an earlier impact.
        /// </summary>
        int GetRemainingDamageTargetHP(uint activator, uint target);

        /// <summary>
        /// Reuses a payload's contributing sources within one impact. Delayed phases and
        /// recurring pulses receive fresh trackers, while conditions can still be checked per target.
        /// </summary>
        IReadOnlyList<StatAdjustmentSource> GetAbilityImpactStatSources(uint activator, StatType payload);

        bool TryTriggerAreaAbilityPulse(uint activator);

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
        /// The summary of the activator's most recently COMPLETED ability impact, or null if
        /// none completed since the last clear. Observability seam for the engine test
        /// harness: a queued weapon ability's damage rides the same landed hit as the weapon
        /// swing, so an HP drop alone cannot attribute damage to the ability - a completed
        /// summary with impacted targets can.
        /// </summary>
        AbilityImpactSummary GetLastCompletedAbilityImpactSummary(uint activator);

        /// <summary>
        /// Clears the activator's last completed impact summary so a subsequent
        /// <see cref="GetLastCompletedAbilityImpactSummary"/> observation cannot match an
        /// earlier ability's impact.
        /// </summary>
        void ClearLastCompletedAbilityImpactSummary(uint activator);

        /// <summary>
        /// Discards an incomplete tracked impact without applying its queued damage effects.
        /// </summary>
        void AbortAbilityImpact(uint activator);

        bool TryQueueTrackedDamageEffect(uint activator, uint target, int damage, DamageType damageType);

        AbilityImpactSummary GetActiveAbilityImpactSummary(uint activator);

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

        void AddActiveAbilityDefenseIgnorePercentAdjustment(uint activator, int adjustment);

        float GetActiveForceAffinityMagnitudeMultiplier(uint activator);

        float GetActiveForceAffinityRestorativeMagnitudeMultiplier(uint activator);

        int ApplyActiveForceAffinityRestorativeMagnitude(uint activator, int amount);

        int ApplyActiveForceAffinityMagnitude(uint activator, int amount);

        float ApplyActiveForceAffinityMagnitude(uint activator, float amount);

        int ApplyCombatReadinessToActivatedAbilityMagnitude(uint activator, int amount);

        int ApplyCombatReadinessMagnitude(uint creature, int amount);

        void ApplyHostileAbilityEnmity(uint activator, uint target, int bonus = 0);

        int CalculateHostileAbilityEnmity(int bonus, int volleyIndex = 0, int volleyCount = 1);

        /// <summary>Only a melee technique's cast can change shape (its conditional sweep); other abilities keep their authored flags.</summary>
        bool IsAreaImpact(uint activator, AbilityDetail ability);

        bool IsAreaImpact(AbilityImpactSummary summary, AbilityDetail ability);

        bool IsSingleTargetImpact(uint activator, AbilityDetail ability);

        /// <summary>
        /// Returns whether the ability counts as a Force power: a Force skill ability, or an ability
        /// of another skill flagged <see cref="AbilityDetail.IsForcePower"/> (Force Sensitive
        /// Espionage powers).
        /// </summary>
        bool IsForcePower(uint activator, AbilityDetail ability);

        bool IsForcePower(bool flaggedForcePower, SkillType abilitySkillType);

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
        /// Whenever a weapon's OnHit event is fired, add a Leadership combat point if an Aura is active.
        /// </summary>
        void AddLeadershipCombatPoint();

        void RecordLeadershipWeaponHit(uint player, uint target);

        void ApplyAura(uint activator, Type type, bool targetsSelf, bool targetsParty, bool targetsEnemies, VisualEffect successfulImpactVisualEffect = VisualEffect.None);

        /// <summary>
        /// Retrieves the creatures a self-and-party aura applies to: the activator and the party
        /// members currently inside its aura range.
        /// </summary>
        /// <param name="activator">The creature whose aura is checked.</param>
        /// <returns>The activator followed by its party members in aura range.</returns>
        List<uint> GetSelfAndPartyAuraRecipients(uint activator);

        bool RemoveAura(uint activator, Type type, bool sendsWornOffMessage = false);

        bool ToggleAura(uint activator, Type type);

        /// <summary>
        /// Deactivates every aura the player is currently projecting and strips any aura effects they are
        /// receiving as a recipient. Used by flows that must return a player to a clean state, such as the
        /// character rebuilder before a reset.
        /// </summary>
        void ClearAllPlayerAuras(uint player);

        /// <summary>
        /// Removes a creature from all active aura range lists and strips any aura effects they received
        /// as a recipient. Used when a creature leaves the game world in a way that bypasses the normal
        /// AOE exit event (e.g., entering space, being teleported).
        /// </summary>
        /// <param name="target">The creature to remove from all aura ranges.</param>
        void RemoveCreatureFromAllAuraRanges(uint target);

        /// <summary>
        /// Refreshes party-aura eligibility for a creature against all active aura leaders.
        /// If the creature is still tracked in a party aura range but is no longer in the leader's party,
        /// remove the cached range entry and strip relevant aura effects.
        /// </summary>
        /// <param name="target">The creature whose party aura eligibility should be refreshed.</param>
        void RefreshPartyAuraEligibility(uint target);

        /// <summary>
        /// Enrolls a creature that just joined a party in the party auras it is already standing in,
        /// and enrolls the party members already standing in its own aura. The aura enter event only
        /// fires on entry, so without this they would wait until they leave and re-enter the aura.
        /// </summary>
        /// <param name="member">The creature that joined the party.</param>
        void EnrollPartyMemberInAuraRanges(uint member);

        /// <summary>
        /// Re-applies any aura effects that a creature should be receiving based on their current position
        /// in active aura range lists. Used after in-place resurrection (subdual, revive) where the
        /// AOE enter event may not re-fire for a creature that never physically left the AOE.
        /// </summary>
        /// <param name="target">The creature to re-enroll in active auras.</param>
        void ReapplyAuraEffectsForCreature(uint target);

        void ReapplyPlayerAuraAOE(uint player);

        /// <summary>
        /// When a player enters the server, apply the Aura AOE effect.
        /// </summary>
        void ApplyAuraAOE();

        /// <summary>
        /// When a player exits the server, remove all of their Aura effects.
        /// Also removes the player from any aura ranges they are receiving as a recipient.
        /// </summary>
        void ClearAurasOnExit();

        /// <summary>
        /// When a player dies, remove all of their Aura effects.
        /// Also removes the player from any aura ranges they are receiving as a recipient.
        /// </summary>
        void ClearAurasOnDeath();

        /// <summary>
        /// When a player respawns, reapply the aura AOE effect
        /// </summary>
        void ReapplyAuraOnRespawn();

        /// <summary>
        /// When a player enters space mode, remove all of their Aura effects.
        /// Also removes the player from any aura ranges they are receiving as a recipient.
        /// </summary>
        void ClearAurasOnSpaceEntry();

        /// <summary>
        /// Whenever a creature enters the aura, add them to the cache.
        /// </summary>
        void AuraEnter();

        /// <summary>
        /// Whenever a creature exits the aura, remove it from the cache.
        /// </summary>
        void AuraExit();

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

        IReadOnlyList<uint> GetHostileTargetsInSphere(uint activator, Location location, float radius);

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

        /// <summary>
        /// Applies a temporary immunity effect to a particular target.
        /// This will add 20 seconds on top of whatever the ability duration length is.
        /// An existing immunity of the same type is replaced so its timer restarts from this
        /// application without shortening a longer remaining immunity.
        /// If the immunity is one of the hard-CC types, this also grants temporary immunity
        /// to every other hard-CC type for the same duration.
        /// </summary>
        /// <param name="target">The target receiving the immunity</param>
        /// <param name="abilityDuration">The length of the ability's duration. This will be added on top of the 20 seconds.</param>
        /// <param name="immunity">The type of immunity to apply.</param>
        void ApplyTemporaryImmunity(uint target, float abilityDuration, ImmunityType immunity);

        /// <summary>
        /// Applies the remaining post-control immunity after accounting for time that elapsed
        /// while the affected creature was logged out.
        /// </summary>
        void ApplyPostControlImmunity(
            uint target,
            float secondsSinceControlEnded,
            ImmunityType immunity);

        bool HasTemporaryImmunity(uint target, ImmunityType immunity);

        /// <summary>
        /// Checks whether the target is immune to a hard-CC type: it still has immunity to that
        /// specific type, it recently suffered a different hard-CC type and is still within the
        /// shared post-control immunity window, or a hard-CC status is active on it right now -
        /// controls do not stack, they queue behind the post-control window.
        /// </summary>
        bool HasHardCrowdControlImmunity(uint target, ImmunityType immunity);

        /// <summary>The creature's current cast or channel, if one is active (the state <c>UsePerkFeat</c> runs on).</summary>
        bool TryGetActiveAbilityActivation(uint activator, out AbilityActiveActivation activation);

        /// <summary>Records the creature's current cast or channel, replacing any earlier one.</summary>
        void SetActiveAbilityActivation(uint activator, AbilityActiveActivation activation);

        /// <summary>Forgets the creature's current cast or channel.</summary>
        void RemoveActiveAbilityActivation(uint activator);

        /// <summary>The trait snapshot captured when the creature queued its weapon ability, if any.</summary>
        bool TryGetQueuedWeaponTraits(uint activator, out WeaponAbilityTraitActivation traits);

        /// <summary>Records the trait snapshot of a queued weapon ability, replacing any earlier one.</summary>
        void SetQueuedWeaponTraits(uint activator, WeaponAbilityTraitActivation traits);

        /// <summary>Forgets the trait snapshot of the creature's queued weapon ability.</summary>
        void RemoveQueuedWeaponTraits(uint activator);

        /// <summary>The attack roll reserved for the creature's queued weapon ability, if any.</summary>
        bool TryGetQueuedWeaponAttack(uint activator, out AbilityQueuedWeaponAttack reservation);

        /// <summary>Reserves an attack roll for a queued weapon ability, unless one is already reserved.</summary>
        void TryAddQueuedWeaponAttack(uint activator, AbilityQueuedWeaponAttack reservation);

        /// <summary>Forgets the attack roll reserved for the creature's queued weapon ability.</summary>
        void RemoveQueuedWeaponAttack(uint activator);
    }
}
