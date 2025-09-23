using SWLOR.NWN.API.Engine;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Shared.Domain.Model
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
            GuaranteedBonuses = new List<ItemProperty>();
        }
    }
}
