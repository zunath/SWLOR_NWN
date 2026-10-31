namespace SWLOR.Game.Server.Service.PazaakService
{
    public class PazaakSetResult
    {
        public PazaakSetResult(int setNumber, int winnerSeat, int[] totals, bool wasBust, bool wasFullTable)
        {
            SetNumber = setNumber;
            WinnerSeat = winnerSeat;
            Totals = totals;
            WasBust = wasBust;
            WasFullTable = wasFullTable;
        }

        public int SetNumber { get; }

        /// <summary>
        /// Seat index of the set winner, or -1 for a tie.
        /// </summary>
        public int WinnerSeat { get; }

        public int[] Totals { get; }
        public bool WasBust { get; }
        public bool WasFullTable { get; }
    }
}
