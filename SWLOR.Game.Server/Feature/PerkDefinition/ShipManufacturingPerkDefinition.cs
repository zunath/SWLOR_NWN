using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public sealed class ShipManufacturingPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Manufacturing();
            return _builder.Build();
        }
        private void Manufacturing()
        {
            var builder = _builder;
            builder.Create(PerkCategoryType.Engineering, PerkType.ShipManufacturing).Name("Ship Manufacturing").Icon("ife_shipmanuf")
                .AddPerkLevel().GrantsFeat(FeatType.ShipManufacturingTrait).Price(1).RequirementSkill(SkillType.Engineering, 2)
                .Description("Manufacture ship equipment with Engineering requirements below 10. Operating skills are separate.")
                .AddPerkLevel().Price(2).RequirementSkill(SkillType.Engineering, 10)
                .Description("Manufacture ship equipment with Engineering requirements below 20.")
                .AddPerkLevel().Price(3).RequirementSkill(SkillType.Engineering, 20)
                .Description("Manufacture ship equipment with Engineering requirements below 35.")
                .AddPerkLevel().Price(4).RequirementSkill(SkillType.Engineering, 35)
                .Description("Manufacture ship equipment with Engineering requirements below 45.")
                .AddPerkLevel().Price(5).RequirementSkill(SkillType.Engineering, 45)
                .Description("Manufacture every ship recipe through Engineering rank 50.");
        }
    }
}
