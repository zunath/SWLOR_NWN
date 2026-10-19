using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Limits the "receives the effect of" notice for aura buffs. Allies drifting across the edge of an aura
    /// during combat leave and re-enter it many times, and each re-entry would otherwise be announced again.
    /// The buff itself is still applied every time; only the repeated chat line is withheld.
    /// </summary>
    public static class AuraAnnouncementLimiter
    {
        public const int IntervalSeconds = 60;

        private static readonly Dictionary<(uint Source, uint Recipient, Type StatusEffect), long> _lastAnnounced = new();

        /// <summary>
        /// True when the aura buff from this source on this recipient has not been announced within the interval.
        /// A true result records the announcement.
        /// </summary>
        public static bool TryAnnounce(uint source, uint recipient, Type statusEffect, long nowTicks)
        {
            var key = (source, recipient, statusEffect);
            if (_lastAnnounced.TryGetValue(key, out var lastSent) &&
                !PlayerFeedback.IsWarningDue(nowTicks, lastSent, IntervalSeconds))
            {
                return false;
            }

            _lastAnnounced[key] = nowTicks;
            return true;
        }

        public static bool TryAnnounce(uint source, uint recipient, Type statusEffect)
        {
            return TryAnnounce(source, recipient, statusEffect, DateTime.UtcNow.Ticks);
        }

        /// <summary>
        /// Forgets what a source has announced for one aura so switching or replacing that aura announces it afresh,
        /// leaving the source's other auras limited.
        /// </summary>
        public static void ClearAura(uint source, Type statusEffect)
        {
            RemoveWhere(key => key.Source == source && key.StatusEffect == statusEffect);
        }

        /// <summary>
        /// Forgets everything a source has announced when all of its auras end.
        /// </summary>
        public static void ClearSource(uint source)
        {
            RemoveWhere(key => key.Source == source);
        }

        /// <summary>
        /// Forgets what has been announced to a recipient that logged out, died, or left the area for space.
        /// </summary>
        public static void ClearRecipient(uint recipient)
        {
            RemoveWhere(key => key.Recipient == recipient);
        }

        public static void Clear()
        {
            _lastAnnounced.Clear();
        }

        private static void RemoveWhere(Func<(uint Source, uint Recipient, Type StatusEffect), bool> match)
        {
            var keys = new List<(uint Source, uint Recipient, Type StatusEffect)>();
            foreach (var key in _lastAnnounced.Keys)
            {
                if (match(key))
                    keys.Add(key);
            }

            foreach (var key in keys)
                _lastAnnounced.Remove(key);
        }
    }
}
