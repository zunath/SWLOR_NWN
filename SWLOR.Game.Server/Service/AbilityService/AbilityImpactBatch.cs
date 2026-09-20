using System;

namespace SWLOR.Game.Server.Service.AbilityService
{
    public sealed class AbilityImpactBatch<T>
    {
        private int _remaining;
        private bool _failed;
        private readonly Action<T, bool> _applyImpact;

        public AbilityImpactBatch(int count, Action<T, bool> applyImpact)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
            ArgumentNullException.ThrowIfNull(applyImpact);
            _remaining = count;
            _applyImpact = applyImpact;
        }

        public void Apply(T payload)
        {
            if (_failed)
                return;
            if (_remaining == 0)
                throw new InvalidOperationException("The impact batch has already completed.");

            _remaining--;
            try
            {
                _applyImpact(payload, _remaining == 0);
            }
            catch
            {
                _failed = true;
                throw;
            }
        }
    }
}
