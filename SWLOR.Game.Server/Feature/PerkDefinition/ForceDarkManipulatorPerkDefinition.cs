using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public sealed class ForceDarkManipulatorPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            CreepingTerror();
            ForceGrip();
            WeakenResolve();
            FractureFocus();
            MindShroud();
            NightmareField();
            ForceChoke();
            DominateWeakMind();
            CollapseWill();
            EclipseOfResolve();

            return _builder.Build();
        }

        private void CreepingTerror()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.CreepingTerror)
                .Name("Creeping Terror")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .Description("Hobble one target for 6 seconds and applies force damage over time equal to 12 force DMG plus WIL scaling over 18 seconds.")
                .Price(2)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.CreepingTerror1)

                .AddPerkLevel()
                .Description("Hobble up to 2 targets for 6 seconds and applies force damage over time equal to 12 force DMG plus WIL scaling over 18 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 18)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.CreepingTerror2)

                .AddPerkLevel()
                .Description("Hobble nearby enemies for 6 seconds and applies force damage over time equal to 12 force DMG plus WIL scaling over 18 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 42)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.CreepingTerror3);
        }

        private void ForceGrip()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.ForceGrip)
                .Name("Force Grip")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .Description("Immobilize one target for 3 seconds and interrupt activation.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceGrip1)

                .AddPerkLevel()
                .Description("Immobilize one target for 4 seconds and interrupt activation.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 22)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceGrip2)

                .AddPerkLevel()
                .Description("Immobilize up to 2 targets for 4 seconds and interrupt activation.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 48)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceGrip3);
        }

        private void WeakenResolve()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.WeakenResolve)
                .Name("Weaken Resolve")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .Description("Increase force damage taken by 5% for 24 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 8)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.WeakenResolve1)

                .AddPerkLevel()
                .Description("Increase force damage taken by 10% for 24 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 28)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.WeakenResolve2);
        }

        private void FractureFocus()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.FractureFocus)
                .Name("Fracture Focus")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .Description("Increase one target's FP and STM ability costs by 20% for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 12)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.FractureFocus1)

                .AddPerkLevel()
                .Description("Increase nearby enemies' FP and STM ability costs by 25% for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 38)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.FractureFocus2);
        }

        private void MindShroud()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.MindShroud)
                .Name("Mind Shroud")

                .AddPerkLevel()
                .Description("Reduces force damage taken by 5% and grants +10% resistance to confusion, daze, and fear for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.MindShroud1)

                .AddPerkLevel()
                .Description("Reduces force damage taken by 10% and grants +15% resistance to confusion, daze, and fear for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.MindShroud2);
        }

        private void NightmareField()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.NightmareField)
                .Name("Nightmare Field")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .Description("Nearby enemies suffer -10 Accuracy and -10 Evasion for 18 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.NightmareField1);
        }

        private void ForceChoke()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.ForceChoke)
                .Name("Force Choke")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .Description("Daze one target for 3 seconds and applies force damage over time equal to 12 force DMG plus WIL scaling over 12 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceChoke1);
        }

        private void DominateWeakMind()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.DominateWeakMind)
                .Name("Dominate Weak Mind")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .Description("Inflicts Foggy Mind on one non-mechanical target for 8 seconds. Mind resistance shortens the duration. Mind-immune targets suffer -15 Accuracy instead.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.DominateWeakMind1);
        }

        private void CollapseWill()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.CollapseWill)
                .Name("Collapse Will")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .Description("Apply Exposed and Force Erosion for 18 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.CollapseWill1);
        }

        private void EclipseOfResolve()
        {
            _builder.Create(PerkCategoryType.ForceDark, PerkType.EclipseOfResolve)
                .Name("Eclipse of Resolve")
                .ForceAffinity(ForceAffinityType.Dark)

                .AddPerkLevel()
                .Description("Nearby enemies suffer -15% hit chance, -15% evasion chance, and +25% FP and STM costs for 45 seconds.")
                .Price(5)
                .RequirementSkill(SkillType.Force, 50)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.EclipseOfResolve1);
        }

    }
}
