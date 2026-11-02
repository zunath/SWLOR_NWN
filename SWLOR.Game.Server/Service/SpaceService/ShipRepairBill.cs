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

        /// <summary>
        /// The total discount from the owner starport upgrade level and Social score, and the share the owner's city adds (Drydock
        /// Subsidy), capped at 90%.
        /// </summary>
        public static double GetDiscount(int starportLevel, int social, double cityDiscount = 0)
        {
            var starport = starportLevel * StarportDiscountPerLevel;
            var socialDiscount = Math.Clamp((social - 10) * SocialDiscountPerPoint, 0.0, MaximumSocialDiscount);
            return Math.Min(starport + socialDiscount + Math.Max(0, cityDiscount), MaximumTotalDiscount);
        }

        /// <summary>The bill in credits.</summary>
        /// <param name="shieldMissing">Shield points below the maximum.</param>
        /// <param name="hullMissing">Hull points below the maximum.</param>
        /// <param name="sizeMultiplier">The hull repair multiplier.</param>
        /// <param name="starportLevel">The owner effective starport upgrade level.</param>
        /// <param name="social">The owner Social score.</param>
        /// <param name="cityDiscount">The share the owner's city adds to the discount (Drydock Subsidy), or 0.</param>
        public static int Calculate(int shieldMissing, int hullMissing, double sizeMultiplier, int starportLevel, int social, double cityDiscount = 0)
        {
            var price = Math.Max(0, shieldMissing) * PricePerShieldPoint + Math.Max(0, hullMissing) * PricePerHullPoint;
            price -= (int)(price * GetDiscount(starportLevel, social, cityDiscount));
            return (int)Math.Round(price * sizeMultiplier, MidpointRounding.AwayFromZero);
        }
    }
}
