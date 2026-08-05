using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Feature.QuestDefinition;

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
            DisruptionPulse();
            ThermalDetonator();

            return _builder.Build();
        }

        private PerkBuilder CreateGrenadierPerk(PerkType perkType)
        {
            return _builder.Create(PerkCategoryType.DevicesGrenadier, perkType)
                .TriggerPurchase(AbilityTargeting.RefreshClientTargeting)
                .TriggerRefund(AbilityTargeting.RefreshClientTargeting);
        }

        private void FragGrenade()
        {
            CreateGrenadierPerk(PerkType.FragGrenade)
                .Name("Frag Grenade")

                .AddPerkLevel()
                .Description("Deals 18 fire DMG plus PER scaling to enemies in a 3m blast. Consumes explosives.")
                .Price(2)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.FragGrenade1)

                .AddPerkLevel()
                .Description("Deals 32 fire DMG plus PER scaling to enemies in a 3m blast and attempts to inflict Bleed for 12 seconds. Consumes explosives.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.FragGrenade2)

                .AddPerkLevel()
                .Description("Deals 48 fire DMG plus PER scaling to enemies in a 3m blast and attempts to inflict Bleed for 12 seconds. Consumes explosives.")
                .Price(5)
                .RequirementSkill(SkillType.Devices, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(3)
                .GrantsFeat(FeatType.FragGrenade3);
        }

        private void BlastRadius()
        {
            CreateGrenadierPerk(PerkType.BlastRadius)
                .Name("Blast Radius")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BlastRadiusTrait)
                .Description("Grenade abilities, Remote Charge, and Overload Barrage gain +1m blast radius.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.BlastRadiusBonusTenths, 10)

                .AddPerkLevel()
                .Description("Grenade abilities, Remote Charge, and Overload Barrage gain +2m blast radius.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 22)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.BlastRadiusBonusTenths, 20)

                .AddPerkLevel()
                .Description("Grenade abilities, Remote Charge, and Overload Barrage gain +3m blast radius, and Flash Grenade and Adhesive Grenade non-save effect strength increases by 5%.")
                .Price(5)
                .RequirementSkill(SkillType.Devices, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.BlastRadiusBonusTenths, 30)
                .IncreasesStat(StatType.GrenadeControlPotencyBonus, 5);
        }

        private void ConcussionGrenade()
        {
            CreateGrenadierPerk(PerkType.ConcussionGrenade)
                .Name("Concussion Grenade")

                .AddPerkLevel()
                .Description("Deals 14 electrical DMG plus PER scaling in a 3m blast and knocks down for 3 seconds. Affects up to 5 targets. Consumes explosives.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 8)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.ConcussionGrenade1)

                .AddPerkLevel()
                .Description("Deals 28 electrical DMG plus PER scaling in a 3m blast and knocks down for 3 seconds. Affects up to 5 targets. Consumes explosives.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 28)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.ConcussionGrenade2);
        }

        private void FlashGrenade()
        {
            CreateGrenadierPerk(PerkType.FlashGrenade)
                .Name("Flash Grenade")

                .AddPerkLevel()
                .Description("Attempts to inflict Flash, reducing physical and Force ability hit chance by 8% for 30 seconds in a 4m blast. Affects up to 5 targets. Consumes explosives.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 12)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.FlashGrenade1);
        }

        private void IonGrenade()
        {
            CreateGrenadierPerk(PerkType.IonGrenade)
                .Name("Ion Grenade")

                .AddPerkLevel()
                .Description("Deals 20 electrical DMG plus PER scaling in a 3m blast. Deals 50% bonus damage to droids. Consumes explosives.")
                .Price(3)
                .RequirementSkill(SkillType.Devices, 18)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.IonGrenade1)

                .AddPerkLevel()
                .Description("Deals 34 electrical DMG plus PER scaling in a 3m blast. Deals 60% bonus damage to droids and inflicts Shock for 12 seconds. Consumes explosives.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 38)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.IonGrenade2);
        }

        private void AdhesiveGrenade()
        {
            CreateGrenadierPerk(PerkType.AdhesiveGrenade)
                .Name("Adhesive Grenade")

                .AddPerkLevel()
                .Description("Slows enemies in a 4m blast for 6 seconds. Affects up to 3 targets. Consumes explosives.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.AdhesiveGrenade1)

                .AddPerkLevel()
                .Description("Slows enemies in a 4m blast for 12 seconds. Affects up to 5 targets. Consumes explosives.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 42)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.AdhesiveGrenade2);
        }

        private void ClusterGrenade()
        {
            CreateGrenadierPerk(PerkType.ClusterGrenade)
                .Name("Cluster Grenade")

                .AddPerkLevel()
                .Description("Throws three adjacent grenades within 3m of the target point. Each grenade deals 18 fire DMG plus PER scaling in a 2m blast, and overlapping blasts can hit the same enemy. Consumes explosives.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.ClusterGrenade1);
        }

        private void DisruptionPulse()
        {
            CreateGrenadierPerk(PerkType.DisruptionPulse)
                .Name("Disruption Pulse")

                .AddPerkLevel()
                .Description("Emits a 5m disruption pulse at a target point within 12m, dealing 18 electrical DMG plus PER scaling to enemies and reducing physical and Force ability Accuracy by 6% for 12 seconds. Consumes explosives.")
                .Price(4)
                .RequirementSkill(SkillType.Devices, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.DisruptionPulse1);
        }

        private void ThermalDetonator()
        {
            CreateGrenadierPerk(PerkType.ThermalDetonator)
                .Name("Thermal Detonator")

                .AddPerkLevel()
                .Description("Deals 60 fire DMG plus PER scaling in a 5m blast and inflicts Burn for 45 seconds. Consumes explosives.")
                .Price(5)
                .RequirementSkill(SkillType.Devices, 50)
                .RequirementCharacterType(CharacterType.Standard)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.ThermalDetonator1)
                .RequirementQuest(DevicesCapstoneQuestDefinition.ThermalDetonatorMasteryQuestId);
        }

    }
}
