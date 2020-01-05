using System.ComponentModel;

namespace SWLOR.Game.Server.Enumeration
{
    public enum CraftDeviceType
    {
        [Description("Invalid")]
        Invalid = 0,
        [Description("Armorsmith Bench")]
        ArmorsmithBench = 1,
        [Description("Weaponsmith Bench")]
        WeaponsmithBench = 2,
        [Description("Cookpot")]
        Cookpot = 3,
        [Description("Engineering Bench")]
        EngineeringBench = 4,
        [Description("Fabrication Terminal")]
        FabricationTerminal = 5,
        [Description("Medicine Bench")]
        MedicineBench = 6
    }
}
