using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class FirstAidPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new PerkBuilder();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            RangedHealing();
            FrugalMedic();
            RecoveryItems();
            BactaRecovery();
            StasisField();
            CombatEnhancement();

            return _builder.Build();
        }

        private void RangedHealing()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.RangedHealing)
                .Name("Ranged Healing")

                .AddPerkLevel()
                .Description("Increases the range you can use recovery items and stim packs by 1 meter.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 15)
                .GrantsFeat(FeatType.RangedHealing1)

                .AddPerkLevel()
                .Description("Increases the range you can use recovery items and stim packs by 2 meter.")
                .Price(4)
                .RequirementSkill(SkillType.FirstAid, 25)
                .GrantsFeat(FeatType.RangedHealing2)

                .AddPerkLevel()
                .Description("Increases the range you can use recovery items and stim packs by 3 meter.")
                .Price(5)
                .RequirementSkill(SkillType.FirstAid, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.RangedHealing3)

                .AddPerkLevel()
                .Description("Increases the range you can use recovery items and stim packs by 4 meter.")
                .Price(6)
                .RequirementSkill(SkillType.FirstAid, 45)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.RangedHealing4);
        }

        private void FrugalMedic()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.FrugalMedic)
                .Name("Frugal Medic")

                .AddPerkLevel()
                .Description("10% chance to use a recovery item or stim pack without consuming a charge.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 10)
                .GrantsFeat(FeatType.FrugalMedic1)

                .AddPerkLevel()
                .Description("20% chance to use a recovery item or stim pack without consuming a charge.")
                .Price(4)
                .RequirementSkill(SkillType.FirstAid, 25)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.FrugalMedic2)

                .AddPerkLevel()
                .Description("30% chance to use a recovery item or stim pack without consuming a charge.")
                .Price(5)
                .RequirementSkill(SkillType.FirstAid, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .GrantsFeat(FeatType.FrugalMedic3);
        }

        private void RecoveryItems()
        {
            _builder.Create(PerkCategoryType.FirstAid, PerkType.RecoveryItems)
                .Name("Recovery Items")

                .AddPerkLevel()
                .Description("Enables you to use tier 1 recovery items and stim packs.")
                .Price(3)
                .GrantsFeat(FeatType.RecoveryItems1)

                .AddPerkLevel()
                .Description("Enables you to use tier 2 recovery items and stim packs.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 10)
                .GrantsFeat(FeatType.RecoveryItems2)

                .AddPerkLevel()
                .Description("Enables you to use tier 3 recovery items and stim packs.")
                .Price(4)
                .RequirementSkill(SkillType.FirstAid, 20)
                .GrantsFeat(FeatType.RecoveryItems3)

                .AddPerkLevel()
                .Description("Enables you to use tier 4 recovery items and stim packs.")
                .Price(5)
                .RequirementSkill(SkillType.FirstAid, 30)
                .GrantsFeat(FeatType.RecoveryItems4)

                .AddPerkLevel()
                .Description("Enables you to use tier 5 recovery items and stim packs.")
                .Price(5)
                .RequirementSkill(SkillType.FirstAid, 40)
                .GrantsFeat(FeatType.RecoveryItems5);
        }

        private void BactaRecovery()
        {

        }

        private void StasisField()
        {

        }

        private void CombatEnhancement()
        {

        }
    }
}
