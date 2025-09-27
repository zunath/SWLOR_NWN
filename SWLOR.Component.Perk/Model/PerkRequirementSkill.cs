using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Skill.Contracts;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Perk.Model
{
    /// <summary>
    /// Adds a minimum skill level as a requirement to purchase or activate a perk.
    /// </summary>
    public class PerkRequirementSkill : IPerkRequirement
    {
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        public SkillType Type { get; }
        public int RequiredRank { get; }

        public PerkRequirementSkill(SkillType type, int requiredRank, IDatabaseService db, IServiceProvider serviceProvider)
        {
            Type = type;
            RequiredRank = requiredRank;
            _db = db;
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded service to break circular dependency
        private ISkillService SkillService => _serviceProvider.GetRequiredService<ISkillService>();

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
                var skillDetails = SkillService.GetSkillDetails(Type);
                return $"{skillDetails.Name} rank {RequiredRank}";
            }
        }
    }

}
