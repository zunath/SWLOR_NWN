using System;

namespace SWLOR.Game.Server.Enumeration
{
    public enum AchievementType
    {
        [Achievement("Invalid", "Invalid", false)]
        Invalid = 0,
        [Achievement("Kill Enemies I", "Kill 10 enemies.", true)]
        KillEnemies1 = 1,
        [Achievement("Kill Enemies II", "Kill 50 enemies.", true)]
        KillEnemies2 = 2,
        [Achievement("Kill Enemies III", "Kill 500 enemies.", true)]
        KillEnemies3 = 3,
        [Achievement("Kill Enemies IV", "Kill 2,000 enemies.", true)]
        KillEnemies4 = 4,
        [Achievement("Kill Enemies V", "Kill 10,000 enemies.", true)]
        KillEnemies5 = 5,
        [Achievement("Kill Enemies VI", "Kill 100,000 enemies.", true)]
        KillEnemies6 = 6,
        [Achievement("Learn Perks I", "Learn 1 Perk", true)]
        LearnPerks1 = 7,
        [Achievement("Learn Perks II", "Learn 20 Perks", true)]
        LearnPerks2 = 8,
        [Achievement("Learn Perks III", "Learn 50 Perks", true)]
        LearnPerks3 = 9,
        [Achievement("Learn Perks IV", "Learn 100 Perks", true)]
        LearnPerks4 = 10,
        [Achievement("Learn Perks V", "Learn 500 Perks", true)]
        LearnPerks5 = 11,
        [Achievement("Gain Skill Points I", "Gain 1 Skill Point", true)]
        GainSkills1 = 12,
        [Achievement("Gain Skill Points II", "Gain 50 Skill Points", true)]
        GainSkills2 = 13,
        [Achievement("Gain Skill Points III", "Gain 150 Skill Points", true)]
        GainSkills3 = 14,
        [Achievement("Gain Skill Points IV", "Gain 250 Skill Points", true)]
        GainSkills4 = 15,
        [Achievement("Gain Skill Points V", "Gain 500 Skill Points", true)]
        GainSkills5 = 16,
        [Achievement("Gain Skill Points VI", "Gain 1000 Skill Points", true)]
        GainSkills6 = 17,
        [Achievement("Complete Quests I", "Complete 1 Quest", true)]
        CompleteQuests1 = 18,
        [Achievement("Complete Quests II", "Complete 10 Quests", true)]
        CompleteQuests2 = 19,
        [Achievement("Complete Quests III", "Complete 50 Quests", true)]
        CompleteQuests3 = 20,
        [Achievement("Complete Quests IV", "Complete 100 Quests", true)]
        CompleteQuests4 = 21,
        [Achievement("Complete Quests V", "Complete 500 Quests", true)]
        CompleteQuests5 = 22,
        [Achievement("Complete Quests VI", "Complete 1000 Quests", true)]
        CompleteQuests6 = 23,
        [Achievement("Complete Quests VII", "Complete 1500 Quests", true)]
        CompleteQuests7 = 24,
        [Achievement("Complete Quests VIII", "Complete 2000 Quests", true)]
        CompleteQuests8 = 25,
        [Achievement("Complete Quests IX", "Complete 3500 Quests", true)]
        CompleteQuests9 = 26,
        [Achievement("Complete Quests X", "Complete 5000 Quests", true)]
        CompleteQuests10 = 27,
        [Achievement("Craft Items I", "Craft 1 Items", true)]
        CraftItems1 = 28,
        [Achievement("Craft Items II", "Craft 10 Items", true)]
        CraftItems2 = 29,
        [Achievement("Craft Items III", "Craft 50 Items", true)]
        CraftItems3 = 30,
        [Achievement("Craft Items IV", "Craft 100 Items", true)]
        CraftItems4 = 31,
        [Achievement("Craft Items V", "Craft 500 Items", true)]
        CraftItems5 = 32,
        [Achievement("Craft Items VI", "Craft 1000 Items", true)]
        CraftItems6 = 33,
        [Achievement("Craft Items VII", "Craft 1500 Items", true)]
        CraftItems7 = 34,
        [Achievement("Craft Items VIII", "Craft 2000 Items", true)]
        CraftItems8 = 35,
        [Achievement("Craft Items IX", "Craft 3500 Items", true)]
        CraftItems9 = 36,
        [Achievement("Craft Items X", "Craft 5000 Items", true)]
        CraftItems10 = 37,
    }

    public class AchievementAttribute: Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }

        public AchievementAttribute(string name, string description, bool isActive)
        {
            Name = name;
            Description = description;
            IsActive = isActive;
        }
    }
}
