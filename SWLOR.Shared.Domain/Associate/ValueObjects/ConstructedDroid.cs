using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Shared.Domain.Associate.ValueObjects
{
    public class ConstructedDroid
    {
        public string Name { get; set; }
        public string SerializedCPU { get; set; }
        public string SerializedHead { get; set; }
        public string SerializedBody { get; set; }
        public string SerializedArms { get; set; }
        public string SerializedLegs { get; set; }
        public int PortraitId { get; set; }
        public int SoundSetId { get; set; }
        public Dictionary<CreaturePartType, int> AppearanceParts { get; set; }
        public List<DroidPerk> LearnedPerks { get; set; }
        public List<DroidPerk> ActivePerks { get; set; }
        public Dictionary<InventorySlotType, string> EquippedItems { get; set; }
        public Dictionary<string, string> Inventory { get; set; }

        public ConstructedDroid()
        {
            Name = string.Empty;
            SerializedCPU = string.Empty;
            SerializedHead = string.Empty;
            SerializedBody = string.Empty;
            SerializedArms = string.Empty;
            SerializedLegs = string.Empty;
            PortraitId = -1;
            SoundSetId = -1;
            AppearanceParts = new Dictionary<CreaturePartType, int>();
            LearnedPerks = new List<DroidPerk>();
            ActivePerks = new List<DroidPerk>();
            EquippedItems = new Dictionary<InventorySlotType, string>();
            Inventory = new Dictionary<string, string>();
        }
    }
}
