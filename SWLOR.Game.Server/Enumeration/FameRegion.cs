using System.ComponentModel;

namespace SWLOR.Game.Server.Enumeration
{
    public enum FameRegion
    {
        [Description("Invalid")]
        Invalid = 0,
        [Description("Global")]
        Global = 1,
        [Description("CZ-220")]
        CZ220 = 2,
        [Description("Veles Colony")]
        VelesColony = 3,
        [Description("Coxxion Organization")]
        CoxxionOrganization = 4
    }
}
