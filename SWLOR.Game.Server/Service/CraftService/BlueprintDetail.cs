using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service.CraftService
{
    public class BlueprintDetail
    {
        public RecipeType Recipe { get; set; }
        public int Level { get; set; }
        public int LicensedRuns { get; set; }
        public int ItemBonuses { get; set; }
        public int CreditReduction { get; set; }
        public int TimeReduction { get; set; }
        public int EnhancementSlots { get; set; }
        public bool RandomEnhancementSlotGranted { get; set; }
        public List<ItemProperty> GuaranteedBonuses { get; set; }

        /// <summary>A ship module blueprint's guaranteed bonuses: blueprint traits stamped on every module it crafts.</summary>
        public List<ShipComponentTraitType> GuaranteedTraits { get; set; }

        public BlueprintDetail()
        {
            Recipe = RecipeType.Invalid;
            GuaranteedBonuses = new List<ItemProperty>();
            GuaranteedTraits = new List<ShipComponentTraitType>();
        }
    }
}
