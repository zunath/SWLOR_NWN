namespace SWLOR.Game.Server.Service.CombatRecorderService
{
    public sealed class CombatRecordHealingTotals
    {
        public int Count { get; set; }
        public long Requested { get; set; }
        public long Effective { get; set; }

        /// <summary>Healing the PvP healing reduction took off before it landed; not part of <see cref="Requested"/>.</summary>
        public long PvpReduced { get; set; }
        public long Overheal => Requested - Effective;
    }
}
