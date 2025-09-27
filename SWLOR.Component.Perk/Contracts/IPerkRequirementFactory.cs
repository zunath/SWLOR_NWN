using SWLOR.Shared.Domain.Associate.Enums;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Perk.Contracts
{
    /// <summary>
    /// Factory for creating PerkRequirement instances with proper dependency injection.
    /// </summary>
    public interface IPerkRequirementFactory
    {
        /// <summary>
        /// Creates a skill requirement.
        /// </summary>
        IPerkRequirement CreateSkillRequirement(SkillType skill, int requiredRank);

        /// <summary>
        /// Creates a quest requirement.
        /// </summary>
        IPerkRequirement CreateQuestRequirement(string questId);

        /// <summary>
        /// Creates a character type requirement.
        /// </summary>
        IPerkRequirement CreateCharacterTypeRequirement(CharacterType characterType);

        /// <summary>
        /// Creates an unlock requirement.
        /// </summary>
        IPerkRequirement CreateUnlockRequirement(PerkType perkType);

        /// <summary>
        /// Creates a must-have-perk requirement.
        /// </summary>
        IPerkRequirement CreateMustHavePerkRequirement(PerkType mustHavePerkType, int mustHavePerkLevel = 0);

        /// <summary>
        /// Creates a cannot-have-perk requirement.
        /// </summary>
        IPerkRequirement CreateCannotHavePerkRequirement(PerkType cannotHavePerkType);

        /// <summary>
        /// Creates a beast level requirement.
        /// </summary>
        IPerkRequirement CreateBeastLevelRequirement(int level);

        /// <summary>
        /// Creates a beast role requirement.
        /// </summary>
        IPerkRequirement CreateBeastRoleRequirement(BeastRoleType role);
    }
}
