using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class FirstAidPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            MedKit();
            Resuscitation();
            TreatmentKit();
            Shielding();
            Infusion();
            AdrenalStim();

            return _builder.Build();
        }



        private void MedKit()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.MedKit)
                .Name("Med Kit")

                .AddPerkLevel()
                .Description("Restores 30 HP to a single target. Consumes medical supplies on use.")
                .Price(1)
                .DroidAISlots(1)
                .GrantsFeat(FeatType.MedKit1)

                .AddPerkLevel()
                .Description("Restores 50 HP to a single target. Consumes medical supplies on use.")
                .Price(2)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.FirstAid, 10)
                .GrantsFeat(FeatType.MedKit2)

                .AddPerkLevel()
                .Description("Restores 80 HP to a single target. Consumes medical supplies on use.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.FirstAid, 20)
                .GrantsFeat(FeatType.MedKit3)

                .AddPerkLevel()
                .Description("Restores 110 HP to a single target. Consumes medical supplies on use.")
                .Price(4)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.FirstAid, 30)
                .GrantsFeat(FeatType.MedKit4);
        }



        private void Resuscitation()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.Resuscitation)
                .Name("Resuscitation")

                .AddPerkLevel()
                .Description("Revives a single target back from the brink of death with 1 HP. Consumes medical supplies on use.")
                .Price(4)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.FirstAid, 15)
                .GrantsFeat(FeatType.Resuscitation1)

                .AddPerkLevel()
                .Description("Revives an unconscious target with (WIL)% HP. Consumes medical supplies on use.")
                .Price(4)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.FirstAid, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Resuscitation2);
        }


        private void TreatmentKit()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.TreatmentKit)
                .Name("Treatment Kit")

                .AddPerkLevel()
                .Description("Removes bleed and poison from a single target. Consumes medical supplies on use.")
                .Price(2)
                .DroidAISlots(1)
                .RequirementSkill(SkillType.FirstAid, 5)
                .GrantsFeat(FeatType.TreatmentKit1)

                .AddPerkLevel()
                .Description("Removes bleed, poison, shock, disease, and burn from a single target. Consumes medical supplies on use.")
                .Price(2)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.FirstAid, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.TreatmentKit2);
        }



        private void Shielding()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.Shielding)
                .Name("Shielding")

                .AddPerkLevel()
                .Description("Improves a single target's Physical Defense by 5 for 15 minutes. Consumes stim pack on use.")
                .Price(2)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.FirstAid, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Shielding1)

                .AddPerkLevel()
                .Description("Improves a single target's Physical Defense by 10 for 15 minutes. Consumes stim pack on use.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.FirstAid, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Shielding2)

                .AddPerkLevel()
                .Description("Improves a single target's Physical Defense by 15 for 15 minutes. Consumes stim pack on use.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.FirstAid, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Shielding3);
        }


        private void Infusion()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.Infusion)
                .Name("Infusion")

                .AddPerkLevel()
                .Description("Grants your target regeneration which heals 60 HP every six seconds for 24 seconds. Consumes stim pack on use.")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.FirstAid, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Infusion1)

                .AddPerkLevel()
                .Description("Grants your target regeneration which heals 120 HP every six seconds for 24 seconds. Consumes stim pack on use.")
                .Price(4)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.FirstAid, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Infusion2);
        }


        private void AdrenalStim()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.AdrenalStim)
                .Name("Adrenal Stim")

                .AddPerkLevel()
                .Description("Restores STM at the cost of one's Willpower. Consumes stim pack on use.")
                .Price(2)
                .RequirementSkill(SkillType.FirstAid, 10)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.AdrenalStim1)

                .AddPerkLevel()
                .Description("Restores STM at the cost of one's Willpower. Consumes stim pack on use.")
                .Price(2)
                .RequirementSkill(SkillType.FirstAid, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.AdrenalStim2)

                .AddPerkLevel()
                .Description("Restores STM at the cost of one's Willpower. Consumes stim pack on use.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 50)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.AdrenalStim3);
        }
    }
}
