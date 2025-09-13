

namespace SWLOR.Game.Server.Core.NWNX.Enum
{
    public class ItemPropertyUnpacked
    {
        public ItemPropertyUnpacked()
        {
            Id = string.Empty;
            Tag = string.Empty;
        }

        public string Id { get; set; }
        public int Property { get; set; }
        public int SubType { get; set; }
        public int CostTable { get; set; }
        public int CostTableValue { get; set; }
        public int Param1 { get; set; }
        public int Param1Value { get; set; }
        public int UsesPerDay { get; set; }
        public int ChanceToAppear { get; set; }
        public bool IsUseable { get; set; }
        public int SpellId { get; set; }
        public uint Creator { get; set; }
        public string Tag { get; set; }
    }
}