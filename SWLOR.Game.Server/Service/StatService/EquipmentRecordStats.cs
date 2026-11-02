using System;

namespace SWLOR.Game.Server.Service.StatService
{
    /// <summary>
    /// Player records that hold a running sum of what equipped gear grants.
    /// </summary>
    [Flags]
    public enum EquipmentRecordStats
    {
        None = 0,
        Defense = 1,
        Evasion = 2,
        FP = 4,
        Stamina = 8,
        Control = 16,
        Craftsmanship = 32,
        CPBonus = 64,
        All = Defense | Evasion | FP | Stamina | Control | Craftsmanship | CPBonus
    }
}
