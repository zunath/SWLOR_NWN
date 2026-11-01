namespace SWLOR.Game.Server.Entity
{
    public class CasinoDailyPlay : EntityBase
    {
        public DateTime DayUtc { get; set; }
        public int Spins { get; set; }
        public int ProfitableWins { get; set; }
        public DateTime NextSpinUtc { get; set; }
    }
}
