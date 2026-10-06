using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class PreparedVolleyAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipPreparedVolley1, PerkType.ShipPreparedVolley).Name("Prepared Volley I").Level(1).SkillType(SkillType.Gunnery).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipPreparedVolley, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipPreparedVolley1));
            builder.Create(FeatType.ShipPreparedVolley2, PerkType.ShipPreparedVolley).Name("Prepared Volley II").Level(2).SkillType(SkillType.Gunnery).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipPreparedVolley, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipPreparedVolley2));
            builder.Create(FeatType.ShipPreparedVolley3, PerkType.ShipPreparedVolley).Name("Prepared Volley III").Level(3).SkillType(SkillType.Gunnery).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipPreparedVolley, 30f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipPreparedVolley3));
            return builder.Build();
        }
    }
}
