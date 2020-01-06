using System;

namespace SWLOR.Game.Server.Enumeration
{
    public enum CraftBlueprint
    {
        [CraftBlueprint(CraftBlueprintCategory.Unknown, "Invalid", false, 99, "", 0, Skill.Unknown, CraftDeviceType.Invalid, PerkType.None, 0, ComponentType.None, 0, 0, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        Invalid = 0,
        [CraftBlueprint(CraftBlueprintCategory.HeavyVibroblades, "Basic Heavy Vibroblade GA", true, 5, " greataxe_b", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.LargeBlade, 1, 2, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        BasicHeavyVibrobladeGA = 1,
        [CraftBlueprint(CraftBlueprintCategory.HeavyVibroblades, "Heavy Vibroblade GA1", true, 7, " greataxe_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 1, ComponentType.LargeBlade, 1, 2, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        HeavyVibrobladeGA1 = 2,
        [CraftBlueprint(CraftBlueprintCategory.HeavyVibroblades, "Heavy Vibroblade GA2", true, 12, " greataxe_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 3, ComponentType.LargeBlade, 1, 2, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        HeavyVibrobladeGA2 = 3,
        [CraftBlueprint(CraftBlueprintCategory.HeavyVibroblades, "Heavy Vibroblade GA3", true, 17, " greataxe_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 5, ComponentType.LargeBlade, 1, 2, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        HeavyVibrobladeGA3 = 4,
        [CraftBlueprint(CraftBlueprintCategory.HeavyVibroblades, "Heavy Vibroblade GA4", true, 22, " greataxe_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 7, ComponentType.LargeBlade, 1, 2, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 6, BaseStructure.Invalid)]
        HeavyVibrobladeGA4 = 5,
        [CraftBlueprint(CraftBlueprintCategory.Vibroblades, "Basic Vibroblade BA", true, 6, " battleaxe_b", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicVibrobladeBA = 6,
        [CraftBlueprint(CraftBlueprintCategory.Vibroblades, "Vibroblade BA1", true, 8, " battleaxe_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 1, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        VibrobladeBA1 = 7,
        [CraftBlueprint(CraftBlueprintCategory.Vibroblades, "Vibroblade BA2", true, 13, " battleaxe_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 3, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        VibrobladeBA2 = 8,
        [CraftBlueprint(CraftBlueprintCategory.Vibroblades, "Vibroblade BA3", true, 18, " battleaxe_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 5, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        VibrobladeBA3 = 9,
        [CraftBlueprint(CraftBlueprintCategory.Vibroblades, "Vibroblade BA4", true, 23, " battleaxe_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 7, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        VibrobladeBA4 = 10,
        [CraftBlueprint(CraftBlueprintCategory.Vibroblades, "Basic Vibroblade BS", true, 5, " bst_sword_b", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicVibrobladeBS = 11,
        [CraftBlueprint(CraftBlueprintCategory.Vibroblades, "Vibroblade BS1", true, 7, " bst_sword_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 1, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        VibrobladeBS1 = 12,
        [CraftBlueprint(CraftBlueprintCategory.Vibroblades, "Vibroblade BS2", true, 12, " bst_sword_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 3, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        VibrobladeBS2 = 13,
        [CraftBlueprint(CraftBlueprintCategory.Vibroblades, "Vibroblade BS3", true, 17, " bst_sword_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 5, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        VibrobladeBS3 = 14,
        [CraftBlueprint(CraftBlueprintCategory.Vibroblades, "Vibroblade BS4", true, 22, " bst_sword_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 7, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        VibrobladeBS4 = 15,
        [CraftBlueprint(CraftBlueprintCategory.FinesseVibroblades, "Basic Finesse Vibroblade D", true, 0, " dagger_b", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.SmallBlade, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicFinesseVibrobladeD = 16,
        [CraftBlueprint(CraftBlueprintCategory.FinesseVibroblades, "Finesse Vibroblade D1", true, 5, " dagger_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 1, ComponentType.SmallBlade, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        FinesseVibrobladeD1 = 17,
        [CraftBlueprint(CraftBlueprintCategory.FinesseVibroblades, "Finesse Vibroblade D2", true, 10, " dagger_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 3, ComponentType.SmallBlade, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        FinesseVibrobladeD2 = 18,
        [CraftBlueprint(CraftBlueprintCategory.FinesseVibroblades, "Finesse Vibroblade D3", true, 15, " dagger_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 5, ComponentType.SmallBlade, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        FinesseVibrobladeD3 = 19,
        [CraftBlueprint(CraftBlueprintCategory.FinesseVibroblades, "Finesse Vibroblade D4", true, 20, " dagger_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 7, ComponentType.SmallBlade, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        FinesseVibrobladeD4 = 20,
        [CraftBlueprint(CraftBlueprintCategory.HeavyVibroblades, "Basic Heavy Vibroblade GS", true, 7, " greatsword_b", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.LargeBlade, 1, 2, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        BasicHeavyVibrobladeGS = 21,
        [CraftBlueprint(CraftBlueprintCategory.HeavyVibroblades, "Heavy Vibroblade GS1", true, 8, " greatsword_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 1, ComponentType.LargeBlade, 1, 2, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        HeavyVibrobladeGS1 = 22,
        [CraftBlueprint(CraftBlueprintCategory.HeavyVibroblades, "Heavy Vibroblade GS2", true, 13, " greatsword_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 3, ComponentType.LargeBlade, 1, 2, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        HeavyVibrobladeGS2 = 23,
        [CraftBlueprint(CraftBlueprintCategory.HeavyVibroblades, "Heavy Vibroblade GS3", true, 18, " greatsword_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 5, ComponentType.LargeBlade, 1, 2, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        HeavyVibrobladeGS3 = 24,
        [CraftBlueprint(CraftBlueprintCategory.HeavyVibroblades, "Heavy Vibroblade GS4", true, 23, " greatsword_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 7, ComponentType.LargeBlade, 1, 2, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 6, BaseStructure.Invalid)]
        HeavyVibrobladeGS4 = 25,
        [CraftBlueprint(CraftBlueprintCategory.Vibroblades, "Basic Vibroblade LS", true, 2, " longsword_b", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicVibrobladeLS = 26,
        [CraftBlueprint(CraftBlueprintCategory.Vibroblades, "Vibroblade LS1", true, 6, " longsword_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 1, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        VibrobladeLS1 = 27,
        [CraftBlueprint(CraftBlueprintCategory.Vibroblades, "Vibroblade LS2", true, 11, " longsword_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 3, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        VibrobladeLS2 = 28,
        [CraftBlueprint(CraftBlueprintCategory.Vibroblades, "Vibroblade LS3", true, 16, " longsword_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 5, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        VibrobladeLS3 = 29,
        [CraftBlueprint(CraftBlueprintCategory.Vibroblades, "Vibroblade LS4", true, 21, " longsword_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 7, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        VibrobladeLS4 = 30,
        [CraftBlueprint(CraftBlueprintCategory.FinesseVibroblades, "Basic Finesse Vibroblade R", true, 6, " rapier_b", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicFinesseVibrobladeR = 31,
        [CraftBlueprint(CraftBlueprintCategory.FinesseVibroblades, "Finesse Vibroblade R1", true, 8, " rapier_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 1, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        FinesseVibrobladeR1 = 32,
        [CraftBlueprint(CraftBlueprintCategory.FinesseVibroblades, "Finesse Vibroblade R2", true, 13, " rapier_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 3, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        FinesseVibrobladeR2 = 33,
        [CraftBlueprint(CraftBlueprintCategory.FinesseVibroblades, "Finesse Vibroblade R3", true, 18, " rapier_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 5, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        FinesseVibrobladeR3 = 34,
        [CraftBlueprint(CraftBlueprintCategory.FinesseVibroblades, "Finesse Vibroblade R4", true, 23, " rapier_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 7, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        FinesseVibrobladeR4 = 35,
        [CraftBlueprint(CraftBlueprintCategory.Vibroblades, "Basic Vibroblade K", true, 4, " katana_b", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicVibrobladeK = 36,
        [CraftBlueprint(CraftBlueprintCategory.Vibroblades, "Vibroblade K1", true, 7, " katana_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 1, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        VibrobladeK1 = 37,
        [CraftBlueprint(CraftBlueprintCategory.Vibroblades, "Vibroblade K2", true, 12, " katana_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 3, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        VibrobladeK2 = 38,
        [CraftBlueprint(CraftBlueprintCategory.Vibroblades, "Vibroblade K3", true, 17, " katana_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 5, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        VibrobladeK3 = 39,
        [CraftBlueprint(CraftBlueprintCategory.Vibroblades, "Vibroblade K4", true, 22, " katana_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 7, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        VibrobladeK4 = 40,
        [CraftBlueprint(CraftBlueprintCategory.FinesseVibroblades, "Basic Finesse Vibroblade SS", true, 4, " shortsword_b", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.SmallBlade, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicFinesseVibrobladeSS = 41,
        [CraftBlueprint(CraftBlueprintCategory.FinesseVibroblades, "Finesse Vibroblade SS1", true, 7, " shortsword_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 1, ComponentType.SmallBlade, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        FinesseVibrobladeSS1 = 42,
        [CraftBlueprint(CraftBlueprintCategory.FinesseVibroblades, "Finesse Vibroblade SS2", true, 12, " shortsword_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 3, ComponentType.SmallBlade, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        FinesseVibrobladeSS2 = 43,
        [CraftBlueprint(CraftBlueprintCategory.FinesseVibroblades, "Finesse Vibroblade SS3", true, 17, " shortsword_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 5, ComponentType.SmallBlade, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        FinesseVibrobladeSS3 = 44,
        [CraftBlueprint(CraftBlueprintCategory.FinesseVibroblades, "Finesse Vibroblade SS4", true, 22, " shortsword_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 7, ComponentType.SmallBlade, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        FinesseVibrobladeSS4 = 45,
        [CraftBlueprint(CraftBlueprintCategory.Batons, "Basic Baton C", true, 1, " club_b", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.WoodBatonFrame, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicBatonC = 46,
        [CraftBlueprint(CraftBlueprintCategory.Batons, "Baton C1", true, 4, " club_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 1, ComponentType.WoodBatonFrame, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        BatonC1 = 47,
        [CraftBlueprint(CraftBlueprintCategory.Batons, "Baton C2", true, 9, " club_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 3, ComponentType.WoodBatonFrame, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        BatonC2 = 48,
        [CraftBlueprint(CraftBlueprintCategory.Batons, "Baton C3", true, 14, " club_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 5, ComponentType.WoodBatonFrame, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        BatonC3 = 49,
        [CraftBlueprint(CraftBlueprintCategory.Batons, "Baton C4", true, 19, " club_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 7, ComponentType.WoodBatonFrame, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        BatonC4 = 50,
        [CraftBlueprint(CraftBlueprintCategory.Batons, "Basic Baton M", true, 1, " mace_b", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.MetalBatonFrame, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicBatonM = 51,
        [CraftBlueprint(CraftBlueprintCategory.Batons, "Baton M1", true, 5, " mace_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 1, ComponentType.MetalBatonFrame, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        BatonM1 = 52,
        [CraftBlueprint(CraftBlueprintCategory.Batons, "Baton M2", true, 10, " mace_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 3, ComponentType.MetalBatonFrame, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        BatonM2 = 53,
        [CraftBlueprint(CraftBlueprintCategory.Batons, "Baton M3", true, 15, " mace_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 5, ComponentType.MetalBatonFrame, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        BatonM3 = 54,
        [CraftBlueprint(CraftBlueprintCategory.Batons, "Baton M4", true, 20, " mace_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 7, ComponentType.MetalBatonFrame, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        BatonM4 = 55,
        [CraftBlueprint(CraftBlueprintCategory.Batons, "Basic Baton MS", true, 3, " morningstar_b", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.MetalBatonFrame, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicBatonMS = 56,
        [CraftBlueprint(CraftBlueprintCategory.Batons, "Baton MS1", true, 6, " morningstar_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 1, ComponentType.MetalBatonFrame, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        BatonMS1 = 57,
        [CraftBlueprint(CraftBlueprintCategory.Batons, "Baton MS2", true, 11, " morningstar_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 3, ComponentType.MetalBatonFrame, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        BatonMS2 = 58,
        [CraftBlueprint(CraftBlueprintCategory.Batons, "Baton MS3", true, 16, " morningstar_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 5, ComponentType.MetalBatonFrame, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        BatonMS3 = 59,
        [CraftBlueprint(CraftBlueprintCategory.Batons, "Baton MS4", true, 21, " morningstar_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 7, ComponentType.MetalBatonFrame, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        BatonMS4 = 60,
        [CraftBlueprint(CraftBlueprintCategory.MartialArts, "Basic Quarterstaff", true, 0, " quarterstaff_b", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.Shaft, 1, 2, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        BasicQuarterstaff = 61,
        [CraftBlueprint(CraftBlueprintCategory.MartialArts, "Quarterstaff I", true, 5, " quarterstaff_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 1, ComponentType.Shaft, 1, 2, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        QuarterstaffI = 62,
        [CraftBlueprint(CraftBlueprintCategory.MartialArts, "Quarterstaff II", true, 10, " quarterstaff_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 3, ComponentType.Shaft, 1, 2, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        QuarterstaffII = 63,
        [CraftBlueprint(CraftBlueprintCategory.MartialArts, "Quarterstaff III", true, 15, " quarterstaff_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 5, ComponentType.Shaft, 1, 2, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        QuarterstaffIII = 64,
        [CraftBlueprint(CraftBlueprintCategory.MartialArts, "Quarterstaff IV", true, 20, " quarterstaff_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 7, ComponentType.Shaft, 1, 2, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 6, BaseStructure.Invalid)]
        QuarterstaffIV = 65,
        [CraftBlueprint(CraftBlueprintCategory.TwinVibroblades, "Basic Twin Vibroblade DA", true, 7, " doubleaxe_b", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.LargeBlade, 2, 4, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        BasicTwinVibrobladeDA = 66,
        [CraftBlueprint(CraftBlueprintCategory.TwinVibroblades, "Twin Vibroblade DA1", true, 8, " doubleaxe_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 1, ComponentType.LargeBlade, 2, 4, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        TwinVibrobladeDA1 = 67,
        [CraftBlueprint(CraftBlueprintCategory.TwinVibroblades, "Twin Vibroblade DA2", true, 13, " doubleaxe_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 3, ComponentType.LargeBlade, 2, 4, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        TwinVibrobladeDA2 = 68,
        [CraftBlueprint(CraftBlueprintCategory.TwinVibroblades, "Twin Vibroblade DA3", true, 18, " doubleaxe_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 5, ComponentType.LargeBlade, 2, 4, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        TwinVibrobladeDA3 = 69,
        [CraftBlueprint(CraftBlueprintCategory.TwinVibroblades, "Twin Vibroblade DA4", true, 23, " doubleaxe_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 7, ComponentType.LargeBlade, 2, 4, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 6, BaseStructure.Invalid)]
        TwinVibrobladeDA4 = 70,
        [CraftBlueprint(CraftBlueprintCategory.TwinVibroblades, "Basic Twin Vibroblade TS", true, 5, " twinblade_b", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.LargeBlade, 2, 4, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        BasicTwinVibrobladeTS = 71,
        [CraftBlueprint(CraftBlueprintCategory.TwinVibroblades, "Twin Vibroblade TS1", true, 7, " twinblade_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 1, ComponentType.LargeBlade, 2, 4, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        TwinVibrobladeTS1 = 72,
        [CraftBlueprint(CraftBlueprintCategory.TwinVibroblades, "Twin Vibroblade TS2", true, 12, " twinblade_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 3, ComponentType.LargeBlade, 2, 4, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        TwinVibrobladeTS2 = 73,
        [CraftBlueprint(CraftBlueprintCategory.TwinVibroblades, "Twin Vibroblade TS3", true, 17, " twinblade_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 5, ComponentType.LargeBlade, 2, 4, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        TwinVibrobladeTS3 = 74,
        [CraftBlueprint(CraftBlueprintCategory.TwinVibroblades, "Twin Vibroblade TS4", true, 22, " twinblade_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 7, ComponentType.LargeBlade, 2, 4, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 6, BaseStructure.Invalid)]
        TwinVibrobladeTS4 = 75,
        [CraftBlueprint(CraftBlueprintCategory.FinesseVibroblades, "Basic Finesse Vibroblade K", true, 2, " kukri_b", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicFinesseVibrobladeK = 76,
        [CraftBlueprint(CraftBlueprintCategory.FinesseVibroblades, "Finesse Vibroblade K1", true, 6, " kukri_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 1, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        FinesseVibrobladeK1 = 77,
        [CraftBlueprint(CraftBlueprintCategory.FinesseVibroblades, "Finesse Vibroblade K2", true, 11, " kukri_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 3, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        FinesseVibrobladeK2 = 78,
        [CraftBlueprint(CraftBlueprintCategory.FinesseVibroblades, "Finesse Vibroblade K3", true, 16, " kukri_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 5, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        FinesseVibrobladeK3 = 79,
        [CraftBlueprint(CraftBlueprintCategory.FinesseVibroblades, "Finesse Vibroblade K4", true, 21, " kukri_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 7, ComponentType.MediumBlade, 1, 2, ComponentType.MediumHandle, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        FinesseVibrobladeK4 = 80,
        [CraftBlueprint(CraftBlueprintCategory.Polearms, "Basic Polearm H", true, 7, " halberd_b", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.LargeBlade, 1, 2, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        BasicPolearmH = 81,
        [CraftBlueprint(CraftBlueprintCategory.Polearms, "Polearm H1", true, 8, " halberd_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 1, ComponentType.LargeBlade, 1, 2, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        PolearmH1 = 82,
        [CraftBlueprint(CraftBlueprintCategory.Polearms, "Polearm H2", true, 13, " halberd_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 3, ComponentType.LargeBlade, 1, 2, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        PolearmH2 = 83,
        [CraftBlueprint(CraftBlueprintCategory.Polearms, "Polearm H3", true, 18, " halberd_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 5, ComponentType.LargeBlade, 1, 2, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        PolearmH3 = 84,
        [CraftBlueprint(CraftBlueprintCategory.Polearms, "Polearm H4", true, 23, " halberd_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 7, ComponentType.LargeBlade, 1, 2, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 6, BaseStructure.Invalid)]
        PolearmH4 = 85,
        [CraftBlueprint(CraftBlueprintCategory.Polearms, "Basic Polearm S", true, 5, " spear_b", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.LargeBlade, 1, 2, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        BasicPolearmS = 86,
        [CraftBlueprint(CraftBlueprintCategory.Polearms, "Polearm S1", true, 7, " spear_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 1, ComponentType.LargeBlade, 1, 2, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        PolearmS1 = 87,
        [CraftBlueprint(CraftBlueprintCategory.Polearms, "Polearm S2", true, 12, " spear_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 3, ComponentType.LargeBlade, 1, 2, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        PolearmS2 = 88,
        [CraftBlueprint(CraftBlueprintCategory.Polearms, "Polearm S3", true, 17, " spear_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 5, ComponentType.LargeBlade, 1, 2, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        PolearmS3 = 89,
        [CraftBlueprint(CraftBlueprintCategory.Polearms, "Polearm S4", true, 22, " spear_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 7, ComponentType.LargeBlade, 1, 2, ComponentType.LargeHandle, 1, 2, ComponentType.None, 0, 0, 6, BaseStructure.Invalid)]
        PolearmS4 = 90,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Small Blade", true, 1, " small_blade", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.Metal, 1, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        SmallBlade = 91,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Medium Blade", true, 2, " medium_blade", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.Metal, 1, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        MediumBlade = 92,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Large Blade", true, 3, " large_blade", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.Metal, 1, 6, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        LargeBlade = 93,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Wood Baton Frame", true, 0, " w_baton_frame", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        WoodBatonFrame = 94,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Metal Baton Frame", true, 2, " m_baton_frame", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        MetalBatonFrame = 95,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Shaft", true, 2, " shaft", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.Organic, 3, 6, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        Shaft = 96,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Small Handle", true, 1, " small_handle", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.Organic, 2, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        SmallHandle = 97,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Medium Handle", true, 1, " medium_handle", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.Organic, 3, 3, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        MediumHandle = 98,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Large Handle", true, 3, " large_handle", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.Organic, 4, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        LargeHandle = 99,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Ranged Weapon Core", true, 3, " r_weapon_core", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.None, 0, ComponentType.Metal, 2, 4, ComponentType.Electronics, 1, 2, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        RangedWeaponCore = 100,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Rifle Barrel", true, 2, " rifle_barrel", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.None, 0, ComponentType.Metal, 3, 3, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        RifleBarrel = 101,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Pistol Barrel", true, 1, " pistol_barrel", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.None, 0, ComponentType.Metal, 2, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        PistolBarrel = 102,
        [CraftBlueprint(CraftBlueprintCategory.BlasterRifles, "Basic Blaster Rifle", true, 3, " rifle_b", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.None, 0, ComponentType.RangedWeaponCore, 1, 2, ComponentType.RifleBarrel, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        BasicBlasterRifle = 103,
        [CraftBlueprint(CraftBlueprintCategory.BlasterRifles, "Blaster Rifle I", true, 6, " rifle_1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.RangedWeaponCore, 1, 2, ComponentType.RifleBarrel, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        BlasterRifleI = 104,
        [CraftBlueprint(CraftBlueprintCategory.BlasterRifles, "Blaster Rifle II", true, 11, " rifle_2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 3, ComponentType.RangedWeaponCore, 1, 2, ComponentType.RifleBarrel, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        BlasterRifleII = 105,
        [CraftBlueprint(CraftBlueprintCategory.BlasterRifles, "Blaster Rifle III", true, 16, " rifle_3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.RangedWeaponCore, 1, 2, ComponentType.RifleBarrel, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        BlasterRifleIII = 106,
        [CraftBlueprint(CraftBlueprintCategory.BlasterRifles, "Blaster Rifle IV", true, 21, " rifle_4", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.RangedWeaponCore, 1, 2, ComponentType.RifleBarrel, 1, 2, ComponentType.None, 0, 0, 6, BaseStructure.Invalid)]
        BlasterRifleIV = 107,
        [CraftBlueprint(CraftBlueprintCategory.BlasterPistols, "Basic Blaster Pistol", true, 1, " blaster_b", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.None, 0, ComponentType.RangedWeaponCore, 1, 2, ComponentType.PistolBarrel, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        BasicBlasterPistol = 108,
        [CraftBlueprint(CraftBlueprintCategory.BlasterPistols, "Blaster Pistol I", true, 5, " blaster_1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.RangedWeaponCore, 1, 2, ComponentType.PistolBarrel, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        BlasterPistolI = 109,
        [CraftBlueprint(CraftBlueprintCategory.BlasterPistols, "Blaster Pistol II", true, 10, " blaster_2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 3, ComponentType.RangedWeaponCore, 1, 2, ComponentType.PistolBarrel, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        BlasterPistolII = 110,
        [CraftBlueprint(CraftBlueprintCategory.BlasterPistols, "Blaster Pistol III", true, 15, " blaster_3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.RangedWeaponCore, 1, 2, ComponentType.PistolBarrel, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        BlasterPistolIII = 111,
        [CraftBlueprint(CraftBlueprintCategory.BlasterPistols, "Blaster Pistol IV", true, 20, " blaster_4", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.RangedWeaponCore, 1, 2, ComponentType.PistolBarrel, 1, 2, ComponentType.None, 0, 0, 6, BaseStructure.Invalid)]
        BlasterPistolIV = 112,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Green Crystal Cluster", true, 1, " c_cluster_green", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.None, 0, ComponentType.GreenCrystal, 4, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        GreenCluster = 113,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Blue Crystal Cluster", true, 1, " c_cluster_blue", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.None, 0, ComponentType.BlueCrystal, 4, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        BlueCluster = 114,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Red Crystal Cluster", true, 1, " c_cluster_red", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.None, 0, ComponentType.RedCrystal, 4, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        RedCluster = 115,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Yellow Crystal Cluster", true, 1, " c_cluster_yellow", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.None, 0, ComponentType.YellowCrystal, 4, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        YellowCluster = 116,
        [CraftBlueprint(CraftBlueprintCategory.RedMods, "Attack Bonus +1", true, 9, " rune_ab1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.RedCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        AttackBonusPlus1 = 117,
        [CraftBlueprint(CraftBlueprintCategory.RedMods, "Constitution +1", true, 8, " rune_con1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.RedCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ConstitutionPlus1 = 118,
        [CraftBlueprint(CraftBlueprintCategory.RedMods, "Dexterity +1", true, 9, " rune_dex1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.RedCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        DexterityPlus1 = 119,
        [CraftBlueprint(CraftBlueprintCategory.RedMods, "Strength +1", true, 10, " rune_str1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.RedCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        StrengthPlus1 = 120,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Cooldown Reduction +1", true, 11, " rune_cstspd1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.BlueCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        CooldownReductionPlus1 = 121,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Charisma +1", true, 8, " rune_cha1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.BlueCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        CharismaPlus1 = 122,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Intelligence +1", true, 9, " rune_int1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.BlueCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        IntelligencePlus1 = 123,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Wisdom +1", true, 10, " rune_wis1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.BlueCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        WisdomPlus1 = 124,
        [CraftBlueprint(CraftBlueprintCategory.RedMods, "Damage +1", true, 15, " rune_dmg1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.RedCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        DamagePlus1 = 125,
        [CraftBlueprint(CraftBlueprintCategory.RedMods, "Hit Points +5", true, 14, " rune_hp1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.RedCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        HitPointsPlus5 = 126,
        [CraftBlueprint(CraftBlueprintCategory.YellowMods, "Armor Class +1", true, 16, " rune_ac1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.YellowCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ArmorClassPlus1 = 127,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Light Potency +1", true, 15, " rune_alt1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.BlueCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        LightPotencyPlus1 = 128,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Dark Potency +1", true, 15, " rune_evo1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.BlueCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        DarkPotencyPlus1 = 129,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "FP +5", true, 14, " rune_mana1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.BlueCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        FPPlus5 = 130,
        [CraftBlueprint(CraftBlueprintCategory.GreenMods, "Cooking +1", true, 14, " rune_cooking1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.GreenCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        CookingPlus1 = 131,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Mind Potency +1", true, 15, " rune_sum1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.BlueCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        MindPotencyPlus1 = 132,
        [CraftBlueprint(CraftBlueprintCategory.GreenMods, "Harvesting +1", true, 16, " rune_mining1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.GreenCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        HarvestingPlus1 = 133,
        [CraftBlueprint(CraftBlueprintCategory.GreenMods, "Armorsmith +1", true, 17, " rune_armsmth1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.GreenCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ArmorsmithPlus1 = 134,
        [CraftBlueprint(CraftBlueprintCategory.GreenMods, "Weaponsmith +1", true, 17, " rune_wpnsmth1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.GreenCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        WeaponsmithPlus1 = 135,
        [CraftBlueprint(CraftBlueprintCategory.GreenMods, "Engineering +1", true, 18, " rune_engin1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.GreenCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        EngineeringPlus1 = 136,
        [CraftBlueprint(CraftBlueprintCategory.YellowMods, "Medicine +1", true, 16, " rune_faid1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.YellowCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        MedicinePlus1 = 137,
        [CraftBlueprint(CraftBlueprintCategory.YellowMods, "Improved Enmity +1", true, 17, " rune_enmup1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.YellowCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ImprovedEnmityPlus1 = 138,
        [CraftBlueprint(CraftBlueprintCategory.YellowMods, "Sneak Attack +1", true, 18, " rune_snkatk1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.YellowCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        SneakAttackPlus1 = 139,
        [CraftBlueprint(CraftBlueprintCategory.RedMods, "Enhancement Bonus +1", true, 22, " rune_eb1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 3, ComponentType.RedCluster, 2, 3, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        EnhancementBonusPlus1 = 140,
        [CraftBlueprint(CraftBlueprintCategory.RedMods, "HP Regen +1", true, 20, " rune_hpregen1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 3, ComponentType.RedCluster, 2, 3, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        HPRegenPlus1 = 141,
        [CraftBlueprint(CraftBlueprintCategory.RedMods, "Base Attack Bonus +1", true, 22, " rune_bab1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 3, ComponentType.RedCluster, 2, 3, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        BaseAttackBonusPlus1 = 142,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "FP Regen +1", true, 21, " rune_manareg1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 3, ComponentType.BlueCluster, 2, 3, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        FPRegenPlus1 = 143,
        [CraftBlueprint(CraftBlueprintCategory.YellowMods, "Durability +1", true, 19, " rune_dur1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 3, ComponentType.YellowCluster, 2, 3, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        DurabilityPlus1 = 144,
        [CraftBlueprint(CraftBlueprintCategory.YellowMods, "Reduced Enmity -1", true, 21, " rune_enmdown1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 3, ComponentType.YellowCluster, 2, 3, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ReducedEnmity1 = 145,
        [CraftBlueprint(CraftBlueprintCategory.Throwing, "Basic Dart", true, 2, " dart_b", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.SmallBlade, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicDart = 146,
        [CraftBlueprint(CraftBlueprintCategory.YellowMods, "Level Decrease -3", true, 22, " rune_lvldown1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 3, ComponentType.YellowCluster, 2, 3, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        LevelDecrease3 = 147,
        [CraftBlueprint(CraftBlueprintCategory.RedMods, "Attack Bonus +2", true, 24, " rune_ab2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 3, ComponentType.RedCluster, 2, 3, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        AttackBonusPlus2 = 148,
        [CraftBlueprint(CraftBlueprintCategory.RedMods, "Constitution +2", true, 23, " rune_con2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 3, ComponentType.RedCluster, 2, 3, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ConstitutionPlus2 = 149,
        [CraftBlueprint(CraftBlueprintCategory.RedMods, "Dexterity +2", true, 24, " rune_dex2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 3, ComponentType.RedCluster, 2, 3, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        DexterityPlus2 = 150,
        [CraftBlueprint(CraftBlueprintCategory.RedMods, "Strength +2", true, 25, " rune_str2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 3, ComponentType.RedCluster, 2, 3, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        StrengthPlus2 = 151,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Cooldown Reduction +2", true, 26, " rune_cstspd2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 3, ComponentType.BlueCluster, 2, 3, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        CooldownReductionPlus2 = 152,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Charisma +2", true, 23, " rune_cha2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 3, ComponentType.BlueCluster, 2, 3, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        CharismaPlus2 = 153,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Intelligence +2", true, 24, " rune_int2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 3, ComponentType.BlueCluster, 2, 3, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        IntelligencePlus2 = 154,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Wisdom +2", true, 25, " rune_wis2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 3, ComponentType.BlueCluster, 2, 3, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        WisdomPlus2 = 155,
        [CraftBlueprint(CraftBlueprintCategory.YellowMods, "Luck +1", true, 27, " rune_luck1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 3, ComponentType.YellowCluster, 2, 3, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        LuckPlus1 = 156,
        [CraftBlueprint(CraftBlueprintCategory.YellowMods, "Meditate +1", true, 27, " rune_med1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 3, ComponentType.YellowCluster, 2, 3, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        MeditatePlus1 = 157,
        [CraftBlueprint(CraftBlueprintCategory.RedMods, "Damage +2", true, 30, " rune_dmg2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.RedCluster, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        DamagePlus2 = 158,
        [CraftBlueprint(CraftBlueprintCategory.RedMods, "Hit Points +10", true, 29, " rune_hp2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.RedCluster, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        HitPointsPlus10 = 159,
        [CraftBlueprint(CraftBlueprintCategory.YellowMods, "Armor Class +2", true, 31, " rune_ac2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.YellowCluster, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ArmorClassPlus2 = 160,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Light Potency +2", true, 30, " rune_alt2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.BlueCluster, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        LightPotencyPlus2 = 161,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Dark Potency +2", true, 30, " rune_evo2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.BlueCluster, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        DarkPotencyPlus2 = 162,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "FP +10", true, 29, " rune_mana2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.BlueCluster, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        FPPlus10 = 163,
        [CraftBlueprint(CraftBlueprintCategory.GreenMods, "Cooking +2", true, 29, " rune_cooking2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.GreenCluster, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        CookingPlus2 = 164,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Mind Potency +2", true, 30, " rune_sum2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.BlueCluster, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        MindPotencyPlus2 = 165,
        [CraftBlueprint(CraftBlueprintCategory.GreenMods, "Harvesting +2", true, 31, " rune_mining2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.GreenCluster, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        HarvestingPlus2 = 166,
        [CraftBlueprint(CraftBlueprintCategory.GreenMods, "Armorsmith +2", true, 32, " rune_armsmth2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.GreenCluster, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ArmorsmithPlus2 = 167,
        [CraftBlueprint(CraftBlueprintCategory.GreenMods, "Weaponsmith +2", true, 32, " rune_wpnsmth2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.GreenCluster, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        WeaponsmithPlus2 = 168,
        [CraftBlueprint(CraftBlueprintCategory.GreenMods, "Engineering +2", true, 33, " rune_engin2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.GreenCluster, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        EngineeringPlus2 = 169,
        [CraftBlueprint(CraftBlueprintCategory.YellowMods, "Medicine +2", true, 31, " rune_faid2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.YellowCluster, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        MedicinePlus2 = 170,
        [CraftBlueprint(CraftBlueprintCategory.YellowMods, "Improved Enmity +2", true, 32, " rune_enmup2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.YellowCluster, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ImprovedEnmityPlus2 = 171,
        [CraftBlueprint(CraftBlueprintCategory.YellowMods, "Sneak Attack +2", true, 32, " rune_snkatk2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.YellowCluster, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        SneakAttackPlus2 = 172,
        [CraftBlueprint(CraftBlueprintCategory.RedMods, "Enhancement Bonus +2", true, 37, " rune_eb2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.RedCluster, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        EnhancementBonusPlus2 = 173,
        [CraftBlueprint(CraftBlueprintCategory.RedMods, "HP Regen +2", true, 35, " rune_hpregen2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.RedCluster, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        HPRegenPlus2 = 174,
        [CraftBlueprint(CraftBlueprintCategory.RedMods, "Base Attack Bonus +2", true, 37, " rune_bab2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.RedCluster, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        BaseAttackBonusPlus2 = 175,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "FP Regen +2", true, 35, " rune_manareg2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.BlueCluster, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        FPRegenPlus2 = 176,
        [CraftBlueprint(CraftBlueprintCategory.YellowMods, "Durability +2", true, 34, " rune_dur2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.YellowCluster, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        DurabilityPlus2 = 177,
        [CraftBlueprint(CraftBlueprintCategory.YellowMods, "Reduced Enmity -2", true, 36, " rune_enmdown2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.YellowCluster, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ReducedEnmity2 = 178,
        [CraftBlueprint(CraftBlueprintCategory.RedMods, "Attack Bonus +3", true, 39, " rune_ab3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.RedCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        AttackBonusPlus3 = 179,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Mind Potency +3", true, 45, " rune_sum3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.BlueCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        MindPotencyPlus3 = 180,
        [CraftBlueprint(CraftBlueprintCategory.RedMods, "Constitution +3", true, 38, " rune_con3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.RedCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ConstitutionPlus3 = 181,
        [CraftBlueprint(CraftBlueprintCategory.RedMods, "Dexterity +3", true, 39, " rune_dex3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.RedCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        DexterityPlus3 = 182,
        [CraftBlueprint(CraftBlueprintCategory.RedMods, "Strength +3", true, 40, " rune_str3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.RedCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        StrengthPlus3 = 183,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Cooldown Reduction +3", true, 41, " rune_cstspd3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.BlueCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        CooldownReductionPlus3 = 184,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Charisma +3", true, 38, " rune_cha3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.BlueCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        CharismaPlus3 = 185,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Intelligence +3", true, 39, " rune_int3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.BlueCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        IntelligencePlus3 = 186,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Wisdom +3", true, 40, " rune_wis3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.BlueCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        WisdomPlus3 = 187,
        [CraftBlueprint(CraftBlueprintCategory.RedMods, "Damage +3", true, 45, " rune_dmg3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.RedCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        DamagePlus3 = 188,
        [CraftBlueprint(CraftBlueprintCategory.RedMods, "Hit Points +15", true, 44, " rune_hp3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.RedCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        HitPointsPlus15 = 189,
        [CraftBlueprint(CraftBlueprintCategory.YellowMods, "Armor Class +3", true, 46, " rune_ac3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.YellowCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ArmorClassPlus3 = 190,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Light Potency +3", true, 45, " rune_alt3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.BlueCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        LightPotencyPlus3 = 191,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Dark Potency +3", true, 45, " rune_evo3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.BlueCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        DarkPotencyPlus3 = 192,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "FP +15", true, 44, " rune_mana3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.BlueCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        FPPlus15 = 193,
        [CraftBlueprint(CraftBlueprintCategory.GreenMods, "Cooking +3", true, 44, " rune_cooking3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.GreenCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        CookingPlus3 = 194,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Electrical Potency +1", true, 15, " rune_ele1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.BlueCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ElectricalPotencyPlus1 = 195,
        [CraftBlueprint(CraftBlueprintCategory.GreenMods, "Harvesting +3", true, 46, " rune_mining3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.GreenCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        HarvestingPlus3 = 196,
        [CraftBlueprint(CraftBlueprintCategory.GreenMods, "Armorsmith +3", true, 47, " rune_armsmth3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.GreenCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ArmorsmithPlus3 = 197,
        [CraftBlueprint(CraftBlueprintCategory.GreenMods, "Weaponsmith +3", true, 47, " rune_wpnsmth3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.GreenCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        WeaponsmithPlus3 = 198,
        [CraftBlueprint(CraftBlueprintCategory.GreenMods, "Engineering +3", true, 48, " rune_engin3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.GreenCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        EngineeringPlus3 = 199,
        [CraftBlueprint(CraftBlueprintCategory.YellowMods, "Medicine +3", true, 46, " rune_faid3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.YellowCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        MedicinePlus3 = 200,
        [CraftBlueprint(CraftBlueprintCategory.YellowMods, "Improved Enmity +3", true, 47, " rune_enmup3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.YellowCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ImprovedEnmityPlus3 = 201,
        [CraftBlueprint(CraftBlueprintCategory.YellowMods, "Sneak Attack +3", true, 47, " rune_snkatk3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.YellowCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        SneakAttackPlus3 = 202,
        [CraftBlueprint(CraftBlueprintCategory.RedMods, "Enhancement Bonus +3", true, 49, " rune_eb3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.RedCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        EnhancementBonusPlus3 = 203,
        [CraftBlueprint(CraftBlueprintCategory.RedMods, "HP Regen +3", true, 50, " rune_hpregen3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.RedCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        HPRegenPlus3 = 204,
        [CraftBlueprint(CraftBlueprintCategory.RedMods, "Base Attack Bonus +3", true, 52, " rune_bab3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.RedCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        BaseAttackBonusPlus3 = 205,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "FP Regen +3", true, 50, " rune_manareg3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.BlueCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        FPRegenPlus3 = 206,
        [CraftBlueprint(CraftBlueprintCategory.YellowMods, "Durability +3", true, 49, " rune_dur3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.YellowCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        DurabilityPlus3 = 207,
        [CraftBlueprint(CraftBlueprintCategory.YellowMods, "Reduced Enmity -3", true, 51, " rune_enmdown3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.YellowCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ReducedEnmity3 = 208,
        [CraftBlueprint(CraftBlueprintCategory.YellowMods, "Luck +2", true, 42, " rune_luck2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 3, ComponentType.YellowCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        LuckPlus2 = 209,
        [CraftBlueprint(CraftBlueprintCategory.YellowMods, "Meditate +2", true, 42, " rune_med2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 3, ComponentType.YellowCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        MeditatePlus2 = 210,
        [CraftBlueprint(CraftBlueprintCategory.Lightsabers, "Basic Training Foil (Blue)", true, 2, " lightsaber_b", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 1, ComponentType.Emitter, 1, 2, ComponentType.SaberHilt, 1, 2, ComponentType.BlueCluster, 2, 3, 1, BaseStructure.Invalid)]
        BasicTrainingFoilBlue = 211,
        [CraftBlueprint(CraftBlueprintCategory.Lightsabers, "Training Foil I (Blue)", true, 7, " lightsaber_1", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 3, ComponentType.Emitter, 1, 2, ComponentType.SaberHilt, 1, 2, ComponentType.BlueCluster, 2, 3, 2, BaseStructure.Invalid)]
        TrainingFoilIBlue = 212,
        [CraftBlueprint(CraftBlueprintCategory.Lightsabers, "Training Foil II (Blue)", true, 12, " lightsaber_2", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 5, ComponentType.Emitter, 1, 2, ComponentType.SaberHilt, 1, 2, ComponentType.BlueCluster, 2, 3, 3, BaseStructure.Invalid)]
        TrainingFoilIIBlue = 213,
        [CraftBlueprint(CraftBlueprintCategory.Lightsabers, "Training Foil III (Blue)", true, 17, " lightsaber_3", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 7, ComponentType.Emitter, 1, 2, ComponentType.SaberHilt, 1, 2, ComponentType.BlueCluster, 2, 3, 4, BaseStructure.Invalid)]
        TrainingFoilIIIBlue = 214,
        [CraftBlueprint(CraftBlueprintCategory.Lightsabers, "Training Foil IV (Blue)", true, 22, " lightsaber_4", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 9, ComponentType.Emitter, 1, 2, ComponentType.SaberHilt, 1, 2, ComponentType.BlueCluster, 2, 3, 5, BaseStructure.Invalid)]
        TrainingFoilIVBlue = 215,
        [CraftBlueprint(CraftBlueprintCategory.Saberstaffs, "Basic Training Foil Staff (Blue)", true, 7, " saberstaff_b", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 1, ComponentType.Emitter, 2, 2, ComponentType.SaberHilt, 1, 2, ComponentType.BlueCluster, 2, 3, 2, BaseStructure.Invalid)]
        BasicTrainingFoilStaffBlue = 216,
        [CraftBlueprint(CraftBlueprintCategory.Saberstaffs, "Training Foil Staff I (Blue)", true, 8, " saberstaff_1", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 3, ComponentType.Emitter, 2, 4, ComponentType.SaberHilt, 1, 2, ComponentType.BlueCluster, 2, 3, 3, BaseStructure.Invalid)]
        TrainingFoilStaffIBlue = 217,
        [CraftBlueprint(CraftBlueprintCategory.Saberstaffs, "Training Foil Staff II (Blue)", true, 13, " saberstaff_2", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 5, ComponentType.Emitter, 2, 4, ComponentType.SaberHilt, 1, 2, ComponentType.BlueCluster, 2, 3, 4, BaseStructure.Invalid)]
        TrainingFoilStaffIIBlue = 218,
        [CraftBlueprint(CraftBlueprintCategory.Saberstaffs, "Training Foil Staff III (Blue)", true, 18, " saberstaff_3", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 7, ComponentType.Emitter, 2, 4, ComponentType.SaberHilt, 1, 2, ComponentType.BlueCluster, 2, 3, 5, BaseStructure.Invalid)]
        TrainingFoilStaffIIIBlue = 219,
        [CraftBlueprint(CraftBlueprintCategory.Saberstaffs, "Training Foil Staff IV (Blue)", true, 23, " saberstaff_4", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 9, ComponentType.Emitter, 2, 4, ComponentType.SaberHilt, 1, 2, ComponentType.BlueCluster, 2, 3, 6, BaseStructure.Invalid)]
        TrainingFoilStaffIVBlue = 220,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Saber Hilt", true, 1, " ls_hilt", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 1, ComponentType.Metal, 3, 3, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        SaberHilt = 221,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Power Crystal Cluster", true, 1, " c_cluster_power", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.None, 0, ComponentType.PowerCrystal, 2, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        PowerCluster = 222,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Light Armor Segment", true, 1, " l_armor_segment", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.None, 0, ComponentType.Leather, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        LightArmorSegment = 223,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Force Armor Segment", true, 2, " f_armor_segment", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.None, 0, ComponentType.Leather, 3, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ForceArmorSegment = 224,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Heavy Armor Segment", true, 3, " h_armor_segment", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.None, 0, ComponentType.Leather, 4, 6, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        HeavyArmorSegment = 225,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Fiberplast Padding", true, 0, " padding_fiber", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.None, 0, ComponentType.Fiberplast, 3, 3, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        FiberplastPadding = 226,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Light Armor Core", true, 2, " core_l_armor", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.None, 0, ComponentType.Cloth, 1, 3, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        LightArmorCore = 227,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Force Armor Core", true, 3, " core_f_armor", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.None, 0, ComponentType.Cloth, 1, 3, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ForceArmorCore = 228,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Heavy Armor Core", true, 4, " core_h_armor", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.None, 0, ComponentType.Cloth, 1, 3, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        HeavyArmorCore = 229,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Metal Reinforcement", true, 0, " padding_metal", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.None, 0, ComponentType.Metal, 2, 4, ComponentType.Fiberplast, 1, 2, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        MetalReinforcement = 230,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Basic Breastplate", true, 3, " breastplate_b", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.None, 0, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicBreastplate = 231,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Breastplate I", true, 6, " breastplate_1", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 1, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        BreastplateI = 232,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Breastplate II", true, 11, " breastplate_2", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 3, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        BreastplateII = 233,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Breastplate III", true, 16, " breastplate_3", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 5, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        BreastplateIII = 234,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Breastplate IV", true, 21, " breastplate_4", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        BreastplateIV = 235,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Basic Force Robes", true, 2, " force_robe_b", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.None, 0, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicForceRobes = 236,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Force Robes I", true, 6, " force_robe_1", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 1, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        ForceRobesI = 237,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Force Robes II", true, 11, " force_robe_2", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 3, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        ForceRobesII = 238,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Force Robes III", true, 16, " force_robe_3", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 5, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        ForceRobesIII = 239,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Force Robes IV", true, 21, " force_robe_4", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        ForceRobesIV = 240,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Basic Leather Tunic", true, 1, " leather_tunic_b", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.None, 0, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicLeatherTunic = 241,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Leather Tunic I", true, 5, " leather_tunic_1", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 1, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        LeatherTunicI = 242,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Leather Tunic II", true, 10, " leather_tunic_2", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 3, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        LeatherTunicII = 243,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Leather Tunic III", true, 15, " leather_tunic_3", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 5, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        LeatherTunicIII = 244,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Leather Tunic IV", true, 20, " leather_tunic_4", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        LeatherTunicIV = 245,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Basic Small Shield", true, 3, " small_shield_b", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.None, 0, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicSmallShield = 246,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Small Shield I", true, 6, " small_shield_1", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 1, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        SmallShieldI = 247,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Small Shield II", true, 11, " small_shield_2", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 3, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        SmallShieldII = 248,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Small Shield III", true, 16, " small_shield_3", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 5, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        SmallShieldIII = 249,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Small Shield IV", true, 21, " small_shield_4", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        SmallShieldIV = 250,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Basic Large Shield", true, 4, " large_shield_b", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.None, 0, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicLargeShield = 251,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Large Shield I", true, 7, " large_shield_1", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 1, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        LargeShieldI = 252,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Large Shield II", true, 12, " large_shield_2", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 3, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        LargeShieldII = 253,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Large Shield III", true, 17, " large_shield_3", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 5, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        LargeShieldIII = 254,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Large Shield IV", true, 22, " large_shield_4", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        LargeShieldIV = 255,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Basic Tower Shield", true, 5, " tower_shield_b", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.None, 0, ComponentType.HeavyArmorCore, 2, 4, ComponentType.HeavyArmorSegment, 2, 4, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicTowerShield = 256,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Tower Shield I", true, 7, " tower_shield_1", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 1, ComponentType.HeavyArmorCore, 2, 4, ComponentType.HeavyArmorSegment, 2, 4, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        TowerShieldI = 257,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Tower Shield II", true, 12, " tower_shield_2", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 3, ComponentType.HeavyArmorCore, 2, 4, ComponentType.HeavyArmorSegment, 2, 4, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        TowerShieldII = 258,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Tower Shield III", true, 17, " tower_shield_3", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 5, ComponentType.HeavyArmorCore, 2, 4, ComponentType.HeavyArmorSegment, 2, 4, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        TowerShieldIII = 259,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Tower Shield IV", true, 22, " tower_shield_4", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.HeavyArmorCore, 2, 4, ComponentType.HeavyArmorSegment, 2, 4, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        TowerShieldIV = 260,
        [CraftBlueprint(CraftBlueprintCategory.MartialArts, "Basic Power Glove", true, 3, " powerglove_b", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.None, 0, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicPowerGlove = 261,
        [CraftBlueprint(CraftBlueprintCategory.MartialArts, "Power Glove I", true, 6, " powerglove_1", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 1, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        PowerGloveI = 262,
        [CraftBlueprint(CraftBlueprintCategory.MartialArts, "Power Glove II", true, 11, " powerglove_2", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 3, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        PowerGloveII = 263,
        [CraftBlueprint(CraftBlueprintCategory.MartialArts, "Power Glove III", true, 16, " powerglove_3", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 5, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        PowerGloveIII = 264,
        [CraftBlueprint(CraftBlueprintCategory.MartialArts, "Power Glove IV", true, 21, " powerglove_4", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        PowerGloveIV = 265,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Light Belt I", true, 7, " light_belt_1", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 1, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        LightBeltI = 266,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Light Belt II", true, 14, " light_belt_2", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 3, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        LightBeltII = 267,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Light Belt III", true, 22, " light_belt_3", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 5, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        LightBeltIII = 268,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Force Belt I", true, 7, " force_belt_1", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 1, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ForceBeltI = 269,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Force Belt II", true, 15, " force_belt_2", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 3, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        ForceBeltII = 270,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Force Belt III", true, 23, " force_belt_3", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 5, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        ForceBeltIII = 271,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Heavy Belt I", true, 8, " heavy_belt_1", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 1, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        HeavyBeltI = 272,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Heavy Belt II", true, 16, " heavy_belt_2", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 3, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        HeavyBeltII = 273,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Heavy Belt III", true, 24, " heavy_belt_3", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 5, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        HeavyBeltIII = 274,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Prismatic Force Belt", true, 24, " prism_belt_f", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.PowerCluster, 1, 2, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, 2, BaseStructure.Invalid)]
        PrismaticForceBelt = 275,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Prismatic Light Belt", true, 24, " prism_belt_l", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.PowerCluster, 1, 2, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, 2, BaseStructure.Invalid)]
        PrismaticLightBelt = 276,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Prismatic Heavy Belt", true, 24, " prism_belt_h", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.PowerCluster, 1, 2, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, 2, BaseStructure.Invalid)]
        PrismaticHeavyBelt = 277,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Basic Force Boots", true, 3, " force_boots_b", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.None, 0, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicForceBoots = 278,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Force Boots I", true, 6, " force_boots_1", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 1, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        ForceBootsI = 279,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Force Boots II", true, 11, " force_boots_2", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 3, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        ForceBootsII = 280,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Force Boots III", true, 16, " force_boots_3", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 5, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        ForceBootsIII = 281,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Force Boots IV", true, 21, " force_boots_4", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        ForceBootsIV = 282,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Basic Light Boots", true, 2, " light_boots_b", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.None, 0, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicLightBoots = 283,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Light Boots I", true, 6, " light_boots_1", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 1, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        LightBootsI = 284,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Light Boots II", true, 11, " light_boots_2", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 3, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        LightBootsII = 285,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Light Boots III", true, 16, " light_boots_3", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 5, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        LightBootsIII = 286,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Light Boots IV", true, 21, " light_boots_4", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        LightBootsIV = 287,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Basic Heavy Boots", true, 4, " heavy_boots_b", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.None, 0, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicHeavyBoots = 288,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Heavy Boots I", true, 7, " heavy_boots_1", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 1, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        HeavyBootsI = 289,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Heavy Boots II", true, 12, " heavy_boots_2", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 3, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        HeavyBootsII = 290,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Heavy Boots III", true, 17, " heavy_boots_3", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 5, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        HeavyBootsIII = 291,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Heavy Boots IV", true, 22, " heavy_boots_4", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        HeavyBootsIV = 292,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Force Necklace I", true, 5, " force_neck_1", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 1, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        ForceNecklaceI = 293,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Force Necklace II", true, 10, " force_neck_2", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 3, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        ForceNecklaceII = 294,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Force Necklace III", true, 15, " force_neck_3", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 5, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        ForceNecklaceIII = 295,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Force Necklace IV", true, 20, " force_neck_4", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        ForceNecklaceIV = 296,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Heavy Crest I", true, 5, " h_crest_1", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 1, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        HeavyCrestI = 297,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Heavy Crest II", true, 10, " h_crest_2", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 3, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        HeavyCrestII = 298,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Heavy Crest III", true, 15, " h_crest_3", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 5, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        HeavyCrestIII = 299,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Heavy Crest IV", true, 20, " h_crest_4", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        HeavyCrestIV = 300,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Light Choker I", true, 4, " lt_choker_1", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 1, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        LightChokerI = 301,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Light Choker II", true, 9, " lt_choker_2", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 3, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        LightChokerII = 302,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Light Choker III", true, 14, " lt_choker_3", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 5, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        LightChokerIII = 303,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Light Choker IV", true, 19, " lt_choker_4", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        LightChokerIV = 304,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Prism Force Necklace", true, 24, " prism_neck_f", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.PowerCluster, 1, 2, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, 2, BaseStructure.Invalid)]
        PrismForceNecklace = 305,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Prism Light Necklace", true, 24, " prism_neck_l", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.PowerCluster, 1, 2, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, 2, BaseStructure.Invalid)]
        PrismLightNecklace = 306,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Prism Heavy Necklace", true, 24, " prism_neck_h", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.PowerCluster, 1, 2, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, 2, BaseStructure.Invalid)]
        PrismHeavyNecklace = 307,
        [CraftBlueprint(CraftBlueprintCategory.GreenMods, "Fabrication +1", true, 17, " rune_fab1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.GreenCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        FabricationPlus1 = 308,
        [CraftBlueprint(CraftBlueprintCategory.GreenMods, "Fabrication +2", true, 32, " rune_fab2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.GreenCluster, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        FabricationPlus2 = 309,
        [CraftBlueprint(CraftBlueprintCategory.GreenMods, "Fabrication +3", true, 47, " rune_fab3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.GreenCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        FabricationPlus3 = 310,
        [CraftBlueprint(CraftBlueprintCategory.ControlTowers, "Small Control Tower", true, 0, " control_tower", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.SmallStructureFrame, 1, 2, ComponentType.Mainframe, 1, 2, ComponentType.PowerRelay, 1, 2, 5, BaseStructure.SmallControlTower)]
        SmallControlTower = 311,
        [CraftBlueprint(CraftBlueprintCategory.ControlTowers, "Medium Control Tower", true, 10, " control_tower", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.MediumStructureFrame, 1, 2, ComponentType.Mainframe, 2, 3, ComponentType.PowerRelay, 2, 3, 5, BaseStructure.MediumControlTower)]
        MediumControlTower = 312,
        [CraftBlueprint(CraftBlueprintCategory.ControlTowers, "Large Control Tower", true, 6, " control_tower", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 3, ComponentType.LargeStructureFrame, 1, 2, ComponentType.Mainframe, 3, 4, ComponentType.PowerRelay, 3, 4, 5, BaseStructure.LargeControlTower)]
        LargeControlTower = 313,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Mainframe", true, 3, " mainframe", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.ComputingModule, 3, 6, ComponentType.ConstructionParts, 2, 4, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        Mainframe = 314,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Power Relay", true, 2, " power_relay", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.PowerCore, 3, 6, ComponentType.ConstructionParts, 2, 4, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        PowerRelay = 315,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Small Structure Frame", true, 2, " s_frame_small", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Metal, 2, 6, ComponentType.Fiberplast, 1, 3, ComponentType.Organic, 1, 3, 2, BaseStructure.Invalid)]
        SmallStructureFrame = 316,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Medium Structure Frame", true, 4, " s_frame_medium", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.SmallStructureFrame, 1, 2, ComponentType.Metal, 2, 6, ComponentType.Organic, 1, 3, 3, BaseStructure.Invalid)]
        MediumStructureFrame = 317,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Large Structure Frame", true, 6, " s_frame_large", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.MediumStructureFrame, 1, 2, ComponentType.Metal, 2, 6, ComponentType.Organic, 1, 3, 4, BaseStructure.Invalid)]
        LargeStructureFrame = 318,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Construction Parts", true, 0, " const_parts", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Metal, 2, 8, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ConstructionParts = 319,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Computing Module", true, 1, " computing_module", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Electronics, 3, 6, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ComputingModule = 320,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Power Core", true, 0, " power_core", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.PowerCrystal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        PowerCore = 321,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureStatues, "Obelisk, Large", true, 6, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 3, 6, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.ObeliskLarge)]
        Obelisk, Large = 322,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Electrical Potency +2", true, 30, " rune_ele2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.BlueCluster, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ElectricalPotencyPlus2 = 323,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureStatues, "Obelisk, Small", true, 5, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.ObeliskSmall)]
        ObeliskSmall = 324,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureMisc, "Ladder, Light", true, 1, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.LadderLight)]
        LadderLight = 325,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureMisc, "Ladder, Dark", true, 2, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.LadderDark)]
        LadderDark = 326,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureStatues, "Statue, Huge", true, 8, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.StatueHuge)]
        StatueHuge = 327,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureMisc, "Mining Well", true, 7, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 1, 2, ComponentType.Organic, 1, 2, ComponentType.Fiberplast, 1, 2, 1, BaseStructure.MiningWell)]
        MiningWell = 328,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Electrical Potency +3", true, 45, " rune_ele3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.BlueCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ElectricalPotencyPlus3 = 329,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureStatues, "Statue, Wizard", true, 5, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.StatueWizard)]
        StatueWizard = 330,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureMisc, "Gong", true, 0, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.Gong)]
        Gong = 331,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureStatues, "Statue, Monster", true, 7, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.StatueMonster)]
        Statue, Monster = 332,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Dark Defense +1", true, 16, " rune_ddef1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.BlueCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        DarkDefensePlus1 = 333,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Dark Defense +2", true, 31, " rune_ddef2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.BlueCluster, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        DarkDefensePlus2 = 334,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureLighting, "Brazier, Stone", true, 4, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.BrazierStone)]
        Brazier, Stone = 335,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureStatues, "Statue, Guardian", true, 5, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.StatueGuardian)]
        StatueGuardian = 336,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureDoors, "Doorway, Metal", true, 6, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.DoorwayMetal)]
        DoorwayMetal = 337,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureStatues, "Flaming Statue", true, 7, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.FlamingStatue)]
        FlamingStatue = 338,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureLighting, "Brazier, Round", true, 3, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.BrazierRound)]
        BrazierRound = 339,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureAltars, "Pedestal", true, 3, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Pedestal)]
        Pedestal = 340,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureMisc, "Rope Coil", true, 3, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Fiberplast, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.RopeCoil)]
        RopeCoil = 341,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureStatues, "Statue, Wyvern", true, 4, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.StatueWyvern)]
        StatueWyvern = 342,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureAltars, "Pedestal, Evil", true, 4, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.PedestalEvil)]
        PedestalEvil = 343,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureMisc, "Birdbath", true, 2, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.Birdbath)]
        Birdbath = 344,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureStatues, "Sphinx Statue", true, 3, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.SphinxStatue)]
        SphinxStatue = 345,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureMisc, "Mining Well Platform", true, 6, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.Organic, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.MiningWellPlatform)]
        MiningWellPlatform = 346,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureAltars, "Pedestal, Sword", true, 5, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.PedestalSword)]
        PedestalSword = 347,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureDoors, "Doorway, Stone", true, 5, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.DoorwayStone)]
        DoorwayStone = 348,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureStatues, "Female Statue", true, 5, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.FemaleStatue)]
        FemaleStatue = 349,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureMisc, "Gnomish Contraption", true, 2, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.GnomishContraption)]
        GnomishContraption = 350,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureStatues, "Pillar, Stone", true, 4, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.PillarStone)]
        PillarStone = 351,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Dark Defense +3", true, 46, " rune_ddef3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.BlueCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        DarkDefensePlus3 = 352,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureStatues, "Statue of Lathander", true, 2, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.StatueofLathander)]
        StatueofLathander = 353,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureStatues, "Pillar, Flame", true, 6, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.PillarFlame)]
        PillarFlame = 354,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureMisc, "Cage", true, 5, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.Cage)]
        Cage = 355,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureLighting, "Lamp Post", true, 5, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.LampPost)]
        LampPost = 356,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureLighting, "Torch Bracket", true, 5, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.TorchBracket)]
        TorchBracket = 357,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureStatues, "Pillar, Wood", true, 3, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.PillarWood)]
        Pillar, Wood = 358,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureStatues, "Statue, Cyric", true, 4, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.StatueCyric)]
        StatueCyric = 359,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureStatues, "Sea Idol", true, 3, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.SeaIdol)]
        SeaIdol = 360,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureMisc, "Fountain", true, 6, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.Fountain)]
        Fountain = 361,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureStatues, "Monster Statue", true, 6, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.MonsterStatue)]
        MonsterStatue = 362,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureMisc, "Easel", true, 4, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.Easel)]
        Easel = 363,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Light Defense +1", true, 16, " rune_ldef1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.BlueCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        LightDefensePlus1 = 364,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureMisc, "Keg", true, 1, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.Keg)]
        Keg = 365,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureStatues, "Dran Statue", true, 8, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 3, 6, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.DranStatue)]
        DranStatue = 366,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Light Defense +2", true, 31, " rune_ldef2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.BlueCluster, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        LightDefensePlus2 = 367,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Light Defense +3", true, 46, " rune_ldef3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.BlueCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        LightDefensePlus3 = 368,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureMisc, "Net", true, 1, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Leather, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.Net)]
        Net = 369,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureBeds, "Bed, Extra Large", true, 4, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 2, 4, ComponentType.Leather, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.BedExtraLarge)]
        Bed, ExtraLarge = 370,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Mind Defense +1", true, 16, " rune_mdef1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.BlueCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        MindDefensePlus1 = 371,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureFlooring, "Carpet, Round, Blue", true, 3, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Leather, 3, 6, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.CarpetRoundBlue)]
        CarpetRoundBlue = 372,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureAltars, "Altar, Evil", true, 0, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.AltarEvil)]
        AltarEvil = 373,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureTables, "Table, Wood, Large", true, 2, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.TableWoodLarge)]
        TableWoodLarge = 374,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureTables, "Table, Wood, With Fish", true, 1, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.TableWoodWithFish)]
        TableWoodWithFish = 375,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureTables, "Table, Stone, Small", true, 3, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.TableStoneSmall)]
        TableStoneSmall = 376,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureAltars, "Altar, Hand", true, 0, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.AltarHand)]
        AltarHand = 377,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureMisc, "Window", true, 4, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 1, 2, ComponentType.Metal, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Window)]
        Window = 378,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureSeating, "Cushions", true, 0, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Leather, 1, 2, ComponentType.Fiberplast, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Cushions)]
        Cushions = 379,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureLighting, "Candle", true, 1, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Fiberplast, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.Candle)]
        Candle = 380,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Mind Defense +2", true, 31, " rune_mdef2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.BlueCluster, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        MindDefensePlus2 = 381,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureFlooring, "Bear Skin Rug", true, 1, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Leather, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.BearSkinRug)]
        BearSkinRug = 382,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureLighting, "Chandelier", true, 0, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.Chandelier)]
        Chandelier = 383,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Mind Defense +3", true, 46, " rune_mdef3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.BlueCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        MindDefensePlus3 = 384,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureMisc, "Urn", true, 2, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.Urn)]
        Urn = 385,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureAltars, "Altar, Stone", true, 1, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.AltarStone)]
        AltarStone = 386,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureBeds, "Cot", true, 2, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 1, 2, ComponentType.Leather, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Cot)]
        Cot = 387,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureTables, "Table, Wood", true, 0, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.TableWood)]
        TableWood = 388,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Electrical Defense +1", true, 16, " rune_edef1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.BlueCluster, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ElectricalDefensePlus1 = 389,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureFlooring, "Throw Rug", true, 0, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Leather, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.ThrowRug)]
        ThrowRug = 390,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureTables, "Table, Stone, Large", true, 5, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Metal, 3, 6, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.TableStoneLarge)]
        TableStoneLarge = 391,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureBeds, "Bed, Stone, Yellow", true, 5, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Metal, 1, 2, ComponentType.Leather, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.BedStoneYellow)]
        BedStone, Yellow = 392,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureBeds, "Bed, Large", true, 3, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Organic, 1, 2, ComponentType.Leather, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.BedLarge)]
        BedLarge = 393,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureMisc, "Vase, Rounded", true, 2, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.VaseRounded)]
        Vase, Rounded = 394,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureFlooring, "Carpet", true, 2, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Leather, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.Carpet)]
        Carpet = 395,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureBeds, "Bed, Wood, Yellow", true, 3, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Metal, 1, 2, ComponentType.Leather, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.BedWoodYellow)]
        BedWoodYellow = 396,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureStatues, "Overgrown Pillar", true, 5, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.OvergrownPillar)]
        OvergrownPillar = 397,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureMisc, "Tome", true, 5, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Fiberplast, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.Tome)]
        Tome = 398,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureMisc, "Bird Cage", true, 2, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.BirdCage)]
        BirdCage = 399,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureStatues, "Pillar, Wood, Dark", true, 3, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.PillarWoodDark)]
        PillarWood, Dark = 400,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureBeds, "Bunk Bed", true, 4, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Organic, 2, 4, ComponentType.Leather, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.BunkBed)]
        BunkBed = 401,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureMisc, "Vase, Tall", true, 2, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.VaseTall)]
        VaseTall = 402,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureBeds, "Bed Roll", true, 0, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Leather, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.BedRoll)]
        BedRoll = 403,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureSeating, "Ottoman", true, 0, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.None, 0, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.Ottoman)]
        Ottoman = 404,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Electrical Defense +2", true, 31, " rune_edef2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.BlueCluster, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ElectricalDefensePlus2 = 405,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureStatues, "Pillar, Rounded", true, 4, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.PillarRounded)]
        PillarRounded = 406,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureMisc, "Painting 2", true, 1, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Fiberplast, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.Painting2)]
        Painting2 = 407,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureLighting, "Candelabra", true, 4, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.Candelabra)]
        Candelabra = 408,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureMisc, "Potted Plant", true, 1, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.PottedPlant)]
        PottedPlant = 409,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureMisc, "Painting 1", true, 1, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Fiberplast, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.Painting1)]
        Painting1 = 410,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureFlooring, "Carpet, Red", true, 2, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Leather, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.CarpetFancy)]
        CarpetRed = 411,
        [CraftBlueprint(CraftBlueprintCategory.BlueMods, "Electrical Defense +3", true, 46, " rune_edef3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.BlueCluster, 4, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ElectricalDefensePlus3 = 412,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureTables, "Illithid Table", true, 7, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 3, 6, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.IllithidTable)]
        IllithidTable = 413,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureFlooring, "Carpet, Red, Small", true, 1, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Leather, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.CarpetFancySmaller)]
        CarpetRedSmall = 414,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureAltars, "Drow Altar", true, 6, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 3, 6, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.DrowAltar)]
        DrowAltar = 415,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureMisc, "Dartboard", true, 4, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.Dartboard)]
        Dartboard = 416,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureMisc, "Map", true, 0, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Fiberplast, 1, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.Map)]
        Map = 417,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureMisc, "Floor-anchored shackles", true, 6, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.Flooranchoredshackles)]
        Flooranchoredshackles = 418,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureTables, "Round Wooden Table", true, 6, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.RoundWoodenTable)]
        RoundWoodenTable = 419,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureTables, "Drow Bar", true, 4, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 3, 6, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.DrowBar)]
        DrowBar = 420,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureStatues, "Shrine of Umberlee", true, 2, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.ShrineofUmberlee)]
        ShrineofUmberlee = 421,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureStatues, "Rune Pillar", true, 4, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.RunePillar)]
        RunePillar = 422,
        [CraftBlueprint(CraftBlueprintCategory.Throwing, "Dart I", true, 5, " dart_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.SmallBlade, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        DartI = 423,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureMisc, "Mirror", true, 3, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 1, 2, ComponentType.Metal, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Mirror)]
        Mirror = 424,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureSeating, "Footstool", true, 0, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.Footstool)]
        Footstool = 425,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureTables, "Drow Table", true, 7, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 3, 6, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.DrowTable)]
        DrowTable = 426,
        [CraftBlueprint(CraftBlueprintCategory.CraftingDevices, "Molecular Reassembly Terminal", true, 5, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 3, ComponentType.PowerCluster, 5, 5, ComponentType.ConstructionParts, 1, 1, ComponentType.None, 0, 0, 0, BaseStructure.MolecularReassemblyTerminal)]
        MolecularReassemblyTerminal = 427,
        [CraftBlueprint(CraftBlueprintCategory.Throwing, "Dart II", true, 10, " dart_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.SmallBlade, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        DartII = 428,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureSeating, "Bench, Stone, Dwarven", true, 3, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.BenchStoneDwarven)]
        BenchStoneDwarven = 429,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureSeating, "Illithid Chair", true, 2, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.IllithidChair)]
        IllithidChair = 430,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureSeating, "Bench, Wood", true, 2, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.BenchWood)]
        BenchWood = 431,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureSeating, "Chair, Wood, Medium", true, 1, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.ChairWoodMedium)]
        ChairWoodMedium = 432,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureSeating, "Bench, Large", true, 4, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.BenchLarge)]
        BenchLarge = 433,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureSeating, "Drow Chair", true, 1, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.DrowChair)]
        DrowChair = 434,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureSeating, "Bench, Wood, Large", true, 3, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 3, 6, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.BenchWoodLarge)]
        BenchWoodLarge = 435,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureSeating, "Bench, Wood, Small", true, 1, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.BenchWoodSmall)]
        BenchWoodSmall = 436,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureSeating, "Chair, Wood", true, 0, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.ChairWood)]
        ChairWood = 437,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureSeating, "Chair, Wood, Small", true, 0, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.ChairWoodSmall)]
        ChairWoodSmall, Small = 438,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureSeating, "Throne, Wood", true, 1, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 3, 6, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.ThroneWood)]
        ThroneWood = 439,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureSeating, "Chair, Stone", true, 2, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.ChairStone)]
        ChairStone = 440,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureSeating, "Bench, Wood, Small 2", true, 1, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.BenchWoodSmall2)]
        BenchWoodSmall2 = 441,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureSeating, "Chair, Shell", true, 2, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.ChairShell)]
        Chair, Shell = 442,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureSeating, "Couch, Wood, Yellow", true, 1, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.CouchWoodYellow)]
        CouchWoodYellow = 443,
        [CraftBlueprint(CraftBlueprintCategory.ItemStorage, "Barrel", true, 1, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 3, 6, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.Barrel)]
        Barrel = 444,
        [CraftBlueprint(CraftBlueprintCategory.Throwing, "Dart III", true, 15, " dart_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.SmallBlade, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        DartIII = 445,
        [CraftBlueprint(CraftBlueprintCategory.Throwing, "Dart IV", true, 20, " dart_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.SmallBlade, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        DartIV = 446,
        [CraftBlueprint(CraftBlueprintCategory.ItemStorage, "Crate", true, 2, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 4, 8, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.Crate)]
        Crate = 447,
        [CraftBlueprint(CraftBlueprintCategory.Throwing, "Basic Shuriken", true, 3, " shuriken_b", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.SmallBlade, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicShuriken = 448,
        [CraftBlueprint(CraftBlueprintCategory.Throwing, "Shuriken I", true, 6, " shuriken_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.SmallBlade, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        ShurikenI = 449,
        [CraftBlueprint(CraftBlueprintCategory.ItemStorage, "Book Shelf", true, 1, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 3, 6, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.BookShelf)]
        BookShelf = 450,
        [CraftBlueprint(CraftBlueprintCategory.ItemStorage, "Chest", true, 3, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 5, 10, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.Chest)]
        Chest = 451,
        [CraftBlueprint(CraftBlueprintCategory.Throwing, "Shuriken II", true, 11, " shuriken_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.SmallBlade, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        ShurikenII = 452,
        [CraftBlueprint(CraftBlueprintCategory.Throwing, "Shuriken III", true, 16, " shuriken_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.SmallBlade, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        ShurikenIII = 453,
        [CraftBlueprint(CraftBlueprintCategory.ItemStorage, "Desk", true, 2, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Organic, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.Desk)]
        Desk = 454,
        [CraftBlueprint(CraftBlueprintCategory.Throwing, "Shuriken IV", true, 21, " shuriken_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.SmallBlade, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        ShurikenIV = 455,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Throwing Weapon Repair Kit I", true, 7, " th_rep_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 1, ComponentType.Organic, 2, 4, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ThrowingWeaponRepairKitI = 456,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Throwing Weapon Repair Kit II", true, 17, " th_rep_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 3, ComponentType.Organic, 2, 4, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        ThrowingWeaponRepairKitII = 457,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Throwing Weapon Repair Kit III", true, 27, " th_rep_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 5, ComponentType.Organic, 2, 4, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        ThrowingWeaponRepairKitIII = 458,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Throwing Weapon Repair Kit IV", true, 7, " th_rep_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 7, ComponentType.Organic, 2, 4, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        ThrowingWeaponRepairKitIV = 459,
        [CraftBlueprint(CraftBlueprintCategory.CraftingDevices, "Refinery", true, 0, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 2, ComponentType.Organic, 2, 2, ComponentType.None, 0, 0, 0, BaseStructure.Refinery)]
        Refinery = 464,
        [CraftBlueprint(CraftBlueprintCategory.CraftingDevices, "Armorsmith Workbench", true, 0, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 2, ComponentType.Organic, 2, 2, ComponentType.None, 0, 0, 0, BaseStructure.ArmorsmithWorkbench)]
        ArmorsmithWorkbench = 465,
        [CraftBlueprint(CraftBlueprintCategory.CraftingDevices, "Weaponsmith Bench", true, 0, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 2, ComponentType.Organic, 2, 2, ComponentType.None, 0, 0, 0, BaseStructure.WeaponsmithBench)]
        WeaponsmithBench = 466,
        [CraftBlueprint(CraftBlueprintCategory.CraftingDevices, "Cookpot", true, 0, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 2, ComponentType.Organic, 2, 2, ComponentType.None, 0, 0, 0, BaseStructure.Cookpot)]
        Cookpot = 467,
        [CraftBlueprint(CraftBlueprintCategory.CraftingDevices, "Engineering Bench", true, 0, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 2, ComponentType.Organic, 2, 2, ComponentType.None, 0, 0, 0, BaseStructure.EngineeringBench)]
        EngineeringBench = 468,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureStatues, "Mysterious Obelisk", true, 5, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.MysteriousObelisk)]
        MysteriousObelisk = 470,
        [CraftBlueprint(CraftBlueprintCategory.Buildings, "Small Building", true, 5, " building", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.SmallStructureFrame, 1, 2, ComponentType.ConstructionParts, 4, 8, ComponentType.PowerRelay, 1, 2, 3, BaseStructure.SmallBuilding)]
        SmallBuilding = 471,
        [CraftBlueprint(CraftBlueprintCategory.Buildings, "Medium Building", true, 15, " building", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.MediumStructureFrame, 1, 2, ComponentType.ConstructionParts, 6, 12, ComponentType.PowerRelay, 1, 3, 3, BaseStructure.MediumBuilding)]
        MediumBuilding = 472,
        [CraftBlueprint(CraftBlueprintCategory.Buildings, "Large Building", true, 20, " building", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 3, ComponentType.LargeStructureFrame, 1, 2, ComponentType.ConstructionParts, 8, 16, ComponentType.PowerRelay, 1, 4, 3, BaseStructure.LargeBuilding)]
        LargeBuilding = 473,
        [CraftBlueprint(CraftBlueprintCategory.Harvesters, "Basic Resource Harvester", true, 0, " harvest_r_b", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.None, 0, ComponentType.Electronics, 3, 6, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicResourceHarvester = 474,
        [CraftBlueprint(CraftBlueprintCategory.Harvesters, "Resource Harvester I", true, 10, " harvest_r_1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.Electronics, 3, 6, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        ResourceHarvesterI = 475,
        [CraftBlueprint(CraftBlueprintCategory.Harvesters, "Resource Harvester II", true, 20, " harvest_r_2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 3, ComponentType.Electronics, 3, 6, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        ResourceHarvesterII = 476,
        [CraftBlueprint(CraftBlueprintCategory.Harvesters, "Resource Harvester III", true, 30, " harvest_r_3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.Electronics, 3, 6, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        ResourceHarvesterIII = 477,
        [CraftBlueprint(CraftBlueprintCategory.Harvesters, "Resource Harvester IV", true, 40, " harvest_r_4", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.Electronics, 3, 6, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        ResourceHarvesterIV = 478,
        [CraftBlueprint(CraftBlueprintCategory.Harvesters, "Basic Resource Scanner", true, 0, " scanner_r_b", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.None, 0, ComponentType.Electronics, 3, 6, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicResourceScanner = 479,
        [CraftBlueprint(CraftBlueprintCategory.Harvesters, "Resource Scanner I", true, 10, " scanner_r_1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.Electronics, 3, 6, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        ResourceScannerI = 480,
        [CraftBlueprint(CraftBlueprintCategory.Harvesters, "Resource Scanner II", true, 20, " scanner_r_2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 3, ComponentType.Electronics, 3, 6, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        ResourceScannerII = 481,
        [CraftBlueprint(CraftBlueprintCategory.Harvesters, "Resource Scanner III", true, 30, " scanner_r_3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.Electronics, 3, 6, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        ResourceScannerIII = 482,
        [CraftBlueprint(CraftBlueprintCategory.Harvesters, "Resource Scanner IV", true, 40, " scanner_r_4", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.Electronics, 3, 6, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        ResourceScannerIV = 483,
        [CraftBlueprint(CraftBlueprintCategory.Fuel, "Fuel Cell", true, 2, " fuel_cell", 5, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.None, 0, ComponentType.PowerCrystal, 1, 1, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        FuelCell5 = 484,
        [CraftBlueprint(CraftBlueprintCategory.Fuel, "Fuel Cell", true, 12, " fuel_cell", 15, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.PowerCrystal, 2, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        FuelCell15 = 485,
        [CraftBlueprint(CraftBlueprintCategory.Fuel, "Fuel Cell", true, 22, " fuel_cell", 20, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 3, ComponentType.PowerCrystal, 3, 3, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        FuelCell20 = 486,
        [CraftBlueprint(CraftBlueprintCategory.Fuel, "Fuel Cell", true, 32, " fuel_cell", 25, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.PowerCrystal, 4, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        FuelCell25 = 487,
        [CraftBlueprint(CraftBlueprintCategory.Fuel, "Fuel Cell", true, 42, " fuel_cell", 35, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.PowerCrystal, 5, 5, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        FuelCell35 = 488,
        [CraftBlueprint(CraftBlueprintCategory.Silos, "Basic Stronidium Silo", true, 0, " silo", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.MediumStructureFrame, 1, 2, ComponentType.ConstructionParts, 2, 4, ComponentType.None, 0, 0, 0, BaseStructure.BasicStronidiumSilo)]
        BasicStronidiumSilo = 489,
        [CraftBlueprint(CraftBlueprintCategory.Silos, "Stronidium Silo I", true, 5, " silo", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.MediumStructureFrame, 1, 2, ComponentType.ConstructionParts, 2, 4, ComponentType.None, 0, 0, 1, BaseStructure.StronidiumSiloI)]
        StronidiumSiloI = 490,
        [CraftBlueprint(CraftBlueprintCategory.Silos, "Stronidium Silo II", true, 10, " silo", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 3, ComponentType.MediumStructureFrame, 1, 2, ComponentType.ConstructionParts, 2, 4, ComponentType.None, 0, 0, 2, BaseStructure.StronidiumSiloII)]
        StronidiumSiloII = 491,
        [CraftBlueprint(CraftBlueprintCategory.Silos, "Stronidium Silo III", true, 15, " silo", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 5, ComponentType.MediumStructureFrame, 1, 2, ComponentType.ConstructionParts, 2, 4, ComponentType.None, 0, 0, 3, BaseStructure.StronidiumSiloIII)]
        StronidiumSiloIII = 492,
        [CraftBlueprint(CraftBlueprintCategory.Silos, "Stronidium Silo IV", true, 20, " silo", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 7, ComponentType.MediumStructureFrame, 1, 2, ComponentType.ConstructionParts, 2, 4, ComponentType.None, 0, 0, 4, BaseStructure.StronidiumSiloIV)]
        StronidiumSiloIV = 493,
        [CraftBlueprint(CraftBlueprintCategory.Silos, "Basic Fuel Silo", true, 0, " silo", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.MediumStructureFrame, 1, 2, ComponentType.ConstructionParts, 2, 4, ComponentType.None, 0, 0, 0, BaseStructure.BasicFuelSilo)]
        BasicFuelSilo = 494,
        [CraftBlueprint(CraftBlueprintCategory.Silos, "Fuel Silo I", true, 5, " silo", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.MediumStructureFrame, 1, 2, ComponentType.ConstructionParts, 2, 4, ComponentType.None, 0, 0, 1, BaseStructure.FuelSiloI)]
        FuelSiloI = 495,
        [CraftBlueprint(CraftBlueprintCategory.Silos, "Fuel Silo II", true, 10, " silo", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 3, ComponentType.MediumStructureFrame, 1, 2, ComponentType.ConstructionParts, 2, 4, ComponentType.None, 0, 0, 2, BaseStructure.FuelSiloII)]
        FuelSiloII = 496,
        [CraftBlueprint(CraftBlueprintCategory.Silos, "Fuel Silo III", true, 15, " silo", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 5, ComponentType.MediumStructureFrame, 1, 2, ComponentType.ConstructionParts, 2, 4, ComponentType.None, 0, 0, 3, BaseStructure.FuelSiloIII)]
        FuelSiloIII = 497,
        [CraftBlueprint(CraftBlueprintCategory.Silos, "Fuel Silo IV", true, 20, " silo", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 7, ComponentType.MediumStructureFrame, 1, 2, ComponentType.ConstructionParts, 2, 4, ComponentType.None, 0, 0, 4, BaseStructure.FuelSiloIV)]
        FuelSiloIV = 498,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Healing Kit I", true, 2, " healing_kit", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.None, 0, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        HealingKitI = 499,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Healing Kit II", true, 12, " healing_kit_p1", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 1, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        HealingKitII = 500,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Healing Kit III", true, 22, " healing_kit_p2", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 3, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        HealingKitIII = 501,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Healing Kit IV", true, 32, " healing_kit_p3", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 5, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        HealingKitIV = 502,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Healing Kit V", true, 42, " healing_kit_p4", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 7, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        HealingKitV = 503,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Dexterity Stim I", true, 14, " stim_dex1", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 1, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        DexterityStimI = 504,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Intelligence Stim I", true, 15, " stim_int1", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 1, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        IntelligenceStimI = 505,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Strength Stim I", true, 16, " stim_str1", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 1, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        StrengthStimI = 506,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Charisma Stim I", true, 17, " stim_cha1", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 1, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        CharismaStimI = 507,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Constitution Stim I", true, 18, " stim_con1", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 1, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        ConstitutionStimI = 508,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Wisdom Stim I", true, 19, " stim_wis1", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 1, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        WisdomStimI = 509,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Dexterity Stim II", true, 24, " stim_dex2", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 3, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        DexterityStimII = 510,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Intelligence Stim II", true, 25, " stim_int2", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 3, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        IntelligenceStimII = 511,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Strength Stim II", true, 26, " stim_str2", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 3, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        StrengthStimII = 512,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Charisma Stim II", true, 27, " stim_cha2", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 3, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        CharismaStimII = 513,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Constitution Stim II", true, 28, " stim_con2", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 3, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        ConstitutionStimII = 514,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Wisdom Stim II", true, 29, " stim_wis2", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 3, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        WisdomStimII = 515,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Dexterity Stim III", true, 34, " stim_dex3", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 5, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        DexterityStimIII = 516,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Intelligence Stim III", true, 35, " stim_int3", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 5, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        IntelligenceStimIII = 517,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Strength Stim III", true, 36, " stim_str3", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 5, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        StrengthStimIII = 518,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Charisma Stim III", true, 37, " stim_cha3", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 5, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        CharismaStimIII = 519,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Constitution Stim III", true, 38, " stim_con3", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 5, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        ConstitutionStimIII = 520,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Wisdom Stim III", true, 39, " stim_wis3", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 5, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        WisdomStimIII = 521,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Bandages", true, 1, " bandages", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.None, 0, ComponentType.Fiberplast, 1, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        Bandages = 522,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Resuscitation Device I", true, 15, " res_kit_1", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 1, ComponentType.Fiberplast, 2, 4, ComponentType.Electronics, 1, 2, ComponentType.Metal, 1, 2, 1, BaseStructure.Invalid)]
        ResuscitationDeviceI = 523,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Resuscitation Device II", true, 25, " res_kit_2", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 3, ComponentType.Fiberplast, 2, 4, ComponentType.Electronics, 1, 2, ComponentType.Metal, 1, 2, 2, BaseStructure.Invalid)]
        ResuscitationDeviceII = 524,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Resuscitation Device III", true, 35, " res_kit_3", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 5, ComponentType.Fiberplast, 2, 4, ComponentType.Electronics, 1, 2, ComponentType.Metal, 1, 2, 3, BaseStructure.Invalid)]
        ResuscitationDeviceIII = 525,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Resuscitation Device IV", true, 45, " res_kit_4", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 7, ComponentType.Fiberplast, 2, 4, ComponentType.Electronics, 1, 2, ComponentType.Metal, 1, 2, 4, BaseStructure.Invalid)]
        ResuscitationDeviceIV = 526,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Force Pack I", true, 3, " force_pack_1", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.None, 0, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        ForcePackI = 527,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Force Pack II", true, 13, " force_pack_2", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 1, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        ForcePackII = 528,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Force Pack III", true, 23, " force_pack_3", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 3, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        ForcePackIII = 529,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Force Pack IV", true, 33, " force_pack_4", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 5, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        ForcePackIV = 530,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Force Pack V", true, 43, " force_pack_5", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 7, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        ForcePackV = 531,
        [CraftBlueprint(CraftBlueprintCategory.Medical, "Poison Treatment Kit", true, 5, " treatment_kit", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.None, 0, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 2, 4, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        PoisonTreatmentKit = 532,
        [CraftBlueprint(CraftBlueprintCategory.Enhancements, "Medical Enhancer I", true, 7, " m_enhancer", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.None, 0, ComponentType.Electronics, 2, 4, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 1, 2, 0, BaseStructure.Invalid)]
        MedicalEnhancerI = 533,
        [CraftBlueprint(CraftBlueprintCategory.Enhancements, "Medical Enhancer II", true, 17, " m_enhancer2", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 1, ComponentType.Electronics, 2, 4, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 1, 2, 1, BaseStructure.Invalid)]
        MedicalEnhancerII = 534,
        [CraftBlueprint(CraftBlueprintCategory.Enhancements, "Medical Enhancer III", true, 27, " m_enhancer3", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 3, ComponentType.Electronics, 2, 4, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 1, 2, 2, BaseStructure.Invalid)]
        MedicalEnhancerIII = 535,
        [CraftBlueprint(CraftBlueprintCategory.Enhancements, "Medical Enhancer IV", true, 37, " m_enhancer4", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 5, ComponentType.Electronics, 2, 4, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 1, 2, 3, BaseStructure.Invalid)]
        MedicalEnhancerIV = 536,
        [CraftBlueprint(CraftBlueprintCategory.Enhancements, "Medical Enhancer V", true, 47, " m_enhancer5", 1, Skill.Medicine, CraftDeviceType.MedicineBench, PerkType.MedicalBlueprints, 7, ComponentType.Electronics, 2, 4, ComponentType.Fiberplast, 2, 4, ComponentType.Herb, 1, 2, 4, BaseStructure.Invalid)]
        MedicalEnhancerV = 537,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Blaster Pistol Repair Kit I", true, 5, " bp_rep_1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.Electronics, 2, 4, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        BlasterPistolRepairKitI = 538,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Blaster Pistol Repair Kit II", true, 15, " bp_rep_2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 3, ComponentType.Electronics, 2, 4, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BlasterPistolRepairKitII = 539,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Blaster Pistol Repair Kit III", true, 25, " bp_rep_3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.Electronics, 2, 4, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        BlasterPistolRepairKitIII = 540,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Blaster Pistol Repair Kit IV", true, 35, " bp_rep_4", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.Electronics, 2, 4, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        BlasterPistolRepairKitIV = 541,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Blaster Rifle Repair Kit I", true, 6, " br_rep_1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 1, ComponentType.Electronics, 2, 4, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        BlasterRifleRepairKitI = 542,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Blaster Rifle Repair Kit II", true, 16, " br_rep_2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 3, ComponentType.Electronics, 2, 4, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BlasterRifleRepairKitII = 543,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Blaster Rifle Repair Kit III", true, 26, " br_rep_3", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.Electronics, 2, 4, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        BlasterRifleRepairKitIII = 544,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Blaster Rifle Repair Kit IV", true, 36, " br_rep_4", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.Electronics, 2, 4, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        BlasterRifleRepairKitIV = 545,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Baton Repair Kit I", true, 7, " bt_rep_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 1, ComponentType.Organic, 2, 4, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        BatonRepairKitI = 546,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Baton Repair Kit II", true, 17, " bt_rep_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 3, ComponentType.Organic, 2, 4, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BatonRepairKitII = 547,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Baton Repair Kit III", true, 27, " bt_rep_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 5, ComponentType.Organic, 2, 4, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        BatonRepairKitIII = 548,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Baton Repair Kit IV", true, 37, " bt_rep_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 7, ComponentType.Organic, 2, 4, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        BatonRepairKitIV = 549,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Finesse Vibroblade Repair Kit I", true, 8, " fv_rep_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        FinesseVibrobladeRepairKitI = 550,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Finesse Vibroblade Repair Kit II", true, 18, " fv_rep_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 3, ComponentType.Metal, 2, 4, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        FinesseVibrobladeRepairKitII = 551,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Finesse Vibroblade Repair Kit III", true, 28, " fv_rep_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 5, ComponentType.Metal, 2, 4, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        FinesseVibrobladeRepairKitIII = 552,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Finesse Vibroblade Repair Kit IV", true, 38, " fv_rep_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 7, ComponentType.Metal, 2, 4, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        FinesseVibrobladeRepairKitIV = 553,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Heavy Vibroblade Repair Kit I", true, 9, " hv_rep_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 1, ComponentType.Metal, 3, 6, ComponentType.Organic, 3, 6, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        HeavyVibrobladeRepairKitI = 554,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Heavy Vibroblade Repair Kit II", true, 19, " hv_rep_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 3, ComponentType.Metal, 3, 6, ComponentType.Organic, 3, 6, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        HeavyVibrobladeRepairKitII = 555,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Heavy Vibroblade Repair Kit III", true, 29, " hv_rep_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 5, ComponentType.Metal, 3, 6, ComponentType.Organic, 3, 6, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        HeavyVibrobladeRepairKitIII = 556,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Heavy Vibroblade Repair Kit IV", true, 39, " hv_rep_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 7, ComponentType.Metal, 3, 6, ComponentType.Organic, 3, 6, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        HeavyVibrobladeRepairKitIV = 557,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Lightsaber Repair Kit I", true, 7, " ls_rep_1", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 1, ComponentType.PowerCluster, 2, 4, ComponentType.Electronics, 2, 4, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        LightsaberRepairKitI = 558,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Lightsaber Repair Kit II", true, 17, " ls_rep_2", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 3, ComponentType.PowerCluster, 2, 4, ComponentType.Electronics, 2, 4, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        LightsaberRepairKitII = 559,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Lightsaber Repair Kit III", true, 27, " ls_rep_3", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 5, ComponentType.PowerCluster, 2, 4, ComponentType.Electronics, 2, 4, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        LightsaberRepairKitIII = 560,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Lightsaber Repair Kit IV", true, 37, " ls_rep_4", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 7, ComponentType.PowerCluster, 2, 4, ComponentType.Electronics, 2, 4, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        LightsaberRepairKitIV = 561,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Martial Arts Weapon Repair Kit I", true, 11, " ma_rep_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 1, ComponentType.Organic, 2, 4, ComponentType.Leather, 2, 4, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        MartialArtsWeaponRepairKitI = 562,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Martial Arts Weapon Repair Kit II", true, 21, " ma_rep_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 3, ComponentType.Organic, 2, 4, ComponentType.Leather, 2, 4, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        MartialArtsWeaponRepairKitII = 563,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Martial Arts Weapon Repair Kit III", true, 31, " ma_rep_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 5, ComponentType.Organic, 2, 4, ComponentType.Leather, 2, 4, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        MartialArtsWeaponRepairKitIII = 564,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Martial Arts Weapon Repair Kit IV", true, 41, " ma_rep_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 7, ComponentType.Organic, 2, 4, ComponentType.Leather, 2, 4, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        MartialArtsWeaponRepairKitIV = 565,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Polearm Repair Kit I", true, 12, " po_rep_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 1, ComponentType.Metal, 3, 6, ComponentType.Organic, 3, 6, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        PolearmRepairKitI = 566,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Polearm Repair Kit II", true, 22, " po_rep_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 3, ComponentType.Metal, 3, 6, ComponentType.Organic, 3, 6, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        PolearmRepairKitII = 567,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Polearm Repair Kit III", true, 32, " po_rep_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 5, ComponentType.Metal, 3, 6, ComponentType.Organic, 3, 6, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        PolearmRepairKitIII = 568,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Polearm Repair Kit IV", true, 42, " po_rep_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 7, ComponentType.Metal, 3, 6, ComponentType.Organic, 3, 6, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        PolearmRepairKitIV = 569,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Saberstaff Repair Kit I", true, 10, " ss_rep_1", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 1, ComponentType.PowerCluster, 3, 6, ComponentType.Electronics, 3, 6, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        SaberstaffRepairKitI = 570,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Saberstaff Repair Kit II", true, 20, " ss_rep_2", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 3, ComponentType.PowerCluster, 3, 6, ComponentType.Electronics, 3, 6, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        SaberstaffRepairKitII = 571,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Saberstaff Repair Kit III", true, 30, " ss_rep_3", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 5, ComponentType.PowerCluster, 3, 6, ComponentType.Electronics, 3, 6, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        SaberstaffRepairKitIII = 572,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Saberstaff Repair Kit IV", true, 40, " ss_rep_4", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 7, ComponentType.PowerCluster, 3, 6, ComponentType.Electronics, 3, 6, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        SaberstaffRepairKitIV = 573,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Twin Vibroblade Repair Kit I", true, 14, " tb_rep_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 1, ComponentType.Metal, 3, 6, ComponentType.Organic, 3, 6, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        TwinVibrobladeRepairKitI = 574,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Twin Vibroblade Repair Kit II", true, 24, " tb_rep_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 3, ComponentType.Metal, 3, 6, ComponentType.Organic, 3, 6, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        TwinVibrobladeRepairKitII = 575,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Twin Vibroblade Repair Kit III", true, 34, " tb_rep_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 5, ComponentType.Metal, 3, 6, ComponentType.Organic, 3, 6, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        TwinVibrobladeRepairKitIII = 576,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Twin Vibroblade Repair Kit IV", true, 44, " tb_rep_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 7, ComponentType.Metal, 3, 6, ComponentType.Organic, 3, 6, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        TwinVibrobladeRepairKitIV = 577,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Vibroblade Repair Kit I", true, 7, " vb_rep_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 1, ComponentType.Metal, 2, 4, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        VibrobladeRepairKitI = 578,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Vibroblade Repair Kit II", true, 17, " vb_rep_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 3, ComponentType.Metal, 2, 4, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        VibrobladeRepairKitII = 579,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Vibroblade Repair Kit III", true, 27, " vb_rep_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 5, ComponentType.Metal, 2, 4, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        VibrobladeRepairKitIII = 580,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Vibroblade Repair Kit IV", true, 37, " vb_rep_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.WeaponBlueprints, 7, ComponentType.Metal, 2, 4, ComponentType.Organic, 2, 4, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        VibrobladeRepairKitIV = 581,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Force Armor Repair Kit I", true, 12, " fa_rep_1", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 1, ComponentType.Leather, 2, 4, ComponentType.Fiberplast, 2, 4, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ForceArmorRepairKitI = 582,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Force Armor Repair Kit II", true, 22, " fa_rep_2", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 3, ComponentType.Leather, 2, 4, ComponentType.Fiberplast, 2, 4, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        ForceArmorRepairKitII = 583,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Force Armor Repair Kit III", true, 32, " fa_rep_3", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 5, ComponentType.Leather, 2, 4, ComponentType.Fiberplast, 2, 4, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        ForceArmorRepairKitIII = 584,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Force Armor Repair Kit IV", true, 42, " fa_rep_4", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.Leather, 2, 4, ComponentType.Fiberplast, 2, 4, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        ForceArmorRepairKitIV = 585,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Heavy Armor Repair Kit I", true, 14, " ha_rep_1", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 1, ComponentType.Metal, 3, 6, ComponentType.Cloth, 3, 6, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        HeavyArmorRepairKitI = 586,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Heavy Armor Repair Kit II", true, 24, " ha_rep_2", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 3, ComponentType.Metal, 3, 6, ComponentType.Cloth, 3, 6, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        HeavyArmorRepairKitII = 587,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Heavy Armor Repair Kit III", true, 34, " ha_rep_3", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 5, ComponentType.Metal, 3, 6, ComponentType.Cloth, 3, 6, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        HeavyArmorRepairKitIII = 588,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Heavy Armor Repair Kit IV", true, 44, " ha_rep_4", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.Metal, 3, 6, ComponentType.Cloth, 3, 6, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        HeavyArmorRepairKitIV = 589,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Light Armor Repair Kit I", true, 10, " la_rep_1", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 1, ComponentType.Fiberplast, 2, 4, ComponentType.Leather, 2, 4, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        LightArmorRepairKitI = 590,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Light Armor Repair Kit II", true, 20, " la_rep_2", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 3, ComponentType.Fiberplast, 2, 4, ComponentType.Leather, 2, 4, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        LightArmorRepairKitII = 591,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Light Armor Repair Kit III", true, 30, " la_rep_3", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 5, ComponentType.Fiberplast, 2, 4, ComponentType.Leather, 2, 4, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        LightArmorRepairKitIII = 592,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Light Armor Repair Kit IV", true, 40, " la_rep_4", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.Fiberplast, 2, 4, ComponentType.Leather, 2, 4, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        LightArmorRepairKitIV = 593,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Shield Repair Kit I", true, 8, " sh_rep_1", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 1, ComponentType.Organic, 2, 4, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        ShieldRepairKitI = 594,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Shield Repair Kit II", true, 18, " sh_rep_2", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 3, ComponentType.Organic, 2, 4, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        ShieldRepairKitII = 595,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Shield Repair Kit III", true, 28, " sh_rep_3", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 5, ComponentType.Organic, 2, 4, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        ShieldRepairKitIII = 596,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Shield Repair Kit IV", true, 38, " sh_rep_4", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.Organic, 2, 4, ComponentType.Metal, 2, 4, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        ShieldRepairKitIV = 597,
        [CraftBlueprint(CraftBlueprintCategory.Silos, "Basic Resource Silo", true, 0, " silo", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.MediumStructureFrame, 1, 2, ComponentType.ConstructionParts, 2, 4, ComponentType.None, 0, 0, 0, BaseStructure.BasicResourceSilo)]
        BasicResourceSilo = 598,
        [CraftBlueprint(CraftBlueprintCategory.Silos, "Resource Silo I", true, 5, " silo", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.MediumStructureFrame, 1, 2, ComponentType.ConstructionParts, 2, 4, ComponentType.None, 0, 0, 1, BaseStructure.ResourceSiloI)]
        ResourceSiloI = 599,
        [CraftBlueprint(CraftBlueprintCategory.Silos, "Resource Silo II", true, 10, " silo", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 3, ComponentType.MediumStructureFrame, 1, 2, ComponentType.ConstructionParts, 2, 4, ComponentType.None, 0, 0, 2, BaseStructure.ResourceSiloII)]
        ResourceSiloII = 600,
        [CraftBlueprint(CraftBlueprintCategory.Silos, "Resource Silo III", true, 15, " silo", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 5, ComponentType.MediumStructureFrame, 1, 2, ComponentType.ConstructionParts, 2, 4, ComponentType.None, 0, 0, 3, BaseStructure.ResourceSiloIII)]
        ResourceSiloIII = 601,
        [CraftBlueprint(CraftBlueprintCategory.Silos, "Resource Silo IV", true, 20, " silo", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 7, ComponentType.MediumStructureFrame, 1, 2, ComponentType.ConstructionParts, 2, 4, ComponentType.None, 0, 0, 4, BaseStructure.ResourceSiloIV)]
        ResourceSiloIV = 602,
        [CraftBlueprint(CraftBlueprintCategory.Drills, "Basic Resource Drill", true, 0, " drill", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.LargeStructureFrame, 1, 2, ComponentType.ConstructionParts, 6, 12, ComponentType.None, 0, 0, 0, BaseStructure.BasicResourceDrill)]
        BasicResourceDrill = 603,
        [CraftBlueprint(CraftBlueprintCategory.Drills, "Resource Drill I", true, 5, " drill", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.LargeStructureFrame, 1, 2, ComponentType.ConstructionParts, 6, 12, ComponentType.None, 0, 0, 1, BaseStructure.ResourceDrillI)]
        ResourceDrillI = 604,
        [CraftBlueprint(CraftBlueprintCategory.Drills, "Resource Drill II", true, 10, " drill", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 3, ComponentType.LargeStructureFrame, 1, 2, ComponentType.ConstructionParts, 6, 12, ComponentType.None, 0, 0, 2, BaseStructure.ResourceDrillII)]
        ResourceDrillII = 605,
        [CraftBlueprint(CraftBlueprintCategory.Drills, "Resource Drill III", true, 15, " drill", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 5, ComponentType.LargeStructureFrame, 1, 2, ComponentType.ConstructionParts, 6, 12, ComponentType.None, 0, 0, 3, BaseStructure.ResourceDrillIII)]
        ResourceDrillIII = 606,
        [CraftBlueprint(CraftBlueprintCategory.Drills, "Resource Drill IV", true, 20, " drill", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 7, ComponentType.LargeStructureFrame, 1, 2, ComponentType.ConstructionParts, 6, 12, ComponentType.None, 0, 0, 4, BaseStructure.ResourceDrillIV)]
        ResourceDrillIV = 607,
        [CraftBlueprint(CraftBlueprintCategory.Harvesters, "Basic Mineral Scanner", true, 5, " scanner_m_b", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.None, 0, ComponentType.Electronics, 3, 6, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicMineralScanner = 608,
        [CraftBlueprint(CraftBlueprintCategory.CraftingDevices, "Fabrication Terminal", true, 0, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 2, ComponentType.Organic, 2, 2, ComponentType.None, 0, 0, 0, BaseStructure.FabricationTerminal)]
        FabricationTerminal = 609,
        [CraftBlueprint(CraftBlueprintCategory.CraftingDevices, "Medical Terminal", true, 0, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 1, ComponentType.Metal, 2, 2, ComponentType.Organic, 2, 2, ComponentType.None, 0, 0, 0, BaseStructure.MedicalTerminal)]
        MedicalTerminal = 610,
        [CraftBlueprint(CraftBlueprintCategory.Components, "Emitter", true, 1, " emitter", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 1, ComponentType.Electronics, 2, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        Emitter = 611,
        [CraftBlueprint(CraftBlueprintCategory.Lightsabers, "Basic Training Foil (Red)", true, 2, " lightsaber_r_b", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 1, ComponentType.Emitter, 1, 2, ComponentType.SaberHilt, 1, 2, ComponentType.RedCluster, 2, 3, 1, BaseStructure.Invalid)]
        BasicTrainingFoilRed = 612,
        [CraftBlueprint(CraftBlueprintCategory.Lightsabers, "Training Foil I (Red)", true, 7, " lightsaber_r_1", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 3, ComponentType.Emitter, 1, 2, ComponentType.SaberHilt, 1, 2, ComponentType.RedCluster, 2, 3, 2, BaseStructure.Invalid)]
        TrainingFoilIRed = 613,
        [CraftBlueprint(CraftBlueprintCategory.Lightsabers, "Training Foil II (Red)", true, 12, " lightsaber_r_2", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 5, ComponentType.Emitter, 1, 2, ComponentType.SaberHilt, 1, 2, ComponentType.RedCluster, 2, 3, 3, BaseStructure.Invalid)]
        TrainingFoilIIRed = 614,
        [CraftBlueprint(CraftBlueprintCategory.Lightsabers, "Training Foil III (Red)", true, 17, " lightsaber_r_3", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 7, ComponentType.Emitter, 1, 2, ComponentType.SaberHilt, 1, 2, ComponentType.RedCluster, 2, 3, 4, BaseStructure.Invalid)]
        TrainingFoilIIIRed = 615,
        [CraftBlueprint(CraftBlueprintCategory.Lightsabers, "Training Foil IV (Red)", true, 22, " lightsaber_r_4", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 9, ComponentType.Emitter, 1, 2, ComponentType.SaberHilt, 1, 2, ComponentType.RedCluster, 2, 3, 5, BaseStructure.Invalid)]
        TrainingFoilIVRed = 616,
        [CraftBlueprint(CraftBlueprintCategory.Saberstaffs, "Basic Training Foil Staff (Red)", true, 7, " saberstaff_r_b", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 1, ComponentType.Emitter, 2, 2, ComponentType.SaberHilt, 1, 2, ComponentType.RedCluster, 2, 3, 2, BaseStructure.Invalid)]
        BasicTrainingFoilStaffRed = 617,
        [CraftBlueprint(CraftBlueprintCategory.Saberstaffs, "Training Foil Staff I (Red)", true, 8, " saberstaff_r_1", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 3, ComponentType.Emitter, 2, 4, ComponentType.SaberHilt, 1, 2, ComponentType.RedCluster, 2, 3, 3, BaseStructure.Invalid)]
        TrainingFoilStaffIRed = 618,
        [CraftBlueprint(CraftBlueprintCategory.Saberstaffs, "Training Foil Staff II (Red)", true, 13, " saberstaff_r_2", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 5, ComponentType.Emitter, 2, 4, ComponentType.SaberHilt, 1, 2, ComponentType.RedCluster, 2, 3, 4, BaseStructure.Invalid)]
        TrainingFoilStaffIIRed = 619,
        [CraftBlueprint(CraftBlueprintCategory.Saberstaffs, "Training Foil Staff III (Red)", true, 18, " saberstaff_r_3", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 7, ComponentType.Emitter, 2, 4, ComponentType.SaberHilt, 1, 2, ComponentType.RedCluster, 2, 3, 5, BaseStructure.Invalid)]
        TrainingFoilStaffIIIRed = 620,
        [CraftBlueprint(CraftBlueprintCategory.Saberstaffs, "Training Foil Staff IV (Red)", true, 23, " saberstaff_r_4", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 9, ComponentType.Emitter, 2, 4, ComponentType.SaberHilt, 1, 2, ComponentType.RedCluster, 2, 3, 6, BaseStructure.Invalid)]
        TrainingFoilStaffIVRed = 621,
        [CraftBlueprint(CraftBlueprintCategory.Lightsabers, "Basic Training Foil (Green)", true, 2, " lightsaber_g_b", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 1, ComponentType.Emitter, 1, 2, ComponentType.SaberHilt, 1, 2, ComponentType.GreenCluster, 2, 3, 1, BaseStructure.Invalid)]
        BasicTrainingFoilGreen = 622,
        [CraftBlueprint(CraftBlueprintCategory.Lightsabers, "Training Foil I (Green)", true, 7, " lightsaber_g_1", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 3, ComponentType.Emitter, 1, 2, ComponentType.SaberHilt, 1, 2, ComponentType.GreenCluster, 2, 3, 2, BaseStructure.Invalid)]
        TrainingFoilIGreen = 623,
        [CraftBlueprint(CraftBlueprintCategory.Lightsabers, "Training Foil II (Green)", true, 12, " lightsaber_g_2", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 5, ComponentType.Emitter, 1, 2, ComponentType.SaberHilt, 1, 2, ComponentType.GreenCluster, 2, 3, 3, BaseStructure.Invalid)]
        TrainingFoilIIGreen = 624,
        [CraftBlueprint(CraftBlueprintCategory.Lightsabers, "Training Foil III (Green)", true, 17, " lightsaber_g_3", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 7, ComponentType.Emitter, 1, 2, ComponentType.SaberHilt, 1, 2, ComponentType.GreenCluster, 2, 3, 4, BaseStructure.Invalid)]
        TrainingFoilIIIGreen = 625,
        [CraftBlueprint(CraftBlueprintCategory.Lightsabers, "Training Foil IV (Green)", true, 22, " lightsaber_g_4", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 9, ComponentType.Emitter, 1, 2, ComponentType.SaberHilt, 1, 2, ComponentType.GreenCluster, 2, 3, 5, BaseStructure.Invalid)]
        TrainingFoilIVGreen = 626,
        [CraftBlueprint(CraftBlueprintCategory.Saberstaffs, "Basic Training Foil Staff (Green)", true, 7, " saberstaff_g_b", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 1, ComponentType.Emitter, 2, 2, ComponentType.SaberHilt, 1, 2, ComponentType.GreenCluster, 2, 3, 2, BaseStructure.Invalid)]
        BasicTrainingFoilStaffGreen = 627,
        [CraftBlueprint(CraftBlueprintCategory.Saberstaffs, "Training Foil Staff I (Green)", true, 8, " saberstaff_g_1", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 3, ComponentType.Emitter, 2, 4, ComponentType.SaberHilt, 1, 2, ComponentType.GreenCluster, 2, 3, 3, BaseStructure.Invalid)]
        TrainingFoilStaffIGreen = 628,
        [CraftBlueprint(CraftBlueprintCategory.Saberstaffs, "Training Foil Staff II (Green)", true, 13, " saberstaff_g_2", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 5, ComponentType.Emitter, 2, 4, ComponentType.SaberHilt, 1, 2, ComponentType.GreenCluster, 2, 3, 4, BaseStructure.Invalid)]
        TrainingFoilStaffIIGreen = 629,
        [CraftBlueprint(CraftBlueprintCategory.Saberstaffs, "Training Foil Staff III (Green)", true, 18, " saberstaff_g_3", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 7, ComponentType.Emitter, 2, 4, ComponentType.SaberHilt, 1, 2, ComponentType.GreenCluster, 2, 3, 5, BaseStructure.Invalid)]
        TrainingFoilStaffIIIGreen = 630,
        [CraftBlueprint(CraftBlueprintCategory.Saberstaffs, "Training Foil Staff IV (Green)", true, 23, " saberstaff_g_4", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 9, ComponentType.Emitter, 2, 4, ComponentType.SaberHilt, 1, 2, ComponentType.GreenCluster, 2, 3, 6, BaseStructure.Invalid)]
        TrainingFoilStaffIVGreen = 631,
        [CraftBlueprint(CraftBlueprintCategory.Lightsabers, "Basic Training Foil (Yellow)", true, 2, " lightsaber_y_b", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 1, ComponentType.Emitter, 1, 2, ComponentType.SaberHilt, 1, 2, ComponentType.YellowCluster, 2, 3, 1, BaseStructure.Invalid)]
        BasicTrainingFoilYellow = 632,
        [CraftBlueprint(CraftBlueprintCategory.Lightsabers, "Training Foil I (Yellow)", true, 7, " lightsaber_y_1", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 3, ComponentType.Emitter, 1, 2, ComponentType.SaberHilt, 1, 2, ComponentType.YellowCluster, 2, 3, 2, BaseStructure.Invalid)]
        TrainingFoilIYellow = 633,
        [CraftBlueprint(CraftBlueprintCategory.Lightsabers, "Training Foil II (Yellow)", true, 12, " lightsaber_y_2", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 5, ComponentType.Emitter, 1, 2, ComponentType.SaberHilt, 1, 2, ComponentType.YellowCluster, 2, 3, 3, BaseStructure.Invalid)]
        TrainingFoilIIYellow = 634,
        [CraftBlueprint(CraftBlueprintCategory.Lightsabers, "Training Foil III (Yellow)", true, 17, " lightsaber_y_3", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 7, ComponentType.Emitter, 1, 2, ComponentType.SaberHilt, 1, 2, ComponentType.YellowCluster, 2, 3, 4, BaseStructure.Invalid)]
        TrainingFoilIIIYellow = 635,
        [CraftBlueprint(CraftBlueprintCategory.Lightsabers, "Training Foil IV (Yellow)", true, 22, " lightsaber_y_4", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 9, ComponentType.Emitter, 1, 2, ComponentType.SaberHilt, 1, 2, ComponentType.YellowCluster, 2, 3, 5, BaseStructure.Invalid)]
        TrainingFoilIVYellow = 636,
        [CraftBlueprint(CraftBlueprintCategory.Saberstaffs, "Basic Training Foil Staff (Yellow)", true, 7, " saberstaff_y_b", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 1, ComponentType.Emitter, 2, 2, ComponentType.SaberHilt, 1, 2, ComponentType.YellowCluster, 2, 3, 2, BaseStructure.Invalid)]
        BasicTrainingFoilStaffYellow = 637,
        [CraftBlueprint(CraftBlueprintCategory.Saberstaffs, "Training Foil Staff I (Yellow)", true, 8, " saberstaff_y_1", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 3, ComponentType.Emitter, 2, 4, ComponentType.SaberHilt, 1, 2, ComponentType.YellowCluster, 2, 3, 3, BaseStructure.Invalid)]
        TrainingFoilStaffIYellow = 638,
        [CraftBlueprint(CraftBlueprintCategory.Saberstaffs, "Training Foil Staff II (Yellow)", true, 13, " saberstaff_y_2", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 5, ComponentType.Emitter, 2, 4, ComponentType.SaberHilt, 1, 2, ComponentType.YellowCluster, 2, 3, 4, BaseStructure.Invalid)]
        TrainingFoilStaffIIYellow = 639,
        [CraftBlueprint(CraftBlueprintCategory.Saberstaffs, "Training Foil Staff III (Yellow)", true, 18, " saberstaff_y_3", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 7, ComponentType.Emitter, 2, 4, ComponentType.SaberHilt, 1, 2, ComponentType.YellowCluster, 2, 3, 5, BaseStructure.Invalid)]
        TrainingFoilStaffIIIYellow = 640,
        [CraftBlueprint(CraftBlueprintCategory.Saberstaffs, "Training Foil Staff IV (Yellow)", true, 23, " saberstaff_y_4", 1, Skill.Lightsaber, CraftDeviceType.EngineeringBench, PerkType.LightsaberBlueprints, 9, ComponentType.Emitter, 2, 4, ComponentType.SaberHilt, 1, 2, ComponentType.YellowCluster, 2, 3, 6, BaseStructure.Invalid)]
        TrainingFoilStaffIVYellow = 641,
        [CraftBlueprint(CraftBlueprintCategory.FurnitureFlooring, "Wookiee Rug", true, 10, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 3, ComponentType.Leather, 1, 1, ComponentType.Fiberplast, 2, 2, ComponentType.None, 0, 0, 1, BaseStructure.WookieeRug)]
        WookieeRug = 642,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Basic Force Helmet", true, 1, " helmet_fb", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 0, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicForceHelmet = 643,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Force Helmet I", true, 5, " helmet_f1", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 1, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        ForceHelmetI = 644,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Force Helmet II", true, 9, " helmet_f2", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 3, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        ForceHelmetII = 645,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Force Helmet III", true, 14, " helmet_f3", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 5, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        ForceHelmetIII = 646,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Force Helmet IV", true, 19, " helmet_f4", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        ForceHelmetIV = 647,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Basic Heavy Helmet", true, 2, " helmet_hb", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 0, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicHeavyHelmet = 649,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Heavy Helmet I", true, 7, " helmet_h1", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 1, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        HeavyHelmetI = 650,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Heavy Helmet II", true, 12, " helmet_h2", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 3, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        HeavyHelmetII = 651,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Heavy Helmet III", true, 17, " helmet_h3", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 3, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        HeavyHelmetIII = 652,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Heavy Helmet IV", true, 23, " helmet_h4", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        HeavyHelmetIV = 653,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Basic Light Helmet", true, 0, " helmet_lb", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 0, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicLightHelmet = 654,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Light Helmet I", true, 5, " helmet_l1", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 1, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        LightHelmetI = 655,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Light Helmet II", true, 10, " helmet_l2", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 3, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        LightHelmetII = 656,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Light Helmet III", true, 15, " helmet_l3", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 5, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        LightHelmetIII = 657,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Light Helmet IV", true, 20, " helmet_l4", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        LightHelmetIV = 658,
        [CraftBlueprint(CraftBlueprintCategory.StarshipProduction, "Hyperdrive", true, 8, " hyperdrive", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.ComputingModule, 3, 3, ComponentType.Electronics, 4, 12, ComponentType.PowerRelay, 2, 2, 4, BaseStructure.Invalid)]
        Hyperdrive = 659,
        [CraftBlueprint(CraftBlueprintCategory.StarshipProduction, "Hull Plating", true, 4, " hull_plating", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.Metal, 4, 12, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        HullPlating = 660,
        [CraftBlueprint(CraftBlueprintCategory.StarshipProduction, "Light Starship Blaster", true, 0, " ship_blaster_1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.RifleBarrel, 2, 6, ComponentType.PowerCluster, 4, 8, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        LightStarshipBlaster = 661,
        [CraftBlueprint(CraftBlueprintCategory.StarshipProduction, "Starship Dock", true, 10, " silo", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 5, ComponentType.LargeStructureFrame, 1, 2, ComponentType.ConstructionParts, 6, 12, ComponentType.PowerRelay, 1, 2, 4, BaseStructure.StarshipDock)]
        StarshipDock = 662,
        [CraftBlueprint(CraftBlueprintCategory.StarshipProduction, "Starship 1 (Light Transport 1)", true, 0, " starship", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 7, ComponentType.Hyperdrive, 1, 1, ComponentType.HullPlating, 4, 6, ComponentType.StarshipWeapon, 1, 1, 4, BaseStructure.StarshipLightFreighter1)]
        Starship1LightTransport1 = 663,
        [CraftBlueprint(CraftBlueprintCategory.StarshipProduction, "Starship 2 (Light Escort 1)", true, 0, " starship", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 7, ComponentType.Hyperdrive, 1, 1, ComponentType.HullPlating, 2, 6, ComponentType.StarshipWeapon, 4, 5, 4, BaseStructure.StarshipLightEscort1)]
        Starship2LightEscort1 = 664,
        [CraftBlueprint(CraftBlueprintCategory.RepairKits, "Starship Repair Kit", true, 30, " ss_rep", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 5, ComponentType.Metal, 3, 6, ComponentType.ComputingModule, 1, 4, ComponentType.PowerCluster, 1, 3, 0, BaseStructure.Invalid)]
        StarshipRepairKit = 665,
        [CraftBlueprint(CraftBlueprintCategory.StarshipProduction, "Starship Auxiliary Blaster", true, 25, " sswpn1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.RifleBarrel, 2, 4, ComponentType.PowerCluster, 2, 4, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        StarshipAuxiliaryBlaster = 666,
        [CraftBlueprint(CraftBlueprintCategory.StarshipProduction, "Starship Auxiliary Light Cannon", true, 46, " sswpn2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.RifleBarrel, 4, 8, ComponentType.PowerCluster, 4, 8, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        StarshipAuxiliaryLightCannon = 667,
        [CraftBlueprint(CraftBlueprintCategory.StarshipProduction, "Auxiliary Shield Generator (Small)", true, 23, " ssshld1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.Electronics, 4, 6, ComponentType.PowerRelay, 2, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        AuxiliaryShieldGeneratorSmall = 668,
        [CraftBlueprint(CraftBlueprintCategory.StarshipProduction, "Auxiliary Shield Generator (Medium)", true, 45, " ssshld2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.Electronics, 4, 6, ComponentType.PowerRelay, 2, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        AuxiliaryShieldGeneratorMedium = 669,
        [CraftBlueprint(CraftBlueprintCategory.StarshipProduction, "Auxiliary Thruster (Small)", true, 20, " ssspd1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.PowerCluster, 3, 4, ComponentType.PowerRelay, 2, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        AuxiliaryThrusterSmall = 670,
        [CraftBlueprint(CraftBlueprintCategory.StarshipProduction, "Auxiliary Thruster (Medium)", true, 38, " ssspd2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.PowerCluster, 5, 8, ComponentType.PowerRelay, 4, 4, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        AuxiliaryThrusterMedium = 671,
        [CraftBlueprint(CraftBlueprintCategory.StarshipProduction, "Auxiliary Targeter (Basic)", true, 25, " ssrang1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.ComputingModule, 3, 4, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        AuxiliaryTargeterBasic = 672,
        [CraftBlueprint(CraftBlueprintCategory.StarshipProduction, "Auxiliary Targeter (Improved)", true, 42, " ssrang2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.ComputingModule, 5, 8, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        AuxiliaryTargeterImproved = 673,
        [CraftBlueprint(CraftBlueprintCategory.StarshipProduction, "Additional Fuel Tank (Small)", true, 20, " ssfuel1", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 5, ComponentType.Metal, 2, 4, ComponentType.SmallStructureFrame, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        AdditionalFuelTankSmall = 674,
        [CraftBlueprint(CraftBlueprintCategory.StarshipProduction, "Additional Fuel Tank (Medium)", true, 40, " ssfuel2", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.Metal, 2, 4, ComponentType.MediumStructureFrame, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        AdditionalFuelTankMedium = 675,
        [CraftBlueprint(CraftBlueprintCategory.StarshipProduction, "Additional Stronidium Tank (Small)", true, 20, " ssstron1", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 5, ComponentType.ConstructionParts, 1, 2, ComponentType.SmallStructureFrame, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        AdditionalStronidiumTankSmall = 676,
        [CraftBlueprint(CraftBlueprintCategory.StarshipProduction, "Additional Stronidium Tank (Medium)", true, 40, " ssstron2", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.ConstructionParts, 2, 4, ComponentType.MediumStructureFrame, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        AdditionalStronidiumTankMedium = 677,
        [CraftBlueprint(CraftBlueprintCategory.StarshipProduction, "Cloaking Generator (Small)", true, 20, " ssstlth1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.Electronics, 3, 4, ComponentType.PowerRelay, 2, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        CloakingGeneratorSmall = 678,
        [CraftBlueprint(CraftBlueprintCategory.StarshipProduction, "Cloaking Generator (Medium)", true, 38, " ssstlth2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.Electronics, 5, 8, ComponentType.PowerRelay, 4, 4, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        CloakingGeneratorMedium = 679,
        [CraftBlueprint(CraftBlueprintCategory.StarshipProduction, "Scanning Array (Small)", true, 24, " ssscan1", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 5, ComponentType.Electronics, 3, 4, ComponentType.ComputingModule, 2, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        ScanningArraySmall = 680,
        [CraftBlueprint(CraftBlueprintCategory.StarshipProduction, "Scanning Array (Medium)", true, 40, " ssscan2", 1, Skill.Engineering, CraftDeviceType.EngineeringBench, PerkType.EngineeringBlueprints, 7, ComponentType.Electronics, 5, 8, ComponentType.ComputingModule, 4, 4, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        ScanningArrayMedium = 681,
        [CraftBlueprint(CraftBlueprintCategory.SpecialFurniture, "Jukebox", true, 7, " furniture", 1, Skill.Fabrication, CraftDeviceType.FabricationTerminal, PerkType.FabricationBlueprints, 3, ComponentType.ConstructionParts, 2, 2, ComponentType.None, 0, 0, ComponentType.None, 0, 0, 0, BaseStructure.Jukebox)]
        Jukebox = 682,
        [CraftBlueprint(CraftBlueprintCategory.Throwing, "Basic Throwing Axe", true, 2, " axe_b", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.SmallBlade, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        BasicThrowingAxe = 683,
        [CraftBlueprint(CraftBlueprintCategory.Throwing, "Throwing Axe I", true, 5, " axe_1", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.SmallBlade, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        ThrowingAxeI = 684,
        [CraftBlueprint(CraftBlueprintCategory.Throwing, "Throwing Axe II", true, 10, " axe_2", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.SmallBlade, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        ThrowingAxeII = 685,
        [CraftBlueprint(CraftBlueprintCategory.Throwing, "Throwing Axe III", true, 15, " axe_3", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.SmallBlade, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        ThrowingAxeIII = 686,
        [CraftBlueprint(CraftBlueprintCategory.Throwing, "Throwing Axe IV", true, 20, " axe_4", 1, Skill.Weaponsmith, CraftDeviceType.WeaponsmithBench, PerkType.None, 0, ComponentType.SmallBlade, 1, 2, ComponentType.SmallHandle, 1, 2, ComponentType.None, 0, 0, 5, BaseStructure.Invalid)]
        ThrowingAxeIV = 687,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Basic Light Bracer", true, 2, " bracer_light_b", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.None, 0, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        BasicLightBracer = 688,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Light Bracer I", true, 6, " bracer_light_1", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 1, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        LightBracerI = 689,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Light Bracer II", true, 10, " bracer_light_2", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 3, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        LightBracerII = 690,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Light Bracer III", true, 14, " bracer_light_3", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 5, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        LightBracerIII = 691,
        [CraftBlueprint(CraftBlueprintCategory.LightArmor, "Light Bracer IV", true, 18, " bracer_light_4", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.LightArmorCore, 1, 2, ComponentType.LightArmorSegment, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        LightBracerIV = 692,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Basic Force Bracer", true, 3, " bracer_force_b", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.None, 0, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        BasicForceBracer = 693,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Force Bracer I", true, 9, " bracer_force_1", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 1, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        ForceBracerI = 694,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Force Bracer II", true, 11, " bracer_force_2", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 3, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        ForceBracerII = 695,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Force Bracer III", true, 15, " bracer_force_3", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 5, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        ForceBracerIII = 696,
        [CraftBlueprint(CraftBlueprintCategory.ForceArmor, "Force Bracer IV", true, 19, " bracer_force_4", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.ForceArmorCore, 1, 2, ComponentType.ForceArmorSegment, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        ForceBracerIV = 697,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Basic Heavy Bracer", true, 6, " bracer_heavy_b", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.None, 0, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 0, BaseStructure.Invalid)]
        BasicHeavyBracer = 698,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Heavy Bracer I", true, 8, " bracer_heavy_1", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 1, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 1, BaseStructure.Invalid)]
        HeavyBracerI = 699,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Heavy Bracer II", true, 12, " bracer_heavy_2", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 3, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 2, BaseStructure.Invalid)]
        HeavyBracerII = 700,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Heavy Bracer III", true, 16, " bracer_heavy_3", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 5, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 3, BaseStructure.Invalid)]
        HeavyBracerIII = 701,
        [CraftBlueprint(CraftBlueprintCategory.HeavyArmor, "Heavy Bracer IV", true, 20, " bracer_heavy_4", 1, Skill.Armorsmith, CraftDeviceType.ArmorsmithBench, PerkType.ArmorBlueprints, 7, ComponentType.HeavyArmorCore, 1, 2, ComponentType.HeavyArmorSegment, 1, 2, ComponentType.None, 0, 0, 4, BaseStructure.Invalid)]
        HeavyBracerIV = 702,

    }

    public class CraftBlueprintAttribute : Attribute
    {
        public CraftBlueprintCategory Category { get; set; }
        public string ItemName { get; set; }
        public bool IsActive { get; set; }
        public int BaseLevel { get; set; }
        public string Resref { get; set; }
        public int Quantity { get; set; }
        public Skill Skill { get; set; }
        public CraftDeviceType CraftDevice { get; set; }
        public PerkType Perk { get; set; }
        public int RequiredPerkLevel { get; set; }
        public ComponentType MainComponentType { get; set; }
        public int MainComponentMinimum { get; set; }
        public int MainComponentMaximum { get; set; }
        public ComponentType SecondaryComponentType { get; set; }
        public int SecondaryComponentMinimum { get; set; }
        public int SecondaryComponentMaximum { get; set; }
        public ComponentType TertiaryComponentType { get; set; }
        public int TertiaryComponentMinimum { get; set; }
        public int TertiaryComponentMaximum { get; set; }
        public int EnhancementSlots { get; set; }
        public BaseStructure BaseStructureID { get; set; }

        public CraftBlueprintAttribute(
                    CraftBlueprintCategory category,
                    string itemName,
                    bool isActive,
                    int baseLevel,
                    string resref,
                    int quantity,
                    Skill skill,
                    CraftDeviceType craftDevice,
                    PerkType perk,
                    int requiredPerkLevel,
                    ComponentType mainComponentType,
                    int mainComponentMinimum,
                    int mainComponentMaximum,
                    ComponentType secondaryComponentType,
                    int secondaryComponentMinimum,
                    int secondaryComponentMaximum,
                    ComponentType tertiaryComponentType,
                    int tertiaryComponentMinimum,
                    int tertiaryComponentMaximum,
                    int enhancementSlots,
                    BaseStructure baseStructureId)
        {
            Category = category;
            ItemName = itemName;
            IsActive = isActive;
            BaseLevel = baseLevel;
            Resref = resref;
            Quantity = quantity;
            Skill = skill;
            CraftDevice = craftDevice;
            Perk = perk;
            RequiredPerkLevel = requiredPerkLevel;
            MainComponentType = mainComponentType;
            MainComponentMinimum = mainComponentMinimum;
            MainComponentMaximum = mainComponentMaximum;
            SecondaryComponentType = secondaryComponentType;
            SecondaryComponentMinimum = secondaryComponentMinimum;
            SecondaryComponentMaximum = secondaryComponentMaximum;
            TertiaryComponentType = tertiaryComponentType;
            TertiaryComponentMinimum = tertiaryComponentMinimum;
            TertiaryComponentMaximum = tertiaryComponentMaximum;
            EnhancementSlots = enhancementSlots;
            BaseStructureID = baseStructureId;
        }
    }
}
