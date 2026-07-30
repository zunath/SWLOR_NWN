

namespace SWLOR.NWN.API.NWNX.Enum
{
    public class QuickBarSlot
    {
        public uint? Item { get; set; }
        public uint? SecondaryItem { get; set; }
        public QuickBarSlotType ObjectType { get; set; }
        public int MultiClass { get; set; }
        public string Resref { get; set; } = string.Empty;
        public string CommandLabel { get; set; } = string.Empty;
        public string CommandLine { get; set; } = string.Empty;
        public string ToolTip { get; set; } = string.Empty;
        public int INTParam1 { get; set; }
        public int MetaType { get; set; }
        public int DomainLevel { get; set; }
        public int AssociateType { get; set; }
        public uint? Associate { get; set; }
    }
}
