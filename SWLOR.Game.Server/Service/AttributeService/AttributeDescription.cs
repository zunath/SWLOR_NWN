namespace SWLOR.Game.Server.Service.AttributeService
{
    public static class AttributeDescription
    {
        public const string MightSummary =
            "Improves melee weapon damage, maximum STM, STM regeneration, and carrying capacity.";

        public const string PerceptionSummary =
            "Improves melee accuracy, ranged weapon damage, critical hit chance, and detection.";

        public const string VitalitySummary =
            "Improves maximum HP, HP regeneration, Physical Defense, and resistance to critical hits.";

        public const string WillpowerSummary =
            "Improves Force attack, Force Defense, maximum FP, FP regeneration, First Aid, and detection.";

        public const string AgilitySummary =
            "Improves ranged accuracy, Evasion, Stealth, and ship combat effectiveness.";

        public const string SocialSummary =
            "Improves XP gain and Leadership capabilities.";

        public const string MightDetails =
            "Might: " + MightSummary + "\n\n" +
            "Primary Skills: Vibroblade, Heavy Vibroblade, Spear, Twin Blade, Katar, Staff, Smithery, Gathering\n\n" +
            "Other Notes:\n\n" +
            "Improves damage dealt by melee weapons.\n" +
            "Increases maximum STM.\n" +
            "Improves natural STM regeneration.\n" +
            "Improves STM recovery while resting.\n" +
            "Improves harvesting item acquisition.";

        public const string PerceptionDetails =
            "Perception: " + PerceptionSummary + "\n\n" +
            "Primary Skills: Vibroknife, Lightsaber, Saberstaff, Katar, Pistol, Rifle, Fabrication, Devices\n\n" +
            "Other Notes:\n\n" +
            "Improves accuracy of melee weapons.\n" +
            "Improves damage dealt by ranged weapons, including throwing weapons.\n" +
            "Improves physical ability damage for pistols, rifles, throwing, and Devices.\n" +
            "Improves critical hit chance.\n" +
            "Improves detection.";

        public const string VitalityDetails =
            "Vitality: " + VitalitySummary + "\n\n" +
            "Primary Skills: Armor, Smithery, Engineering\n\n" +
            "Other Notes:\n\n" +
            "Increases maximum HP through NWN base mechanics.\n" +
            "Improves Physical Defense.\n" +
            "Reduces incoming physical damage.\n" +
            "Improves natural HP regeneration.\n" +
            "Improves HP recovery while resting.\n" +
            "Reduces enemy critical hit chance against you.";

        public const string WillpowerDetails =
            "Willpower: " + WillpowerSummary + "\n\n" +
            "Primary Skills: Force, Fabrication, Agriculture, First Aid\n\n" +
            "Other Notes:\n\n" +
            "Increases maximum FP.\n" +
            "Improves natural FP regeneration.\n" +
            "Improves FP recovery while resting.\n" +
            "Improves Force Defense.\n" +
            "Improves Force and First Aid ability effectiveness.\n" +
            "Improves effectiveness of ship combat modules.\n" +
            "Improves detection.";

        public const string AgilityDetails =
            "Agility: " + AgilitySummary + "\n\n" +
            "Primary Skills: Vibroknife, Lightsaber, Saberstaff, Katar, Pistol, Rifle, Throwing, Engineering\n\n" +
            "Other Notes:\n\n" +
            "Improves Evasion.\n" +
            "Improves accuracy of ranged and throwing weapons.\n" +
            "Improves Stealth.\n" +
            "Improves effectiveness of ship combat modules.";

        public const string SocialDetails =
            "Social: " + SocialSummary + "\n\n" +
            "Primary Skills: Leadership, Agriculture\n\n" +
            "Other Notes:\n\n" +
            "Improves guild point acquisition.\n" +
            "Improves quest credit rewards.\n" +
            "Improves XP gain.\n" +
            "Reduces XP debt on death.\n" +
            "Reduces ship repair bills.";

        public static string BuildOverview()
        {
            return "Your character is guided by six core attributes: Might, Vitality, Perception, Willpower, Agility, and Social.\n\n" +
                   "Might: " + MightSummary + "\n" +
                   "Vitality: " + VitalitySummary + "\n" +
                   "Perception: " + PerceptionSummary + "\n" +
                   "Willpower: " + WillpowerSummary + "\n" +
                   "Agility: " + AgilitySummary + "\n" +
                   "Social: " + SocialSummary + "\n\n";
        }
    }
}
