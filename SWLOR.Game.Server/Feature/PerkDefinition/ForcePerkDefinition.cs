using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class ForcePerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            ForceDrain();
            ForceLightning();
            ForceSpark();
            CreepingTerror();
            ForceRage();
            Benevolence();
            ForcePush();
            ForceLeap();
            ComprehendSpeech();
            MindTrick();
            ForceBody();

            return _builder.Build();
        }


        private void ForceDrain()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.ForceDrain)
                .Name("Force Drain")

                .AddPerkLevel()
                .Description("Steals 10 HP from a target every six seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceDrain1)

                .AddPerkLevel()
                .Description("Steals 15 HP from a target every six seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceDrain2)

                .AddPerkLevel()
                .Description("Steals 20 HP from a target every six seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceDrain3)

                .AddPerkLevel()
                .Description("Steals 25 HP from a target every six seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceDrain4)

                .AddPerkLevel()
                .Description("Steals 30 HP from a target every six seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceDrain5);
        }


        private void ForceLightning()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.ForceLightning)
                .Name("Force Lightning")

                .AddPerkLevel()
                .Description("Deals DMG equal to your Willpower Score to up to 5 targets in a radius. Consumes FP, but if none remain, will consume HP instead.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceLightning1)

                .AddPerkLevel()
                .Description("Deals 10 DMG, scaling with your Willpower Score, to up to 5 targets in a radius. Consumes FP, but if none remain, will consume HP instead.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceLightning2)

                .AddPerkLevel()
                .Description("Deals 20 DMG, scaling with your Willpower Score, to up to 5 targets in a radius. Consumes FP, but if none remain, will consume HP instead.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceLightning3)

                .AddPerkLevel()
                .Description("Deals 3 DMG, scaling with your Willpower Score, to up to 5 targets in a radius. Consumes FP, but if none remain, will consume HP instead.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 50)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceLightning4);
        }


        private void ForceSpark()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.ForceSpark)
                .Name("Force Spark")

                .AddPerkLevel()
                .Description("Deals DMG equal to your Willpower Score to a single target and reduces target's Evasion by 2 for one minute. Consumes FP, but if none remain, will consume Health instead.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceSpark1)

                .AddPerkLevel()
                .Description("Deals 15 DMG, scaling with your Willpower Score, to a single target and reduces target's Evasion by 4 for one minute. Consumes FP, but if none remain, will consume Health instead.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceSpark2)

                .AddPerkLevel()
                .Description("Deals 30 DMG, scaling with your Willpower Score, to a single target and reduces target's Evasion by 6 for one minute. Consumes FP, but if none remain, will consume Health instead.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceSpark3);
        }


        private void CreepingTerror()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.CreepingTerror)
                .Name("Creeping Terror")

                .AddPerkLevel()
                .Description("Entangles a target for 6 seconds and inflicts Terror which deals DMG equal to half your Willpower Score every six seconds for 24 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.Benevolence)
                .GrantsFeat(FeatType.CreepingTerror1)

                .AddPerkLevel()
                .Description("Entangles a target for 6 seconds and inflicts Terror which deals 12 DMG equal to your Willpower Score every six seconds for 24 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.Benevolence)
                .GrantsFeat(FeatType.CreepingTerror2)

                .AddPerkLevel()
                .Description("Entangles a target for 6 seconds and inflicts Terror which deals 16 DMG equal to your Willpower Score * 1.5 every six seconds for 24 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.Benevolence)
                .GrantsFeat(FeatType.CreepingTerror3);
        }


        private void ForceRage()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.ForceRage)
                .Name("Force Rage")

                .AddPerkLevel()
                .Description("Increases your target's Attack by 10 for 15 minutes.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceRage1)

                .AddPerkLevel()
                .Description("Increases your target's Attack by 20 for 15 minutes.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceRage2);
        }



        private void Benevolence()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.Benevolence)
                .Name("Benevolence")

                .AddPerkLevel()
                .Description("Restores 30 HP to a single target. If your target is not yourself, you will heal significantly more and restore FP and STM while providing minor regeneration. This comes at the cost of an increased FP cost. It will also drain your stamina, though a lack of stamina won't prevent this added effect.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.CreepingTerror)
                .GrantsFeat(FeatType.Benevolence1)

                .AddPerkLevel()
                .Description("Restores 60 HP to a single target. If your target is not yourself, you will heal significantly more and restore FP and STM while providing minor regeneration. This comes at the cost of an increased FP cost. It will also drain your stamina, though a lack of stamina won't prevent this added effect.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.CreepingTerror)
                .GrantsFeat(FeatType.Benevolence2)

                .AddPerkLevel()
                .Description("Restores 90 HP to a single target. If your target is not yourself, you will heal significantly more and restore FP and STM while providing minor regeneration. This comes at the cost of an increased FP cost. It will also drain your stamina, though a lack of stamina won't prevent this added effect.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .RequirementCannotHavePerk(PerkType.CreepingTerror)
                .GrantsFeat(FeatType.Benevolence3);
        }



        private void ForcePush()
        {
            _builder.Create(PerkCategoryType.ForceUniversal, PerkType.ForcePush)
                .Name("Force Push")

                .AddPerkLevel()
                .Description("Knocks down a target for 2 seconds, scaling with WIL.")
                .Price(1)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForcePush1)

                .AddPerkLevel()
                .Description("Knocks down a target for 2 seconds, scaling with WIL.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForcePush2)

                .AddPerkLevel()
                .Description("Knocks down a target for 2 seconds, scaling with WIL.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForcePush3)

                .AddPerkLevel()
                .Description("Knocks down a target for 2 seconds, scaling with WIL.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForcePush4);
        }



        private void ForceLeap()
        {
            _builder.Create(PerkCategoryType.ForceUniversal, PerkType.ForceLeap)
                .Name("Force Leap")

                .AddPerkLevel()
                .Description("Leap to a distant target instantly, inflicting 8 DMG and stunning for 2 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceLeap1)

                .AddPerkLevel()
                .Description("Leap to a distant target instantly, inflicting 15 DMG and stunning for 2 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceLeap2)

                .AddPerkLevel()
                .Description("Leap to a distant target instantly, inflicting 23 DMG and stunning for 2 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceLeap3);
        }



        private void ComprehendSpeech()
        {
            _builder.Create(PerkCategoryType.ForceUniversal, PerkType.ComprehendSpeech)
                .Name("Comprehend Speech")

                .AddPerkLevel()
                .Description("The caster counts has having 5 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.")
                .Price(1)
                .RequirementSkill(SkillType.Force, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ComprehendSpeech1)

                .AddPerkLevel()
                .Description("The caster counts has having 10 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.")
                .Price(1)
                .RequirementSkill(SkillType.Force, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ComprehendSpeech2)

                .AddPerkLevel()
                .Description("The caster counts has having 15 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.")
                .Price(1)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ComprehendSpeech3)

                .AddPerkLevel()
                .Description("The caster counts has having 20 extra ranks in all languages for the purpose of understanding others speaking, so long as they concentrate.")
                .Price(1)
                .RequirementSkill(SkillType.Force, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ComprehendSpeech4);
        }



        private void MindTrick()
        {
            _builder.Create(PerkCategoryType.ForceUniversal, PerkType.MindTrick)
                .Name("Mind Trick")

                .AddPerkLevel()
                .Description("Confuses a single non-mechanical target for six seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.MindTrick1)

                .AddPerkLevel()
                .Description("Confuses all hostile non-mechanical targets within 10m for six seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.MindTrick2);
        }



        private void ForceBody()
        {
            _builder.Create(PerkCategoryType.ForceUniversal, PerkType.ForceBody)
                .Name("Force Body")

                .AddPerkLevel()
                .Description("Grants FP regeneration based on your Willpower Score, at the cost of your Vitality.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceBody1)

                .AddPerkLevel()
                .Description("Grants FP regeneration based on your Willpower Score, at the cost of your Vitality.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceBody2);
        }

    }
}

