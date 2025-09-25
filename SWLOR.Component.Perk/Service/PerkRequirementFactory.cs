using SWLOR.Component.Perk.Contracts;
using SWLOR.Component.Perk.Model;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Beasts.Contracts;
using SWLOR.Shared.Domain.Beasts.Enums;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Quest.Contracts;

namespace SWLOR.Component.Perk.Service
{
    /// <summary>
    /// Factory for creating PerkRequirement instances with proper dependency injection.
    /// </summary>
    public class PerkRequirementFactory : IPerkRequirementFactory
    {
        private readonly IDatabaseService _db;
        private readonly ISkillService _skillService;
        private readonly IQuestService _questService;
        private readonly IPerkService _perkService;
        private readonly IBeastMasteryService _beastMasteryService;

        public PerkRequirementFactory(
            IDatabaseService db,
            ISkillService skillService,
            IQuestService questService,
            IPerkService perkService,
            IBeastMasteryService beastMasteryService)
        {
            _db = db;
            _skillService = skillService;
            _questService = questService;
            _perkService = perkService;
            _beastMasteryService = beastMasteryService;
        }

        public IPerkRequirement CreateSkillRequirement(SkillType skill, int requiredRank)
        {
            return new PerkRequirementSkill(skill, requiredRank, _db, _skillService);
        }

        public IPerkRequirement CreateQuestRequirement(string questId)
        {
            return new PerkRequirementQuest(_db, _questService, questId);
        }

        public IPerkRequirement CreateCharacterTypeRequirement(CharacterType characterType)
        {
            return new PerkRequirementCharacterType(_db, _perkService, characterType);
        }

        public IPerkRequirement CreateUnlockRequirement(PerkType perkType)
        {
            return new PerkRequirementUnlock(_db, perkType);
        }

        public IPerkRequirement CreateMustHavePerkRequirement(PerkType mustHavePerkType, int mustHavePerkLevel = 0)
        {
            return new PerkRequirementMustHavePerk(_db, _perkService, mustHavePerkType, mustHavePerkLevel);
        }

        public IPerkRequirement CreateCannotHavePerkRequirement(PerkType cannotHavePerkType)
        {
            return new PerkRequirementCannotHavePerk(_db, _perkService, cannotHavePerkType);
        }

        public IPerkRequirement CreateBeastLevelRequirement(int level)
        {
            return new PerkRequirementBeastLevel(_db, level);
        }

        public IPerkRequirement CreateBeastRoleRequirement(BeastRoleType role)
        {
            return new PerkRequirementBeastRole(_db, role, _beastMasteryService);
        }
    }
}
