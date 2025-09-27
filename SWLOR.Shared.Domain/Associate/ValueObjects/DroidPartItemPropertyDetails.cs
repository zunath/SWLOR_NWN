using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Shared.Domain.Associate.ValueObjects
{
    public class DroidPartItemPropertyDetails
    {
        public ItemPropertyDroidPartSubType PartType { get; set; }
        public int Tier { get; set; }
        public int Level { get; set; }
        public int HP { get; set; }
        public int STM { get; set; }
        public int AISlots { get; set; }
        public int AGI { get; set; }
        public int MGT { get; set; }
        public int PER { get; set; }
        public int SOC { get; set; }
        public int VIT { get; set; }
        public int WIL { get; set; }
        public int OneHanded { get; set; }
        public int TwoHanded { get; set; }
        public int MartialArts { get; set; }
        public int Ranged { get; set; }

        public DroidPartItemPropertyDetails()
        {
            PartType = ItemPropertyDroidPartSubType.Invalid;
        }
    }
}
