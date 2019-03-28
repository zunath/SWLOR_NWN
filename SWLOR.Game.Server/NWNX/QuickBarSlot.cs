using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.NWNX
{
    public class QuickBarSlot
    {
        public NWItem Item { get; set; }
        public NWItem SecondaryItem { get; set; }
        public QuickBarSlotType ObjectType { get; set; }
        public int MultiClass { get; set; }
        public string Resref { get; set; }
        public string CommandLabel { get; set; }
        public string CommandLine { get; set; }
        public string ToolTip { get; set; }
        public int INTParam1 { get; set; }
        public int MetaType { get; set; }
        public int DomainLevel { get; set; }
        public int AssociateType { get; set; }
        public NWObject Associate { get; set; }
    }
}
