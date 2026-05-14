using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public sealed class ForceLightConsularPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Benevolence();
            Pacify();
            Renewal();
            Clarity();
            MindTrick();
            ComprehendSpeech();
            ForceMend();
            ForceSanctuary();
            CircleOfHarmony();

            return _builder.Build();
        }

        private void Benevolence()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.Benevolence)
                .Name("Benevolence")

                .AddPerkLevel()
                .Description("Restores 8% of the target's maximum HP plus WIL scaling to a single target. Healing gains +25% when targeting someone other than yourself.")
                .Price(2)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.Benevolence1)

                .AddPerkLevel()
                .Description("Restores 14% of the target's maximum HP plus WIL scaling to a single target. Healing gains +25% when targeting someone other than yourself.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 18)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.Benevolence2)

                .AddPerkLevel()
                .Description("Restores 20% of the target's maximum HP plus WIL scaling to a single target. Healing gains +25% when targeting someone other than yourself.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 42)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.Benevolence3);
        }

        private void Pacify()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.Pacify)
                .Name("Pacify")

                .AddPerkLevel()
                .Description("Reduce a target's outgoing weapon and force damage by 5% for 20 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.Pacify1)

                .AddPerkLevel()
                .Description("Reduce up to 2 nearby enemies' outgoing weapon and force damage by 8% for 20 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 28)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.Pacify2)

                .AddPerkLevel()
                .Description("Reduces nearby enemies' outgoing weapon and force damage by 12% for 20 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 48)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.Pacify3);
        }

        private void Renewal()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.Renewal)
                .Name("Renewal")

                .AddPerkLevel()
                .Description("Applies regeneration to a single ally, restoring 2% of maximum HP plus WIL scaling every 3 seconds for 18 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 8)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.Renewal1)

                .AddPerkLevel()
                .Description("Applies regeneration to a single ally, restoring 3% of maximum HP plus WIL scaling every 3 seconds for 18 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.Renewal2)

                .AddPerkLevel()
                .Description("Applies regeneration to a single ally, restoring 4% of maximum HP plus WIL scaling every 3 seconds for 18 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.Renewal3);
        }

        private void Clarity()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.Clarity)
                .Name("Clarity")

                .AddPerkLevel()
                .Description("Restores 10% of maximum STM to an ally and increases physical and force ability hit chance by 4% for 15 seconds. Self-target restores FP instead.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 12)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.Clarity1)

                .AddPerkLevel()
                .Description("Restores 18% of maximum STM to an ally and increases physical and force ability hit chance by 6% for 15 seconds. Self-target restores FP instead.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 38)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.Clarity2);
        }

        private void MindTrick()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.MindTrick)
                .Name("Mind Trick")

                .AddPerkLevel()
                .Description("Confuse one non-mechanical target for 5 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.MindTrick1)

                .AddPerkLevel()
                .Description("Confuse up to 2 non-mechanical targets for 5 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.MindTrick2);
        }

        private void ComprehendSpeech()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.ComprehendSpeech)
                .Name("Comprehend Speech")

                .AddPerkLevel()
                .Description("For 15 minutes, you count as having 15 additional ranks in all languages for understanding spoken speech.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 22)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ComprehendSpeech1);
        }

        private void ForceMend()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.ForceMend)
                .Name("Force Mend")

                .AddPerkLevel()
                .Description("Removes one major negative effect from a single ally and restores HP equal to 16% of the target's maximum HP plus WIL scaling.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceMend1);
        }

        private void ForceSanctuary()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.ForceSanctuary)
                .Name("Force Sanctuary")

                .AddPerkLevel()
                .Description("Creates a 4m sanctuary for 18 seconds. Allies inside gain regeneration equal to 2% of maximum HP plus WIL scaling every 3 seconds and take 5% less force damage.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceSanctuary1);
        }

        private void CircleOfHarmony()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.CircleOfHarmony)
                .Name("Circle of Harmony")

                .AddPerkLevel()
                .Description("Restores 14% of maximum HP plus WIL scaling to nearby allies, removes one standard negative effect, and grants 1 FP and 1 STM every 3 seconds for 18 seconds.")
                .Price(5)
                .RequirementSkill(SkillType.Force, 50)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.CircleOfHarmony1);
        }

    }
}
