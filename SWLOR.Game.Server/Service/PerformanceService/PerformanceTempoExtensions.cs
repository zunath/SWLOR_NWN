namespace SWLOR.Game.Server.Service.PerformanceService
{
    public static class PerformanceTempoExtensions
    {
        public static double GetBeatSeconds(this PerformanceTempo tempo)
        {
            return tempo switch
            {
                PerformanceTempo.Lively => 1.2,
                PerformanceTempo.Upbeat => 1.0,
                _ => 1.5
            };
        }

        public static double GetXPMultiplier(this PerformanceTempo tempo)
        {
            return tempo switch
            {
                PerformanceTempo.Lively => 1.15,
                PerformanceTempo.Upbeat => 1.3,
                _ => 1.0
            };
        }

        public static PerformanceTempo Next(this PerformanceTempo tempo)
        {
            return tempo switch
            {
                PerformanceTempo.Relaxed => PerformanceTempo.Lively,
                PerformanceTempo.Lively => PerformanceTempo.Upbeat,
                _ => PerformanceTempo.Relaxed
            };
        }
    }
}
