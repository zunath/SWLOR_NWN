using System.ComponentModel;

namespace SWLOR.Game.Server.Enumeration
{
    public enum AssociationType
    {
        [Description("Jedi Order")]
        JediOrder = 1,
        [Description("Mandalorian")]
        Mandalorian = 2,
        [Description("Sith Empire")]
        SithEmpire = 3,
        [Description("Smugglers")]
        Smugglers = 4,
        [Description("Unaligned")]
        Unaligned = 5,
        [Description("Hutt Cartel")]
        HuttCartel = 6,
        [Description("Republic")]
        Republic = 7,
        [Description("Czerka")]
        Czerka = 8,
        [Description("Sith Order")]
        SithOrder = 9
    }
}
