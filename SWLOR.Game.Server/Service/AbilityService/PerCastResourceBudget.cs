using System;

namespace SWLOR.Game.Server.Service.AbilityService
{
    /// <summary>
    /// A fixed resource allowance spent across the targets struck by a single ability activation.
    /// Area abilities with no target cap pay their per-hit riders once per struck enemy, so the
    /// payout would otherwise scale with the size of the pull; this bounds one activation the way
    /// Twin Blade's Sweeping Advance does ("restore 2 STM per target hit, up to 6 STM").
    /// </summary>
    public sealed class PerCastResourceBudget
    {
        private readonly int _amountPerHit;

        public PerCastResourceBudget(int amountPerHit, int maximumPerCast)
        {
            if (amountPerHit <= 0)
                throw new ArgumentOutOfRangeException(nameof(amountPerHit));
            if (maximumPerCast <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumPerCast));

            _amountPerHit = amountPerHit;
            Remaining = maximumPerCast;
        }

        /// <summary>How much of this activation's allowance is still unspent.</summary>
        public int Remaining { get; private set; }

        /// <summary>
        /// Claims the next hit's share, trimmed to whatever allowance is left. Returns zero once
        /// the activation's ceiling has been reached.
        /// </summary>
        public int Take()
        {
            var amount = Math.Min(_amountPerHit, Remaining);
            Remaining -= amount;
            return amount;
        }
    }
}
