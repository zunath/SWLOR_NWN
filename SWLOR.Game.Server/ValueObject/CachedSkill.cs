using System.Collections.Generic;

namespace SWLOR.Game.Server.ValueObject
{
    public class CachedSkill
    {
        public int SkillID { get; }
        public int SkillCategoryID { get; }
        public string Name { get; }
        public int MaxRank { get; }
        public bool IsActive { get; }
        public string Description { get; }
        public int Primary { get; }
        public int Secondary { get; }
        public int Tertiary { get; }
        public bool ContributesToSkillCap { get; }

        public HashSet<CachedSkillXPRequirement> SkillXPRequirements { get; set; }

        public CachedSkill(Data.Entity.Skill skill)
        {
            SkillID = skill.SkillID;
            SkillCategoryID = skill.SkillCategoryID;
            Name = skill.Name;
            MaxRank = skill.MaxRank;
            IsActive = skill.IsActive;
            Description = skill.Description;
            Primary = skill.Primary;
            Secondary = skill.Secondary;
            Tertiary = skill.Tertiary;
            ContributesToSkillCap = skill.ContributesToSkillCap;

            SkillXPRequirements = new HashSet<CachedSkillXPRequirement>();
            foreach (var xpReq in skill.SkillXPRequirements)
            {
                SkillXPRequirements.Add(new CachedSkillXPRequirement(xpReq));
            }
        }
    }
}
