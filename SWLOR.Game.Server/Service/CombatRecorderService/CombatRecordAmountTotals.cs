namespace SWLOR.Game.Server.Service.CombatRecorderService
{
    public sealed class CombatRecordAmountTotals
    {
        public int Count { get; set; }
        public long Amount { get; set; }

        public void Add(long amount)
        {
            Count++;
            Amount += amount;
        }
    }
}
