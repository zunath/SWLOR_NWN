using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class ForcePerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new PerkBuilder();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            ForcePush();
            BurstOfSpeed();
            ThrowLightsaber();
            ForceStun();
            ComprehendSpeech();
            BattleInsight();
            MindTrick();
            ForceHeal();
            ForceBurst();
            ForceBody();
            DrainLife();
            ForceLightning();
            ForceMind();

            return _builder.Build();
        }

        private void ForcePush()
        {
            _builder.Create(PerkCategoryType.ForceUniversal, PerkType.ForcePush)
                .Name("Force Push")

                .AddPerkLevel()
                .Description("Knockdown a small target. If resisted, target is slowed for 6 seconds.")
                .Price(2)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForcePush1)

                .AddPerkLevel()
                .Description("Knockdown a medium or smaller target. If resisted, target is slowed for 6 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForcePush2)

                .AddPerkLevel()
                .Description("Knockdown a large or smaller target. If resisted, target is slowed for 6 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForcePush3)

                .AddPerkLevel()
                .Description("Knockdown any size target. If resisted, target is slowed for 6 seconds.")
                .Price(5)
                .RequirementSkill(SkillType.Force, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForcePush4);
        }

        private void BurstOfSpeed()
        {
            _builder.Create(PerkCategoryType.ForceUniversal, PerkType.BurstOfSpeed)
                .Name("Burst of Speed")

                .AddPerkLevel()
                .Description("Increases your speed by 20% while concentrating.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.BurstOfSpeed1)                

                .AddPerkLevel()
                .Description("Increases your speed by 30% while concentrating.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.BurstOfSpeed2)

                .AddPerkLevel()
                .Description("Increases your speed by 40% while concentrating.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.BurstOfSpeed3)

                .AddPerkLevel()
                .Description("Increases your speed by 50% while concentrating.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.BurstOfSpeed4)

                .AddPerkLevel()
                .Description("Increases your speed by 60% while concentrating.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.BurstOfSpeed5);
        }

        private void ThrowLightsaber()
        {
            _builder.Create(PerkCategoryType.ForceUniversal, PerkType.ThrowLightsaber)
                .Name("Throw Lightsaber")

                .AddPerkLevel()
                .Description("Throw your equipped lightsaber up to 15m for 5.0 DMG. Can hit up to 1 targets along the path thrown.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ThrowLightsaber1)

                .AddPerkLevel()
                .Description("Throw your equipped lightsaber up to 15m for 7.5 DMG. Can hit up to 2 targets along the path thrown.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ThrowLightsaber2)

                .AddPerkLevel()
                .Description("Throw your equipped lightsaber up to 15m for 9.0 DMG. Can hit up to 3 targets along the path thrown.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ThrowLightsaber3);
        }

        private void ForceStun()
        {
            _builder.Create(PerkCategoryType.ForceUniversal, PerkType.ForceStun)
                .Name("Force Stun")

                .AddPerkLevel()
                .Description("Single target is Tranquilized while the caster concentrates or, if resisted, gets -5 to AB and Evasion.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceStun1)

                .AddPerkLevel()
                .Description("Target and nearest other enemy within 10m is Tranquilized while the caster concentrates or, if resisted, get -5 to AB and Evasion.")
                .Price(7)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceStun2)

                .AddPerkLevel()
                .Description("Target and all other enemies within 10m are Tranquilized while the caster concentrates or, if resisted, get -5 to AB and Evasion.")
                .Price(10)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceStun3);
        }

        private void ComprehendSpeech()
        {
            _builder.Create(PerkCategoryType.ForceUniversal, PerkType.ComprehendSpeech)
                .Name("Comprehend Speech")

                .AddPerkLevel()
                .Description("The caster counts has having 5 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ComprehendSpeech1)

                .AddPerkLevel()
                .Description("The caster counts has having 10 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ComprehendSpeech2)

                .AddPerkLevel()
                .Description("The caster counts has having 15 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ComprehendSpeech3)

                .AddPerkLevel()
                .Description("The caster counts has having 20 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ComprehendSpeech4);
        }

        private void BattleInsight()
        {
            _builder.Create(PerkCategoryType.ForceUniversal, PerkType.BattleInsight)
                .Name("Battle Insight")

                .AddPerkLevel()
                .Description("The caster gets -5 AB & Evasion but nearby party members get +3 AB & Evasion.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.BattleInsight1)

                .AddPerkLevel()
                .Description("The caster gets -8 AB & Evasion but nearby party members get +6 AB & Evasion.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.BattleInsight2);
        }

        private void MindTrick()
        {
            _builder.Create(PerkCategoryType.ForceUniversal, PerkType.MindTrick)
                .Name("Mind Trick")

                .AddPerkLevel()
                .Description("Applies Confusion effect to a single non-mechanical target with lower WIL than the caster, while the caster concentrates.")
                .Price(7)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.MindTrick1)

                .AddPerkLevel()
                .Description("Applies Confusion effect to all hostile non-mechanical targets within 10m with lower WIL than the caster, while the caster concentrates.")
                .Price(7)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.MindTrick2);
        }

        private void ForceHeal()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.ForceHeal)
                .Name("Force Heal")

                .AddPerkLevel()
                .Description("Heals a single target for 2 HP every six seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceHeal1)

                .AddPerkLevel()
                .Description("Heals a single target for 4 HP every six seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceHeal2)

                .AddPerkLevel()
                .Description("Heals a single target for 6 HP every six seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceHeal3)

                .AddPerkLevel()
                .Description("Heals a single target for 8 HP every six seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceHeal4)

                .AddPerkLevel()
                .Description("Heals a single target for 10 HP every six seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceHeal5);
        }

        private void ForceBurst()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.ForceBurst)
                .Name("Force Burst")

                .AddPerkLevel()
                .Description("Deals 6.0 DMG to a single target.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceBurst1)

                .AddPerkLevel()
                .Description("Deals 8.5 DMG to a single target.")
                .Price(5)
                .RequirementSkill(SkillType.Force, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceBurst2)

                .AddPerkLevel()
                .Description("Deals 12.0 DMG to a single target.")
                .Price(6)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceBurst3)

                .AddPerkLevel()
                .Description("Deals 13.5 DMG to a single target.")
                .Price(7)
                .RequirementSkill(SkillType.Force, 50)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceBurst4);
        }
        private void ForceMind()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.ForceMind)
                .Name("Force Mind")

                .AddPerkLevel()
                .Description("Converts 25% of the user's FP into HP.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceMind1)

                .AddPerkLevel()
                .Description("Converts 50% of the user's FP into HP.")
                .Price(6)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceMind2);
        }

        private void DrainLife()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.ForceDrain)
                .Name("Drain Life")

                .AddPerkLevel()
                .Description("Steals 1 HP from a target every six seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceDrain1)

                .AddPerkLevel()
                .Description("Steals 2 HP from a target every six seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceDrain2)

                .AddPerkLevel()
                .Description("Steals 3 HP from a target every six seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceDrain3)

                .AddPerkLevel()
                .Description("Steals 4 HP from a target every six seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceDrain4)

                .AddPerkLevel()
                .Description("Steals 5 HP from a target every six seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceDrain5);
        }

        private void ForceLightning()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.ForceLightning)
                .Name("Force Lightning")

                .AddPerkLevel()
                .Description("Deals 6.0 DMG to a single target.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceLightning1)

                .AddPerkLevel()
                .Description("Deals 8.5 DMG to a single target.")
                .Price(5)
                .RequirementSkill(SkillType.Force, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceLightning2)

                .AddPerkLevel()
                .Description("Deals 12.0 DMG to a single target.")
                .Price(6)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceLightning3)

                .AddPerkLevel()
                .Description("Deals 13.5 DMG to a single target.")
                .Price(7)
                .RequirementSkill(SkillType.Force, 50)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceLightning4);
        }

        private void ForceBody()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.ForceBody)
                .Name("Force Body")

                .AddPerkLevel()
                .Description("Converts 25% of the user's HP into FP.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceBody1)

                .AddPerkLevel()
                .Description("Converts 50% of the user's HP into FP.")
                .Price(6)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceBody2);
        }
    }
}
