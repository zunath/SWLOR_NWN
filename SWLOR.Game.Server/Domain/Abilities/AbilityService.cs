using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// Abilities: the combat impacts content applies (part 9 of the ability split, Readmes/MaintainabilityRefactorAbilitySplit.md)
    /// and the success visual and impact gesture of an ability. The catalog, the impact state, the impact lifecycle, the
    /// activation and target checks, the auras, the temporary immunities, the activation state <c>UsePerkFeat</c> runs on, the
    /// per-target hit roll, resolution and hostile impact, the impact messages and visuals, Dark Force, the damage delivery, the
    /// status application and the damage inputs and stages are parts of their own. The logic that used to be static in
    /// <c>Service.Ability</c> is moved here unchanged; static callers reach it through <c>GameServices.Ability</c>. Calls into
    /// the combat services sit behind one-line <c>protected virtual</c> methods whose bodies are the original calls.
    /// </summary>
    public class AbilityService : IAbilityService
    {
        private readonly IAbilityImpactState _impactState;
        private readonly IAbilityImpactLifecycleService _lifecycle;
        private readonly IAbilityTargetImpactService _targetImpact;
        private readonly IAbilityImpactMessageService _impactMessage;
        private readonly IAbilityImpactVisualService _impactVisual;
        private readonly ICombatImpactGeometryService _geometry;
        private readonly ITelegraphService _telegraph;
        private readonly INamedAnimationService _namedAnimation;
        private readonly ICombatResourcesService _resources;
        private readonly ICombatAbilityImpactService _abilityImpact;

        public AbilityService(
            IAbilityImpactState impactState,
            IAbilityImpactLifecycleService lifecycle,
            IAbilityTargetImpactService targetImpact,
            IAbilityImpactMessageService impactMessage,
            IAbilityImpactVisualService impactVisual,
            ICombatImpactGeometryService geometry,
            ITelegraphService telegraph,
            INamedAnimationService namedAnimation,
            ICombatResourcesService resources,
            ICombatAbilityImpactService abilityImpact)
        {
            _impactState = impactState;
            _lifecycle = lifecycle;
            _targetImpact = targetImpact;
            _impactMessage = impactMessage;
            _impactVisual = impactVisual;
            _geometry = geometry;
            _telegraph = telegraph;
            _namedAnimation = namedAnimation;
            _resources = resources;
            _abilityImpact = abilityImpact;
        }

        /// <summary>
        /// How long the visual-only impact flash lingers on an instant area ability, in seconds.
        /// Long enough to read the shape, short enough not to be mistaken for a pre-cast telegraph.
        /// </summary>
        public const float DefaultImpactFlashDuration = 0.3f;

        /// <summary>
        /// Plays the definition's finite receipt burst after a heal, buff, or other noncombat
        /// action succeeds. Call for the actual recipient while its ability impact is active.
        /// Failed actions must not call this method. Combat impacts report success internally.
        /// </summary>
        public void PlaySuccessfulImpactVisualEffect(uint activator, uint recipient)
        {
            _impactVisual.PlaySuccessfulImpactVisualEffect(_impactState.GetTrackedAbilityImpact(activator)?.VisualEffects, recipient);
        }

        /// <summary>
        /// Captures the originating visual for a delayed noncombat pulse without changing combat
        /// tracking or bonuses. Capture once per pulse, then report its successful recipients.
        /// </summary>
        public Action<uint> CaptureSuccessfulImpactVisualEffect(uint activator)
        {
            var effect = GetSuccessfulImpactVisualEffect(activator);
            var visuals = new AbilityImpactVisualEffects(effect);
            return recipient => _impactVisual.PlaySuccessfulImpactVisualEffect(visuals, recipient);
        }

        /// <summary>Returns the current impact's visual reference for a later independent combat event.</summary>
        public VisualEffect GetSuccessfulImpactVisualEffect(uint activator)
        {
            return _impactState.GetTrackedAbilityImpact(activator)?.Ability.SuccessfulImpactVisualEffect ?? VisualEffect.None;
        }

        /// <summary>
        /// Applies the standard hostile combat impact used by weapon and martial abilities.
        /// </summary>
        public int ApplyCombatImpact(
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
            bool isAreaImpact = false)
        {
            _lifecycle.PrepareCombatImpactDamageBonuses(activator, baseDamage);
            var totalDamage = 0;
            _impactState.RecordAbilityImpactShape(activator, skillType, isArea || isAreaImpact);

            if (isArea)
            {
                var center = GetIsObjectValid(target) ? GetLocation(target) : targetLocation;
                var creatures = new List<uint>();

                foreach (var creature in ObjectSnapshot.InShape(Shape.Sphere, 5.0f, center, true))
                {
                    if (!GetIsObjectValid(creature))
                        continue;

                    if (_geometry.HasAbilityLineOfSight(activator, creature))
                        creatures.Add(creature);

                }

                if (areaVisualEffect != VisualEffect.None &&
                    GetIsObjectValid(GetAreaFromLocation(center)))
                {
                    VisualEffectPlayback.ApplyAtLocation(areaVisualEffect, center);
                }

                totalDamage = ApplyCombatImpactToCreatures(
                    activator,
                    creatures,
                    skillType,
                    baseDamage,
                    statusEffect,
                    duration,
                    additionalStatusEffects,
                    statusEffectFactory,
                    damageType,
                    statusResistanceType,
                    targetVisualEffect,
                    damagePercentAdjustment,
                    baseDamageAdjustment,
                    enmityBonus: enmityBonus,
                    beforeImpact: beforeImpact,
                    afterSuccessfulHit: afterSuccessfulHit,
                    beforeSuccessfulImpactRiders: beforeSuccessfulImpactRiders,
                    hitChancePercentAdjustment: hitChancePercentAdjustment,
                    criticalRatePercentAdjustment: criticalRatePercentAdjustment,
                    useNPCStatScaling: useNPCStatScaling,
                    awardsCombatPoints: awardsCombatPoints,
                    effectDamageType: effectDamageType,
                    combatImpactDamageAbility: combatImpactDamageAbility,
                    resolvesHit: resolvesHit,
                    canCritical: canCritical,
                    useUnscaledDamage: useUnscaledDamage);
            }
            else if (GetIsObjectValid(target))
            {
                totalDamage = ApplyCombatImpactToCreatures(
                    activator,
                    new[] { target },
                    skillType,
                    baseDamage,
                    statusEffect,
                    duration,
                    additionalStatusEffects,
                    statusEffectFactory,
                    damageType,
                    statusResistanceType,
                    targetVisualEffect,
                    damagePercentAdjustment,
                    baseDamageAdjustment,
                    enmityBonus: enmityBonus,
                    beforeImpact: beforeImpact,
                    afterSuccessfulHit: afterSuccessfulHit,
                    beforeSuccessfulImpactRiders: beforeSuccessfulImpactRiders,
                    hitChancePercentAdjustment: hitChancePercentAdjustment,
                    criticalRatePercentAdjustment: criticalRatePercentAdjustment,
                    useNPCStatScaling: useNPCStatScaling,
                    awardsCombatPoints: awardsCombatPoints,
                    effectDamageType: effectDamageType,
                    combatImpactDamageAbility: combatImpactDamageAbility,
                    resolvesHit: resolvesHit,
                    canCritical: canCritical,
                    useUnscaledDamage: useUnscaledDamage);
            }

            if (playImpactAnimation)
                PlayCombatImpactAnimation(activator, impactAnimation);

            return totalDamage;
        }

        /// <summary>
        /// Applies a hostile combat impact after a visible telegraph resolves.
        /// </summary>
        public int ApplyTelegraphedCombatImpact(
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
            float impactFlashDuration = DefaultImpactFlashDuration,
            bool useUnscaledDamage = false,
            Action<TelegraphGeometry> onGeometryResolved = null)
        {
            _lifecycle.PrepareCombatImpactDamageBonuses(activator, baseDamage);
            _impactState.RecordAbilityImpactShape(activator, skillType, true);
            var trackedImpact = _impactState.GetTrackedAbilityImpact(activator);
            var backOffsetOrigin = trackedImpact?.Ability.Targeting?.Flags
                .HasFlag(AbilityTargetingFlags.BackOffsetOrigin) == true;
            var geometry = _geometry.ResolveCombatImpactGeometry(
                activator, target, targetLocation, shape, lengthOrRadius, width, centerOnActivator, backOffsetOrigin,
                trackedImpact?.ActivationAreaTelegraphs);

            // Directional visuals must use the same captured footprint as the damage.
            onGeometryResolved?.Invoke(geometry);

            if (telegraphDuration <= 0f)
            {
                // Instant-cast area abilities cannot use a pre-cast telegraph without violating the
                // Bible's "Instant" activation time, so they flash their shape at impact instead.
                // The flash is purely visual: damage below is still applied immediately. Compare
                // the actual impact geometry with the activation marker before suppressing a redraw.
                ShowAreaImpactFlash(
                    activator,
                    geometry,
                    impactFlashDuration,
                    trackedImpact?.Ability.ImpactDelay > 0f ? null : trackedImpact?.ActivationAreaTelegraphs);

                var totalDamage = ApplyCombatImpactInShape(
                    activator,
                    geometry,
                    skillType,
                    baseDamage,
                    duration,
                    statusEffect,
                    additionalStatusEffects,
                    statusEffectFactory,
                    damageType,
                    statusResistanceType,
                    targetVisualEffect,
                    areaVisualEffect,
                    damagePercentAdjustment,
                    baseDamageAdjustment,
                    maxTargets,
                    enmityBonus,
                    beforeImpact,
                    afterSuccessfulHit,
                    beforeSuccessfulImpactRiders,
                    hitChancePercentAdjustment,
                    criticalRatePercentAdjustment,
                    useNPCStatScaling,
                    awardsCombatPoints,
                    effectDamageType,
                    alwaysApplyAreaVisualEffect,
                    combatImpactDamageAbility,
                    sendsNoTargetMessage,
                    resolvesHit,
                    canCritical,
                    useUnscaledDamage);
                if (playImpactAnimation)
                    PlayCombatImpactAnimation(activator, impactAnimation);

                if (trackedImpact != null)
                    afterImpactAction?.Invoke(trackedImpact.Summary);

                return totalDamage;
            }

            return ScheduleTelegraphedCombatImpact(
                activator,
                skillType,
                baseDamage,
                duration,
                statusEffect,
                shape,
                telegraphDuration,
                additionalStatusEffects,
                statusEffectFactory,
                damageType,
                statusResistanceType,
                targetVisualEffect,
                areaVisualEffect,
                damagePercentAdjustment,
                baseDamageAdjustment,
                afterImpactAction,
                maxTargets,
                impactAnimation,
                enmityBonus,
                beforeImpact,
                afterSuccessfulHit,
                beforeSuccessfulImpactRiders,
                hitChancePercentAdjustment,
                criticalRatePercentAdjustment,
                useNPCStatScaling,
                awardsCombatPoints,
                effectDamageType,
                playImpactAnimation,
                alwaysApplyAreaVisualEffect,
                combatImpactDamageAbility,
                sendsNoTargetMessage,
                resolvesHit,
                canCritical,
                useUnscaledDamage,
                geometry,
                trackedImpact);
        }

        /// <summary>
        /// The delayed half of <see cref="ApplyTelegraphedCombatImpact"/>: draws the telegraph, whose action applies the impact to
        /// the hostile creatures inside it when it resolves, and defers the cast's stamina cost until then.
        /// </summary>
        private int ScheduleTelegraphedCombatImpact(
            uint activator,
            SkillType skillType,
            int baseDamage,
            int duration,
            Type statusEffect,
            CombatImpactAreaShape shape,
            float telegraphDuration,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory,
            CombatDamageType damageType,
            ResistanceType statusResistanceType,
            VisualEffect targetVisualEffect,
            VisualEffect areaVisualEffect,
            Func<uint, int> damagePercentAdjustment,
            Func<uint, int> baseDamageAdjustment,
            Action<AbilityImpactSummary> afterImpactAction,
            int maxTargets,
            Animation impactAnimation,
            int enmityBonus,
            Action<uint> beforeImpact,
            Action<uint> afterSuccessfulHit,
            Func<uint, StatusEffectCategory?> beforeSuccessfulImpactRiders,
            int hitChancePercentAdjustment,
            int criticalRatePercentAdjustment,
            bool useNPCStatScaling,
            bool awardsCombatPoints,
            DamageType? effectDamageType,
            bool playImpactAnimation,
            bool alwaysApplyAreaVisualEffect,
            AbilityType combatImpactDamageAbility,
            bool sendsNoTargetMessage,
            bool resolvesHit,
            bool canCritical,
            bool useUnscaledDamage,
            TelegraphGeometry geometry,
            AbilityImpactTracker trackedImpact)
        {
            var areaVisualLocation = Location(
                geometry.Area,
                geometry.Position,
                0f);
            var deferredNextAbilityDamageBonus =
                (trackedImpact?.NextAbilityDamageBonus ?? 0) -
                (trackedImpact?.StatusAppliedNextAttackDamageBonus ?? 0);
            var action = BuildTelegraphedCombatImpactAction(
                skillType,
                baseDamage,
                duration,
                statusEffect,
                additionalStatusEffects,
                statusEffectFactory,
                shape,
                areaVisualLocation,
                trackedImpact?.Ability,
                trackedImpact,
                deferredNextAbilityDamageBonus,
                trackedImpact?.NextAbilityCriticalRatePercentAdjustment ?? 0,
                trackedImpact?.NextAbilityDefenseIgnorePercentAdjustment ?? 0,
                trackedImpact?.NextAttackEnmityBonus ?? 0,
                damageType,
                statusResistanceType,
                targetVisualEffect,
                areaVisualEffect,
                damagePercentAdjustment,
                baseDamageAdjustment,
                afterImpactAction,
                maxTargets,
                enmityBonus,
                beforeImpact,
                afterSuccessfulHit,
                beforeSuccessfulImpactRiders,
                hitChancePercentAdjustment,
                criticalRatePercentAdjustment,
                useNPCStatScaling,
                awardsCombatPoints,
                effectDamageType,
                alwaysApplyAreaVisualEffect,
                combatImpactDamageAbility,
                sendsNoTargetMessage,
                resolvesHit,
                canCritical,
                useUnscaledDamage);

            switch (shape)
            {
                case CombatImpactAreaShape.Sphere:
                    _telegraph.CreateSphereTelegraph(
                        activator,
                        GetPositionFromLocation(areaVisualLocation),
                        geometry.Size.X,
                        telegraphDuration,
                        true,
                        action);
                    break;
                case CombatImpactAreaShape.Cone:
                    _telegraph.CreateConeTelegraph(
                        activator,
                        geometry.Position,
                        geometry.Rotation,
                        geometry.Size.X,
                        geometry.Size.Y,
                        telegraphDuration,
                        true,
                        action);
                    break;
                case CombatImpactAreaShape.Line:
                    _telegraph.CreateLineTelegraph(
                        activator,
                        geometry.Position,
                        geometry.Rotation,
                        geometry.Size.X,
                        geometry.Size.Y,
                        telegraphDuration,
                        true,
                        action);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
            }

            DeferAbilityStaminaCostContext(activator, trackedImpact?.Ability);

            if (playImpactAnimation)
                PlayCombatImpactAnimation(activator, impactAnimation);

            return 0;
        }

        /// <summary>
        /// Renders a short, purely visual telegraph showing the area an instant ability just struck.
        /// Unlike a pre-cast telegraph this carries no action and does not gate damage, so it gives
        /// positional feedback without changing an ability's Bible-mandated "Instant" activation time.
        /// </summary>
        private void ShowAreaImpactFlash(
            uint activator,
            TelegraphGeometry geometry,
            float flashDuration,
            IReadOnlyList<TelegraphGeometry> activationAreaTelegraphs)
        {
            if (flashDuration <= 0f || geometry.Size.X <= 0f)
                return;
            if (geometry.Shape == TelegraphType.None)
                return;
            if (!_telegraph.ShouldShowImpactFlash(geometry, activationAreaTelegraphs))
                return;

            _telegraph.CreateTelegraph(
                activator,
                geometry.Position,
                geometry.Shape == TelegraphType.Sphere ? 0f : geometry.Rotation,
                geometry.Size,
                flashDuration,
                true,
                geometry.Shape,
                null,
                isImpactFlash: true);
        }

        private int ApplyCombatImpactInShape(
            uint activator,
            TelegraphGeometry geometry,
            SkillType skillType,
            int baseDamage,
            int duration,
            Type statusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory,
            CombatDamageType damageType,
            ResistanceType statusResistanceType,
            VisualEffect targetVisualEffect,
            VisualEffect areaVisualEffect,
            Func<uint, int> damagePercentAdjustment,
            Func<uint, int> baseDamageAdjustment,
            int maxTargets,
            int enmityBonus,
            Action<uint> beforeImpact,
            Action<uint> afterSuccessfulHit,
            Func<uint, StatusEffectCategory?> beforeSuccessfulImpactRiders,
            int hitChancePercentAdjustment,
            int criticalRatePercentAdjustment,
            bool useNPCStatScaling,
            bool awardsCombatPoints,
            DamageType? effectDamageType,
            bool alwaysApplyAreaVisualEffect,
            AbilityType combatImpactDamageAbility,
            bool sendsNoTargetMessage,
            bool resolvesHit,
            bool canCritical,
            bool useUnscaledDamage)
        {
            _impactState.RecordAbilityImpactShape(activator, skillType, true);

            var origin = Location(geometry.Area, geometry.Position, 0f);
            var creatures = _geometry.GetHostileCreaturesInCombatImpactShape(activator, geometry)
                .Where(creature => _geometry.HasAbilityLineOfSight(activator, creature))
                .ToList();

            if (alwaysApplyAreaVisualEffect || creatures.Count > 0)
            {
                if (areaVisualEffect != VisualEffect.None)
                {
                    VisualEffectPlayback.ApplyAtLocation(areaVisualEffect, origin);
                }
            }

            return ApplyCombatImpactToCreatures(
                activator,
                creatures,
                skillType,
                baseDamage,
                statusEffect,
                duration,
                additionalStatusEffects,
                statusEffectFactory,
                damageType,
                statusResistanceType,
                targetVisualEffect,
                damagePercentAdjustment,
                baseDamageAdjustment,
                maxTargets,
                enmityBonus,
                beforeImpact,
                afterSuccessfulHit,
                beforeSuccessfulImpactRiders,
                hitChancePercentAdjustment: hitChancePercentAdjustment,
                criticalRatePercentAdjustment: criticalRatePercentAdjustment,
                useNPCStatScaling: useNPCStatScaling,
                awardsCombatPoints: awardsCombatPoints,
                effectDamageType: effectDamageType,
                combatImpactDamageAbility: combatImpactDamageAbility,
                sendsNoTargetMessage: sendsNoTargetMessage,
                resolvesHit: resolvesHit,
                canCritical: canCritical,
                useUnscaledDamage: useUnscaledDamage);
        }

        private ApplyTelegraphEffect BuildTelegraphedCombatImpactAction(
            SkillType skillType,
            int baseDamage,
            int duration,
            Type statusEffect,
            IEnumerable<Type> additionalStatusEffects,
            Func<IStatusEffect> statusEffectFactory,
            CombatImpactAreaShape shape,
            Location areaVisualLocation,
            AbilityDetail ability,
            AbilityImpactTracker sequenceOwner,
            int nextAbilityDamageBonus,
            int nextAbilityCriticalRatePercentAdjustment,
            int nextAbilityDefenseIgnorePercentAdjustment,
            int nextAttackEnmityBonus,
            CombatDamageType damageType,
            ResistanceType statusResistanceType,
            VisualEffect targetVisualEffect,
            VisualEffect areaVisualEffect,
            Func<uint, int> damagePercentAdjustment,
            Func<uint, int> baseDamageAdjustment,
            Action<AbilityImpactSummary> afterImpactAction,
            int maxTargets,
            int enmityBonus,
            Action<uint> beforeImpact,
            Action<uint> afterSuccessfulHit,
            Func<uint, StatusEffectCategory?> beforeSuccessfulImpactRiders,
            int hitChancePercentAdjustment,
            int criticalRatePercentAdjustment,
            bool useNPCStatScaling,
            bool awardsCombatPoints,
            DamageType? effectDamageType,
            bool alwaysApplyAreaVisualEffect,
            AbilityType combatImpactDamageAbility,
            bool sendsNoTargetMessage,
            bool resolvesHit,
            bool canCritical,
            bool useUnscaledDamage)
        {
            var resolveDamageBonusesFromOwner = sequenceOwner?.ResolveDamageBonuses != null;
            return (creator, creatures) =>
            {
                var impactStarted = false;
                var impactEnded = false;
                try
                {
                    if (!GetIsObjectValid(creator) || GetCurrentHitPoints(creator) <= 0)
                        return;

                    var hostileCreatures = creatures
                        .Where(creature =>
                            GetIsObjectValid(creature) &&
                            GetIsReactionTypeHostile(creature, creator) &&
                            _geometry.HasAbilityLineOfSight(creator, creature))
                        .ToList();

                    if (maxTargets > 0)
                    {
                        var impactPosition = GetPositionFromLocation(areaVisualLocation);
                        hostileCreatures = CombatImpactShapeGeometry
                            .TakeClosestToOrigin(hostileCreatures, impactPosition, GetPosition, maxTargets)
                            .ToList();
                    }

                    if (hostileCreatures.Count <= 0)
                    {
                        if (alwaysApplyAreaVisualEffect && areaVisualEffect != VisualEffect.None)
                        {
                            VisualEffectPlayback.ApplyAtLocation(areaVisualEffect, areaVisualLocation);
                        }

                        if (sendsNoTargetMessage)
                            _impactMessage.SendCombatImpactNoTargetsMessage(creator, ability);
                        return;
                    }

                    if (ability != null)
                    {
                        _lifecycle.BeginTelegraphedAbilityImpact(
                            creator,
                            ability,
                            nextAbilityDamageBonus,
                            nextAbilityCriticalRatePercentAdjustment,
                            nextAbilityDefenseIgnorePercentAdjustment,
                            nextAttackEnmityBonus,
                            sequenceOwner,
                            resolveDamageBonusesFromOwner);
                        impactStarted = true;
                        _impactState.RecordAbilityImpactShape(creator, skillType, true);
                    }

                    if (areaVisualEffect != VisualEffect.None)
                    {
                        VisualEffectPlayback.ApplyAtLocation(areaVisualEffect, areaVisualLocation);
                    }

                    ApplyCombatImpactToCreatures(
                        creator,
                        hostileCreatures,
                        skillType,
                        baseDamage,
                        statusEffect,
                        duration,
                        additionalStatusEffects,
                        statusEffectFactory,
                        damageType,
                        statusResistanceType,
                        targetVisualEffect,
                        damagePercentAdjustment,
                        baseDamageAdjustment,
                        maxTargets,
                        enmityBonus,
                        beforeImpact,
                        afterSuccessfulHit,
                        beforeSuccessfulImpactRiders,
                        hitChancePercentAdjustment: hitChancePercentAdjustment,
                        criticalRatePercentAdjustment: criticalRatePercentAdjustment,
                        useNPCStatScaling: useNPCStatScaling,
                        awardsCombatPoints: awardsCombatPoints,
                        effectDamageType: effectDamageType,
                        combatImpactDamageAbility: combatImpactDamageAbility,
                        sendsNoTargetMessage: sendsNoTargetMessage,
                        resolvesHit: resolvesHit,
                        canCritical: canCritical,
                        useUnscaledDamage: useUnscaledDamage);

                    if (ability != null)
                    {
                        var summary = _lifecycle.EndAbilityImpact(creator);
                        impactEnded = true;
                        ApplyAbilityImpactEffects(creator, summary);
                        afterImpactAction?.Invoke(summary);
                    }

                }
                finally
                {
                    if (impactStarted && !impactEnded)
                    {
                        _lifecycle.AbortAbilityImpact(creator);
                    }

                    CompleteDeferredAbilityStaminaCostContext(creator, ability);
                }
            };
        }

        private int ApplyCombatImpactToCreatures(
            uint activator,
            IEnumerable<uint> creatures,
            SkillType skillType,
            int baseDamage,
            Type statusEffect,
            int duration,
            IEnumerable<Type> additionalStatusEffects = null,
            Func<IStatusEffect> statusEffectFactory = null,
            CombatDamageType damageType = CombatDamageType.Physical,
            ResistanceType statusResistanceType = ResistanceType.Invalid,
            VisualEffect targetVisualEffect = VisualEffect.None,
            Func<uint, int> damagePercentAdjustment = null,
            Func<uint, int> baseDamageAdjustment = null,
            int maxTargets = 0,
            int enmityBonus = 0,
            Action<uint> beforeImpact = null,
            Action<uint> afterSuccessfulHit = null,
            Func<uint, StatusEffectCategory?> beforeSuccessfulImpactRiders = null,
            int hitChancePercentAdjustment = 0,
            int criticalRatePercentAdjustment = 0,
            bool useNPCStatScaling = false,
            bool awardsCombatPoints = true,
            DamageType? effectDamageType = null,
            AbilityType combatImpactDamageAbility = AbilityType.Invalid,
            bool sendsNoTargetMessage = true,
            bool resolvesHit = true,
            bool canCritical = true,
            bool useUnscaledDamage = false)
        {
            var totalDamage = 0;
            var affectedCount = 0;
            var trackedAbility = _impactState.GetTrackedAbilityImpact(activator)?.Ability;

            var impactTargets = creatures.Distinct().ToList();
            if (beforeImpact != null)
            {
                // Capture prerequisites for the entire target set before damage or riders
                // on any hit can change another target's eligibility during this impact.
                foreach (var creature in impactTargets)
                {
                    if (GetIsObjectValid(creature) && GetIsReactionTypeHostile(creature, activator))
                        beforeImpact(creature);
                }
            }

            foreach (var creature in impactTargets)
            {
                if (!GetIsObjectValid(creature) || !GetIsReactionTypeHostile(creature, activator))
                    continue;

                if (maxTargets > 0 && affectedCount >= maxTargets)
                    break;

                totalDamage += _targetImpact.ApplyHostileCombatImpact(
                    activator,
                    creature,
                    skillType,
                    baseDamage,
                    statusEffect,
                    duration,
                    additionalStatusEffects,
                    statusEffectFactory,
                    damageType,
                    statusResistanceType,
                    targetVisualEffect,
                    damagePercentAdjustment,
                    baseDamageAdjustment,
                    enmityBonus,
                    afterSuccessfulHit,
                    beforeSuccessfulImpactRiders,
                    hitChancePercentAdjustment,
                    criticalRatePercentAdjustment,
                    useNPCStatScaling,
                    awardsCombatPoints,
                    effectDamageType,
                    combatImpactDamageAbility,
                    resolvesHit,
                    canCritical,
                    useUnscaledDamage);
                affectedCount++;
            }

            if (affectedCount <= 0 && sendsNoTargetMessage)
            {
                _impactMessage.SendCombatImpactNoTargetsMessage(activator, trackedAbility);
            }

            return totalDamage;
        }

        /// <summary>Plays the current ability's declared impact gesture independently of target outcomes.</summary>
        public void PlayAbilityImpactAnimation(uint activator) =>
            PlayCombatImpactAnimation(activator, Animation.Invalid);

        /// <summary>
        /// Plays a non-weapon combat impact animation while preserving explicit throw carriers.
        /// </summary>
        private void PlayCombatImpactAnimation(uint activator, Animation impactAnimation)
        {
            var trackedAbility = _impactState.GetTrackedAbilityImpact(activator)?.Ability;

            // Queued weapon abilities resolve inside the engine's landed auto-attack. Playing a
            // scripted impact animation here would enqueue a second swing after the real hit,
            // which becomes especially visible after a lethal attack has already killed its target.
            if (trackedAbility?.ActivationType == AbilityActivationType.Weapon)
                return;

            var animation = impactAnimation == Animation.Invalid
                ? trackedAbility?.ImpactAnimationType ?? Animation.Invalid
                : impactAnimation;

            if (animation == Animation.Invalid)
                return;

            var authoredImpact = SWLOR.Game.Server.Domain.Abilities.AbilityAnimationBinding.ImpactClip(trackedAbility, activator);
            if (authoredImpact != null)
            {
                // Damage and projectile effects are already dispatched by the ability. Use the
                // named one-shot carrier here, including grenades, without queuing another action.
                _namedAnimation.Play(activator, authoredImpact, equipmentRequirement: trackedAbility.AnimationEquipmentRequirement);
                return;
            }

            // Native impact carriers must not reuse a preceding named clip's replacements.
            _namedAnimation.ReleaseForNativePlayback(activator);

            if (trackedAbility?.ImmediateNativeImpactAnimationDuration > 0f)
            {
                AssignCommand(activator, () => PistolAnimationRemap.PlayAnimationPreservingExplicitThrow(
                    activator, animation, 1f, trackedAbility.ImmediateNativeImpactAnimationDuration, immediate: true));
                return;
            }

            var sourceAnimationName = string.Empty;
            var replacementAnimationName = string.Empty;
            var restoreDelaySeconds = 0f;

            if (impactAnimation == Animation.Invalid && trackedAbility != null)
            {
                sourceAnimationName = trackedAbility.ImpactAnimationSourceAnimationName;
                replacementAnimationName = trackedAbility.ImpactAnimationReplacementAnimationName;
                restoreDelaySeconds = trackedAbility.ImpactAnimationRestoreDelaySeconds;
            }

            if (!string.IsNullOrWhiteSpace(sourceAnimationName) &&
                !string.IsNullOrWhiteSpace(replacementAnimationName))
            {
                AssignCommand(activator, () =>
                {
                    PistolAnimationRemap.PlayAnimationWithTemporaryReplacementPreservingExplicitThrow(
                        activator,
                        animation,
                        1.0f,
                        restoreDelaySeconds,
                        sourceAnimationName,
                        replacementAnimationName,
                        restoreDelaySeconds);
                });
                return;
            }

            AssignCommand(
                activator,
                () => PistolAnimationRemap.PlayAnimationPreservingExplicitThrow(
                    activator,
                    animation));
        }

        protected virtual void ApplyAbilityImpactEffects(uint activator, AbilityImpactSummary summary) => _abilityImpact.ApplyAbilityImpactEffects(activator, summary);

        protected virtual void DeferAbilityStaminaCostContext(uint creature, AbilityDetail ability) => _resources.DeferAbilityStaminaCostContext(creature, ability);

        protected virtual void CompleteDeferredAbilityStaminaCostContext(uint creature, AbilityDetail ability) => _resources.CompleteDeferredAbilityStaminaCostContext(creature, ability);
    }
}
