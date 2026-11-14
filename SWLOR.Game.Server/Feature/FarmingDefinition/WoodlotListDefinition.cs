using System.Collections.Generic;
using SWLOR.Game.Server.Service.FarmingService;

namespace SWLOR.Game.Server.Feature.FarmingDefinition
{
    /// <summary>
    /// The five woodlot saplings of the Player City Revamp, one for each harvesting tree tier. A sapling takes 72 hours to grow
    /// whatever its tier and yields 4 logs of the tier's wood (<see cref="WoodlotRules"/>). They are kept apart from the 26 crops
    /// of <see cref="CropListDefinition"/> so that the lists animals eat and farmhands are fed from never hold wood.
    /// </summary>
    public class WoodlotListDefinition : ICropListDefinition
    {
        private readonly CropBuilder _builder = new();

        public Dictionary<CropType, CropDefinition> BuildCrops()
        {
            Sapling(CropType.WoodSapling, "Wood Tree", 1);
            Sapling(CropType.FineWoodSapling, "Fine Wood Tree", 2);
            Sapling(CropType.AncientWoodSapling, "Ancient Wood Tree", 3);
            Sapling(CropType.AraciaWoodSapling, "Aracia Wood Tree", 4);
            Sapling(CropType.HyphaeWoodSapling, "Hyphae Wood Tree", 5);

            return _builder.Build();
        }

        private void Sapling(CropType type, string name, int tier)
        {
            _builder.Create(type)
                .Name(name)
                .Tier(tier)
                .Sapling()
                .Seed(WoodlotRules.GetSaplingResref(tier))
                .Harvest(WoodlotRules.GetWoodResref(tier));
        }
    }
}
