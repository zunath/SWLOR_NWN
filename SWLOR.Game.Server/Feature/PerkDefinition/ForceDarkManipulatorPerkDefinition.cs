using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Feature.QuestDefinition;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public sealed class ForceDarkManipulatorPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            CreepingTerror();
            WeakenResolve();
            NightmareField();
            ForceChoke();
            CollapseWill();
            EclipseOfResolve();

            return _builder.Build();
        }

        private void CreepingTerror()
        {
            _builder.Create(PerkCategoryType.ForceAlter, PerkType.CreepingTerror)
                .Name("Creeping Terror")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .Description("Creates a visible 5m field within 15m for 30 seconds. Enemies inside are Hobbled and take 10 force DMG plus WIL scaling every 3 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 2)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.CreepingTerror1)

                .AddPerkLevel()
                .Description("Creates a visible 5m field within 15m for 30 seconds. Enemies inside are Hobbled and take 14 force DMG plus WIL scaling every 3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.CreepingTerror2)

                .AddPerkLevel()
                .Description("Creates a visible 8m field within 15m for 30 seconds. Enemies inside are Hobbled and take 18 force DMG plus WIL scaling every 3 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 38)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.CreepingTerror3);
        }

        private void WeakenResolve()
        {
            _builder.Create(PerkCategoryType.ForceSense, PerkType.WeakenResolve)
                .Name("Weaken Resolve")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .Description("Increases force damage taken by 5% for 24 seconds.")
                .Price(3)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.WeakenResolve1)

                .AddPerkLevel()
                .Description("Increases force damage taken by 10% for 24 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 28)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.WeakenResolve2);
        }

        private void NightmareField()
        {
            _builder.Create(PerkCategoryType.ForceSense, PerkType.NightmareField)
                .Name("Nightmare Field")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .Description("Enemies within 5m suffer -10% physical and Force ability Accuracy and -10% Evasion for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 18)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.NightmareField1);
        }

        private void ForceChoke()
        {
            _builder.Create(PerkCategoryType.ForceAlter, PerkType.ForceChoke)
                .Name("Force Choke")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .Description("Immobilizes one target for 30 seconds, interrupts activation, and deals 8 force DMG plus WIL scaling over the duration.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 8)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceChoke1)

                .AddPerkLevel()
                .Description("Immobilizes one target for 30 seconds, interrupts activation, and deals 16 force DMG plus WIL scaling over the duration.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 20)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceChoke2)

                .AddPerkLevel()
                .Description("Immobilizes one target for 30 seconds, interrupts activation, and deals 24 force DMG plus WIL scaling over the duration.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceChoke3)

                .AddPerkLevel()
                .Description("Immobilizes one target for 30 seconds, interrupts activation, and deals 34 force DMG plus WIL scaling over the duration.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 48)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceChoke4);
        }

        private void CollapseWill()
        {
            _builder.Create(PerkCategoryType.ForceSense, PerkType.CollapseWill)
                .Name("Collapse Will")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .GrantsFeat(FeatType.CollapseWillTrait)
                .Description("Nightmare Field and Eclipse of Resolve also apply Exposed and Force Erosion for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 42)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .IncreasesStat(StatType.DarkManipulatorCollapseWill, 1);
        }

        private void EclipseOfResolve()
        {
            _builder.Create(PerkCategoryType.ForceSense, PerkType.EclipseOfResolve)
                .Name("Eclipse of Resolve")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .Description("Enemies within 5m suffer -12% physical and Force ability Accuracy, -12% Evasion, and +20% FP and STM costs for 30 seconds.")
                .Price(5)
                .RequirementSkill(SkillType.Force, 50)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.EclipseOfResolve1)
                .RequirementQuest(ForceCapstoneQuestDefinition.EclipseOfResolveMasteryQuestId);
        }

    }
}
