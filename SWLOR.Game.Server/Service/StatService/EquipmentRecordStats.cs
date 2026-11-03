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
        FPRegen = 128,
        STMRegen = 256,
        Attack = 512,
        Accuracy = 1024,
        ForceAttack = 2048,
        Resistance = 4096,
        Stealth = 8192,
        Detection = 16384,
        TrapBonus = 32768,
        TrapDisarm = 65536,
        PoisonBonus = 131072,
        Lockpicking = 262144,
        All = Defense | Evasion | FP | Stamina | Control | Craftsmanship | CPBonus | FPRegen | STMRegen | Attack | Accuracy |
              ForceAttack | Resistance | Stealth | Detection | TrapBonus | TrapDisarm | PoisonBonus | Lockpicking
    }
}
