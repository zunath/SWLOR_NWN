namespace SWLOR.CombatSim.Report
{
    public static class Percentile
    {
        public static double? Of(IReadOnlyList<double> values, double percentile)
        {
            if (values == null || values.Count == 0)
                return null;

            var sorted = values.OrderBy(v => v).ToList();
            var rank = percentile / 100.0 * (sorted.Count - 1);
            var lower = (int)Math.Floor(rank);
            var upper = (int)Math.Ceiling(rank);
            return sorted[lower] + (sorted[upper] - sorted[lower]) * (rank - lower);
        }
    }
}
