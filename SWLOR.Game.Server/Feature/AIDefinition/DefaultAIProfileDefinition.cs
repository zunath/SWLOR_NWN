using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AIDefinition
{
    public sealed class DefaultAIProfileDefinition : IAIProfileDefinition
    {
        private readonly AIProfileBuilder _builder = new();
        private readonly IReadOnlyList<KeyValuePair<FeatType, AbilityDetail>> _registeredAbilities;

        public DefaultAIProfileDefinition()
        {
            _registeredAbilities = Ability.GetAllAbilityDetails()
                .OrderByDescending(x => x.Value.IsHostileAbility && x.Value.IsAreaAbility)
                .ThenByDescending(x => x.Value.RequiresTarget && !x.Value.IsHostileAbility)
                .ThenByDescending(x => !x.Value.RequiresTarget && !x.Value.IsHostileAbility)
                .ThenByDescending(x => x.Value.AbilityLevel)
                .ThenBy(x => x.Key.ToString())
                .ToList();

            if (_registeredAbilities.Count <= 0)
            {
                throw new InvalidOperationException("Ability cache must be loaded before AI profiles are built.");
            }
        }

        public Dictionary<AIProfileType, AIProfile> BuildProfiles()
        {
            Generic();
            DroidCompanion();
            BeastCompanion();

            return _builder.Build();
        }

        private void Generic()
        {
            _builder
                .Create(AIProfileType.Generic)
                .Name("Generic NPC")
                .DecisionThrottle(0.25f)
                .MaxCandidateActions(MaxKnownActionCandidates());

            AddRegisteredAbilities();

            _builder
                .AttackHighestEnmity()
                .Score(AIScoreBand.BasicAttack)
                .Priority(999);
        }

        private void DroidCompanion()
        {
            _builder
                .Create(AIProfileType.DroidCompanion)
                .Name("Droid Companion")
                .DecisionThrottle(0.25f)
                .MaxCandidateActions(MaxKnownActionCandidates());

            AddRegisteredAbilities();

            _builder
                .AttackHighestEnmity()
                .Score(AIScoreBand.BasicAttack)
                .Priority(999);
        }

        private void BeastCompanion()
        {
            _builder
                .Create(AIProfileType.BeastCompanion)
                .Name("Beast Companion")
                .DecisionThrottle(0.25f)
                .MaxCandidateActions(MaxKnownActionCandidates());

            AddRegisteredAbilities();

            _builder
                .AttackHighestEnmity()
                .Score(AIScoreBand.BasicAttack)
                .Priority(999);
        }

        private void AddRegisteredAbilities()
        {
            var priority = 100;

            foreach (var (feat, ability) in _registeredAbilities)
            {
                _builder.Ability(feat);

                if (ability.AITargetSelector != null)
                    _builder.Target(ability.AITargetSelector);

                var action = _builder
                    .Score(ability.AIScore ?? AIScore.Ability(ability))
                    .Priority(priority);

                priority++;
            }
        }

        private int MaxKnownActionCandidates()
        {
            return _registeredAbilities.Count + 8;
        }
    }
}
