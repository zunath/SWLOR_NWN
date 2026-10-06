using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class CapacitorTransferAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipCapacitorTransfer1, PerkType.ShipCapacitorTransfer).Name("Capacitor Transfer I").Level(1).SkillType(SkillType.ShipSystems).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipCapacitorTransfer, 24f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipCapacitorTransfer1));
            builder.Create(FeatType.ShipCapacitorTransfer2, PerkType.ShipCapacitorTransfer).Name("Capacitor Transfer II").Level(2).SkillType(SkillType.ShipSystems).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipCapacitorTransfer, 24f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipCapacitorTransfer2));
            builder.Create(FeatType.ShipCapacitorTransfer3, PerkType.ShipCapacitorTransfer).Name("Capacitor Transfer III").Level(3).SkillType(SkillType.ShipSystems).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipCapacitorTransfer, 24f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipCapacitorTransfer3));
            return builder.Build();
        }
    }
}
