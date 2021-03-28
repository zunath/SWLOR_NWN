using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
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
            builder.Create(PerkCategoryType.Cybertech, PerkType.ImplantInstallation)
                .Name("Implant Installation")
                
                .AddPerkLevel()
                .Description("Grants a temporary buff which enables your target to install tier 2 implants.")
                .Price(2)
                .RequirementSkill(SkillType.Cybertech, 10)
                .GrantsFeat(Feat.ImplantInstallation1)

                .AddPerkLevel()
                .Description("Grants a temporary buff which enables your target to install tier 3 implants.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 20)
                .GrantsFeat(Feat.ImplantInstallation2)

                .AddPerkLevel()
                .Description("Grants a temporary buff which enables your target to install tier 4 implants.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 30)
                .GrantsFeat(Feat.ImplantInstallation3)

                .AddPerkLevel()
                .Description("Grants a temporary buff which enables your target to install tier 5 implants.")
                .Price(4)
                .RequirementSkill(SkillType.Cybertech, 40)
                .GrantsFeat(Feat.ImplantInstallation4);
        }

        private void NeckImplantBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Cybertech, PerkType.NeckImplantBlueprints)
                .Name("Neck Implant Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Neck Implant blueprints.")
                .Price(1)
                .GrantsFeat(Feat.NeckImplantBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Neck Implant blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Cybertech, 10)
                .GrantsFeat(Feat.NeckImplantBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Neck Implant blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Cybertech, 20)
                .GrantsFeat(Feat.NeckImplantBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Neck Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 30)
                .GrantsFeat(Feat.NeckImplantBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Neck Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 40)
                .GrantsFeat(Feat.NeckImplantBlueprints5);
        }

        private void HeadImplantBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Cybertech, PerkType.HeadImplantBlueprints)
                .Name("Head Implant Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Head Implant blueprints.")
                .Price(1)
                .GrantsFeat(Feat.HeadImplantBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Head Implant blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Cybertech, 10)
                .GrantsFeat(Feat.HeadImplantBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Head Implant blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Cybertech, 20)
                .GrantsFeat(Feat.HeadImplantBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Head Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 30)
                .GrantsFeat(Feat.HeadImplantBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Head Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 40)
                .GrantsFeat(Feat.HeadImplantBlueprints5);
        }

        private void ArmImplantBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Cybertech, PerkType.ArmImplantBlueprints)
                .Name("Arm Implant Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Arm Implant blueprints.")
                .Price(1)
                .GrantsFeat(Feat.ArmImplantBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Arm Implant blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Cybertech, 10)
                .GrantsFeat(Feat.ArmImplantBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Arm Implant blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Cybertech, 20)
                .GrantsFeat(Feat.ArmImplantBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Arm Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 30)
                .GrantsFeat(Feat.ArmImplantBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Arm Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 40)
                .GrantsFeat(Feat.ArmImplantBlueprints5);
        }

        private void LegImplantBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Cybertech, PerkType.LegImplantBlueprints)
                .Name("Leg Implant Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Leg Implant blueprints.")
                .Price(1)
                .GrantsFeat(Feat.LegImplantBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Leg Implant blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Cybertech, 10)
                .GrantsFeat(Feat.LegImplantBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Leg Implant blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Cybertech, 20)
                .GrantsFeat(Feat.LegImplantBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Leg Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 30)
                .GrantsFeat(Feat.LegImplantBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Leg Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 40)
                .GrantsFeat(Feat.LegImplantBlueprints5);
        }

        private void FootImplantBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Cybertech, PerkType.FootImplantBlueprints)
                .Name("Foot Implant Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Foot Implant blueprints.")
                .Price(1)
                .GrantsFeat(Feat.FootImplantBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Foot Implant blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Cybertech, 10)
                .GrantsFeat(Feat.FootImplantBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Foot Implant blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Cybertech, 20)
                .GrantsFeat(Feat.FootImplantBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Foot Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 30)
                .GrantsFeat(Feat.FootImplantBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Foot Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 40)
                .GrantsFeat(Feat.FootImplantBlueprints5);
        }

        private void ChestImplantBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Cybertech, PerkType.ChestImplantBlueprints)
                .Name("Chest Implant Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Chest Implant blueprints.")
                .Price(1)
                .GrantsFeat(Feat.ChestImplantBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Chest Implant blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Cybertech, 10)
                .GrantsFeat(Feat.ChestImplantBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Chest Implant blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Cybertech, 20)
                .GrantsFeat(Feat.ChestImplantBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Chest Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 30)
                .GrantsFeat(Feat.ChestImplantBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Chest Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 40)
                .GrantsFeat(Feat.ChestImplantBlueprints5);
        }

        private void HandImplantBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Cybertech, PerkType.HandImplantBlueprints)
                .Name("Hand Implant Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Hand Implant blueprints.")
                .Price(1)
                .GrantsFeat(Feat.HeadImplantBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Hand Implant blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Cybertech, 10)
                .GrantsFeat(Feat.HeadImplantBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Hand Implant blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Cybertech, 20)
                .GrantsFeat(Feat.HeadImplantBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Hand Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 30)
                .GrantsFeat(Feat.HeadImplantBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Hand Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 40)
                .GrantsFeat(Feat.HeadImplantBlueprints5);
        }

        private void EyeImplantBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Cybertech, PerkType.EyeImplantBlueprints)
                .Name("Eye Implant Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Eye Implant blueprints.")
                .Price(1)
                .GrantsFeat(Feat.EyeImplantBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Eye Implant blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Cybertech, 10)
                .GrantsFeat(Feat.EyeImplantBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Eye Implant blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Cybertech, 20)
                .GrantsFeat(Feat.EyeImplantBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Eye Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 30)
                .GrantsFeat(Feat.EyeImplantBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Eye Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 40)
                .GrantsFeat(Feat.EyeImplantBlueprints5);
        }

    }
}
