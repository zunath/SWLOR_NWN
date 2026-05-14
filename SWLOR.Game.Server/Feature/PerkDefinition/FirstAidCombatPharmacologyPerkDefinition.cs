using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public sealed class FirstAidCombatPharmacologyPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            AdrenalStim();
            Shielding();
            Coagulant();
            PainSuppressant();
            Antitoxin();
            FieldPharmacist();
            FocusStim();
            EmergencyCocktail();

            return _builder.Build();
        }

        private void AdrenalStim()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.AdrenalStim)
                .Name("Adrenal Stim")

                .AddPerkLevel()
                .Description("Restores 10% of maximum STM and restores 1 STM every 3 seconds for 12 seconds. Consumes a stim pack.")
                .Price(2)
                .GrantsFeat(FeatType.AdrenalStim1)

                .AddPerkLevel()
                .Description("Restores 18% of maximum STM and grants STM regeneration for 12 seconds. Consumes a stim pack.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 12)
                .GrantsFeat(FeatType.AdrenalStim2)

                .AddPerkLevel()
                .Description("Restores 25% of maximum STM and grants STM regeneration for 12 seconds. Consumes a stim pack.")
                .Price(4)
                .RequirementSkill(SkillType.FirstAid, 30)
                .GrantsFeat(FeatType.AdrenalStim3);
        }

        private void Shielding()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.Shielding)
                .Name("Shielding")

                .AddPerkLevel()
                .Description("Reduces physical and force damage taken by 5% for 3 minutes. Consumes a stim pack.")
                .Price(2)
                .RequirementSkill(SkillType.FirstAid, 5)
                .GrantsFeat(FeatType.Shielding1)

                .AddPerkLevel()
                .Description("Reduces physical and force damage taken by 8% for 3 minutes. Consumes a stim pack.")
                .Price(4)
                .RequirementSkill(SkillType.FirstAid, 25)
                .GrantsFeat(FeatType.Shielding2)

                .AddPerkLevel()
                .Description("Reduces physical and force damage taken by 11% for 3 minutes. Consumes a stim pack.")
                .Price(4)
                .RequirementSkill(SkillType.FirstAid, 42)
                .GrantsFeat(FeatType.Shielding3);
        }

        private void Coagulant()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.Coagulant)
                .Name("Coagulant")

                .AddPerkLevel()
                .Description("Grants 50% Bleed resistance and 10% resistance to incoming physical damage over time effects for 2 minutes. Consumes a stim pack.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 8)
                .GrantsFeat(FeatType.Coagulant1)

                .AddPerkLevel()
                .Description("Grants Bleed immunity and 20% resistance to physical damage over time effects for 2 minutes. Consumes a stim pack.")
                .Price(4)
                .RequirementSkill(SkillType.FirstAid, 40)
                .GrantsFeat(FeatType.Coagulant2);
        }

        private void PainSuppressant()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.PainSuppressant)
                .Name("Pain Suppressant")

                .AddPerkLevel()
                .Description("Grants temporary HP and 10% damage reduction for 18 seconds. Consumes a stim pack.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 15)
                .GrantsFeat(FeatType.PainSuppressant1)

                .AddPerkLevel()
                .Description("Grants temporary HP and 15% damage reduction for 18 seconds. Consumes a stim pack.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 35)
                .GrantsFeat(FeatType.PainSuppressant2);
        }

        private void Antitoxin()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.Antitoxin)
                .Name("Antitoxin")

                .AddPerkLevel()
                .Description("Grants 50% Poison and Disease resistance for 2 minutes and removes one Poison effect. Consumes a stim pack.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 18)
                .GrantsFeat(FeatType.Antitoxin1);
        }

        private void FieldPharmacist()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.FieldPharmacist)
                .Name("Field Pharmacist")

                .AddPerkLevel()
                .Description("Stim pack effects last 15% longer and have a 10% chance not to consume the stim pack.")
                .IncreasesStat(StatType.StimPackDurationPercentAdjustment, 15)
                .Price(2)
                .RequirementSkill(SkillType.FirstAid, 22)

                .AddPerkLevel()
                .Description("Stim pack effects last 25% longer and have a 20% chance not to consume the stim pack.")
                .IncreasesStat(StatType.StimPackDurationPercentAdjustment, 25)
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 38)

                .AddPerkLevel()
                .Description("Stim pack effects last 35% longer and have a 30% chance not to consume the stim pack.")
                .IncreasesStat(StatType.StimPackDurationPercentAdjustment, 35)
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 48);
        }

        private void FocusStim()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.FocusStim)
                .Name("Focus Stim")

                .AddPerkLevel()
                .Description("Increases physical and Force ability Accuracy by 5% for 2 minutes. Consumes a stim pack.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 28)
                .GrantsFeat(FeatType.FocusStim1)

                .AddPerkLevel()
                .Description("Increases physical and Force ability Accuracy by 8% for 2 minutes. Consumes a stim pack.")
                .Price(4)
                .RequirementSkill(SkillType.FirstAid, 45)
                .GrantsFeat(FeatType.FocusStim2);
        }

        private void EmergencyCocktail()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.EmergencyCocktail)
                .Name("Emergency Cocktail")

                .AddPerkLevel()
                .Description("Applies full-strength Adrenal Stim, Pain Suppressant, and Antitoxin effects for 18 seconds. Consumes extra stim packs.")
                .Price(5)
                .RequirementSkill(SkillType.FirstAid, 50)
                .GrantsFeat(FeatType.EmergencyCocktail1);
        }

    }
}
