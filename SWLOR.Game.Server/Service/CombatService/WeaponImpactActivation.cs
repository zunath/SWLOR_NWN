using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>Per-activation limits for a weapon ability with independently resolved impacts.</summary>
    public sealed class WeaponImpactActivation
    {
        private readonly HashSet<uint> _damageTargets = new();
        private readonly HashSet<uint> _riderTargets = new();
        private bool _criticalNotified;
        public bool TryDamageEffects(uint target) => _damageTargets.Add(target);
        public bool TryHitRiders(uint target) => _riderTargets.Add(target);
        public bool TryCriticalNotification()
        {
            if (_criticalNotified) return false;
            _criticalNotified = true;
            return true;
        }
    }
}
