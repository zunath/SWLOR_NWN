namespace SWLOR.Game.Server.Service.AIService
{
    public enum AITriggerType
    {
        Invalid = 0,
        Spawn = 1,
        Heartbeat = 2,
        Perception = 3,
        CombatRound = 4,
        Damaged = 5,
        Attacked = 6,
        Disturbed = 7,
        Aggro = 8,
        AbilityCompleted = 9,
        BossTimer = 10,
        Death = 11
    }
}
