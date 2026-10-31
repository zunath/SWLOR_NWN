using System;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The repair bill of a ship (plan section 7, Cost): the missing shield and hull priced per point, reduced by the
    /// owner's starport and Social discounts, then scaled by the hull's repair multiplier (Small x1.0, Medium x1.5,
    /// Large x2.5, Capital x4.0 in the Starships (Revamp) tab).
    /// </summary>
    public static class ShipRepairBill
    {
        public const int PricePerShieldPoint = 50;
        public const int PricePerHullPoint = 100;

        /// <summary>A legacy capital ship, which has no revamp hull row, keeps the flat multiplier it always had.</summary>
        public const double LegacyCapitalMultiplier = 5.0;

        public const double StarportDiscountPerLevel = 0.05;
        public const double SocialDiscountPerPoint = 0.02;
        public const double MaximumSocialDiscount = 0.20;
        public const double MaximumTotalDiscount = 0.90;

        /// <summary>The multiplier for a ship: its revamp hull row, or the legacy rule for ships without one.</summary>
        public static double GetSizeMultiplier(ShipHullEntry hull, bool legacyCapitalShip)
        {
            if (hull != null && hull.RepairMultiplier > 0)
                return hull.RepairMultiplier;

            return legacyCapitalShip ? LegacyCapitalMultiplier : 1.0;
        }

        /// <summary>The total discount from the owner starport upgrade level and Social score, capped at 90%.</summary>
        public static double GetDiscount(int starportLevel, int social)
        {
            var starport = starportLevel * StarportDiscountPerLevel;
            var socialDiscount = Math.Clamp((social - 10) * SocialDiscountPerPoint, 0.0, MaximumSocialDiscount);
            return Math.Min(starport + socialDiscount, MaximumTotalDiscount);
        }

        /// <summary>The bill in credits.</summary>
        /// <param name="shieldMissing">Shield points below the maximum.</param>
        /// <param name="hullMissing">Hull points below the maximum.</param>
        /// <param name="sizeMultiplier">The hull repair multiplier.</param>
        /// <param name="starportLevel">The owner effective starport upgrade level.</param>
        /// <param name="social">The owner Social score.</param>
        public static int Calculate(int shieldMissing, int hullMissing, double sizeMultiplier, int starportLevel, int social)
        {
            var price = Math.Max(0, shieldMissing) * PricePerShieldPoint + Math.Max(0, hullMissing) * PricePerHullPoint;
            price -= (int)(price * GetDiscount(starportLevel, social));
            return (int)Math.Round(price * sizeMultiplier, MidpointRounding.AwayFromZero);
        }
    }
}
