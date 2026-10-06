using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public sealed class ShipManufacturingPerkDefinition : IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();
            builder.Create(PerkCategoryType.Engineering, PerkType.ShipManufacturing).Name("Ship Manufacturing")
                .AddPerkLevel().Price(1).RequirementSkill(SkillType.Engineering, 2)
                .Description("Manufacture ship equipment with Engineering requirements below 10. Operating skills are separate.")
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Engineering, 10)
                .Description("Manufacture ship equipment with Engineering requirements below 20.")
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Engineering, 20)
                .Description("Manufacture ship equipment with Engineering requirements below 35.")
                .AddPerkLevel().Price(4).RequirementSkill(SkillType.Engineering, 35)
                .Description("Manufacture ship equipment with Engineering requirements below 45.")
                .AddPerkLevel().Price(5).RequirementSkill(SkillType.Engineering, 45)
                .Description("Manufacture every ship recipe through Engineering rank 50.");
            return builder.Build();
        }
    }
}
