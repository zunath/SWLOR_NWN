using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class ForcePerkDefinition : IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();

            ForcePush(builder);
            BurstOfSpeed(builder);
            ThrowLightsaber(builder);
            ForceStun(builder);
            ComprehendSpeech(builder);
            BattleInsight(builder);
            MindTrick(builder);

            return builder.Build();
        }

        private void ForcePush(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ForceUniversal, PerkType.ForcePush)
                .Name("Force Push")

                .AddPerkLevel()
                .Description("Knockdown a small target. If resisted, target is slowed for 6 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(Feat.ForcePush1)

                .AddPerkLevel()
                .Description("Knockdown a medium or smaller target. If resisted, target is slowed for 6 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(Feat.ForcePush2)

                .AddPerkLevel()
                .Description("Knockdown a large or smaller target. If resisted, target is slowed for 6 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(Feat.ForcePush3)

                .AddPerkLevel()
                .Description("Knockdown any size target. If resisted, target is slowed for 6 seconds.")
                .Price(5)
                .RequirementSkill(SkillType.Force, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(Feat.ForcePush4);
        }

        private void BurstOfSpeed(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ForceUniversal, PerkType.BurstOfSpeed)
                .Name("Burst of Speed")

                .AddPerkLevel()
                .Description("Increases your speed by 20% while concentrating.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(Feat.BurstOfSpeed1)

                .AddPerkLevel()
                .Description("Increases your speed by 30% while concentrating.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(Feat.BurstOfSpeed2)

                .AddPerkLevel()
                .Description("Increases your speed by 40% while concentrating.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(Feat.BurstOfSpeed3)

                .AddPerkLevel()
                .Description("Increases your speed by 50% while concentrating.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(Feat.BurstOfSpeed4)

                .AddPerkLevel()
                .Description("Increases your speed by 60% while concentrating.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(Feat.BurstOfSpeed5);
        }

        private void ThrowLightsaber(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ForceUniversal, PerkType.ThrowLightsaber)
                .Name("Throw Lightsaber")

                .AddPerkLevel()
                .Description("Throw your equipped lightsaber up to 15m for (1d6 + INT modifier + 50% WIS modifier). Can hit up to 1 targets along the path thrown.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(Feat.ThrowLightsaber1)

                .AddPerkLevel()
                .Description("Throw your equipped lightsaber up to 15m for (1d8 + INT modifier + 50% WIS modifier). Can hit up to 2 targets along the path thrown.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(Feat.ThrowLightsaber2)

                .AddPerkLevel()
                .Description("Throw your equipped lightsaber up to 15m for (1d10 + INT modifier + 50% WIS modifier). Can hit up to 3 targets along the path thrown.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(Feat.ThrowLightsaber3);
        }

        private void ForceStun(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ForceUniversal, PerkType.ForceStun)
                .Name("Force Stun")

                .AddPerkLevel()
                .Description("Single target is Tranquilized while the caster concentrates or, if resisted, gets -5 to AB and AC.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(Feat.ForceStun1)

                .AddPerkLevel()
                .Description("Target and nearest other enemy within 10m is Tranquilized while the caster concentrates or, if resisted, get -5 to AB and AC.")
                .Price(7)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(Feat.ForceStun2)

                .AddPerkLevel()
                .Description("Target and all other enemies within 10 are Tranquilized while the caster concentrates or, if resisted, get -5 to AB and AC.")
                .Price(10)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(Feat.ForceStun3);
        }

        private void ComprehendSpeech(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ForceUniversal, PerkType.ComprehendSpeech)
                .Name("Comprehend Speech")

                .AddPerkLevel()
                .Description("The caster counts has having 5 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(Feat.ComprehendSpeech1)

                .AddPerkLevel()
                .Description("The caster counts has having 10 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(Feat.ComprehendSpeech2)

                .AddPerkLevel()
                .Description("The caster counts has having 15 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(Feat.ComprehendSpeech3)

                .AddPerkLevel()
                .Description("The caster counts has having 20 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(Feat.ComprehendSpeech4);
        }

        private void BattleInsight(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ForceUniversal, PerkType.BattleInsight)
                .Name("Battle Insight")

                .AddPerkLevel()
                .Description("The caster gets -5 AB & AC but nearby party members get +3 AB & AC.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(Feat.BattleInsight1)

                .AddPerkLevel()
                .Description("The caster gets -8 AB & AC but nearby party members get +6 AB & AC.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(Feat.BattleInsight2);
        }

        private void MindTrick(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ForceUniversal, PerkType.MindTrick)
                .Name("Mind Trick")

                .AddPerkLevel()
                .Description("Applies Confusion effect to a single non-mechanical target with lower WIS than the caster, while the caster concentrates.")
                .Price(7)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(Feat.MindTrick1)

                .AddPerkLevel()
                .Description("Applies Confusion effect to all hostile non-mechanical targets within 10m with lower WIS than the caster, while the caster concentrates.")
                .Price(7)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(Feat.MindTrick2);
        }

    }
}
