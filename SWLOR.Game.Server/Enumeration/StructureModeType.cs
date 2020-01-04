using System.ComponentModel;

namespace SWLOR.Game.Server.Enumeration
{
    public enum StructureModeType
    {
        [Description("None")]
        None = 0,
        [Description("Residence")]
        Residence = 1,
        [Description("Workshop")]
        Workshop = 2,
        [Description("Storefront")]
        Storefront = 3
    }
}
