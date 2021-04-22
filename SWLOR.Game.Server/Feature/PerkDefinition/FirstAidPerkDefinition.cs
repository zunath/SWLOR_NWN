using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class FirstAidPerkDefinition : IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();

            RangedHealing(builder);
            FrugalMedic(builder);
            RecoveryItems(builder);
            StimPacks(builder);

            return builder.Build();
        }

        private void RangedHealing(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.FirstAid, PerkType.RangedHealing)
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

        private void FrugalMedic(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.FirstAid, PerkType.FrugalMedic)
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

        private void RecoveryItems(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.FirstAid, PerkType.RecoveryItems)
                .Name("Recovery Items")

                .AddPerkLevel()
                .Description("Enables you to use tier 1 recovery items.")
                .Price(1)
                .GrantsFeat(FeatType.RecoveryItems1)

                .AddPerkLevel()
                .Description("Enables you to use tier 2 recovery items.")
                .Price(1)
                .RequirementSkill(SkillType.FirstAid, 10)
                .GrantsFeat(FeatType.RecoveryItems2)

                .AddPerkLevel()
                .Description("Enables you to use tier 3 recovery items.")
                .Price(2)
                .RequirementSkill(SkillType.FirstAid, 20)
                .GrantsFeat(FeatType.RecoveryItems3)

                .AddPerkLevel()
                .Description("Enables you to use tier 4 recovery items.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 30)
                .GrantsFeat(FeatType.RecoveryItems4)

                .AddPerkLevel()
                .Description("Enables you to use tier 5 recovery items.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 40)
                .GrantsFeat(FeatType.RecoveryItems5);
        }

        private void StimPacks(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.FirstAid, PerkType.StimPacks)
                .Name("Stim Packs")

                .AddPerkLevel()
                .Description("Enables you to use tier 1 stim packs.")
                .Price(1)
                .GrantsFeat(FeatType.StimPacks1)

                .AddPerkLevel()
                .Description("Enables you to use tier 2 stim packs.")
                .Price(1)
                .RequirementSkill(SkillType.FirstAid, 10)
                .GrantsFeat(FeatType.StimPacks2)

                .AddPerkLevel()
                .Description("Enables you to use tier 3 stim packs.")
                .Price(2)
                .RequirementSkill(SkillType.FirstAid, 20)
                .GrantsFeat(FeatType.StimPacks3)

                .AddPerkLevel()
                .Description("Enables you to use tier 4 stim packs.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 30)
                .GrantsFeat(FeatType.StimPacks4)

                .AddPerkLevel()
                .Description("Enables you to use tier 5 stim packs.")
                .Price(3)
                .RequirementSkill(SkillType.FirstAid, 40)
                .GrantsFeat(FeatType.StimPacks5);
        }
    }
}
