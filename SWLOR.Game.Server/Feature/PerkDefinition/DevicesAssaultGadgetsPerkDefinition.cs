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
            ArcProjector();
            IonLance();
            RailDart();
            TacticalUplink();
            CryoSprayer();
            OverloadBarrage();

            return _builder.Build();
        }

        private void Flamethrower()
        {
            _builder.Create(PerkCategoryType.DevicesAssaultGadgets, PerkType.Flamethrower)
                .Name("Flamethrower")

                .AddPerkLevel()
                .Description("Deals 16 fire DMG plus PER scaling to hostile targets in a cone.")
                .Price(2)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.Flamethrower1)

                .AddPerkLevel()
                .Description("Deals 28 fire DMG plus PER scaling to hostile targets in a cone and attempts to inflict Burning.")
                .Price(2)
                .RequirementSkill(SkillType.Devices, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.Flamethrower2)

                .AddPerkLevel()
                .Description("Deals 42 fire DMG plus PER scaling to hostile targets in a cone and attempts to inflict Burning.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(3)
                .GrantsFeat(FeatType.Flamethrower3);
        }

        private void WristRocket()
        {
            _builder.Create(PerkCategoryType.DevicesAssaultGadgets, PerkType.WristRocket)
                .Name("Wrist Rocket")

                .AddPerkLevel()
                .Description("Deals 20 fire DMG plus PER scaling to one target.")
                .Price(2)
                .RequirementSkill(SkillType.Devices, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.WristRocket1)

                .AddPerkLevel()
                .Description("Deals 34 fire DMG plus PER scaling to one target and knock down for 2 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.WristRocket2)

                .AddPerkLevel()
                .Description("Deals 48 fire DMG plus PER scaling to one target and knock down for 3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(3)
                .GrantsFeat(FeatType.WristRocket3);
        }

        private void SonicBurst()
        {
            _builder.Create(PerkCategoryType.DevicesAssaultGadgets, PerkType.SonicBurst)
                .Name("Sonic Burst")

                .AddPerkLevel()
                .Description("Deals 10 sonic DMG to nearby hostile targets and interrupts activation.")
                .Price(2)
                .RequirementSkill(SkillType.Devices, 8)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.SonicBurst1)

                .AddPerkLevel()
                .Description("Deals 14 sonic DMG to nearby hostile targets, interrupts activation, and reduces Accuracy by 6% for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 28)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.SonicBurst2)

                .AddPerkLevel()
                .Description("Deals 18 sonic DMG to nearby hostile targets, interrupts activation, and reduces Accuracy by 10% for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 42)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(3)
                .GrantsFeat(FeatType.SonicBurst3);
        }

        private void GadgetHarness()
        {
            _builder.Create(PerkCategoryType.DevicesAssaultGadgets, PerkType.GadgetHarness)
                .Name("Gadget Harness")

                .AddPerkLevel()
                .GrantsFeat(FeatType.GadgetHarnessTrait)
                .Description("Assault Gadget abilities gain +8% Accuracy and +8% critical chance.")
                .Price(2)
                .RequirementSkill(SkillType.Devices, 12)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.AssaultGadgetAccuracyPercentAdjustment, 8)
                .IncreasesStat(StatType.AssaultGadgetCriticalRatePercentAdjustment, 8);
        }

        private void ArcProjector()
        {
            _builder.Create(PerkCategoryType.DevicesAssaultGadgets, PerkType.ArcProjector)
                .Name("Arc Projector")

                .AddPerkLevel()
                .Description("Projects a focused electrical arc up to 15m, dealing 18 electrical DMG plus PER scaling to one target.")
                .Price(2)
                .RequirementSkill(SkillType.Devices, 12)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.ArcProjector1)

                .AddPerkLevel()
                .Description("Projects a stronger electrical arc up to 15m, dealing 32 electrical DMG plus PER scaling to one target.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.ArcProjector2)

                .AddPerkLevel()
                .Description("Projects an overcharged electrical arc up to 15m, dealing 46 electrical DMG plus PER scaling to one target.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(3)
                .GrantsFeat(FeatType.ArcProjector3);
        }

        private void IonLance()
        {
            _builder.Create(PerkCategoryType.DevicesAssaultGadgets, PerkType.IonLance)
                .Name("Ion Lance")

                .AddPerkLevel()
                .Description("Fires a focused ion beam from a wrist projector in an 8m line, dealing 12 electrical DMG plus PER scaling to hostile targets in the line.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.IonLance1)

                .AddPerkLevel()
                .Description("Fires a focused ion beam from a wrist projector in an 8m line, dealing 22 electrical DMG plus PER scaling to hostile targets in the line.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 32)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.IonLance2)

                .AddPerkLevel()
                .Description("Fires a focused ion beam from a wrist projector in an 8m line, dealing 32 electrical DMG plus PER scaling to hostile targets in the line.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 48)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(3)
                .GrantsFeat(FeatType.IonLance3);
        }

        private void RailDart()
        {
            _builder.Create(PerkCategoryType.DevicesAssaultGadgets, PerkType.RailDart)
                .Name("Rail Dart")

                .AddPerkLevel()
                .Description("Fires a dart that deals 18 physical DMG plus PER scaling and attempts to inflict Bleed.")
                .Price(2)
                .RequirementSkill(SkillType.Devices, 18)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.RailDart1)

                .AddPerkLevel()
                .Description("Fires a dart that deals 34 physical DMG plus PER scaling and attempts to inflict Bleed.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 38)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.RailDart2);
        }

        private void TacticalUplink()
        {
            _builder.Create(PerkCategoryType.DevicesAssaultGadgets, PerkType.TacticalUplink)
                .Name("Tactical Uplink")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TacticalUplinkTrait)
                .Description("After an Assault Gadget ability damages an enemy, you and nearby allies gain Tactical Uplink for 10 seconds: +5% Device ability Accuracy and +5% Device critical chance.")
                .Price(2)
                .RequirementSkill(SkillType.Devices, 22)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.AssaultGadgetTacticalUplink, 1);
        }

        private void CryoSprayer()
        {
            _builder.Create(PerkCategoryType.DevicesAssaultGadgets, PerkType.CryoSprayer)
                .Name("Cryo Sprayer")

                .AddPerkLevel()
                .Description("Deals 22 ice DMG plus PER scaling to hostile targets in a cone and slows movement for 5 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.CryoSprayer1);
        }

        private void OverloadBarrage()
        {
            _builder.Create(PerkCategoryType.DevicesAssaultGadgets, PerkType.OverloadBarrage)
                .Name("Overload Barrage")

                .AddPerkLevel()
                .Description("Unleashes three attacks at your primary target's location: a 42 fire DMG burst plus Burning for 45 seconds, a 48 fire DMG single-target hit plus brief Knockdown, and a 24 sonic DMG burst that interrupts activation and reduces Accuracy by 10% for 45 seconds.")
                .Price(5)
                .RequirementSkill(SkillType.Devices, 50)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.OverloadBarrage1);
        }

    }
}
