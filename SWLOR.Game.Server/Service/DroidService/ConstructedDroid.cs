using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;

namespace SWLOR.Game.Server.Service.DroidService
{
    public class ConstructedDroid
    {
        public string Name { get; set; }
        public string SerializedCPU { get; set; }
        public string SerializedHead { get; set; }
        public string SerializedBody { get; set; }
        public string SerializedArms { get; set; }
        public string SerializedLegs { get; set; }
        public Dictionary<CreaturePart, int> AppearanceParts { get; set; }

        public Dictionary<InventorySlot, string> EquippedItems { get; set; }
        public Dictionary<string, string> Inventory { get; set; }

        public ConstructedDroid()
        {
            Name = string.Empty;
            SerializedCPU = string.Empty;
            SerializedHead = string.Empty;
            SerializedBody = string.Empty;
            SerializedArms = string.Empty;
            SerializedLegs = string.Empty;
            AppearanceParts = new Dictionary<CreaturePart, int>();
            EquippedItems = new Dictionary<InventorySlot, string>();
            Inventory = new Dictionary<string, string>();
        }
    }
}
