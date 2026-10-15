namespace SWLOR.Game.Server.Service.CombatRecorderService
{
    public enum CombatRecordEventType : byte
    {
        RecordingStarted = 0,
        RecordingStopped = 1,
        Participant = 2,
        AbilityCast = 3,
        AbilityQueued = 4,
        AbilityImpact = 5,
        AbilityCost = 6,
        Attack = 7,
        Damage = 8,
        TemporaryHitPointsAbsorbed = 9,
        Healing = 10,
        TemporaryHitPointsGranted = 11,
        Resource = 12,
        StatusApplied = 13,
        StatusRefreshed = 14,
        StatusRemoved = 15,
        Death = 16,
        Resurrection = 17,
        Subdual = 18,
        Snapshot = 19,
        AbilityDenied = 20,
        AbilityInterrupted = 21,
        TemporaryHitPointsTrimmed = 22
    }
}
