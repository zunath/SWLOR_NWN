using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public sealed class DevicesAssaultGadgetsPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Flamethrower();
            WristRocket();
            SonicBurst();
            GadgetHarness();
            RailDart();
            CryoSprayer();
            OverloadBarrage();

            return _builder.Build();
        }

        private void Flamethrower()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.Flamethrower)
                .Name("Flamethrower")

                .AddPerkLevel()
                .Description("Deals fire DMG plus PER scaling to hostile targets in a cone.")
                .Price(2)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Flamethrower1)

                .AddPerkLevel()
                .Description("Deals increased fire DMG plus PER scaling to hostile targets in a cone and attempts to inflict Burning.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Flamethrower2)

                .AddPerkLevel()
                .Description("Deals high fire DMG plus PER scaling to hostile targets in a cone and attempts to inflict Burning.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Flamethrower3);
        }

        private void WristRocket()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.WristRocket)
                .Name("Wrist Rocket")

                .AddPerkLevel()
                .Description("Deals fire DMG plus PER scaling to one target.")
                .Price(2)
                .RequirementSkill(SkillType.Devices, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.WristRocket1)

                .AddPerkLevel()
                .Description("Deals increased fire DMG plus PER scaling to one target and knock down for 2 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.WristRocket2)

                .AddPerkLevel()
                .Description("Deals high fire DMG plus PER scaling to one target and knock down for 3 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.WristRocket3);
        }

        private void SonicBurst()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.SonicBurst)
                .Name("Sonic Burst")

                .AddPerkLevel()
                .Description("Deals sonic DMG to nearby hostile targets and interrupt activation.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 8)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.SonicBurst1)

                .AddPerkLevel()
                .Description("Deals sonic DMG to nearby hostile targets and interrupt activation and reduce Accuracy.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 28)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.SonicBurst2)

                .AddPerkLevel()
                .Description("Deals sonic DMG to nearby hostile targets and interrupt activation and reduce Accuracy.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 42)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.SonicBurst3);
        }

        private void GadgetHarness()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.GadgetHarness)
                .Name("Gadget Harness")

                .AddPerkLevel()
                .Description("Assault Gadget abilities gain +5% Accuracy and +5% critical chance.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 12)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.AssaultGadgetAccuracyPercentAdjustment, 5)
                .IncreasesStat(StatType.AssaultGadgetCriticalRatePercentAdjustment, 5)

                .AddPerkLevel()
                .Description("Assault Gadget abilities gain +10% Accuracy and +10% critical chance.")
                .Price(2)
                .RequirementSkill(SkillType.Devices, 22)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.AssaultGadgetAccuracyPercentAdjustment, 10)
                .IncreasesStat(StatType.AssaultGadgetCriticalRatePercentAdjustment, 10)

                .AddPerkLevel()
                .Description("Assault Gadget abilities gain +15% Accuracy, +15% critical chance, and +10% damage.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.AssaultGadgetAccuracyPercentAdjustment, 15)
                .IncreasesStat(StatType.AssaultGadgetCriticalRatePercentAdjustment, 15)
                .IncreasesStat(StatType.AssaultGadgetDamagePercentAdjustment, 10);
        }

        private void RailDart()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.RailDart)
                .Name("Rail Dart")

                .AddPerkLevel()
                .Description("Fires a dart that deals physical DMG plus PER scaling and attempts to inflict Bleed.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 18)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.RailDart1)

                .AddPerkLevel()
                .Description("Fires a dart that deals high physical DMG plus PER scaling and attempts to inflict Bleed.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 38)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.RailDart2);
        }

        private void CryoSprayer()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.CryoSprayer)
                .Name("Cryo Sprayer")

                .AddPerkLevel()
                .Description("Deals ice DMG plus PER scaling to hostile targets in a cone and slows movement for 5 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.CryoSprayer1)

                .AddPerkLevel()
                .Description("Deals high ice DMG plus PER scaling to hostile targets in a cone and immobilize for 2 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 48)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.CryoSprayer2);
        }

        private void OverloadBarrage()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.OverloadBarrage)
                .Name("Overload Barrage")

                .AddPerkLevel()
                .Description("Unleashes a flamethrower burst, wrist rocket, and sonic burst against hostile targets near your primary target.")
                .Price(5)
                .RequirementSkill(SkillType.Devices, 50)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.OverloadBarrage1);
        }

    }
}
