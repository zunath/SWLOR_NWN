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
        public SpecializationType Specialization { get; set; }
        
        public Dictionary<Skill, int> SkillRequirements { get; set; }
        public List<int> QuestRequirements { get; set; }

        public PerkLevel(int price, string description)
        {
            Price = price;
            Description = description;
            Specialization = SpecializationType.None;

            SkillRequirements = new Dictionary<Skill, int>();
            QuestRequirements = new List<int>();
        }

        public PerkLevel(int price, string description, Dictionary<Skill, int> skillRequirements)
        {
            Price = price;
            Description = description;
            Specialization = SpecializationType.None;

            SkillRequirements = skillRequirements;
            QuestRequirements = new List<int>();
        }

        public PerkLevel(int price, string description, Dictionary<Skill, int> skillRequirements, List<int> questRequirements)
        {
            Price = price;
            Description = description;
            Specialization = SpecializationType.None;

            SkillRequirements = skillRequirements;
            QuestRequirements = questRequirements;
        }

        public PerkLevel(int price, string description, SpecializationType specialization, Dictionary<Skill, int> skillRequirements)
        {
            Price = price;
            Description = description;
            Specialization = specialization;

            SkillRequirements = skillRequirements;
            QuestRequirements = new List<int>();
        }

        public PerkLevel(int price, string description, SpecializationType specialization, Dictionary<Skill, int> skillRequirements, List<int> questRequirements)
        {
            Price = price;
            Description = description;
            Specialization = specialization;

            SkillRequirements = skillRequirements;
            QuestRequirements = questRequirements;
        }

    }
}
