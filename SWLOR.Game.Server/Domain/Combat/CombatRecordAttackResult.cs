namespace SWLOR.Game.Server.Domain.Combat
{
    public enum CombatRecordAttackResult : byte
    {
        Unknown = 0,
        Hit = 1,
        Critical = 2,
        Miss = 3,
        Deflect = 4
    }
}
