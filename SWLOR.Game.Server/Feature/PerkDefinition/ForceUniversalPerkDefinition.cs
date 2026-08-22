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
            _builder.Create(PerkCategoryType.ForceAlter, PerkType.ForcePush)
                .Name("Force Push")

                .AddPerkLevel()
                .Description("Deals 8 force DMG to one target in a 5m x 5m cone, knocks it down for 6 seconds, and slows its movement for 12 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForcePush1)

                .AddPerkLevel()
                .Description("Deals 12 force DMG to up to 2 targets in an 8m x 5m cone, knocks them down for 6 seconds, and slows their movement for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 28)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForcePush2)

                .AddPerkLevel()
                .Description("Deals 18 force DMG to up to 3 targets in a 10m x 5m cone, knocks them down for 6 seconds, and slows their movement for 12 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 48)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForcePush3);
        }

        private void ThrowLightsaber()
        {
            _builder.Create(PerkCategoryType.ForceAlter, PerkType.ThrowLightsaber)
                .Name("Throw Lightsaber")

                .AddPerkLevel()
                .Description("Hurls your equipped weapon with the Force through a 15m x 2.5m line, dealing weapon DMG + 10 physical DMG plus WIL/PER scaling to one target in the line.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 8)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ThrowLightsaber1)

                .AddPerkLevel()
                .Description("Hurls your equipped weapon with the Force through a 15m x 2.5m line, dealing weapon DMG + 20 physical DMG plus WIL/PER scaling to the selected target and one additional enemy in the line.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ThrowLightsaber2)

                .AddPerkLevel()
                .Description("Hurls your equipped weapon with the Force through a 15m x 2.5m line, dealing weapon DMG + 30 physical DMG plus WIL/PER scaling to the selected target and up to two additional enemies in the line.")
                .Price(5)
                .RequirementSkill(SkillType.Force, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ThrowLightsaber3);
        }

        private void ForceLeap()
        {
            _builder.Create(PerkCategoryType.ForceControl, PerkType.ForceLeap)
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
            _builder.Create(PerkCategoryType.ForceSense, PerkType.Precognition)
                .Name("Precognition")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PrecognitionTrait)
                .Description("After spending FP on a Force power, gain +5% Defense and +5% Evasion for 30 seconds. This can trigger once every 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 22)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .IncreasesStat(StatType.ForcePrecognition, 1);
        }

        private void ForceConvergence()
        {
            _builder.Create(PerkCategoryType.ForceControl, PerkType.ForceConvergence)
                .Name("Force Convergence")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ForceConvergenceTrait)
                .Description("After spending FP on a Force power, restore 20% of your maximum FP over 10 seconds and gain +5% Force ability Accuracy for 30 seconds. This can trigger once every 45 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 48)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .IncreasesStat(StatType.ForceConvergence, 1);
        }
    }
}
