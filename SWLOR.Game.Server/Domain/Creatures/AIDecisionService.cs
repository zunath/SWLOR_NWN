using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Native;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// The decision side of the profile engine: which creature triggers get a decision, the throttle, the active phase, the
    /// action cache and the boss timer. It picks the action and runs it through <see cref="IAIActionService"/>. Moved here
    /// unchanged from <see cref="AIService"/>.
    /// </summary>
    public class AIDecisionService : IAIDecisionService
    {
        private readonly IAIActionService _action;
        private readonly IActivityService _activity;
        private readonly IAIService _ai;
        private readonly IEnmityService _enmity;
        private readonly IEnmityAttackService _enmityAttack;

        public AIDecisionService(
            IAIActionService action,
            IActivityService activity,
            IAIService ai,
            IEnmityService enmity,
            IEnmityAttackService enmityAttack)
        {
            _action = action;
            _activity = activity;
            _ai = ai;
            _enmity = enmity;
            _enmityAttack = enmityAttack;
        }

        public bool ProcessTrigger(
            uint creature,
            AITriggerType trigger,
            uint eventTarget = OBJECT_INVALID,
            bool bypassDecisionThrottle = false)
        {
            if (trigger != AITriggerType.Death && !CreatureAIRules.IsCreatureAIEnabled(creature))
                return false;

            return ProcessProfileTrigger(
                creature,
                trigger,
                eventTarget,
                _ai.BuildAllies(creature),
                bypassDecisionThrottle);
        }

        private bool ProcessProfileTrigger(
            uint creature,
            AITriggerType trigger,
            uint eventTarget,
            IReadOnlyList<uint> allies,
            bool bypassDecisionThrottle = false)
        {
            if (!GetIsObjectValid(creature) ||
                GetIsPC(creature) ||
                GetIsDM(creature) ||
                GetIsDMPossessed(creature))
            {
                return false;
            }

            if (trigger == AITriggerType.Death ||
                GetCurrentHitPoints(creature) <= 0)
            {
                _ai.ClearAIState(creature);
                return false;
            }

            if (GetAILevel(creature) == AILevel.VeryLow)
                return false;

            if (HasEffectType(creature, EffectTypeScript.Petrify))
                return false;

            var profileType = _ai.GetProfileType(creature);
            if (!_ai.Profiles.TryGetValue(profileType, out var profile))
            {
                profile = _ai.Profiles[AIProfileType.Generic];
            }

            var state = _ai.GetState(creature, profileType);
            RefreshActionCache(creature, profile, state);
            var context = new AIContext(creature, trigger, eventTarget, profile, state, allies);
            UseAttackableEnmityTarget(context);

            if (GetIsObjectValid(context.CurrentEnmityTarget) &&
                _ai.TryStartCombatLeashEvade(creature, context.CurrentEnmityTarget))
            {
                return false;
            }

            if (GetIsObjectValid(context.CurrentEnmityTarget) && state.CombatStartedTime == default)
                state.CombatStartedTime = Clock.UtcNow;

            UpdateActivePhase(context);

            if (_activity.IsBusy(creature))
            {
                ScheduleBossTimer(context);
                return false;
            }

            if (!bypassDecisionThrottle && ShouldThrottle(profile, state, trigger))
                return false;

            state.LastDecisionTime = Clock.UtcNow;

            ProfilerPlugin.PushPerfScope(creature, "NpcAI.Decide");
            try
            {
                var selected = _action.SelectAction(context);
                if (selected == null)
                {
                    AttackHighestEnmityTarget(creature);
                    return false;
                }

                _action.ExecuteAction(context, selected);
                return true;
            }
            finally
            {
                ProfilerPlugin.PopPerfScope();
                ScheduleBossTimer(context);
            }
        }

        private bool ShouldThrottle(AIProfile profile, AIState state, AITriggerType trigger)
        {
            if (trigger is AITriggerType.Spawn or AITriggerType.Death)
                return false;

            if (profile.DecisionThrottleSeconds <= 0f || state.LastDecisionTime == default)
                return false;

            return (Clock.UtcNow - state.LastDecisionTime).TotalSeconds < profile.DecisionThrottleSeconds;
        }

        private void UpdateActivePhase(AIContext context)
        {
            if (context.Profile.PhaseOrder.Count <= 0)
                return;

            var selected = AIPhaseId.Invalid;
            foreach (var phaseId in context.Profile.PhaseOrder)
            {
                var phase = context.Profile.Phases[phaseId];
                if (phase.EnterCondition?.Invoke(context) == true)
                {
                    selected = phaseId;
                }
            }

            if (selected == AIPhaseId.Invalid || selected == context.State.ActivePhase)
                return;

            context.State.ActivePhase = selected;
            context.State.EnteredPhases.Add(selected);
        }

        private void RefreshActionCache(uint creature, AIProfile profile, AIState state)
        {
            var knownFeats = GetKnownFeats(creature, out var featCount, out var featChecksum);
            if (state.ActionCacheFeatCount == featCount &&
                state.ActionCacheFeatChecksum == featChecksum &&
                state.CachedActions.Count > 0)
            {
                return;
            }

            state.ActionCacheFeatCount = featCount;
            state.ActionCacheFeatChecksum = featChecksum;
            state.CachedActions.Clear();
            state.CachedActions.AddRange(FilterKnownActions(profile.Actions, knownFeats));
            state.CachedPhaseActions.Clear();

            foreach (var (phaseId, phase) in profile.Phases)
            {
                state.CachedPhaseActions[phaseId] = FilterKnownActions(phase.Actions, knownFeats).ToList();
            }
        }

        private IEnumerable<AIActionDefinition> FilterKnownActions(
            IEnumerable<AIActionDefinition> actions,
            HashSet<FeatType> knownFeats)
        {
            foreach (var action in actions)
            {
                if (action.Type != AIActionType.Ability ||
                    knownFeats.Contains(action.Feat))
                {
                    yield return action;
                }
            }
        }

        private HashSet<FeatType> GetKnownFeats(uint creature, out int featCount, out int featChecksum)
        {
            var knownFeats = new HashSet<FeatType>();
            featCount = CreaturePlugin.GetFeatCount(creature);
            featChecksum = 17;

            unchecked
            {
                for (var index = 0; index < featCount; index++)
                {
                    var feat = CreaturePlugin.GetFeatByIndex(creature, index);
                    knownFeats.Add(feat);
                    featChecksum = featChecksum * 31 + (int)feat;
                }
            }

            return knownFeats;
        }

        private void ScheduleBossTimer(AIContext context)
        {
            if (!context.Profile.IsBoss ||
                context.State.BossTimerScheduled ||
                !GetIsObjectValid(context.CurrentEnmityTarget))
            {
                return;
            }

            var creature = context.Self;
            context.State.BossTimerScheduled = true;

            DelayCommand(1.0f, () =>
            {
                if (!_ai.TryGetState(creature, out var state))
                    return;

                state.BossTimerScheduled = false;

                if (!GetIsObjectValid(creature) || GetCurrentHitPoints(creature) <= 0)
                {
                    _ai.ClearAIState(creature);
                    return;
                }

                var target = GetHighestEnmityTarget(creature);
                if (!GetIsObjectValid(target) && !GetIsInCombat(creature))
                {
                    state.CombatStartedTime = default;
                    return;
                }

                ProcessTrigger(creature, AITriggerType.BossTimer, target);
            });
        }

        private bool HasEffectType(uint creature, EffectTypeScript effectType)
        {
            if (!MayHaveScriptType(creature, effectType))
                return false;

            var effect = GetFirstEffect(creature);
            while (GetIsEffectValid(effect))
            {
                if (GetEffectType(effect) == effectType)
                    return true;

                effect = GetNextEffect(creature);
            }

            return false;
        }

        // ---- Seams into services that are not converted yet ----
        // Each protected virtual method below has the original call as its body, so the server behaves as it did.
        // A unit test subclass answers them, because the callees read the static enmity tables, the database, the
        // native engine or the other services' statics for real creatures.

        protected virtual uint GetHighestEnmityTarget(uint enemy) => _enmity.GetHighestEnmityTarget(enemy);

        protected virtual void AttackHighestEnmityTarget(uint creature) => _enmityAttack.AttackHighestEnmityTarget(creature);

        protected virtual bool MayHaveScriptType(uint creature, EffectTypeScript effectType) => AppliedEffects.MayHaveScriptType(creature, effectType);

        /// <summary>
        /// Points the context at the highest-enmity creature the NPC can attack, skipping creatures hidden by
        /// invisibility. The body is the original call; a unit test answers it because the context reads the static enmity
        /// tables and the native effect list.
        /// </summary>
        protected virtual void UseAttackableEnmityTarget(AIContext context) => context.UseAttackableEnmityTarget();
    }
}
