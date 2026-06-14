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
            ThrowRock();
            Benevolence();
            ForceJudgment();
            RadiantLance();
            Renewal();
            SereneFocus();
            MindTrick();
            ForceMend();
            ForceSanctuary();
            HarmonicRestoration();

            return _builder.Build();
        }

        private void ThrowRock()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.ThrowRock)
                .Name("Throw Rock")
                .ForceAffinity(ForceAffinityType.Light)

                .AddPerkLevel()
                .Description("Hurls stone or loose debris with the Force up to 15m, dealing 18 physical DMG plus WIL/PER scaling to one target.")
                .Price(2)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ThrowRock1)

                .AddPerkLevel()
                .Description("Hurls a heavier stone or debris with the Force up to 15m, dealing 32 physical DMG plus WIL/PER scaling to one target.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 18)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ThrowRock2)

                .AddPerkLevel()
                .Description("Hurls a crushing mass of stone and debris with the Force up to 15m, dealing 46 physical DMG plus WIL/PER scaling to one target.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ThrowRock3);
        }

        private void Benevolence()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.Benevolence)
                .Name("Benevolence")
                .ForceAffinity(ForceAffinityType.Light)

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
                .RequirementSkill(SkillType.Force, 38)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.Benevolence3);
        }

        private void ForceJudgment()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.ForceJudgment)
                .Name("Force Judgment")
                .ForceAffinity(ForceAffinityType.Light)

                .AddPerkLevel()
                .Description("Deals 14 force DMG plus WIL scaling to one target and reduces outgoing weapon and force damage by 4% for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceJudgment1)

                .AddPerkLevel()
                .Description("Deals 24 force DMG plus WIL scaling to the selected target and one nearby enemy, reducing outgoing weapon and force damage by 6% for 12 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceJudgment2)

                .AddPerkLevel()
                .Description("Deals 36 force DMG plus WIL scaling to the selected target and nearby enemies, reducing outgoing weapon and force damage by 8% for 12 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceJudgment3);
        }

        private void RadiantLance()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.RadiantLance)
                .Name("Radiant Lance")
                .ForceAffinity(ForceAffinityType.Light)

                .AddPerkLevel()
                .Description("Fires a focused lance of radiant Force energy in an 8m line, dealing 12 force DMG plus WIL scaling to hostile targets in the line.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 8)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.RadiantLance1)

                .AddPerkLevel()
                .Description("Fires a focused lance of radiant Force energy in an 8m line, dealing 22 force DMG plus WIL scaling to hostile targets in the line.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.RadiantLance2)

                .AddPerkLevel()
                .Description("Fires a focused lance of radiant Force energy in an 8m line, dealing 32 force DMG plus WIL scaling to hostile targets in the line.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 48)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.RadiantLance3);
        }

        private void Renewal()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.Renewal)
                .Name("Renewal")
                .ForceAffinity(ForceAffinityType.Light)

                .AddPerkLevel()
                .Description("Applies regeneration to a single ally, restoring 2% of maximum HP plus WIL scaling every 3 seconds for 18 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 8)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.Renewal1)

                .AddPerkLevel()
                .Description("Applies regeneration to a single ally, restoring 4% of maximum HP plus WIL scaling every 3 seconds for 18 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.Renewal2)

                .AddPerkLevel()
                .Description("Applies regeneration to a single ally, restoring 6% of maximum HP plus WIL scaling every 3 seconds for 18 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.Renewal3);
        }

        private void SereneFocus()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.SereneFocus)
                .Name("Serene Focus")
                .ForceAffinity(ForceAffinityType.Light)

                .AddPerkLevel()
                .Description("Control powers that restore HP cause affected allies to restore 1 STM and 1 FP every 6 seconds for 12 seconds. This benefit does not trigger when you target yourself.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 12)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .IncreasesStat(StatType.ControlHealingSereneFocus, 1);
        }

        private void MindTrick()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.MindTrick)
                .Name("Mind Trick")

                .AddPerkLevel()
                .Description("Attempts to inflict Confusion on one non-mechanical target for 10 seconds. Caster Willpower increases duration, while target Willpower and Mind Resistance reduce it.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 12)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.MindTrick1)

                .AddPerkLevel()
                .Description("Attempts to inflict Confusion on the selected non-mechanical target and one nearby non-mechanical target for 10 seconds. Caster Willpower increases duration, while target Willpower and Mind Resistance reduce it.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 38)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.MindTrick2);
        }

        private void ForceMend()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.ForceMend)
                .Name("Force Mend")
                .ForceAffinity(ForceAffinityType.Light)

                .AddPerkLevel()
                .Description("Control powers that restore HP can remove one standard negative effect from the target and restore HP equal to 10% of maximum HP plus WIL scaling. This can trigger once every 24 seconds per target.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .IncreasesStat(StatType.ControlHealingForceMend, 1);
        }

        private void ForceSanctuary()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.ForceSanctuary)
                .Name("Force Sanctuary")
                .ForceAffinity(ForceAffinityType.Light)

                .AddPerkLevel()
                .Description("Creates a 4m sanctuary for 18 seconds. Allies inside gain regeneration equal to 2% of maximum HP plus WIL scaling every 3 seconds and take 5% less force damage.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 32)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceSanctuary1);
        }

        private void HarmonicRestoration()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.HarmonicRestoration)
                .Name("Harmonic Restoration")
                .ForceAffinity(ForceAffinityType.Light)

                .AddPerkLevel()
                .Description("When you restore HP to an ally below 50% HP with a Control power, up to two nearby allies recover 6% of maximum HP plus WIL scaling and gain +10 Trauma Resistance rating for 12 seconds. This can trigger once every 20 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .IncreasesStat(StatType.HarmonicRestoration, 1);
        }

    }
}
