using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CompanionControlService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service.AIService
{
    public static class NPCAI
    {
        public const string ProfileLocalVariable = "AI_PROFILE";
        public const string ProfileIdLocalVariable = "AI_PROFILE_ID";

        private static readonly Dictionary<AIProfileType, AIProfile> _profiles = new();
        private static readonly Dictionary<uint, AIState> _states = new();

        private sealed class ActionEvaluation
        {
            public AIActionDefinition Action { get; set; }
            public uint Target { get; set; }
            public int Score { get; set; }
            public int Priority { get; set; }
            public int Index { get; set; }
        }

        public static IReadOnlyDictionary<AIProfileType, AIProfile> Profiles => _profiles;

        public static void CacheProfiles()
        {
            _profiles.Clear();

            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IAIProfileDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IAIProfileDefinition)Activator.CreateInstance(type);
                var profiles = instance.BuildProfiles();

                foreach (var (profileType, profile) in profiles)
                {
                    if (_profiles.ContainsKey(profileType))
                    {
                        throw new InvalidOperationException($"AI profile '{profileType}' is already registered.");
                    }

                    _profiles[profileType] = profile;
                }
            }

            Console.WriteLine($"Loaded {_profiles.Count} AI profiles.");
        }

        public static void ValidateProfiles()
        {
            if (!_profiles.ContainsKey(AIProfileType.Generic))
                throw new InvalidOperationException("AI profile 'Generic' must be registered.");

            if (!_profiles.ContainsKey(AIProfileType.BeastCompanion))
                throw new InvalidOperationException("AI profile 'BeastCompanion' must be registered.");

            if (!_profiles.ContainsKey(AIProfileType.DroidCompanion))
                throw new InvalidOperationException("AI profile 'DroidCompanion' must be registered.");

            foreach (var profile in _profiles.Values)
            {
                ValidateActions(profile, profile.Actions);

                foreach (var phaseId in profile.PhaseOrder)
                {
                    var phase = profile.Phases[phaseId];
                    if (phase.EnterCondition == null)
                    {
                        throw new InvalidOperationException($"AI profile '{profile.Type}' phase '{phase.Id}' has no EnterWhen condition.");
                    }

                    ValidateActions(profile, phase.Actions);
                }
            }
        }

        public static void SetProfile(uint creature, AIProfileType profile)
        {
            SetLocalString(creature, ProfileLocalVariable, profile.ToString());
            SetLocalInt(creature, ProfileIdLocalVariable, (int)profile);
        }

        public static AIProfileType GetProfileType(uint creature)
        {
            var profileName = GetLocalString(creature, ProfileLocalVariable);
            if (!string.IsNullOrWhiteSpace(profileName) &&
                Enum.TryParse(profileName, true, out AIProfileType namedProfile) &&
                namedProfile != AIProfileType.Invalid)
            {
                return namedProfile;
            }

            var profileId = GetLocalInt(creature, ProfileIdLocalVariable);
            if (profileId > 0 && Enum.IsDefined(typeof(AIProfileType), profileId))
                return (AIProfileType)profileId;

            if (BeastMastery.IsPlayerBeast(creature))
                return AIProfileType.BeastCompanion;

            if (Droid.IsDroid(creature))
                return AIProfileType.DroidCompanion;

            return AIProfileType.Generic;
        }

        public static void ClearState(uint creature)
        {
            _states.Remove(creature);
        }

        public static bool ProcessTrigger(
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
                ClearState(creature);
                return false;
            }

            if (GetAILevel(creature) == AILevel.VeryLow)
                return false;

            if (GetHasEffect(creature, EffectTypeScript.Petrify))
                return false;

            var profileType = GetProfileType(creature);
            if (!_profiles.TryGetValue(profileType, out var profile))
            {
                profile = _profiles[AIProfileType.Generic];
            }

            var state = GetState(creature, profileType);
            RefreshActionCache(creature, profile, state);
            var context = new AIContext(creature, trigger, eventTarget, profile, state, allies);

            if (GetIsObjectValid(context.CurrentEnmityTarget) &&
                AI.TryStartCombatLeashEvade(creature, context.CurrentEnmityTarget))
            {
                return false;
            }

            if (GetIsObjectValid(context.CurrentEnmityTarget) && state.CombatStartedTime == default)
                state.CombatStartedTime = DateTime.UtcNow;

            UpdateActivePhase(context);

            if (Activity.IsBusy(creature))
            {
                ScheduleBossTimer(context);
                return false;
            }

            if (!bypassDecisionThrottle && ShouldThrottle(profile, state, trigger))
                return false;

            state.LastDecisionTime = DateTime.UtcNow;

            ProfilerPlugin.PushPerfScope(creature, "NpcAI.Decide");
            try
            {
                var selected = SelectAction(context);
                if (selected == null)
                {
                    Enmity.AttackHighestEnmityTarget(creature);
                    return false;
                }

                ExecuteAction(context, selected);
                return true;
            }
            finally
            {
                ProfilerPlugin.PopPerfScope();
                ScheduleBossTimer(context);
            }
        }

        private static AIState GetState(uint creature, AIProfileType profileType)
        {
            if (!_states.TryGetValue(creature, out var state))
            {
                state = new AIState();
                _states[creature] = state;
            }

            if (state.Profile != profileType)
            {
                state.Profile = profileType;
                state.ActivePhase = AIPhaseId.Invalid;
                state.EnteredPhases.Clear();
                state.CompletedOnceActions.Clear();
                state.Cooldowns.Clear();
                state.LastDecisionTime = default;
                state.CombatStartedTime = default;
                state.ClearActionCache();
            }

            return state;
        }

        private static bool ShouldThrottle(AIProfile profile, AIState state, AITriggerType trigger)
        {
            if (trigger is AITriggerType.Spawn or AITriggerType.Death)
                return false;

            if (profile.DecisionThrottleSeconds <= 0f || state.LastDecisionTime == default)
                return false;

            return (DateTime.UtcNow - state.LastDecisionTime).TotalSeconds < profile.DecisionThrottleSeconds;
        }

        private static void UpdateActivePhase(AIContext context)
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

        private static ActionEvaluation SelectAction(AIContext context)
        {
            var candidates = context.State.CachedActions.AsEnumerable();
            if (context.State.ActivePhase != AIPhaseId.Invalid &&
                context.State.CachedPhaseActions.TryGetValue(context.State.ActivePhase, out var phaseActions))
            {
                candidates = candidates.Concat(phaseActions);
            }

            ActionEvaluation best = null;
            var index = 0;
            foreach (var action in candidates.Take(context.Profile.MaxCandidateActions))
            {
                var evaluation = EvaluateAction(context, action, index);
                index++;

                if (evaluation == null)
                    continue;

                if (best == null ||
                    evaluation.Score > best.Score ||
                    evaluation.Score == best.Score && evaluation.Priority < best.Priority ||
                    evaluation.Score == best.Score && evaluation.Priority == best.Priority && evaluation.Index < best.Index)
                {
                    best = evaluation;
                }
            }

            return best;
        }

        private static void RefreshActionCache(uint creature, AIProfile profile, AIState state)
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

        private static IEnumerable<AIActionDefinition> FilterKnownActions(
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

        private static HashSet<FeatType> GetKnownFeats(uint creature, out int featCount, out int featChecksum)
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

        private static ActionEvaluation EvaluateAction(AIContext context, AIActionDefinition action, int index)
        {
            if (action.OncePerPhase && HasCompletedOncePerPhaseAction(context, action))
                return null;

            if (IsOnCooldown(context, action))
                return null;

            foreach (var guard in action.Guards)
            {
                if (!guard(context))
                    return null;
            }

            var target = ResolveTarget(context, action);
            context.SetEvaluatedTarget(target);

            if (!CanExecuteAction(context, action, target))
                return null;

            var score = action.Score?.Invoke(context) ?? 0;
            if (score <= 0)
                return null;

            return new ActionEvaluation
            {
                Action = action,
                Target = target,
                Score = score,
                Priority = action.Priority,
                Index = index
            };
        }

        private static uint ResolveTarget(AIContext context, AIActionDefinition action)
        {
            if (action.Type == AIActionType.AttackHighestEnmity)
                return context.CurrentEnmityTarget;

            if (action.Type == AIActionType.Ability)
            {
                var ability = Ability.GetAbilityDetail(action.Feat);
                AITargetSelector selector;
                if (action.TargetSelector != null)
                    selector = action.TargetSelector;
                else if (AITarget.TryGetDefaultOverride(action.Feat, out var defaultSelector))
                    selector = defaultSelector;
                else
                    selector = AITarget.InferDefault(action.Feat, ability);

                var selectedTarget = selector(context);
                if (ability.IsHostileAbility && CompanionControl.IsControlledCompanion(context.Self))
                {
                    return CompanionControl.ResolveHostileAbilityTarget(
                        context.Self,
                        ability,
                        context.CurrentEnmityTarget,
                        selectedTarget);
                }

                return selectedTarget;
            }

            if (action.TargetSelector != null)
                return action.TargetSelector(context);

            return context.Self;
        }

        private static bool CanExecuteAction(AIContext context, AIActionDefinition action, uint target)
        {
            if (CompanionControl.IsControlledCompanion(context.Self) &&
                IsHostileIntent(action) &&
                !GetIsObjectValid(context.CurrentEnmityTarget))
            {
                return false;
            }

            switch (action.Type)
            {
                case AIActionType.Ability:
                    return CanUseAbility(context, action, target);
                case AIActionType.AttackHighestEnmity:
                case AIActionType.MoveToTarget:
                case AIActionType.Flee:
                case AIActionType.CallAllies:
                    return GetIsObjectValid(target);
                case AIActionType.ReturnHome:
                    return context.IsOutsideHomeRadius;
                case AIActionType.RandomWalk:
                    return !GetIsInCombat(context.Self) && GetCurrentAction(context.Self) == ActionType.Invalid;
                case AIActionType.Wait:
                case AIActionType.Speak:
                case AIActionType.Script:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsHostileIntent(AIActionDefinition action)
        {
            if (action.Type == AIActionType.AttackHighestEnmity)
                return true;

            return action.Type == AIActionType.Ability &&
                   action.Feat != FeatType.Invalid &&
                   Ability.IsFeatRegistered(action.Feat) &&
                   Ability.GetAbilityDetail(action.Feat).IsHostileAbility;
        }

        private static bool CanUseAbility(AIContext context, AIActionDefinition action, uint target)
        {
            if (action.Feat == FeatType.Invalid || !Ability.IsFeatRegistered(action.Feat))
                return false;

            if (CompanionControl.IsControlledCompanion(context.Self) &&
                !CompanionControl.AreAbilitiesEnabled(context.Self))
            {
                return false;
            }

            if (!GetHasFeat(action.Feat, context.Self))
                return false;

            var ability = Ability.GetAbilityDetail(action.Feat);
            if (ability.IsHostileAbility &&
                ability.IsSingleTargetAbility &&
                (!GetIsObjectValid(target) ||
                 target == context.Self ||
                 !GetIsReactionTypeHostile(target, context.Self)))
            {
                return false;
            }

            if (ability.RequiresTarget && !GetIsObjectValid(target))
                return false;

            if (!GetIsObjectValid(target))
                target = context.Self;

            var targetLocation = GetLocation(target);
            var effectiveLevel = ability.EffectiveLevelPerkType == PerkType.Invalid
                ? 1
                : Perk.GetPerkLevel(context.Self, ability.EffectiveLevelPerkType);

            return Ability.CanUseAbility(context.Self, target, action.Feat, effectiveLevel, targetLocation);
        }

        private static void ExecuteAction(AIContext context, ActionEvaluation evaluation)
        {
            var action = evaluation.Action;
            ProfilerPlugin.PushPerfScope(context.Self, "NpcAI.Execute");
            try
            {
                switch (action.Type)
                {
                    case AIActionType.Ability:
                        ExecuteAbility(
                            context.Self,
                            action,
                            evaluation.Target,
                            context.CurrentEnmityTarget);
                        break;
                    case AIActionType.AttackHighestEnmity:
                        Enmity.AttackHighestEnmityTarget(context.Self);
                        break;
                    case AIActionType.MoveToTarget:
                        AssignCommand(context.Self, () =>
                        {
                            ClearAllActions();
                            ActionMoveToObject(evaluation.Target, true);
                        });
                        break;
                    case AIActionType.Flee:
                        AssignCommand(context.Self, () =>
                        {
                            ClearAllActions();
                            ActionForceMoveToLocation(context.HomeLocation);
                        });
                        break;
                    case AIActionType.ReturnHome:
                        AssignCommand(context.Self, () =>
                        {
                            ClearAllActions();
                            ActionForceMoveToLocation(context.HomeLocation);
                        });
                        break;
                    case AIActionType.RandomWalk:
                        AssignCommand(context.Self, () =>
                        {
                            ClearAllActions();
                            ActionRandomWalk();
                        });
                        break;
                    case AIActionType.Wait:
                        AssignCommand(context.Self, () => ActionWait(action.FloatValue));
                        break;
                    case AIActionType.Speak:
                        AssignCommand(context.Self, () => SpeakString(action.Text));
                        break;
                    case AIActionType.Script:
                        ExecuteScript(action.ScriptName, context.Self);
                        break;
                    case AIActionType.CallAllies:
                        CallAllies(context, evaluation.Target, action.FloatValue);
                        break;
                }

                ApplyCooldown(context, action);
                MarkOncePerPhaseAction(context, action);

                Log.Write(LogGroup.AI, $"{GetName(context.Self)} [{context.Profile.Type}] executed {action.DebugName} with score {evaluation.Score}.");
            }
            finally
            {
                ProfilerPlugin.PopPerfScope();
            }
        }

        private static void ExecuteAbility(
            uint creature,
            AIActionDefinition action,
            uint target,
            uint authorizedAttackTarget)
        {
            if (!GetIsObjectValid(target))
                target = creature;

            var targetLocation = GetLocation(target);
            if (UsePerkFeat.TryUseAbility(creature, target, action.Feat, targetLocation))
                return;

            if (CompanionControl.IsControlledCompanion(creature))
            {
                target = authorizedAttackTarget;
            }
            else if (!GetIsObjectValid(target) || target == creature)
            {
                target = Enmity.GetHighestEnmityTarget(creature);
            }

            if (!GetIsObjectValid(target))
                return;

            Enmity.IssueAttackCommand(creature, target);
        }

        public static bool TryUseBestTargetedSupportAbility(uint creature, uint target, out string abilityName)
        {
            abilityName = string.Empty;
            if (!GetIsObjectValid(creature) ||
                !GetIsObjectValid(target) ||
                GetCurrentHitPoints(target) >= GetMaxHitPoints(target))
            {
                return false;
            }

            var candidates = Ability.GetAllAbilityDetails()
                .Where(x => x.Value.IsHealingAbility &&
                            !x.Value.IsHostileAbility &&
                            x.Value.RequiresTarget &&
                            x.Value.IsSingleTargetAbility &&
                            GetHasFeat(x.Key, creature))
                .OrderByDescending(x => x.Value.AbilityLevel)
                .ThenBy(x => x.Value.Name)
                .ToList();

            foreach (var (feat, ability) in candidates)
            {
                var targetLocation = GetLocation(target);
                if (!UsePerkFeat.TryUseAbility(creature, target, feat, targetLocation))
                    continue;

                abilityName = ability.Name;
                return true;
            }

            return false;
        }

        private static bool IsOnCooldown(AIContext context, AIActionDefinition action)
        {
            if (string.IsNullOrWhiteSpace(action.CooldownId))
                return false;

            return context.State.Cooldowns.TryGetValue(action.CooldownId, out var availableAt) &&
                   availableAt > DateTime.UtcNow;
        }

        private static void ApplyCooldown(AIContext context, AIActionDefinition action)
        {
            if (string.IsNullOrWhiteSpace(action.CooldownId) || action.CooldownSeconds <= 0f)
                return;

            context.State.Cooldowns[action.CooldownId] = DateTime.UtcNow.AddSeconds(action.CooldownSeconds);
        }

        private static bool HasCompletedOncePerPhaseAction(AIContext context, AIActionDefinition action)
        {
            if (context.State.ActivePhase == AIPhaseId.Invalid)
                return false;

            return context.State.CompletedOnceActions.Contains(GetOncePerPhaseKey(context, action));
        }

        private static void MarkOncePerPhaseAction(AIContext context, AIActionDefinition action)
        {
            if (!action.OncePerPhase || context.State.ActivePhase == AIPhaseId.Invalid)
                return;

            context.State.CompletedOnceActions.Add(GetOncePerPhaseKey(context, action));
        }

        private static string GetOncePerPhaseKey(AIContext context, AIActionDefinition action)
        {
            return $"{context.State.ActivePhase}:{action.Type}:{action.DebugName}:{action.Feat}";
        }

        private static void CallAllies(AIContext context, uint target, float radius)
        {
            foreach (var ally in context.Allies)
            {
                if (!GetIsObjectValid(ally) || ally == context.Self)
                    continue;

                if (radius > 0f && GetDistanceBetween(context.Self, ally) > radius)
                    continue;

                if (!GetIsEnemy(target, ally))
                    continue;

                Enmity.ModifyEnmity(target, ally, 1);
            }
        }

        private static void ScheduleBossTimer(AIContext context)
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
                if (!_states.TryGetValue(creature, out var state))
                    return;

                state.BossTimerScheduled = false;

                if (!GetIsObjectValid(creature) || GetCurrentHitPoints(creature) <= 0)
                {
                    ClearState(creature);
                    return;
                }

                var target = Enmity.GetHighestEnmityTarget(creature);
                if (!GetIsObjectValid(target) && !GetIsInCombat(creature))
                {
                    state.CombatStartedTime = default;
                    return;
                }

                AI.ProcessTrigger(creature, AITriggerType.BossTimer, target);
            });
        }

        private static void ValidateActions(AIProfile profile, IEnumerable<AIActionDefinition> actions)
        {
            foreach (var action in actions)
            {
                if (action.Type == AIActionType.Ability)
                {
                    if (action.Feat == FeatType.Invalid)
                    {
                        throw new InvalidOperationException($"AI profile '{profile.Type}' has an ability action with an invalid feat.");
                    }

                    if (!Ability.IsFeatRegistered(action.Feat))
                    {
                        throw new InvalidOperationException($"AI profile '{profile.Type}' references feat '{action.Feat}', but it is not registered as an ability.");
                    }
                }

                if (action.Score == null)
                {
                    throw new InvalidOperationException($"AI profile '{profile.Type}' action '{action.DebugName}' has no score.");
                }
            }
        }

        private static bool GetHasEffect(uint creature, EffectTypeScript effectType)
        {
            var effect = GetFirstEffect(creature);
            while (GetIsEffectValid(effect))
            {
                if (GetEffectType(effect) == effectType)
                    return true;

                effect = GetNextEffect(creature);
            }

            return false;
        }
    }
}
