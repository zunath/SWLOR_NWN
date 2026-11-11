namespace SWLOR.Game.Server.Service
{
    public static class CasinoSlotPresentation
    {
        public const string CreditImage = "cas_credit";
        public const string StarImage = "cas_star";
        public const string HuttImage = "cas_hutt";
        public const string BigWinImage = "cas_bigwin";

        public static string[] GetReels(int payout) => payout switch
        {
            0 => new[] { HuttImage, StarImage, CreditImage },
            100 => new[] { StarImage, StarImage, HuttImage },
            500 => new[] { StarImage, StarImage, StarImage },
            1000 => new[] { HuttImage, HuttImage, HuttImage },
            10000 => new[] { CreditImage, CreditImage, CreditImage },
            _ => throw new ArgumentOutOfRangeException(nameof(payout))
        };

        public static string GetHeadline(int payout) => payout switch
        {
            0 => "TRY AGAIN",
            100 => "BREAK EVEN",
            500 => "WIN!",
            1000 => "BIG WIN!",
            10000 => "JACKPOT!",
            _ => throw new ArgumentOutOfRangeException(nameof(payout))
        };

        public static bool IsBigWin(int payout) => payout >= 1000;
    }
}
