using System;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.HoldingService
{
    /// <summary>
    /// A holding's credit payments (buying, upgrades, the Processing Plant, upkeep and fines) are saved with the change
    /// they pay for, marked pending, before the player's own queue takes the credits and saves them in the character
    /// file with a mark naming the payment. A restart in between leaves a pending payment the next login settles: kept
    /// when the character file holds its mark, undone otherwise. These rules hold no game state, so tests can drive them.
    /// </summary>
    public static class HoldingPaymentRules
    {
        /// <summary>Whether a payment is still waiting on the player's credits; the holding takes no other payment until it settles.</summary>
        public static bool IsPending(PlanetaryHolding holding) =>
            holding != null && !string.IsNullOrWhiteSpace(holding.PendingPaymentId);

        /// <summary>Marks the change just made to the holding as waiting on a payment, to be saved with it.</summary>
        public static void Begin(PlanetaryHolding holding, HoldingPaymentType type, int credits, string paymentId)
        {
            holding.PendingPaymentId = paymentId ?? string.Empty;
            holding.PendingPaymentType = type;
            holding.PendingPaymentCredits = Math.Max(0, credits);
        }

        /// <summary>The payment was saved in the character file: the change stays.</summary>
        public static void Complete(PlanetaryHolding holding)
        {
            holding.PendingPaymentId = string.Empty;
            holding.PendingPaymentType = HoldingPaymentType.Invalid;
            holding.PendingPaymentCredits = 0;
        }

        /// <summary>
        /// The payment was never taken: the change it paid for is undone. Returns true when the holding itself was the
        /// purchase and must be deleted.
        /// </summary>
        public static bool Undo(PlanetaryHolding holding, HoldingDetail detail, DateTime now)
        {
            var type = holding.PendingPaymentType;
            var credits = holding.PendingPaymentCredits;
            Complete(holding);

            switch (type)
            {
                case HoldingPaymentType.Purchase:
                    return true;
                case HoldingPaymentType.Upgrade:
                    var upgrade = detail?.Upgrades.Find(x => x.Level == holding.Level);
                    if (upgrade != null)
                    {
                        foreach (var (commodity, units) in upgrade.Goods)
                            HoldingRules.AddStock(holding.Supplies, commodity, units);
                        holding.Level = upgrade.Level - 1;
                    }
                    break;
                case HoldingPaymentType.Plant:
                    if (holding.HasProcessingPlant && detail?.Plant != null)
                    {
                        foreach (var (commodity, units) in detail.Plant.Goods)
                            HoldingRules.AddStock(holding.Supplies, commodity, units);
                        holding.HasProcessingPlant = false;
                    }
                    break;
                case HoldingPaymentType.Upkeep:
                    holding.UpkeepOwed += credits;
                    break;
                case HoldingPaymentType.Fine:
                    holding.CustomsFine += credits;
                    break;
                default:
                    return false;
            }

            HoldingRules.AddLog(holding, now, $"A restart undid the unpaid {GetName(type)}.");
            return false;
        }

        /// <summary>What the payment pays for, as the player reads it.</summary>
        public static string GetName(HoldingPaymentType type) => type switch
        {
            HoldingPaymentType.Purchase => "purchase",
            HoldingPaymentType.Upgrade => "upgrade",
            HoldingPaymentType.Plant => "Processing Plant",
            HoldingPaymentType.Upkeep => "upkeep payment",
            HoldingPaymentType.Fine => "customs fine payment",
            _ => "payment",
        };
    }
}
