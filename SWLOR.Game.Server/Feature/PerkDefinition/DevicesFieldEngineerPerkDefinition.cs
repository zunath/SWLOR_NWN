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
    public sealed class DevicesFieldEngineerPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            BlasterBeacon();
            BeaconTargeting();
            IncendiaryField();
            SignalJammer();
            RemoteCharge();
            ShockBeacon();
            DiagnosticSweep();
            KillzoneBeacon();

            return _builder.Build();
        }

        private void BlasterBeacon()
        {
            _builder.Create(PerkCategoryType.DevicesFieldEngineer, PerkType.BlasterBeacon)
                .Name("Blaster Beacon")

                .AddPerkLevel()
                .Description("Plants a visible 12m targeting sphere for 30 seconds. Every 3 seconds, one hostile target inside is hit by an automated ranged energy pulse for 3 physical DMG plus PER scaling.")
                .Price(3)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.BlasterBeacon1)

                .AddPerkLevel()
                .Description("Plants a visible 12m targeting sphere for 30 seconds. Every 3 seconds, one hostile target inside is hit by an automated ranged energy pulse for 6 physical DMG plus PER scaling.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.BlasterBeacon2)

                .AddPerkLevel()
                .Description("Plants a visible 14m targeting sphere for 30 seconds. Every 3 seconds, one hostile target inside is hit by an automated ranged energy pulse for 10 physical DMG plus PER scaling.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(3)
                .GrantsFeat(FeatType.BlasterBeacon3);
        }

        private void BeaconTargeting()
        {
            _builder.Create(PerkCategoryType.DevicesFieldEngineer, PerkType.BeaconTargeting)
                .Name("Beacon Targeting")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BeaconTargetingTrait)
                .Description("Beacon pulses gain +4% damage and +1m pulse range.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.BeaconPulseDamagePercentAdjustment, 4)
                .IncreasesStat(StatType.BeaconPulseRangeBonusMeters, 1)

                .AddPerkLevel()
                .Description("Beacon pulses gain +8% damage and +2m pulse range.")
                .Price(5)
                .RequirementSkill(SkillType.Devices, 42)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.BeaconPulseDamagePercentAdjustment, 8)
                .IncreasesStat(StatType.BeaconPulseRangeBonusMeters, 2);
        }

        private void IncendiaryField()
        {
            _builder.Create(PerkCategoryType.DevicesFieldEngineer, PerkType.IncendiaryField)
                .Name("Incendiary Field")

                .AddPerkLevel()
                .Description("Deploys a visible 5m-radius fire field at the target location for 30 seconds. Enemies inside take 8 fire DMG plus PER scaling every 3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 8)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.IncendiaryField1)

                .AddPerkLevel()
                .Description("Deploys a visible 5m-radius fire field at the target location for 30 seconds. Enemies inside take 12 fire DMG plus PER scaling every 3 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.IncendiaryField2)

                .AddPerkLevel()
                .Description("Deploys a visible 5m-radius fire field at the target location for 30 seconds. Enemies inside take 16 fire DMG plus PER scaling every 3 seconds.")
                .Price(5)
                .RequirementSkill(SkillType.Devices, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(3)
                .GrantsFeat(FeatType.IncendiaryField3);
        }

        private void SignalJammer()
        {
            _builder.Create(PerkCategoryType.DevicesFieldEngineer, PerkType.SignalJammer)
                .Name("Signal Jammer")

                .AddPerkLevel()
                .Description("Deploys a signal jammer for 45 seconds. Hostile targets within 5m suffer -6% physical and Force ability Accuracy and cannot benefit from Haste while inside.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 18)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.SignalJammer1);
        }

        private void RemoteCharge()
        {
            _builder.Create(PerkCategoryType.DevicesFieldEngineer, PerkType.RemoteCharge)
                .Name("Remote Charge")

                .AddPerkLevel()
                .Description("Arms a visible charge at your target location that detonates after 3 seconds in a 5m-radius blast for 30 fire DMG plus PER scaling.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 12)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.RemoteCharge1)

                .AddPerkLevel()
                .Description("Arms a visible charge at your target location that detonates after 3 seconds in a 5m-radius blast for 42 fire DMG plus PER scaling and inflicts Knockdown for 6 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 28)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.RemoteCharge2);
        }

        private void ShockBeacon()
        {
            _builder.Create(PerkCategoryType.DevicesFieldEngineer, PerkType.ShockBeacon)
                .Name("Shock Beacon")

                .AddPerkLevel()
                .Description("Plants a visible 5m shock sphere for 30 seconds. Every 3 seconds, one hostile target inside is hit for 10 electrical DMG plus PER scaling and suffers Shock.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 22)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.ShockBeacon1)

                .AddPerkLevel()
                .Description("Plants a visible 5m shock sphere for 30 seconds. Every 3 seconds, one hostile target inside is hit for 14 electrical DMG plus PER scaling and suffers Shock.")
                .Price(5)
                .RequirementSkill(SkillType.Devices, 38)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.ShockBeacon2);
        }

        private void DiagnosticSweep()
        {
            _builder.Create(PerkCategoryType.DevicesFieldEngineer, PerkType.DiagnosticSweep)
                .Name("Diagnostic Sweep")

                .AddPerkLevel()
                .GrantsFeat(FeatType.DiagnosticSweepTrait)
                .Description("Field Engineer beacons, fields, charges, and jammers reveal hidden enemies in their affected area and reduce Evasion by 4% for 30 seconds.")
                .Price(5)
                .RequirementSkill(SkillType.Devices, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.FieldEngineerAreaRevealHidden, 1)
                .IncreasesStat(StatType.FieldEngineerAreaEvasionPenaltyPercent, 4)
                .IncreasesStat(StatType.FieldEngineerAreaEvasionPenaltyDurationSeconds, 30);
        }

        private void KillzoneBeacon()
        {
            _builder.Create(PerkCategoryType.DevicesFieldEngineer, PerkType.KillzoneBeacon)
                .Name("Killzone Beacon")

                .AddPerkLevel()
                .Description("Plants a visible 12m killzone sphere for 45 seconds. Every 3 seconds, all hostile targets inside are hit by one 16 physical DMG plus PER scaling pulse and one 16 electrical DMG plus PER scaling shock pulse.")
                .Price(5)
                .RequirementSkill(SkillType.Devices, 50)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.KillzoneBeacon1)
                .RequirementQuest(DevicesCapstoneQuestDefinition.KillzoneBeaconMasteryQuestId);
        }

    }
}
