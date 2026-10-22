using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service
{
    public static class CasinoDailyLimit
    {
        public const int MaximumSpins = 20;
        public const int MaximumProfitableWins = 3;
        private const int ResetHourUtc = 10;

        public static string GetAccountId(string publicCdKey)
        {
            if (string.IsNullOrWhiteSpace(publicCdKey))
                throw new ArgumentException("An account is required.", nameof(publicCdKey));
            return $"casino-slots:{publicCdKey}";
        }

        public static DateTime GetNextResetUtc(DateTime utcNow) =>
            utcNow.AddHours(-ResetHourUtc).Date.AddDays(1).AddHours(ResetHourUtc);

        public static void ResetIfNeeded(CasinoDailyPlay play, DateTime utcNow)
        {
            var day = utcNow.AddHours(-ResetHourUtc).Date;
            if (play.DayUtc >= day)
                return;
            play.DayUtc = day;
            play.Spins = 0;
            play.ProfitableWins = 0;
        }

        public static string GetDailyFailure(CasinoDailyPlay play)
        {
            if (play.Spins >= MaximumSpins)
                return "You have used all 20 account spins for today. Come back after the daily reset.";
            if (play.ProfitableWins >= MaximumProfitableWins)
                return "You have reached 3 profitable wins on this account today. Come back after the daily reset.";
            return string.Empty;
        }

        public static string GetFailure(CasinoDailyPlay play, DateTime utcNow)
        {
            ResetIfNeeded(play, utcNow);
            var failure = GetDailyFailure(play);
            if (!string.IsNullOrEmpty(failure))
                return failure;
            return utcNow < play.NextSpinUtc ? "Please wait three seconds between spins." : string.Empty;
        }

        public static void RecordSpin(CasinoDailyPlay play, DateTime utcNow, int payout)
        {
            if (payout < 0 || payout > CasinoSlot.MaximumPayout)
                throw new ArgumentOutOfRangeException(nameof(payout));
            var failure = GetFailure(play, utcNow);
            if (!string.IsNullOrEmpty(failure))
                throw new InvalidOperationException(failure);
            play.Spins++;
            if (payout > CasinoSlot.SpinCost)
                play.ProfitableWins++;
            play.NextSpinUtc = utcNow.AddSeconds(3);
        }
    }
}
