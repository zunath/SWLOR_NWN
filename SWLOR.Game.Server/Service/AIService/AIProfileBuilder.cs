using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.AIService
{
    public sealed class AIProfileBuilder
    {
        private readonly Dictionary<AIProfileType, AIProfile> _profiles = new();
        private AIProfile _activeProfile;
        private AIPhaseDefinition _activePhase;
        private AIActionDefinition _activeAction;

        public AIProfileBuilder Create(AIProfileType type)
        {
            _activeProfile = new AIProfile
            {
                Type = type,
                Name = type.ToString()
            };
            _activePhase = null;
            _activeAction = null;
            _profiles[type] = _activeProfile;

            return this;
        }

        public AIProfileBuilder Name(string name)
        {
            _activeProfile.Name = name;
            return this;
        }

        public AIProfileBuilder Boss()
        {
            _activeProfile.IsBoss = true;
            _activeProfile.MaxCandidateActions = Math.Max(_activeProfile.MaxCandidateActions, 24);
            return this;
        }

        public AIProfileBuilder DecisionThrottle(float seconds)
        {
            _activeProfile.DecisionThrottleSeconds = seconds < 0f ? 0f : seconds;
            return this;
        }

        public AIProfileBuilder MaxCandidateActions(int count)
        {
            _activeProfile.MaxCandidateActions = count < 1 ? 1 : count;
            return this;
        }

        public AIProfileBuilder Phase<TPhase>(TPhase phase)
            where TPhase : struct, Enum
        {
            return Phase(AIPhaseId.Create(_activeProfile.Type, phase));
        }

        public AIProfileBuilder Phase(AIPhaseId phaseId)
        {
            _activePhase = new AIPhaseDefinition
            {
                Id = phaseId
            };
            _activeAction = null;
            _activeProfile.Phases[phaseId] = _activePhase;
            _activeProfile.PhaseOrder.Add(phaseId);

            return this;
        }

        public AIProfileBuilder EnterWhen(AIPhaseCondition condition)
        {
            _activePhase.EnterCondition = condition;
            return this;
        }

        public AIProfileBuilder Ability(FeatType feat)
        {
            return AddAction(new AIActionDefinition
            {
                Type = AIActionType.Ability,
                Feat = feat,
                DebugName = feat.ToString()
            });
        }

        public AIProfileBuilder AttackHighestEnmity()
        {
            return AddAction(new AIActionDefinition
            {
                Type = AIActionType.AttackHighestEnmity,
                TargetSelector = AITarget.HighestEnmity(),
                DebugName = nameof(AttackHighestEnmity)
            });
        }

        public AIProfileBuilder MoveToTarget(float desiredRange = 1.5f)
        {
            return AddAction(new AIActionDefinition
            {
                Type = AIActionType.MoveToTarget,
                TargetSelector = AITarget.HighestEnmity(),
                FloatValue = desiredRange,
                DebugName = nameof(MoveToTarget)
            });
        }

        public AIProfileBuilder Flee(float distance = 10f)
        {
            return AddAction(new AIActionDefinition
            {
                Type = AIActionType.Flee,
                TargetSelector = AITarget.HighestEnmity(),
                FloatValue = distance,
                DebugName = nameof(Flee)
            });
        }

        public AIProfileBuilder ReturnHome()
        {
            return AddAction(new AIActionDefinition
            {
                Type = AIActionType.ReturnHome,
                TargetSelector = AITarget.Self(),
                DebugName = nameof(ReturnHome)
            });
        }

        public AIProfileBuilder RandomWalk()
        {
            return AddAction(new AIActionDefinition
            {
                Type = AIActionType.RandomWalk,
                TargetSelector = AITarget.Self(),
                DebugName = nameof(RandomWalk)
            });
        }

        public AIProfileBuilder Wait(float seconds)
        {
            return AddAction(new AIActionDefinition
            {
                Type = AIActionType.Wait,
                TargetSelector = AITarget.Self(),
                FloatValue = seconds,
                DebugName = nameof(Wait)
            });
        }

        public AIProfileBuilder Speak(string text)
        {
            return AddAction(new AIActionDefinition
            {
                Type = AIActionType.Speak,
                TargetSelector = AITarget.Self(),
                Text = text,
                DebugName = nameof(Speak)
            });
        }

        public AIProfileBuilder Script(string scriptName)
        {
            return AddAction(new AIActionDefinition
            {
                Type = AIActionType.Script,
                TargetSelector = AITarget.Self(),
                ScriptName = scriptName,
                DebugName = scriptName
            });
        }

        public AIProfileBuilder CallAllies(float radius = 5f)
        {
            return AddAction(new AIActionDefinition
            {
                Type = AIActionType.CallAllies,
                TargetSelector = AITarget.HighestEnmity(),
                FloatValue = radius,
                DebugName = nameof(CallAllies)
            });
        }

        public AIProfileBuilder Target(AITargetSelector selector)
        {
            _activeAction.TargetSelector = selector;
            return this;
        }

        public AIProfileBuilder When(AIGuard guard)
        {
            _activeAction.Guards.Add(guard);
            return this;
        }

        public AIProfileBuilder Score(int score)
        {
            _activeAction.Score = AIScore.Fixed(score);
            return this;
        }

        public AIProfileBuilder Score(AIScoreCalculation score)
        {
            _activeAction.Score = score;
            return this;
        }

        public AIProfileBuilder Priority(int priority)
        {
            _activeAction.Priority = priority;
            return this;
        }

        public AIProfileBuilder Cooldown(string cooldownId, float seconds)
        {
            _activeAction.CooldownId = cooldownId;
            _activeAction.CooldownSeconds = seconds;
            return this;
        }

        public AIProfileBuilder OncePerPhase()
        {
            _activeAction.OncePerPhase = true;
            return this;
        }

        public Dictionary<AIProfileType, AIProfile> Build()
        {
            return _profiles;
        }

        private AIProfileBuilder AddAction(AIActionDefinition action)
        {
            _activeAction = action;

            if (_activePhase != null)
            {
                _activePhase.Actions.Add(action);
            }
            else
            {
                _activeProfile.Actions.Add(action);
            }

            return this;
        }
    }
}
