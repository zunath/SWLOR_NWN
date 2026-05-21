using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public sealed class DevicesFieldSupportPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            DeflectorShield();
            CapacitorRig();
            WeaponJam();
            PowerCell();
            RayshieldScreen();
            DampeningField();
            GroupDeflector();
            EmergencyBunker();

            return _builder.Build();
        }

        private void DeflectorShield()
        {
            _builder.Create(PerkCategoryType.DevicesFieldSupport, PerkType.DeflectorShield)
                .Name("Deflector Shield")

                .AddPerkLevel()
                .Description("Grants one ally 35 temporary HP plus 6% of the target's maximum HP for 45 seconds.")
                .Price(2)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.DeflectorShield1)

                .AddPerkLevel()
                .Description("Grants one ally 65 temporary HP plus 9% of the target's maximum HP for 45 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.DeflectorShield2)

                .AddPerkLevel()
                .Description("Grants one ally 100 temporary HP plus 12% of the target's maximum HP for 45 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.DeflectorShield3);
        }

        private void CapacitorRig()
        {
            _builder.Create(PerkCategoryType.DevicesFieldSupport, PerkType.CapacitorRig)
                .Name("Capacitor Rig")

                .AddPerkLevel()
                .Description("Deflector Shield, Group Deflector, and Emergency Bunker grant 10% more temporary HP.")
                .Price(2)
                .RequirementSkill(SkillType.Devices, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.DeviceShieldTemporaryHPPercentAdjustment, 10)

                .AddPerkLevel()
                .Description("Deflector Shield, Group Deflector, and Emergency Bunker grant 20% more temporary HP.")
                .Price(2)
                .RequirementSkill(SkillType.Devices, 22)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.DeviceShieldTemporaryHPPercentAdjustment, 20)

                .AddPerkLevel()
                .Description("Deflector Shield, Group Deflector, and Emergency Bunker grant 30% more temporary HP. Deflector Shield and Group Deflector last 10 seconds longer.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.DeviceShieldTemporaryHPPercentAdjustment, 30)
                .IncreasesStat(StatType.DeviceShieldDurationBonusSeconds, 10);
        }

        private void WeaponJam()
        {
            _builder.Create(PerkCategoryType.DevicesFieldSupport, PerkType.WeaponJam)
                .Name("Weapon Jam")

                .AddPerkLevel()
                .Description("Reduce one target's physical and Force ability Accuracy by 6% for 18 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 8)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.WeaponJam1)

                .AddPerkLevel()
                .Description("Reduce one target's physical and Force ability Accuracy by 10% for 18 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 28)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.WeaponJam2);
        }

        private void PowerCell()
        {
            _builder.Create(PerkCategoryType.DevicesFieldSupport, PerkType.PowerCell)
                .Name("Power Cell")

                .AddPerkLevel()
                .Description("Restores 10% of maximum STM to one ally and increases physical and Force ability Accuracy by 4% for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 12)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.PowerCell1)

                .AddPerkLevel()
                .Description("Restores 18% of maximum STM to one ally and increases physical and Force ability Accuracy by 6% for 12 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.PowerCell2)

                .AddPerkLevel()
                .Description("Restores 18% of maximum STM to nearby allies and increases physical and Force ability Accuracy by 6% for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 48)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.PowerCell3);
        }

        private void RayshieldScreen()
        {
            _builder.Create(PerkCategoryType.DevicesFieldSupport, PerkType.RayshieldScreen)
                .Name("Rayshield Screen")

                .AddPerkLevel()
                .Description("Places a 4m screen for 15 seconds. Allies inside take 10% less ranged physical damage.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 18)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.RayshieldScreen1)

                .AddPerkLevel()
                .Description("Places a 4m screen for 18 seconds. Allies inside take 15% less ranged physical damage.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 38)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.RayshieldScreen2);
        }

        private void DampeningField()
        {
            _builder.Create(PerkCategoryType.DevicesFieldSupport, PerkType.DampeningField)
                .Name("Dampening Field")

                .AddPerkLevel()
                .Description("One ally takes 10% less physical and force damage for 10 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.DampeningField1)

                .AddPerkLevel()
                .Description("One ally takes 15% less physical and force damage for 10 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.DampeningField2);
        }

        private void GroupDeflector()
        {
            _builder.Create(PerkCategoryType.DevicesFieldSupport, PerkType.GroupDeflector)
                .Name("Group Deflector")

                .AddPerkLevel()
                .Description("Nearby allies gain 70 temporary HP plus 8% of each target's maximum HP for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 42)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.GroupDeflector1);
        }

        private void EmergencyBunker()
        {
            _builder.Create(PerkCategoryType.DevicesFieldSupport, PerkType.EmergencyBunker)
                .Name("Emergency Bunker")

                .AddPerkLevel()
                .Description("Deploys a shield bunker for 45 seconds. Allies inside gain 60 temporary HP plus 8% of each target's maximum HP and take 15% less ranged physical damage.")
                .Price(5)
                .RequirementSkill(SkillType.Devices, 50)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.EmergencyBunker1);
        }

    }
}
