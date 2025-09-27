using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Perk.Contracts;
using SWLOR.Component.Perk.Model;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Beasts.Contracts;
using SWLOR.Shared.Domain.Beasts.Enums;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Quest.Contracts;

namespace SWLOR.Component.Perk.Service
{
    /// <summary>
    /// Factory for creating PerkRequirement instances with proper dependency injection.
    /// </summary>
    public class PerkRequirementFactory : IPerkRequirementFactory
    {
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;

        public PerkRequirementFactory(
            IDatabaseService db,
            IServiceProvider serviceProvider)
        {
            _db = db;
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IQuestService QuestService => _serviceProvider.GetRequiredService<IQuestService>();
        private IPerkService PerkService => _serviceProvider.GetRequiredService<IPerkService>();
        private IBeastMasteryService BeastMasteryService => _serviceProvider.GetRequiredService<IBeastMasteryService>();

        public IPerkRequirement CreateSkillRequirement(SkillType skill, int requiredRank)
        {
            return new PerkRequirementSkill(skill, requiredRank, _db, _serviceProvider);
        }

        public IPerkRequirement CreateQuestRequirement(string questId)
        {
            return new PerkRequirementQuest(_db, _serviceProvider, questId);
        }

        public IPerkRequirement CreateCharacterTypeRequirement(CharacterType characterType)
        {
            return new PerkRequirementCharacterType(_db, _serviceProvider, characterType);
        }

        public IPerkRequirement CreateUnlockRequirement(PerkType perkType)
        {
            return new PerkRequirementUnlock(_db, perkType);
        }

        public IPerkRequirement CreateMustHavePerkRequirement(PerkType mustHavePerkType, int mustHavePerkLevel = 0)
        {
            return new PerkRequirementMustHavePerk(_db, _serviceProvider, mustHavePerkType, mustHavePerkLevel);
        }

        public IPerkRequirement CreateCannotHavePerkRequirement(PerkType cannotHavePerkType)
        {
            return new PerkRequirementCannotHavePerk(_db, _serviceProvider, cannotHavePerkType);
        }

        public IPerkRequirement CreateBeastLevelRequirement(int level)
        {
            return new PerkRequirementBeastLevel(_db, level);
        }

        public IPerkRequirement CreateBeastRoleRequirement(BeastRoleType role)
        {
            return new PerkRequirementBeastRole(_db, role, _serviceProvider);
        }
    }
}
