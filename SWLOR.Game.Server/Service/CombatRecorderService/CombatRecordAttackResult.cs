namespace SWLOR.Game.Server.Service.CombatRecorderService
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
