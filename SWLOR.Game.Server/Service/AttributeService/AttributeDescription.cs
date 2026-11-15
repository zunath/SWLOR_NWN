namespace SWLOR.Game.Server.Service.AttributeService
{
    public static class AttributeDescription
    {
        public const string MightSummary =
            "Improves melee weapon damage, maximum STM, STM regeneration, and carrying capacity.";

        public const string PerceptionSummary =
            "Improves melee accuracy, ranged weapon damage, ship combat damage, critical hit chance, and detection.";

        public const string VitalitySummary =
            "Improves maximum HP, HP regeneration, physical defense, and resistance to critical hits.";

        public const string WillpowerSummary =
            "Improves Force attack, Force defense, maximum FP, FP regeneration, First Aid, and detection.";

        public const string AgilitySummary =
            "Improves evasion, stealth, ranged weapon accuracy, and ship combat accuracy.";

        public const string SocialSummary =
            "Improves leadership capabilities, companion beast power, and experience gains.";

        public const string MightDetails =
            "Might: " + MightSummary + "\n\n" +
            "Primary Skills: Vibroknife, Vibroblade, Heavy Vibroblade, Spear, Twin Blade, Katar, Unarmed, Staff, Lightsaber, Saberstaff, Gathering\n\n" +
            "Other Notes:\n\n" +
            "Improves damage dealt by melee weapons.\n" +
            "Increases maximum STM.\n" +
            "Improves natural STM regeneration.\n" +
            "Improves STM recovery while resting.\n" +
            "Improves harvesting item acquisition.";

        public const string PerceptionDetails =
            "Perception: " + PerceptionSummary + "\n\n" +
            "Primary Skills:  Devices, Piloting, Vibroknife, Vibroblade, Heavy Vibroblade, Spear, Twin Blade, Katar, Unarmed, Staff, Lightsaber, Saberstaff, "+
            "Pistol, Twin Pistol, Rifle, Throwing, Espionage\n\n" +
            "Other Notes:\n\n" +
            "Improves accuracy of melee weapons.\n" +
            "Improves damage dealt by ranged weapons, including throwing weapons.\n" +
            "Improves damage for pistols, rifles, throwing, and Devices.\n" +
            "Improves damage of ship combat modules.\n" +
            "Improves critical hit chance.\n" +
            "Improves detection.\n" +
            "Improves slicing trace bonus.";

        public const string VitalityDetails =
            "Vitality: " + VitalitySummary + "\n\n" +
            "Primary Skills: none\n\n" +
            "Other Notes:\n\n" +
            "Increases maximum HP.\n" +
            "Improves Physical Defense.\n" +
            "Reduces incoming physical damage.\n" +
            "Improves natural HP regeneration.\n" +
            "Improves HP recovery while resting.\n" +
            "Reduces enemy critical hit chance against you.";

        public const string WillpowerDetails =
            "Willpower: " + WillpowerSummary + "\n\n" +
            "Primary Skills: Force, First Aid, Agriculture\n\n" +
            "Other Notes:\n\n" +
            "Increases maximum FP.\n" +
            "Improves natural FP regeneration.\n" +
            "Improves FP recovery while resting.\n" +
            "Improves Force Defense.\n" +
            "Improves Force and First Aid ability effectiveness.\n" +
            "Improves fishing outcomes.\n" +
            "Improves detection.";

        public const string AgilityDetails =
            "Agility: " + AgilitySummary + "\n\n" +
            "Primary Skills: Pistol, Twin Pistol, Rifle, Throwing, Piloting, Espionage\n\n" +
            "Other Notes:\n\n" +
            "Improves Evasion.\n" +
            "Improves accuracy of ranged and throwing weapons.\n" +
            "Improves accuracy of ship combat modules.\n" +
            "Improves Stealth.";

        public const string SocialDetails =
            "Social: " + SocialSummary + "\n\n" +
            "Primary Skills: Leadership, Beast Mastery\n\n" +
            "Other Notes:\n\n" +
            "Improves guild point acquisition.\n" +
            "Improves quest credit rewards.\n" +
            "Improves XP gain.\n" +
            "Reduces XP debt on death.\n" +
            "Reduces ship repair bills.";

        public static string BuildOverview()
        {
            return "Your character is guided by six core attributes: Might, Vitality, Perception, Willpower, Agility, and Social.\n\n" +
                   "Might: " + MightSummary + "\n\n" +
                   "Vitality: " + VitalitySummary + "\n\n" +
                   "Perception: " + PerceptionSummary + "\n\n" +
                   "Willpower: " + WillpowerSummary + "\n\n" +
                   "Agility: " + AgilitySummary + "\n\n" +
                   "Social: " + SocialSummary + "\n\n";
        }
    }
}
