using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public sealed class FirstAidTraumaMedicPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            MedKit();
            TreatmentKit();
            MedicalInjectorRig();
            EmergencySealant();
            KoltoMist();
            Resuscitation();
            Infusion();
            EmergencyTriage();

            return _builder.Build();
        }

        private void MedKit()
        {
            _builder.Create(PerkCategoryType.FirstAidTraumaMedic, PerkType.MedKit)
                .Name("Med Kit")

                .AddPerkLevel()
                .Description("Restores 7 HP plus 1.25% of the target's maximum HP to a single target. Each WIL point above 10 adds 6.25% to the healing, up to double at 26 WIL. Consumes medical supplies.")
                .Price(2)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.MedKit1)

                .AddPerkLevel()
                .Description("Restores 26 HP plus 2.19% of the target's maximum HP to a single target. Each WIL point above 10 adds 6.25% to the healing, up to double at 26 WIL. Consumes medical supplies.")
                .Price(4)
                .RequirementSkill(SkillType.FirstAid, 25)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.MedKit2)

                .AddPerkLevel()
                .Description("Restores 50 HP plus 3.13% of the target's maximum HP to a single target. Each WIL point above 10 adds 6.25% to the healing, up to double at 26 WIL. Consumes medical supplies.")
                .Price(4)
                .RequirementSkill(SkillType.FirstAid, 40)
                .DroidAISlots(3)
                .GrantsFeat(FeatType.MedKit3)

                .AddPerkLevel()
                .Description("Restores 68 HP plus 3.75% of the target's maximum HP to a single target. Each WIL point above 10 adds 6.25% to the healing, up to double at 26 WIL. Consumes medical supplies.")
                .Price(5)
                .RequirementSkill(SkillType.FirstAid, 48)
                .DroidAISlots(4)
                .GrantsFeat(FeatType.MedKit4);
        }

        private void TreatmentKit()
        {
            _builder.Create(PerkCategoryType.FirstAidTraumaMedic, PerkType.TreatmentKit)
                .Name("Treatment Kit")

                .AddPerkLevel()
                .Description("Removes Bleed and Poison from a single target. Consumes medical supplies.")
                .Price(2)
                .RequirementSkill(SkillType.FirstAid, 5)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.TreatmentKit1)

                .AddPerkLevel()
                .Description("Removes Bleed, Poison, Toxin, Burn, Shock, and Disease from a single target. Consumes medical supplies.")
                .Price(2)
                .RequirementSkill(SkillType.FirstAid, 22)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.TreatmentKit2)

                .AddPerkLevel()
                .Description("Removes Bleed, Poison, Toxin, Burn, Shock, and Disease from a single target and grants 50% Fire Resistance, 50% Poison Resistance, 50% Electrical Resistance, 50% Ice Resistance, and 50% Trauma Resistance for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.FirstAid, 42)
                .DroidAISlots(3)
                .GrantsFeat(FeatType.TreatmentKit3);
        }

        private void MedicalInjectorRig()
        {
            _builder.Create(PerkCategoryType.FirstAidTraumaMedic, PerkType.MedicalInjectorRig)
                .Name("Medical Injector Rig")

                .AddPerkLevel()
                .GrantsFeat(FeatType.MedicalInjectorRigTrait)
                .Description("All direct, area, and periodic healing caused by your abilities is increased by 5%.")
                .IncreasesStat(StatType.OutgoingAbilityHealingPercentAdjustment, 5)
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 8)

                .AddPerkLevel()
                .Description("All direct, area, and periodic healing caused by your abilities is increased by 10%.")
                .IncreasesStat(StatType.OutgoingAbilityHealingPercentAdjustment, 10)
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 38);
        }

        private void EmergencySealant()
        {
            _builder.Create(PerkCategoryType.FirstAidTraumaMedic, PerkType.EmergencySealant)
                .Name("Emergency Sealant")

                .AddPerkLevel()
                .GrantsFeat(FeatType.EmergencySealantTrait)
                .Description("Trauma Medic healing and treatment abilities also stop one Bleed or Burn effect. If an effect is removed this way, the target restores 3 HP plus 0.16% of maximum HP every 3 seconds for 30 seconds. Each WIL point above 10 adds 6.25% to the healing, up to double at 26 WIL.")
                .IncreasesStat(StatType.TraumaMedicEmergencySealant, 1)
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 12);
        }

        private void KoltoMist()
        {
            _builder.Create(PerkCategoryType.FirstAidTraumaMedic, PerkType.KoltoMist)
                .Name("Kolto Mist")

                .AddPerkLevel()
                .Description("Deploys a 30-second healing mist cloud at a target location up to 15m away. Allies within 8m heal for 1 HP plus 0.16% of maximum HP every 3 seconds. Each WIL point above 10 adds 6.25% to the healing, up to double at 26 WIL. Consumes medical supplies.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 15)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.KoltoMist1)

                .AddPerkLevel()
                .Description("Deploys a 30-second healing mist cloud at a target location up to 15m away. Allies within 8m heal for 6 HP plus 0.31% of maximum HP every 3 seconds. Each WIL point above 10 adds 6.25% to the healing, up to double at 26 WIL. Consumes medical supplies.")
                .Price(4)
                .RequirementSkill(SkillType.FirstAid, 30)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.KoltoMist2);
        }

        private void Resuscitation()
        {
            _builder.Create(PerkCategoryType.FirstAidTraumaMedic, PerkType.Resuscitation)
                .Name("Resuscitation")

                .AddPerkLevel()
                .Description("Revives an unconscious target with 1 HP. Consumes medical supplies. Taking damage interrupts the cast. Fails while a hostile player is within 20m, or, after a player kill, until your party near the target has gone 15 seconds without taking damage. A player can be revived mid-fight at most once every 5 minutes.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 18)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.Resuscitation1)

                .AddPerkLevel()
                .Description("Revives an unconscious target with 12.5% HP. Each WIL point above 10 adds 6.25% to the healing, up to double at 26 WIL. Consumes medical supplies. Taking damage interrupts the cast. Fails while a hostile player is within 20m, or, after a player kill, until your party near the target has gone 15 seconds without taking damage. A player can be revived mid-fight at most once every 5 minutes.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 35)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.Resuscitation2);
        }

        private void Infusion()
        {
            _builder.Create(PerkCategoryType.FirstAidTraumaMedic, PerkType.Infusion)
                .Name("Infusion")

                .AddPerkLevel()
                .Description("Grants a single target regeneration, healing 4 HP plus 0.31% of maximum HP every 3 seconds for 30 seconds. Each WIL point above 10 adds 6.25% to the healing, up to double at 26 WIL. Consumes medical supplies.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 28)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.Infusion1)

                .AddPerkLevel()
                .Description("Grants a single target regeneration, healing 9 HP plus 0.47% of maximum HP every 3 seconds for 30 seconds. Each WIL point above 10 adds 6.25% to the healing, up to double at 26 WIL. Consumes medical supplies.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 45)
                .DroidAISlots(2)
                .GrantsFeat(FeatType.Infusion2);
        }

        private void EmergencyTriage()
        {
            _builder.Create(PerkCategoryType.FirstAidTraumaMedic, PerkType.EmergencyTriage)
                .Name("Emergency Triage")

                .AddPerkLevel()
                .Description("Restores 51 HP plus 2.81% of the target's maximum HP instantly. Each WIL point above 10 adds 6.25% to the healing, up to double at 26 WIL. Can target allies up to 15m away. Healing is doubled if the target is below 35% HP. Consumes extra medical supplies.")
                .Price(4)
                .RequirementSkill(SkillType.FirstAid, 50)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.EmergencyTriage1);
        }

    }
}
