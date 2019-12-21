using NWN;

namespace SWLOR.Game.Server.NWNX
{
    public class ItemPropertyUnpacked
    {
        public int ItemPropertyID { get; set; }
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
        public NWGameObject Creator { get; set; }
        public string Tag { get; set; }

        public ItemPropertyUnpacked()
        {
            Creator = new NWGameObject();
            Tag = string.Empty;
        }
    }
}
