namespace SWLOR.Game.Server.Service.CombatRecorderService
{
    public sealed class CombatRecordDamageTotals
    {
        public int Hits { get; set; }

        /// <summary>Damage carried by the applied effects before temporary HP.</summary>
        public long Total { get; set; }
        public long HitPointsLost { get; set; }
        public long TemporaryHitPointsAbsorbed { get; set; }

        /// <summary>HP actually removed plus temporary HP consumed.</summary>
        public long Effective => HitPointsLost + TemporaryHitPointsAbsorbed;
    }
}
