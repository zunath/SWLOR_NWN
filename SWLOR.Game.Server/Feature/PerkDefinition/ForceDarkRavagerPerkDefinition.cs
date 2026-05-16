using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public sealed class ForceDarkRavagerPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            ForceSpark();
            ForceBody();
            ForceLightning();
            ForceDrain();
            SaberRend();
            ForceRage();
            DevouringStrike();
            ForceMaelstrom();
            HungerOfTheDark();

            return _builder.Build();
        }

        private void ForceSpark()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.ForceSpark)
                .Name("Force Spark")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .Description("Deals 18 force DMG plus WIL scaling to one target and reduce evasion chance by 4% for 20 seconds.")
                .Price(2)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceSpark1)

                .AddPerkLevel()
                .Description("Deals 32 force DMG plus WIL scaling to one target and reduce evasion chance by 6% for 20 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 22)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceSpark2)

                .AddPerkLevel()
                .Description("Deals 50 force DMG plus WIL scaling to one target and reduce evasion chance by 8% for 20 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 48)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceSpark3);
        }

        private void ForceBody()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.ForceBody)
                .Name("Force Body")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .Description("For 30 seconds, your damaging Dark powers restore 1 FP, but each cast costs HP equal to 2% of your maximum HP.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceBody1)

                .AddPerkLevel()
                .Description("For 30 seconds, damaging Dark powers restore FP. Each cast costs HP, reduced when you damage a target below 50% HP.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 38)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceBody2);
        }

        private void ForceLightning()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.ForceLightning)
                .Name("Force Lightning")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .Description("Deals 14 electrical force DMG plus WIL scaling to up to 3 targets.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 8)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceLightning1)

                .AddPerkLevel()
                .Description("Deals 24 electrical force DMG plus WIL scaling to up to 4 targets.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceLightning2);
        }

        private void ForceDrain()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.ForceDrain)
                .Name("Force Drain")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .Description("Deals 16 force DMG plus WIL scaling and heals you for 35% of damage dealt.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 12)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceDrain1)

                .AddPerkLevel()
                .Description("Deals 28 force DMG plus WIL scaling and heals you for 40% of damage dealt.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 28)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceDrain2)

                .AddPerkLevel()
                .Description("Deals 44 force DMG plus WIL scaling and heals you for 45% of damage dealt.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 42)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceDrain3);
        }

        private void SaberRend()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.SaberRend)
                .Name("Saber Rend")

                .AddPerkLevel()
                .Description("Your next melee attack deals +12 force DMG plus WIL scaling. Requires a melee weapon.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.SaberRend1)

                .AddPerkLevel()
                .Description("Your next melee attack deals +24 force DMG plus WIL scaling. Requires a melee weapon.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.SaberRend2);
        }

        private void ForceRage()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.ForceRage)
                .Name("Force Rage")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .Description("Increases outgoing weapon and force damage by 8% and critical damage by 10% for 20 seconds, but increases damage taken by 5%.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 18)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceRage1)

                .AddPerkLevel()
                .Description("Increases outgoing weapon and force damage by 14% and critical damage by 15% for 20 seconds, but increases damage taken by 8%.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceRage2);
        }

        private void DevouringStrike()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.DevouringStrike)
                .Name("Devouring Strike")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .Description("Deals force DMG to one target. If the target is below 35% HP, damage is increased by 40%.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.DevouringStrike1);
        }

        private void ForceMaelstrom()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.ForceMaelstrom)
                .Name("Force Maelstrom")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .Description("Deals force DMG to nearby enemies and pulls them slightly toward you.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceMaelstrom1);
        }

        private void HungerOfTheDark()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.HungerOfTheDark)
                .Name("Hunger of the Dark")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .Description("For 12 seconds, Dark damage you deal heals you for 25% of damage dealt and defeated enemies restore FP.")
                .Price(5)
                .RequirementSkill(SkillType.Force, 50)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.HungerOfTheDark1);
        }

    }
}
