using System;
using System.Collections.Generic;
using System.Text;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Perk
{
    public class PerkLevel
    {
        public int Price { get; set; }
        public string Description { get; set; }
        
        public Dictionary<SkillType, int> SkillRequirements { get; set; }
        public List<int> QuestRequirements { get; set; }

        public PerkLevel(int price, string description)
        {
            Price = price;
            Description = description;

            SkillRequirements = new Dictionary<SkillType, int>();
            QuestRequirements = new List<int>();
        }

        public PerkLevel(int price, string description, Dictionary<SkillType, int> skillRequirements)
        {
            Price = price;
            Description = description;

            SkillRequirements = skillRequirements;
            QuestRequirements = new List<int>();
        }

        public PerkLevel(int price, string description, Dictionary<SkillType, int> skillRequirements, List<int> questRequirements)
        {
            Price = price;
            Description = description;

            SkillRequirements = skillRequirements;
            QuestRequirements = questRequirements;
        }


    }
}
