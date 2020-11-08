using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class CybertechPerkDefinition : IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();
            ImplantInstallation(builder);
            NeckImplantBlueprints(builder);
            HeadImplantBlueprints(builder);
            ArmImplantBlueprints(builder);
            LegImplantBlueprints(builder);
            FootImplantBlueprints(builder);
            ChestImplantBlueprints(builder);
            HandImplantBlueprints(builder);
            EyeImplantBlueprints(builder);

            return builder.Build();
        }

        private void ImplantInstallation(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Weaponsmith, PerkType.ArmorModInstallation)
                .Name("Implant Installation")

                .AddPerkLevel()
                .Description("Enables you to install tier 1 implants on yourself or another person.")
                .Price(1)
                .RequirementSkill(SkillType.Cybertech, 5)

                .AddPerkLevel()
                .Description("Enables you to install tier 2 implants on yourself or another person.")
                .Price(1)
                .RequirementSkill(SkillType.Cybertech, 15)

                .AddPerkLevel()
                .Description("Enables you to install tier 3 implants on yourself or another person.")
                .Price(2)
                .RequirementSkill(SkillType.Cybertech, 25)

                .AddPerkLevel()
                .Description("Enables you to install tier 4 implants on yourself or another person.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 35)

                .AddPerkLevel()
                .Description("Enables you to install tier 5 implants on yourself or another person.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 45);
        }

        private void NeckImplantBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Cybertech, PerkType.NeckImplantBlueprints)
                .Name("Neck Implant Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Neck Implant blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Neck Implant blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Cybertech, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Neck Implant blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Cybertech, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Neck Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Neck Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 40);
        }

        private void HeadImplantBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Cybertech, PerkType.HeadImplantBlueprints)
                .Name("Head Implant Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Head Implant blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Head Implant blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Cybertech, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Head Implant blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Cybertech, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Head Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Head Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 40);
        }

        private void ArmImplantBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Cybertech, PerkType.ArmImplantBlueprints)
                .Name("Arm Implant Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Arm Implant blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Arm Implant blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Cybertech, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Arm Implant blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Cybertech, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Arm Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Arm Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 40);
        }

        private void LegImplantBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Cybertech, PerkType.LegImplantBlueprints)
                .Name("Leg Implant Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Leg Implant blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Leg Implant blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Cybertech, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Leg Implant blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Cybertech, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Leg Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Leg Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 40);
        }

        private void FootImplantBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Cybertech, PerkType.FootImplantBlueprints)
                .Name("Foot Implant Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Foot Implant blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Foot Implant blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Cybertech, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Foot Implant blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Cybertech, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Foot Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Foot Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 40);
        }

        private void ChestImplantBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Cybertech, PerkType.ChestImplantBlueprints)
                .Name("Chest Implant Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Chest Implant blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Chest Implant blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Cybertech, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Chest Implant blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Cybertech, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Chest Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Chest Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 40);
        }

        private void HandImplantBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Cybertech, PerkType.HandImplantBlueprints)
                .Name("Hand Implant Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Hand Implant blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Hand Implant blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Cybertech, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Hand Implant blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Cybertech, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Hand Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Hand Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 40);
        }

        private void EyeImplantBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Cybertech, PerkType.EyeImplantBlueprints)
                .Name("Eye Implant Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Eye Implant blueprints.")
                .Price(1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Eye Implant blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Cybertech, 10)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Eye Implant blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Cybertech, 20)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Eye Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 30)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Eye Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 40);
        }

    }
}
