using System;

namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>Benefit invalidation leaves trigger cooldowns intact.</summary>
    public sealed class TechniqueResourceState
    {
        private int _discount;
        private DateTime _discountExpires;
        private DateTime _discountAvailable;
        private DateTime _refundAvailable;
        private DateTime _actionAvailable;

        public bool CanActivate(DateTime now) => now >= _actionAvailable;
        public void LockAction(DateTime now) => _actionAvailable = now.AddSeconds(1);

        public bool TryGrantDiscount(int amount, int durationSeconds, int cooldownSeconds, DateTime now)
        {
            if (amount <= 0 || durationSeconds <= 0 || now < _discountAvailable)
                return false;
            _discount = amount;
            _discountExpires = now.AddSeconds(durationSeconds);
            _discountAvailable = now.AddSeconds(Math.Max(0, cooldownSeconds));
            return true;
        }

        public int GetDiscount(DateTime now) => now < _discountExpires ? _discount : 0;

        public int TakeDiscount(int positiveCost, DateTime now)
        {
            if (positiveCost <= 0)
                return 0;
            var discount = Math.Min(positiveCost, GetDiscount(now));
            _discount = 0;
            _discountExpires = DateTime.MinValue;
            return discount;
        }

        /// <summary>The shared gate starts only when <paramref name="restore"/> reports stamina actually restored.</summary>
        public bool TryRefund(int actualPaid, int requested, int cooldownSeconds, DateTime now,
            Func<int, int> restore, out int restored)
        {
            restored = 0;
            if (actualPaid <= 0 || requested <= 0 || now < _refundAvailable)
                return false;
            restored = restore(Math.Min(actualPaid, requested));
            if (restored <= 0)
                return false;
            _refundAvailable = now.AddSeconds(Math.Max(0, cooldownSeconds));
            return true;
        }

        public void ClearBenefits()
        {
            _discount = 0;
            _discountExpires = DateTime.MinValue;
        }
    }
}
