using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Feature.QuestDefinition;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public sealed class DevicesFieldSupportPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            DeflectorShield();
            WeaponJam();
            PowerCell();
            PowerSurge();
            RayshieldScreen();
            DampeningField();
            OverclockRoutine();
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
                .Price(3)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.DeflectorShield1)

                .AddPerkLevel()
                .Description("Grants one ally 65 temporary HP plus 9% of the target's maximum HP for 45 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.DeflectorShield2)

                .AddPerkLevel()
                .Description("Grants one ally 100 temporary HP plus 12% of the target's maximum HP for 45 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(3)
                .GrantsFeat(FeatType.DeflectorShield3);
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
                .DroidAISlots(1)
                .GrantsFeat(FeatType.WeaponJam1);
        }

        private void PowerCell()
        {
            _builder.Create(PerkCategoryType.DevicesFieldSupport, PerkType.PowerCell)
                .Name("Power Cell")

                .AddPerkLevel()
                .Description("Restores 10% of maximum STM to one ally and increases physical and Force ability Accuracy by 4% for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 12)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.PowerCell1)

                .AddPerkLevel()
                .Description("Restores 18% of maximum STM to one ally and increases physical and Force ability Accuracy by 6% for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.PowerCell2)

                .AddPerkLevel()
                .Description("Restores 18% of maximum STM to the selected ally and allies within 5m and increases physical and Force ability Accuracy by 6% for 30 seconds.")
                .Price(5)
                .RequirementSkill(SkillType.Devices, 48)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(3)
                .GrantsFeat(FeatType.PowerCell3);
        }

        private void PowerSurge()
        {
            _builder.Create(PerkCategoryType.DevicesFieldSupport, PerkType.PowerSurge)
                .Name("Power Surge")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PowerSurgeTrait)
                .Description("Power Cell's initial target also gains Power Surge for 30 seconds: +6% physical and Force ability Accuracy, +6% critical chance, and 1 STM every 4 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.PowerCellInitialTargetPowerSurge, 1);
        }

        private void RayshieldScreen()
        {
            _builder.Create(PerkCategoryType.DevicesFieldSupport, PerkType.RayshieldScreen)
                .Name("Rayshield Screen")

                .AddPerkLevel()
                .GrantsFeat(FeatType.RayshieldScreenTrait)
                .Description("Field Support ally buffs from Deflector Shield, Power Cell, Group Deflector, and Emergency Bunker also grant affected allies +8% Physical Defense for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 18)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.FieldSupportPhysicalDefensePercent, 8)
                .IncreasesStat(StatType.FieldSupportPhysicalDefenseDurationSeconds, 30)

                .AddPerkLevel()
                .Description("Field Support ally buffs from Deflector Shield, Power Cell, Group Deflector, and Emergency Bunker also grant affected allies +12% Physical Defense for 30 seconds.")
                .Price(5)
                .RequirementSkill(SkillType.Devices, 38)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.FieldSupportPhysicalDefensePercent, 12)
                .IncreasesStat(StatType.FieldSupportPhysicalDefenseDurationSeconds, 30);
        }

        private void DampeningField()
        {
            _builder.Create(PerkCategoryType.DevicesFieldSupport, PerkType.DampeningField)
                .Name("Dampening Field")

                .AddPerkLevel()
                .GrantsFeat(FeatType.DampeningFieldTrait)
                .Description("Field Support ally buffs from Deflector Shield, Power Cell, Group Deflector, and Emergency Bunker also grant affected allies 6% reduced physical and force damage for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 22)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.FieldSupportPhysicalAndForceDamageReductionPercent, 6)
                .IncreasesStat(StatType.FieldSupportPhysicalAndForceDamageReductionDurationSeconds, 30)

                .AddPerkLevel()
                .Description("Field Support ally buffs from Deflector Shield, Power Cell, Group Deflector, and Emergency Bunker also grant affected allies 10% reduced physical and force damage for 30 seconds.")
                .Price(5)
                .RequirementSkill(SkillType.Devices, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.FieldSupportPhysicalAndForceDamageReductionPercent, 10)
                .IncreasesStat(StatType.FieldSupportPhysicalAndForceDamageReductionDurationSeconds, 30);
        }

        private void OverclockRoutine()
        {
            _builder.Create(PerkCategoryType.DevicesFieldSupport, PerkType.OverclockRoutine)
                .Name("Overclock Routine")

                .AddPerkLevel()
                .GrantsFeat(FeatType.OverclockRoutineTrait)
                .Description("Field Support abilities that affect allies also grant Overclock Routine for 30 seconds. Affected allies gain +4% Combat Readiness, increasing their ability damage, healing, and temporary HP values.")
                .Price(5)
                .RequirementSkill(SkillType.Devices, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.FieldSupportAllyOverclockRoutine, 1);
        }

        private void GroupDeflector()
        {
            _builder.Create(PerkCategoryType.DevicesFieldSupport, PerkType.GroupDeflector)
                .Name("Group Deflector")

                .AddPerkLevel()
                .Description("Allies within 5m gain 70 temporary HP plus 8% of each target's maximum HP for 30 seconds.")
                .Price(5)
                .RequirementSkill(SkillType.Devices, 42)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.GroupDeflector1);
        }

        private void EmergencyBunker()
        {
            _builder.Create(PerkCategoryType.DevicesFieldSupport, PerkType.EmergencyBunker)
                .Name("Emergency Bunker")

                .AddPerkLevel()
                .Description("Deploys an 8m-radius shield bunker at the target location for 45 seconds. Allies inside gain 60 temporary HP plus 8% of each target's maximum HP and take 15% less physical and Force damage.")
                .Price(5)
                .RequirementSkill(SkillType.Devices, 50)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.EmergencyBunker1)
                .RequirementQuest(DevicesCapstoneQuestDefinition.EmergencyBunkerMasteryQuestId);
        }

    }
}
