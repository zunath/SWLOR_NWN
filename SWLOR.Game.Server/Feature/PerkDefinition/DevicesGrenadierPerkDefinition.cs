using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public sealed class DevicesGrenadierPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            FragGrenade();
            BlastRadius();
            ConcussionGrenade();
            FlashGrenade();
            IonGrenade();
            AdhesiveGrenade();
            ClusterGrenade();
            ThermalDetonator();

            return _builder.Build();
        }

        private void FragGrenade()
        {
            _builder.Create(PerkCategoryType.DevicesGrenadier, PerkType.FragGrenade)
                .Name("Frag Grenade")

                .AddPerkLevel()
                .Description("Deals 18 fire DMG plus PER scaling to enemies in a 3m blast. Consumes explosives.")
                .Price(2)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.FragGrenade1)

                .AddPerkLevel()
                .Description("Deals 32 fire DMG plus PER scaling to enemies in a 3m blast and attempts to inflict Bleed.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.FragGrenade2)

                .AddPerkLevel()
                .Description("Deals 48 fire DMG plus PER scaling to enemies in a 3m blast and attempts to inflict Bleed.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.FragGrenade3);
        }

        private void BlastRadius()
        {
            _builder.Create(PerkCategoryType.DevicesGrenadier, PerkType.BlastRadius)
                .Name("Blast Radius")

                .AddPerkLevel()
                .Description("Grenade abilities gain +0.5m blast radius.")
                .Price(2)
                .RequirementSkill(SkillType.Devices, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.GrenadeRadiusBonusTenths, 5)

                .AddPerkLevel()
                .Description("Grenade abilities gain +1m blast radius.")
                .Price(2)
                .RequirementSkill(SkillType.Devices, 22)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.GrenadeRadiusBonusTenths, 10)

                .AddPerkLevel()
                .Description("Grenade abilities gain +1.5m blast radius, and Flash Grenade and Adhesive Grenade non-save effect strength increases by 5%.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.GrenadeRadiusBonusTenths, 15)
                .IncreasesStat(StatType.GrenadeControlPotencyBonus, 5);
        }

        private void ConcussionGrenade()
        {
            _builder.Create(PerkCategoryType.DevicesGrenadier, PerkType.ConcussionGrenade)
                .Name("Concussion Grenade")

                .AddPerkLevel()
                .Description("Deals 14 electrical DMG plus PER scaling in a 3m blast and knock down for 2 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 8)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ConcussionGrenade1)

                .AddPerkLevel()
                .Description("Deals 28 electrical DMG plus PER scaling in a 3m blast and knock down for 2 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 28)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ConcussionGrenade2)

                .AddPerkLevel()
                .Description("Deals 42 electrical DMG plus PER scaling in a 3m blast and knock down for 3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 48)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ConcussionGrenade3);
        }

        private void FlashGrenade()
        {
            _builder.Create(PerkCategoryType.DevicesGrenadier, PerkType.FlashGrenade)
                .Name("Flash Grenade")

                .AddPerkLevel()
                .Description("Attempts to inflict Flash, reducing physical and Force ability hit chance by 8% for 20 seconds in a 4m blast. Consumes explosives.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 12)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.FlashGrenade1)

                .AddPerkLevel()
                .Description("Attempts to inflict Flash, reducing physical and Force ability hit chance by 14% for 20 seconds in a 4m blast. Consumes explosives.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.FlashGrenade2);
        }

        private void IonGrenade()
        {
            _builder.Create(PerkCategoryType.DevicesGrenadier, PerkType.IonGrenade)
                .Name("Ion Grenade")

                .AddPerkLevel()
                .Description("Deals 20 electrical DMG plus PER scaling in a 3m blast. Deals 50% bonus damage to droids. Consumes explosives.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 18)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.IonGrenade1)

                .AddPerkLevel()
                .Description("Deals 34 electrical DMG plus PER scaling in a 3m blast. Deals 60% bonus damage to droids and Shock.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 38)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.IonGrenade2);
        }

        private void AdhesiveGrenade()
        {
            _builder.Create(PerkCategoryType.DevicesGrenadier, PerkType.AdhesiveGrenade)
                .Name("Adhesive Grenade")

                .AddPerkLevel()
                .Description("Slows enemies in a 4m blast for 6 seconds and immobilizes them for 3 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.AdhesiveGrenade1)

                .AddPerkLevel()
                .Description("Slows enemies in a 4m blast for 8 seconds and immobilizes them for 4 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 42)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.AdhesiveGrenade2);
        }

        private void ClusterGrenade()
        {
            _builder.Create(PerkCategoryType.DevicesGrenadier, PerkType.ClusterGrenade)
                .Name("Cluster Grenade")

                .AddPerkLevel()
                .Description("Throws three small grenades at nearby enemies, each dealing 18 fire DMG plus PER scaling in a small blast.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ClusterGrenade1);
        }

        private void ThermalDetonator()
        {
            _builder.Create(PerkCategoryType.DevicesGrenadier, PerkType.ThermalDetonator)
                .Name("Thermal Detonator")

                .AddPerkLevel()
                .Description("Deals heavy fire DMG plus PER scaling in a 5m blast and inflicts Burning. Consumes extra explosives.")
                .Price(5)
                .RequirementSkill(SkillType.Devices, 50)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.ThermalDetonator1);
        }

    }
}
