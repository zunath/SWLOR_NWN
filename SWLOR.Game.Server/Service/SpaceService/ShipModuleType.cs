namespace SWLOR.Game.Server.Service.SpaceService
{
    public enum ShipModuleType
    {
        Invalid = 0,
        Passive = 1,
        CombatLaser = 2,
        IonCannon = 3,
        Missile = 4,
        HullRepairer = 5,
        ShieldRepairer = 6,
        MiningLaser = 7,
        HypermatterInjector = 8,
        StormCannon = 9,
        AssaultConcussionMissile = 10,
        BulwarkShieldGenerator = 11,
        RepairFieldGenerator = 12,
        LaserBattery = 13,
        QuadLaser = 14,
        TurboLaser = 15,
        BeamLaser = 16,
        ProtonBomb = 17,
        StripMiner = 18,
        CapitalEwar = 19,
        // 20 was CapitalPowerDiverter, never used (plan section 16, bug 6); the Power Diverter is a passive.
        ShieldHardener = 21,
        Salvager = 22,
        GasHarvester = 23,
        ProbeLauncher = 24,
        DataAnalyzer = 25,
        CloakingDevice = 26,
        IonWeb = 27,
        Interdictor = 28,
        Jammer = 29,
        TargetPainter = 30,
        Afterburner = 31,
    }
}
