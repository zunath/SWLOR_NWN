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

        public string CheckRequirements(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
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
