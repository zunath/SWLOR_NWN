using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CombatRecorderService
{
    /// <summary>
    /// Pairs a damage application the engine is making with the registration that decided it.
    /// </summary>
    public static class CombatRecordPendingDamageMatcher
    {
        /// <summary>
        /// Drops expired registrations, then returns the index of the oldest registration for this
        /// source and target, or -1. Weapon hits are applied by the engine itself while deferred
        /// damage runs from a script or closure, so the application's origin picks which kind is
        /// tried first; an exact amount wins within a kind, and the other kind is only a fallback
        /// for an exact amount.
        /// </summary>
        public static int FindMatch(
            List<CombatRecordPendingDamage> pending,
            uint source,
            uint target,
            int amount,
            bool isEngineApplied,
            ulong frame,
            long nowMs,
            ulong deferredFrameWindow,
            long weaponWindowMs)
        {
            pending.RemoveAll(registration => registration.IsWeaponHit
                ? nowMs - registration.TimestampMs > weaponWindowMs
                : frame - registration.Frame > deferredFrameWindow);

            var preferWeapon = isEngineApplied;
            var match = Find(pending, source, target, amount, preferWeapon, true);
            if (match < 0)
                match = Find(pending, source, target, amount, preferWeapon, false);
            if (match < 0)
                match = Find(pending, source, target, amount, !preferWeapon, true);
            return match;
        }

        private static int Find(
            List<CombatRecordPendingDamage> pending, uint source, uint target, int amount, bool weaponHit, bool exactAmount)
        {
            return pending.FindIndex(registration =>
                registration.IsWeaponHit == weaponHit &&
                registration.Source == source &&
                registration.Target == target &&
                (!exactAmount || registration.Amount == amount));
        }
    }
}
