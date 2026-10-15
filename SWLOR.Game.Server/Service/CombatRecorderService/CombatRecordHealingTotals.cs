namespace SWLOR.Game.Server.Service.CombatRecorderService
{
    public sealed class CombatRecordHealingTotals
    {
        public int Count { get; set; }
        public long Requested { get; set; }
        public long Effective { get; set; }
        public long Overheal => Requested - Effective;
    }
}
