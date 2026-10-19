using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.PerkService
{
    /// <summary>
    /// Adds a minimum skill level as a requirement to purchase or activate a perk.
    /// </summary>
    public class PerkRequirementSkill : IPerkRequirement
    {
        public SkillType Type { get; }
        public int RequiredRank { get; }

        public PerkRequirementSkill(SkillType type, int requiredRank)
        {
            Type = type;
            RequiredRank = requiredRank;
        }

        public PerkRequirementCategory Category => PerkRequirementCategory.Skill;

        public string CheckRequirements(uint player) => CheckRequirements(DB.Get<Player>(GetObjectUUID(player)), null);

        public string CheckRequirements(Player dbPlayer, Beast activeBeast)
        {
            var skill = dbPlayer.Skills[Type];
            var rank = skill.Rank;

            if (rank >= RequiredRank)
                return string.Empty;

            return $"Your skill rank is too low. (Your rank is {rank} versus required rank {RequiredRank})";
        }

        public string RequirementText
        {
            get
            {
                var skillDetails = Skill.GetSkillDetails(Type);
                return $"{skillDetails.Name} rank {RequiredRank}";
            }
        }
    }

}
