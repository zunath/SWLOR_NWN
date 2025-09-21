using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Data.Entity;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Game.Server.Service.PerkService
{
    /// <summary>
    /// Adds a minimum skill level as a requirement to purchase or activate a perk.
    /// </summary>
    public class PerkRequirementSkill : IPerkRequirement
    {
        private readonly IDatabaseService _db;
        private readonly ISkillService _skillService;
        public SkillType Type { get; }
        public int RequiredRank { get; }

        public PerkRequirementSkill(SkillType type, int requiredRank, IDatabaseService db, ISkillService skillService)
        {
            Type = type;
            RequiredRank = requiredRank;
            _db = db;
            _skillService = skillService;
        }

        public string CheckRequirements(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
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
                var skillDetails = _skillService.GetSkillDetails(Type);
                return $"{skillDetails.Name} rank {RequiredRank}";
            }
        }
    }

}
