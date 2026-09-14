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
            _builder.Create(PerkCategoryType.FirstAidCombatPharmacology, PerkType.AdrenalStim)
                .Name("Adrenal Stim")

                .AddPerkLevel()
                .Description("Restores 10% of maximum STM and restores 1 STM every 3 seconds for 30 seconds. Consumes a stim pack.")
                .Price(2)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.AdrenalStim1)

                .AddPerkLevel()
                .Description("Restores 18% of maximum STM and restores 1 STM every 3 seconds for 30 seconds. Consumes a stim pack.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 12)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.AdrenalStim2)

                .AddPerkLevel()
                .Description("Restores 25% of maximum STM and restores 1 STM every 3 seconds for 30 seconds. Consumes a stim pack.")
                .Price(4)
                .RequirementSkill(SkillType.FirstAid, 30)
                .DroidAISlots(3)
                .GrantsFeat(FeatType.AdrenalStim3);
        }

        private void Shielding()
        {
            _builder.Create(PerkCategoryType.FirstAidCombatPharmacology, PerkType.Shielding)
                .Name("Shielding")

                .AddPerkLevel()
                .Description("Reduces physical and force damage taken by 5% for 3 minutes. Consumes a stim pack.")
                .Price(2)
                .RequirementSkill(SkillType.FirstAid, 5)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.Shielding1)

                .AddPerkLevel()
                .Description("Reduces physical and force damage taken by 8% for 3 minutes. Consumes a stim pack.")
                .Price(4)
                .RequirementSkill(SkillType.FirstAid, 25)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.Shielding2)

                .AddPerkLevel()
                .Description("Reduces physical and force damage taken by 11% for 3 minutes. Consumes a stim pack.")
                .Price(4)
                .RequirementSkill(SkillType.FirstAid, 42)
                .DroidAISlots(3)
                .GrantsFeat(FeatType.Shielding3);
        }

        private void Coagulant()
        {
            _builder.Create(PerkCategoryType.FirstAidCombatPharmacology, PerkType.Coagulant)
                .Name("Coagulant")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CoagulantTrait)
                .Description("Combat Pharmacology stim effects also grant +50 Trauma Resistance and 10% resistance to incoming physical damage over time effects for 2 minutes.")
                .IncreasesStat(StatType.CombatPharmacologyStimCoagulantRank, 1)
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 8)

                .AddPerkLevel()
                .Description("Combat Pharmacology stim effects also grant Trauma immunity and 20% resistance to incoming physical damage over time effects for 2 minutes.")
                .IncreasesStat(StatType.CombatPharmacologyStimCoagulantRank, 2)
                .Price(4)
                .RequirementSkill(SkillType.FirstAid, 40);
        }

        private void PainSuppressant()
        {
            _builder.Create(PerkCategoryType.FirstAidCombatPharmacology, PerkType.PainSuppressant)
                .Name("Pain Suppressant")

                .AddPerkLevel()
                .Description("Grants temporary HP equal to 10% of the target's maximum HP plus WIL scaling and 10% damage reduction for 30 seconds. Consumes a stim pack.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 15)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.PainSuppressant1)

                .AddPerkLevel()
                .Description("Grants temporary HP equal to 15% of the target's maximum HP plus WIL scaling and 15% damage reduction for 30 seconds. Consumes a stim pack.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 35)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.PainSuppressant2);
        }

        private void Antitoxin()
        {
            _builder.Create(PerkCategoryType.FirstAidCombatPharmacology, PerkType.Antitoxin)
                .Name("Antitoxin")

                .AddPerkLevel()
                .Description("Grants 50% Poison Resistance for 2 minutes and removes one Poison or Toxin effect. Poison Resistance also weakens Disease and Toxin effects. Consumes a stim pack.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 18)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.Antitoxin1);
        }

        private void FieldPharmacist()
        {
            _builder.Create(PerkCategoryType.FirstAidCombatPharmacology, PerkType.FieldPharmacist)
                .Name("Field Pharmacist")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FieldPharmacistTrait)
                .Description("Stim pack effects last 15% longer and have a 10% chance not to consume the stim pack.")
                .IncreasesStat(StatType.StimPackDurationPercentAdjustment, 15)
                .IncreasesStat(StatType.StimPackPreserveChance, 10)
                .Price(2)
                .RequirementSkill(SkillType.FirstAid, 22)

                .AddPerkLevel()
                .Description("Stim pack effects last 25% longer and have a 20% chance not to consume the stim pack.")
                .IncreasesStat(StatType.StimPackDurationPercentAdjustment, 25)
                .IncreasesStat(StatType.StimPackPreserveChance, 20)
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 38)

                .AddPerkLevel()
                .Description("Stim pack effects last 35% longer and have a 30% chance not to consume the stim pack.")
                .IncreasesStat(StatType.StimPackDurationPercentAdjustment, 35)
                .IncreasesStat(StatType.StimPackPreserveChance, 30)
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 48);
        }

        private void FocusStim()
        {
            _builder.Create(PerkCategoryType.FirstAidCombatPharmacology, PerkType.FocusStim)
                .Name("Focus Stim")

                .AddPerkLevel()
                .Description("Increases physical and Force ability Accuracy by 5% for 2 minutes. Consumes a stim pack.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 28)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.FocusStim1)

                .AddPerkLevel()
                .Description("Increases physical and Force ability Accuracy by 8% for 2 minutes. Consumes a stim pack.")
                .Price(4)
                .RequirementSkill(SkillType.FirstAid, 45)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.FocusStim2);
        }

        private void EmergencyCocktail()
        {
            _builder.Create(PerkCategoryType.FirstAidCombatPharmacology, PerkType.EmergencyCocktail)
                .Name("Emergency Cocktail")

                .AddPerkLevel()
                .Description("Restores 25% of maximum STM, removes one Poison or Toxin effect, then for 45 seconds restores 1 STM every 3 seconds, grants temporary HP equal to 12% of maximum HP plus WIL scaling, reduces damage taken by 12%, and grants 50% Poison Resistance.")
                .Price(5)
                .RequirementSkill(SkillType.FirstAid, 50)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.EmergencyCocktail1)
                .RequirementQuest(FirstAidCapstoneQuestDefinition.EmergencyCocktailMasteryQuestId);
        }

    }
}
