using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class FirstAidPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            RangedHealing();
            FrugalMedic();
            MedKit();
            KoltoRecovery();
            Resuscitation();
            TreatmentKit();
            StasisField();
            CombatEnhancement();
            Shielding();
            Infusion();
            AdrenalStim();

            return _builder.Build();
        }

        private void RangedHealing()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.RangedHealing)
                .Name("Ranged Healing")

                .AddPerkLevel()
                .Description("Increases the range you can use First Aid abilities by 1 meter.")
                .Price(2)
                .RequirementSkill(SkillType.FirstAid, 5)
                .GrantsFeat(FeatType.RangedHealing1)

                .AddPerkLevel()
                .Description("Increases the range you can use First Aid abilities by 2 meters.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 15)
                .GrantsFeat(FeatType.RangedHealing2)

                .AddPerkLevel()
                .Description("Increases the range you can use First Aid abilities by 3 meters.")
                .Price(4)
                .RequirementSkill(SkillType.FirstAid, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.RangedHealing3)

                .AddPerkLevel()
                .Description("Increases the range you can use First Aid abilities by 4 meters.")
                .Price(5)
                .RequirementSkill(SkillType.FirstAid, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.RangedHealing4);
        }

        private void FrugalMedic()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.FrugalMedic)
                .Name("Frugal Medic")

                .AddPerkLevel()
                .Description("10% chance to use a First Aid ability without consuming supplies or stim packs.")
                .Price(1)
                .RequirementSkill(SkillType.FirstAid, 10)
                .GrantsFeat(FeatType.FrugalMedic1)

                .AddPerkLevel()
                .Description("20% chance to use a First Aid ability without consuming supplies or stim packs.")
                .Price(2)
                .RequirementSkill(SkillType.FirstAid, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.FrugalMedic2)

                .AddPerkLevel()
                .Description("30% chance to use a First Aid ability without consuming supplies or stim packs.")
                .Price(2)
                .RequirementSkill(SkillType.FirstAid, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.FrugalMedic3);
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
                .GrantsFeat(FeatType.MedKit4)

                .AddPerkLevel()
                .Description("Restores 140 HP to a single target. Consumes medical supplies on use.")
                .Price(4)
                .DroidAISlots(5)
                .RequirementSkill(SkillType.FirstAid, 40)
                .GrantsFeat(FeatType.MedKit5);
        }

        private void KoltoRecovery()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.KoltoRecovery)
                .Name("Kolto Recovery")

                .AddPerkLevel()
                .Description("Restores HP to all allies within 3 meters of you. Consumes medical supplies on use.")
                .Price(3)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.FirstAid, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.KoltoRecovery1)

                .AddPerkLevel()
                .Description("Restores HP to all allies within 3 meters of you. Consumes medical supplies on use.")
                .Price(4)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.FirstAid, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.KoltoRecovery2)

                .AddPerkLevel()
                .Description("Restores HP to all allies within 3 meters of you. Consumes medical supplies on use.")
                .Price(5)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.FirstAid, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.KoltoRecovery3);
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
                .GrantsFeat(FeatType.Resuscitation2)

                .AddPerkLevel()
                .Description("Revives an unconscious target with (2*WIL)% HP. Consumes medical supplies on use.")
                .Price(4)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.FirstAid, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Resuscitation3);
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

        private void StasisField()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.StasisField)
                .Name("Stasis Field")

                .AddPerkLevel()
                .Description("Increase your target's Evasion by 10 for 15 minutes. Consumes stim pack on use.")
                .Price(2)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.FirstAid, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.StasisField1)

                .AddPerkLevel()
                .Description("Increase your target's Evasion by 20 for 15 minutes. Consumes stim pack on use.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.FirstAid, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.StasisField2)

                .AddPerkLevel()
                .Description("Increase your target's Evasion by 30 for 15 minutes. Consumes stim pack on use.")
                .Price(4)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.FirstAid, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.StasisField3);
        }

        private void CombatEnhancement()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.CombatEnhancement)
                .Name("Combat Enhancement")

                .AddPerkLevel()
                .Description("Increases the MGT, PER, and VIT of a single target by 1 for 15 minutes. Consumes stim pack on use. Does not stack with Force Inspiration.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.FirstAid, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.CombatEnhancement1)

                .AddPerkLevel()
                .Description("Increases the MGT, PER, and VIT of a single target by 2 for 15 minutes. Consumes stim pack on use. Does not stack with Force Inspiration.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.FirstAid, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.CombatEnhancement2)

                .AddPerkLevel()
                .Description("Increases the MGT, PER, and VIT of a single target by 3 for 15 minutes. Consumes stim pack on use. Does not stack with Force Inspiration.")
                .Price(4)
                .DroidAISlots(5)
                .RequirementSkill(SkillType.FirstAid, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.CombatEnhancement3);
        }

        private void Shielding()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.Shielding)
                .Name("Shielding")

                .AddPerkLevel()
                .Description("Improves a single target's physical defense by 5 for 15 minutes. Consumes stim pack on use.")
                .Price(2)
                .DroidAISlots(2)
                .RequirementSkill(SkillType.FirstAid, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Shielding1)

                .AddPerkLevel()
                .Description("Improves a single target's physical defense by 10 for 15 minutes. Consumes stim pack on use.")
                .Price(3)
                .DroidAISlots(3)
                .RequirementSkill(SkillType.FirstAid, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Shielding2)

                .AddPerkLevel()
                .Description("Improves a single target's physical defense by 15 for 15 minutes. Consumes stim pack on use.")
                .Price(3)
                .DroidAISlots(4)
                .RequirementSkill(SkillType.FirstAid, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Shielding3)

                .AddPerkLevel()
                .Description("Improves a single target's physical defense by 20 for 15 minutes. Consumes stim pack on use.")
                .Price(4)
                .DroidAISlots(5)
                .RequirementSkill(SkillType.FirstAid, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.Shielding4);
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
