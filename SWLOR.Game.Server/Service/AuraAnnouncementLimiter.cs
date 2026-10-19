using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Sends the "receives the effect of" notice for an aura buff once per leader, ally and aura. Allies drifting
    /// across the edge of an aura during combat leave and re-enter it many times, and each re-entry used to
    /// announce the same buff again. The buff itself is still applied every time; only the repeat chat line is
    /// withheld. The record ends with the aura (switched off, replaced, or all auras ended) or the ally leaving.
    /// </summary>
    public static class AuraAnnouncementLimiter
    {
        private static readonly HashSet<(uint Source, uint Recipient, Type StatusEffect)> _announced = new();

        /// <summary>
        /// True when this source's aura has not yet been announced to this recipient since it was last cleared.
        /// </summary>
        public static bool IsFirstAnnouncement(uint source, uint recipient, Type statusEffect)
        {
            return !_announced.Contains((source, recipient, statusEffect));
        }

        /// <summary>
        /// Records that the aura was applied to the recipient, so later applications are not announced again.
        /// </summary>
        public static void RecordApplied(uint source, uint recipient, Type statusEffect)
        {
            _announced.Add((source, recipient, statusEffect));
        }

        /// <summary>
        /// Forgets what a source has announced for one aura so switching or replacing that aura announces it afresh,
        /// leaving the source's other auras announced once.
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
            _announced.Clear();
        }

        private static void RemoveWhere(Func<(uint Source, uint Recipient, Type StatusEffect), bool> match)
        {
            var keys = new List<(uint Source, uint Recipient, Type StatusEffect)>();
            foreach (var key in _announced)
            {
                if (match(key))
                    keys.Add(key);
            }

            foreach (var key in keys)
                _announced.Remove(key);
        }
    }
}
