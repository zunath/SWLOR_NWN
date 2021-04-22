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
            HeadImplantBlueprints(builder);
            ArmImplantBlueprints(builder);
            LegImplantBlueprints(builder);
            BodyImplantBlueprints(builder);

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
                .GrantsFeat(FeatType.ImplantInstallation1)

                .AddPerkLevel()
                .Description("Grants a temporary buff which enables your target to install tier 3 implants.")
                .Price(2)
                .RequirementSkill(SkillType.Cybertech, 20)
                .GrantsFeat(FeatType.ImplantInstallation2)

                .AddPerkLevel()
                .Description("Grants a temporary buff which enables your target to install tier 4 implants.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 30)
                .GrantsFeat(FeatType.ImplantInstallation3)

                .AddPerkLevel()
                .Description("Grants a temporary buff which enables your target to install tier 5 implants.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 40)
                .GrantsFeat(FeatType.ImplantInstallation4);
        }

        private void HeadImplantBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Cybertech, PerkType.HeadImplantBlueprints)
                .Name("Head Implant Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Head Implant blueprints.")
                .Price(1)
                .GrantsFeat(FeatType.HeadImplantBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Head Implant blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Cybertech, 10)
                .GrantsFeat(FeatType.HeadImplantBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Head Implant blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Cybertech, 20)
                .GrantsFeat(FeatType.HeadImplantBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Head Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 30)
                .GrantsFeat(FeatType.HeadImplantBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Head Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 40)
                .GrantsFeat(FeatType.HeadImplantBlueprints5);
        }

        private void ArmImplantBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Cybertech, PerkType.ArmImplantBlueprints)
                .Name("Arm Implant Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Arm Implant blueprints.")
                .Price(1)
                .GrantsFeat(FeatType.ArmImplantBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Arm Implant blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Cybertech, 10)
                .GrantsFeat(FeatType.ArmImplantBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Arm Implant blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Cybertech, 20)
                .GrantsFeat(FeatType.ArmImplantBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Arm Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 30)
                .GrantsFeat(FeatType.ArmImplantBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Arm Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 40)
                .GrantsFeat(FeatType.ArmImplantBlueprints5);
        }

        private void LegImplantBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Cybertech, PerkType.LegImplantBlueprints)
                .Name("Leg Implant Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Leg Implant blueprints.")
                .Price(1)
                .GrantsFeat(FeatType.LegImplantBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Leg Implant blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Cybertech, 10)
                .GrantsFeat(FeatType.LegImplantBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Leg Implant blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Cybertech, 20)
                .GrantsFeat(FeatType.LegImplantBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Leg Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 30)
                .GrantsFeat(FeatType.LegImplantBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Leg Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 40)
                .GrantsFeat(FeatType.LegImplantBlueprints5);
        }

        private void BodyImplantBlueprints(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.Cybertech, PerkType.BodyImplantBlueprints)
                .Name("Body Implant Blueprints")

                .AddPerkLevel()
                .Description("Grants access to tier 1 Body Implant blueprints.")
                .Price(1)
                .GrantsFeat(FeatType.BodyImplantBlueprints1)

                .AddPerkLevel()
                .Description("Grants access to tier 2 Body Implant blueprints.")
                .Price(1)
                .RequirementSkill(SkillType.Cybertech, 10)
                .GrantsFeat(FeatType.BodyImplantBlueprints2)

                .AddPerkLevel()
                .Description("Grants access to tier 3 Body Implant blueprints.")
                .Price(2)
                .RequirementSkill(SkillType.Cybertech, 20)
                .GrantsFeat(FeatType.BodyImplantBlueprints3)

                .AddPerkLevel()
                .Description("Grants access to tier 4 Body Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 30)
                .GrantsFeat(FeatType.BodyImplantBlueprints4)

                .AddPerkLevel()
                .Description("Grants access to tier 5 Body Implant blueprints.")
                .Price(3)
                .RequirementSkill(SkillType.Cybertech, 40)
                .GrantsFeat(FeatType.BodyImplantBlueprints5);
        }
    }
}
