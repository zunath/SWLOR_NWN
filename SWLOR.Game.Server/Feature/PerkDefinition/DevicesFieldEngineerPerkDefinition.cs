using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

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
            RemoteCharge();
            MaintenancePulse();
            ShockBeacon();
            KillzoneBeacon();

            return _builder.Build();
        }

        private void BlasterBeacon()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.BlasterBeacon)
                .Name("Blaster Beacon")

                .AddPerkLevel()
                .Description("Plants a targeting beacon for 18 seconds. Every 3 seconds, one hostile target within 12m is hit by an automated ranged energy pulse for DMG plus PER scaling.")
                .Price(2)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.BlasterBeacon1)

                .AddPerkLevel()
                .Description("Plants a targeting beacon for 21 seconds. Every 3 seconds, one hostile target within 12m is hit by an increased automated ranged energy pulse for DMG plus PER scaling.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.BlasterBeacon2)

                .AddPerkLevel()
                .Description("Plants a targeting beacon for 24 seconds. Every 3 seconds, one hostile target within 14m is hit by a high automated ranged energy pulse for DMG plus PER scaling.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.BlasterBeacon3);
        }

        private void BeaconTargeting()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.BeaconTargeting)
                .Name("Beacon Targeting")

                .AddPerkLevel()
                .Description("Beacon pulses gain +5% Accuracy and +5% critical chance.")
                .Price(2)
                .RequirementSkill(SkillType.Devices, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.BeaconPulseAccuracyPercentAdjustment, 5)
                .IncreasesStat(StatType.BeaconPulseCriticalRatePercentAdjustment, 5)

                .AddPerkLevel()
                .Description("Beacon pulses gain +10% Accuracy, +10% critical chance, and +5% damage.")
                .Price(2)
                .RequirementSkill(SkillType.Devices, 22)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.BeaconPulseAccuracyPercentAdjustment, 10)
                .IncreasesStat(StatType.BeaconPulseCriticalRatePercentAdjustment, 10)
                .IncreasesStat(StatType.BeaconPulseDamagePercentAdjustment, 5)

                .AddPerkLevel()
                .Description("Beacon pulses gain +15% Accuracy, +15% critical chance, +10% damage, and +2m pulse range.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.BeaconPulseAccuracyPercentAdjustment, 15)
                .IncreasesStat(StatType.BeaconPulseCriticalRatePercentAdjustment, 15)
                .IncreasesStat(StatType.BeaconPulseDamagePercentAdjustment, 10)
                .IncreasesStat(StatType.BeaconPulseRangeBonusMeters, 2);
        }

        private void IncendiaryField()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.IncendiaryField)
                .Name("Incendiary Field")

                .AddPerkLevel()
                .Description("Deploys a visible fire field for 12 seconds. Enemies inside take fire DMG plus PER scaling every 3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 8)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.IncendiaryField1)

                .AddPerkLevel()
                .Description("Deploys a visible fire field for 15 seconds. Enemies inside take increased fire DMG plus PER scaling every 3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 28)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.IncendiaryField2)

                .AddPerkLevel()
                .Description("Deploys a visible fire field for 18 seconds. Enemies inside take high fire DMG plus PER scaling every 3 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 42)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.IncendiaryField3);
        }

        private void RemoteCharge()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.RemoteCharge)
                .Name("Remote Charge")

                .AddPerkLevel()
                .Description("Arms a visible charge at your target location that detonates after 3 seconds for fire DMG plus PER scaling.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 12)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.RemoteCharge1)

                .AddPerkLevel()
                .Description("Arms a visible charge that detonates after 3 seconds for fire DMG plus PER scaling and knock down.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.RemoteCharge2)

                .AddPerkLevel()
                .Description("Arms a visible charge that detonates after 3 seconds for heavy fire DMG and knock down.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 48)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.RemoteCharge3);
        }

        private void MaintenancePulse()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.MaintenancePulse)
                .Name("Maintenance Pulse")

                .AddPerkLevel()
                .Description("Restores 12% of maximum HP to one friendly droid or mechanical ally. If you have an active Field Engineer beacon or field, its duration is extended by 3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 18)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.MaintenancePulse1)

                .AddPerkLevel()
                .Description("Restores high HP to one friendly droid or mechanical ally and removes Shock. If you have an active Field Engineer beacon or field, its duration is extended by 5 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 38)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.MaintenancePulse2);
        }

        private void ShockBeacon()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.ShockBeacon)
                .Name("Shock Beacon")

                .AddPerkLevel()
                .Description("Plants a shock beacon for 15 seconds. Every 3 seconds, one hostile target within 10m is hit by an electrical pulse and suffers Shock.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ShockBeacon1)

                .AddPerkLevel()
                .Description("Plants a shock beacon for 18 seconds. Every 3 seconds, one hostile target within 12m is hit by an increased electrical pulse and suffers Shock.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ShockBeacon2);
        }

        private void KillzoneBeacon()
        {
            _builder.Create(PerkCategoryType.Devices, PerkType.KillzoneBeacon)
                .Name("Killzone Beacon")

                .AddPerkLevel()
                .Description("Plants a killzone beacon for 18 seconds. Every 3 seconds, it triggers one energy pulse and one shock pulse against hostile targets within 12m.")
                .Price(5)
                .RequirementSkill(SkillType.Devices, 50)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.KillzoneBeacon1);
        }

    }
}
