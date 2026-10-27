namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// What a component trait changes. Percent stats change the module's own numbers; Evasion and Speed add to the
    /// ship (points, and percent points of speed), Yield adds Module Bonus points and RareFind adds to the rare-find roll.
    /// </summary>
    public enum ShipTraitStat
    {
        Invalid = 0,
        Damage = 1,
        Accuracy = 2,
        CapacitorCost = 3,
        Hull = 4,
        Shield = 5,
        Repair = 6,
        Capacitor = 7,
        CapacitorRegen = 8,
        Defense = 9,
        Resistance = 10,
        Evasion = 11,
        Speed = 12,
        Yield = 13,
        RareFind = 14,
    }
}
