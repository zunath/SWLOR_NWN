using System.Collections.Generic;
using SWLOR.Game.Server.Core;

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

        public BlueprintDetail()
        {
            Recipe = RecipeType.Invalid;
            Level = -1;
            LicensedRuns = -1;
            GuaranteedBonuses = new List<ItemProperty>();
        }
    }
}
