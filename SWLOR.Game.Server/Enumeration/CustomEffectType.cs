using System;

namespace SWLOR.Game.Server.Enumeration
{
    public enum CustomEffectType
    {
        [CustomEffect("None", 0, CustomEffectCategoryType.Invalid)]
        None = 0,
        [CustomEffect("Bleeding", 129, CustomEffectCategoryType.NormalEffect)]
        Bleeding = 1,
        [CustomEffect("Burning", 0, CustomEffectCategoryType.NormalEffect)]
        Burning = 2,
        [CustomEffect("Poison", 0, CustomEffectCategoryType.NormalEffect)]
        Poison = 3,
        [CustomEffect("Force Aura", 0, CustomEffectCategoryType.NormalEffect)]
        ForceAura = 4,
        [CustomEffect("Force Breach", 0, CustomEffectCategoryType.NormalEffect)]
        ForceBreach = 5,
        // 6 is open
        [CustomEffect("Force Shock", 0, CustomEffectCategoryType.NormalEffect)]
        ForceShock = 7,
        [CustomEffect("Absorption Field", 0, CustomEffectCategoryType.NormalEffect)]
        AbsorptionField = 8,
        [CustomEffect("Force Spread", 0, CustomEffectCategoryType.NormalEffect)]
        ForceSpread = 9,
        // 10 is open
        [CustomEffect("Shield Boost", 0, CustomEffectCategoryType.NormalEffect)]
        ShieldBoost = 11,
        [CustomEffect("Meditate", 0, CustomEffectCategoryType.NormalEffect)]
        Meditate = 12,
        [CustomEffect("Rest", 0, CustomEffectCategoryType.NormalEffect)]
        Rest = 13,
        [CustomEffect("Fire Cell", 0, CustomEffectCategoryType.NormalEffect)]
        FireCell = 14,
        [CustomEffect("Balanced Stance", 0, CustomEffectCategoryType.Stance)]
        BalancedStance = 15,
        [CustomEffect("Electric Cell", 0, CustomEffectCategoryType.NormalEffect)]
        ElectricCell = 16,
        [CustomEffect("Sonic Cell", 0, CustomEffectCategoryType.NormalEffect)]
        SonicCell = 17,
        [CustomEffect("Acid Cell", 0, CustomEffectCategoryType.NormalEffect)]
        AcidCell = 18,
        [CustomEffect("Ice Cell", 0, CustomEffectCategoryType.NormalEffect)]
        IceCell = 19,
        [CustomEffect("Divine Cell", 0, CustomEffectCategoryType.NormalEffect)]
        DivineCell = 20,
        // 21 is open
        [CustomEffect("Shield Oath", 0, CustomEffectCategoryType.Stance)]
        ShieldOath = 22,
        [CustomEffect("Precision Targeting", 0, CustomEffectCategoryType.Stance)]
        PrecisionTargeting = 23,
        [CustomEffect("Force Pack", 0, CustomEffectCategoryType.NormalEffect)]
        ForcePack = 24
    }


    public class CustomEffectAttribute : Attribute
    {
        public string Name { get; set; }
        public int IconID { get; set; }
        public CustomEffectCategoryType Category { get; set; }

        public CustomEffectAttribute(string name, int iconID, CustomEffectCategoryType category)
        {
            Name = name;
            IconID = iconID;
            Category = category;
        }
    }
}
