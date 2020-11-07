using System.Collections.Generic;
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

                .AddPerkLevel()
                .Description("Knockdown a medium or smaller target. If resisted, target is slowed for 6 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Knockdown a large or smaller target. If resisted, target is slowed for 6 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Knockdown any size target. If resisted, target is slowed for 6 seconds.")
                .Price(5)
                .RequirementSkill(SkillType.Force, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive);
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

                .AddPerkLevel()
                .Description("Increases your speed by 30% while concentrating.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Increases your speed by 40% while concentrating.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Increases your speed by 50% while concentrating.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Increases your speed by 60% while concentrating.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void ThrowLightsaber(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.ForceUniversal, PerkType.ThrowLightsaber)
                .Name("Throw Lightsaber")

                .AddPerkLevel()
                .Description("Throw your equipped lightsaber up to 15m for (saber damage + INT modifier) * 100%")
                .Price(3)
                .RequirementSkill(SkillType.Force, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Throw your equipped lightsaber up to 15m for (saber damage + INT modifier) * 125%")
                .Price(3)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Throw your equipped lightsaber up to 15m for (saber damage + INT modifier) * 160%")
                .Price(3)
                .RequirementSkill(SkillType.Force, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Throw your equipped lightsaber up to 15m for (saber damage +INT modifier) *200 %. This will chain to a second target within 5m of the first.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Throw your equipped lightsaber up to 15m for (saber damage +INT modifier) *250 %. This will chain to a second and third target within 5m of the first.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 50)
                .RequirementCharacterType(CharacterType.ForceSensitive);
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

                .AddPerkLevel()
                .Description("Target and nearest other enemy within 10m is Tranquilized while the caster concentrates or, if resisted, get -5 to AB and AC.")
                .Price(7)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("Target and all other enemies within 10 are Tranquilized while the caster concentrates or, if resisted, get -5 to AB and AC.")
                .Price(10)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive);
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

                .AddPerkLevel()
                .Description("The caster counts has having 10 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("The caster counts has having 15 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)

                .AddPerkLevel()
                .Description("The caster counts has having 20 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive);
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

                .AddPerkLevel()
                .Description("The caster gets -8 AB & AC but nearby party members get +6 AB & AC.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive);
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

                .AddPerkLevel()
                .Description("Applies Confusion effect to all hostile non-mechanical targets within 10m with lower WIS than the caster, while the caster concentrates.")
                .Price(7)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

    }
}
