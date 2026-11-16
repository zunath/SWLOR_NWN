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

        /// <summary>
        /// Whether the skill a body frame trains in place of this one also meets the requirement
        /// (an astromech's Chassis for Armor). Off for perks that need the humanoid body itself.
        /// </summary>
        public bool AcceptsFrameEquivalent { get; }

        public PerkRequirementSkill(SkillType type, int requiredRank, bool acceptsFrameEquivalent = true)
        {
            Type = type;
            RequiredRank = requiredRank;
            AcceptsFrameEquivalent = acceptsFrameEquivalent;
        }

        public PerkRequirementCategory Category => PerkRequirementCategory.Skill;

        public string CheckRequirements(uint player) => CheckRequirements(DB.Get<Player>(GetObjectUUID(player)), null);

        public string CheckRequirements(Player dbPlayer, Beast activeBeast)
        {
            var type = AcceptsFrameEquivalent
                ? SkillBodyFrames.GetFrameEquivalent(Type, dbPlayer.BodyFrame)
                : Type;
            if (!SkillBodyFrames.IsOpenTo(type, dbPlayer.BodyFrame))
                return SkillBodyFrames.GetRefusal(type);

            var rank = dbPlayer.Skills.TryGetValue(type, out var skill) ? skill.Rank : 0;

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
