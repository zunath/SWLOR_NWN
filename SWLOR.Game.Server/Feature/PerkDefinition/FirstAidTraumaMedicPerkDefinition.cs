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
            _builder.Create(PerkCategoryType.FirstAid, PerkType.MedKit)
                .Name("Med Kit")

                .AddPerkLevel()
                .Description("Restores 10% of the target's maximum HP plus WIL scaling to a single target. Consumes medical supplies.")
                .Price(2)
                .GrantsFeat(FeatType.MedKit1)

                .AddPerkLevel()
                .Description("Restores 20% of the target's maximum HP plus WIL scaling to a single target. Consumes medical supplies.")
                .Price(4)
                .RequirementSkill(SkillType.FirstAid, 25)
                .GrantsFeat(FeatType.MedKit2)

                .AddPerkLevel()
                .Description("Restores 28% of the target's maximum HP plus WIL scaling to a single target. Consumes medical supplies.")
                .Price(4)
                .RequirementSkill(SkillType.FirstAid, 40)
                .GrantsFeat(FeatType.MedKit3)

                .AddPerkLevel()
                .Description("Restores 36% of the target's maximum HP plus WIL scaling to a single target. Consumes medical supplies.")
                .Price(5)
                .RequirementSkill(SkillType.FirstAid, 50)
                .GrantsFeat(FeatType.MedKit4);
        }

        private void TreatmentKit()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.TreatmentKit)
                .Name("Treatment Kit")

                .AddPerkLevel()
                .Description("Removes Bleed and Poison from a single target. Consumes medical supplies.")
                .Price(2)
                .RequirementSkill(SkillType.FirstAid, 5)
                .GrantsFeat(FeatType.TreatmentKit1)

                .AddPerkLevel()
                .Description("Removes Bleed, Poison, Toxin, Burn, Shock, and Disease from a single target. Consumes medical supplies.")
                .Price(2)
                .RequirementSkill(SkillType.FirstAid, 22)
                .GrantsFeat(FeatType.TreatmentKit2)

                .AddPerkLevel()
                .Description("Removes Bleed, Poison, Toxin, Burn, Shock, and Disease from a single target and grants 50% Fire, Poison, Electrical, Ice, and Trauma resistance for 8 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.FirstAid, 42)
                .GrantsFeat(FeatType.TreatmentKit3);
        }

        private void MedicalInjectorRig()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.MedicalInjectorRig)
                .Name("Medical Injector Rig")

                .AddPerkLevel()
                .Description("Med Kit, Kolto Mist, Emergency Triage, and Infusion healing is increased by 10%.")
                .IncreasesStat(StatType.FirstAidMedicalHealingPercentAdjustment, 10)
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 8)

                .AddPerkLevel()
                .Description("Med Kit, Kolto Mist, Emergency Triage, and Infusion healing is increased by 20%.")
                .IncreasesStat(StatType.FirstAidMedicalHealingPercentAdjustment, 20)
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 38);
        }

        private void EmergencySealant()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.EmergencySealant)
                .Name("Emergency Sealant")

                .AddPerkLevel()
                .Description("Stops Bleed or Burn on one target and grants HP regeneration equal to 2% of the target's maximum HP plus WIL scaling every 3 seconds for 12 seconds. Consumes medical supplies.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 12)
                .GrantsFeat(FeatType.EmergencySealant1);
        }

        private void KoltoMist()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.KoltoMist)
                .Name("Kolto Mist")

                .AddPerkLevel()
                .Description("Restores HP over time to nearby allies within 3m for 12 seconds. Total healing equals 7% of each target's maximum HP plus WIL scaling. Consumes medical supplies.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 15)
                .GrantsFeat(FeatType.KoltoMist1)

                .AddPerkLevel()
                .Description("Restores HP over time to nearby allies within 3m for 12 seconds. Total healing equals 12% of each target's maximum HP plus WIL scaling. Consumes medical supplies.")
                .Price(4)
                .RequirementSkill(SkillType.FirstAid, 30)
                .GrantsFeat(FeatType.KoltoMist2);
        }

        private void Resuscitation()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.Resuscitation)
                .Name("Resuscitation")

                .AddPerkLevel()
                .Description("Revives an unconscious target with 1 HP. Consumes medical supplies.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 18)
                .GrantsFeat(FeatType.Resuscitation1)

                .AddPerkLevel()
                .Description("Revives an unconscious target with 20% HP plus WIL scaling. Consumes medical supplies.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 35)
                .GrantsFeat(FeatType.Resuscitation2);
        }

        private void Infusion()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.Infusion)
                .Name("Infusion")

                .AddPerkLevel()
                .Description("Grants a single target regeneration, healing 3% of maximum HP plus WIL scaling every 3 seconds for 15 seconds. Consumes medical supplies.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 28)
                .GrantsFeat(FeatType.Infusion1)

                .AddPerkLevel()
                .Description("Grants a single target regeneration, healing 5% of maximum HP plus WIL scaling every 3 seconds for 15 seconds. Consumes medical supplies.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 48)
                .GrantsFeat(FeatType.Infusion2);
        }

        private void EmergencyTriage()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.EmergencyTriage)
                .Name("Emergency Triage")

                .AddPerkLevel()
                .Description("Restores 18% of the target's maximum HP plus WIL scaling instantly. Healing is doubled if the target is below 35% HP. Consumes extra medical supplies.")
                .Price(4)
                .RequirementSkill(SkillType.FirstAid, 45)
                .GrantsFeat(FeatType.EmergencyTriage1);
        }

    }
}
