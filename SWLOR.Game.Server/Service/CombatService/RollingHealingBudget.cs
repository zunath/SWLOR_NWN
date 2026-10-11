using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>Shares a healing allowance across all receipts within a rolling time window.</summary>
    public sealed class RollingHealingBudget
    {
        private readonly Queue<(DateTime AppliedAt, int Amount)> _receipts = new();
        private readonly TimeSpan _window;
        private int _spent;

        public RollingHealingBudget(TimeSpan window)
        {
            if (window <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(window));

            _window = window;
        }

        public int Take(int maximum, int requested, DateTime now)
        {
            while (_receipts.Count > 0 && _receipts.Peek().AppliedAt <= now - _window)
                _spent -= _receipts.Dequeue().Amount;

            var amount = Math.Clamp(requested, 0, Math.Max(0, maximum - _spent));
            if (amount > 0)
            {
                _receipts.Enqueue((now, amount));
                _spent += amount;
            }

            return amount;
        }
    }
}
