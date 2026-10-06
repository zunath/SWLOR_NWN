using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Ships
{
    public sealed class DepositAnalysisAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder.Create(FeatType.ShipDepositAnalysis1, PerkType.ShipDepositAnalysis).Name("Deposit Analysis I").Level(1).SkillType(SkillType.Astrometrics).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipDepositAnalysis, 24f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipDepositAnalysis1));
            builder.Create(FeatType.ShipDepositAnalysis2, PerkType.ShipDepositAnalysis).Name("Deposit Analysis II").Level(2).SkillType(SkillType.Astrometrics).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipDepositAnalysis, 24f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipDepositAnalysis2));
            builder.Create(FeatType.ShipDepositAnalysis3, PerkType.ShipDepositAnalysis).Name("Deposit Analysis III").Level(3).SkillType(SkillType.Astrometrics).CanBeUsedInSpace().IsCastedAbility().HasRecastDelay(RecastGroup.ShipDepositAnalysis, 24f).ShipTechnique(ShipTechniqueCatalog.Default.Get(FeatType.ShipDepositAnalysis3));
            return builder.Build();
        }
    }
}
