using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public sealed class ForceUniversalPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            ForcePush();
            ThrowLightsaber();
            ForceLeap();
            Precognition();
            ForceConvergence();

            return _builder.Build();
        }

        private void ForcePush()
        {
            _builder.Create(PerkCategoryType.ForceUniversal, PerkType.ForcePush)
                .Name("Force Push")

                .AddPerkLevel()
                .Description("Deals 8 force DMG to one target, knocks down for 2 seconds, and slows movement for 3 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForcePush1)

                .AddPerkLevel()
                .Description("Deals 12 force DMG to the selected target and up to 1 additional target in a line, knocks down for 2 seconds, and slows movement for 3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 28)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForcePush2)

                .AddPerkLevel()
                .Description("Deals 18 force DMG to the selected target and up to 2 additional targets in a cone, knocks down for 2 seconds, and slows movement for 4 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 48)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForcePush3);
        }

        private void ThrowLightsaber()
        {
            _builder.Create(PerkCategoryType.ForceUniversal, PerkType.ThrowLightsaber)
                .Name("Throw Lightsaber")

                .AddPerkLevel()
                .Description("Hurls your equipped weapon with the Force up to 15m, dealing weapon DMG + 10 physical DMG plus WIL/PER scaling to one target.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 8)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ThrowLightsaber1)

                .AddPerkLevel()
                .Description("Hurls your equipped weapon with the Force up to 15m, dealing weapon DMG + 20 physical DMG plus WIL/PER scaling to the selected target and one additional enemy along the path.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ThrowLightsaber2)

                .AddPerkLevel()
                .Description("Hurls your equipped weapon with the Force up to 15m, dealing weapon DMG + 30 physical DMG plus WIL/PER scaling to the selected target and up to two additional enemies along the path.")
                .Price(5)
                .RequirementSkill(SkillType.Force, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ThrowLightsaber3);
        }

        private void ForceLeap()
        {
            _builder.Create(PerkCategoryType.ForceUniversal, PerkType.ForceLeap)
                .Name("Force Leap")

                .AddPerkLevel()
                .Description("Leap to a hostile target up to 15m away, dealing 10 force DMG plus WIL scaling and interrupting activation.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceLeap1)

                .AddPerkLevel()
                .Description("Leap to a hostile target up to 18m away, dealing 18 force DMG plus WIL scaling and interrupting activation.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceLeap2);
        }

        private void Precognition()
        {
            _builder.Create(PerkCategoryType.ForceUniversal, PerkType.Precognition)
                .Name("Precognition")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PrecognitionTrait)
                .Description("After spending FP on a Force power, gain +5% Defense and +5% Evasion for 8 seconds. This can trigger once every 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 22)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .IncreasesStat(StatType.ForcePrecognition, 1);
        }

        private void ForceConvergence()
        {
            _builder.Create(PerkCategoryType.ForceUniversal, PerkType.ForceConvergence)
                .Name("Force Convergence")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ForceConvergenceTrait)
                .Description("After spending FP on a Force power, restore 20% of your maximum FP over 10 seconds and gain +5% Force ability Accuracy for 10 seconds. This can trigger once every 45 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 48)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .IncreasesStat(StatType.ForceConvergence, 1);
        }
    }
}
