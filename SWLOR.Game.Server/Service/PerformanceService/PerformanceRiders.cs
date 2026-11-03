using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>
    /// Which routine and set riders a followed performer's Inspired carries. A rider's strength comes from the
    /// performer it belongs to, so a partner's routine keeps the partner's own Light Feet speed or Peak Form bonus.
    /// </summary>
    public static class PerformanceRiders
    {
        /// <summary>
        /// The dancer's routine, their second routine with the capstone, and a partner's routines when either dancer has
        /// Partner Dance. Never more than two, never the same routine twice, and Cantina Shuffle carries no rider.
        /// </summary>
        public static List<(PerformanceRoutineType Routine, PerformerSnapshot Source)> GetRoutineRiders(PerformerSnapshot dancer)
        {
            var riders = new List<(PerformanceRoutineType, PerformerSnapshot)>(PerformanceAudienceRules.MaxRoutineRiders);
            if (dancer == null)
                return riders;

            AddRoutines(riders, dancer);
            if (dancer.Partner != null && (dancer.HasPartnerTrait || dancer.Partner.HasPartnerTrait))
                AddRoutines(riders, dancer.Partner);

            return riders;
        }

        /// <summary>
        /// The musician's set, their second set with the capstone, and a band partner's sets when either musician has Duet.
        /// Never more than two, never the same set twice, and Cantina Standards carries no rider.
        /// </summary>
        public static List<(PerformanceSetType Set, PerformerSnapshot Source)> GetSetRiders(PerformerSnapshot musician)
        {
            var riders = new List<(PerformanceSetType, PerformerSnapshot)>(PerformanceAudienceRules.MaxSetRiders);
            if (musician == null)
                return riders;

            AddSets(riders, musician);
            if (musician.Partner != null && (musician.HasPartnerTrait || musician.Partner.HasPartnerTrait))
                AddSets(riders, musician.Partner);

            return riders;
        }

        private static void AddRoutines(List<(PerformanceRoutineType, PerformerSnapshot)> riders, PerformerSnapshot source)
        {
            TryAdd(riders, source.Routine, source);
            if (source.HasCapstone)
                TryAdd(riders, source.SecondRoutine, source);
        }

        private static void AddSets(List<(PerformanceSetType, PerformerSnapshot)> riders, PerformerSnapshot source)
        {
            TryAdd(riders, source.Set, source);
            if (source.HasCapstone)
                TryAdd(riders, source.SecondSet, source);
        }

        private static void TryAdd(List<(PerformanceRoutineType, PerformerSnapshot)> riders, PerformanceRoutineType routine, PerformerSnapshot source)
        {
            if (routine == PerformanceRoutineType.CantinaShuffle || riders.Count >= PerformanceAudienceRules.MaxRoutineRiders)
                return;

            foreach (var (existing, _) in riders)
            {
                if (existing == routine)
                    return;
            }

            riders.Add((routine, source));
        }

        private static void TryAdd(List<(PerformanceSetType, PerformerSnapshot)> riders, PerformanceSetType set, PerformerSnapshot source)
        {
            if (set == PerformanceSetType.CantinaStandards || riders.Count >= PerformanceAudienceRules.MaxSetRiders)
                return;

            foreach (var (existing, _) in riders)
            {
                if (existing == set)
                    return;
            }

            riders.Add((set, source));
        }
    }
}
