namespace SWLOR.Game.Server.Service.PerformanceService
{
    public static class PerformanceQualityExtensions
    {
        public static double GetBankingMultiplier(this PerformanceQuality quality)
        {
            return quality switch
            {
                PerformanceQuality.Rough => 0.5,
                PerformanceQuality.Great => 1.25,
                PerformanceQuality.Dazzling => 1.5,
                _ => 1.0
            };
        }
    }
}
