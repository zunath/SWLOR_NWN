

namespace SWLOR.Game.Server.Core.NWNX.Enum
{
    public class ItemPropertyUnpacked
    {
        public ItemPropertyUnpacked()
        {
            Tag = string.Empty;
        }

        public int Property { get; set; }
        public int SubType { get; set; }
        public int CostTable { get; set; }
        public int CostTableValue { get; set; }
        public int Param1 { get; set; }
        public int Param1Value { get; set; }
        public int UsesPerDay { get; set; }
        public int ChanceToAppear { get; set; }
        public bool IsUseable { get; set; }
        public int SpellID { get; set; }
        public uint Creator { get; set; }
        public string Tag { get; set; }
    }
}