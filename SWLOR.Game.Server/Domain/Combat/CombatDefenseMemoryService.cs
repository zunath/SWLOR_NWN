using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Who was guarded or deflected recently. The recent guarded hits and deflections are this instance's own tables; the
    /// service takes no other service, so the damage taken and damage modifiers parts read it without a cycle.
    /// </summary>
    public class CombatDefenseMemoryService : ICombatDefenseMemoryService
    {
        private readonly Dictionary<uint, DateTime> _recentGuardedHits = new();
        private readonly Dictionary<(uint Creature, DeflectionSource Source), DateTime> _recentDeflections = new();

        public void NoteGuardedHit(uint creature)
        {
            _recentGuardedHits[creature] = Clock.UtcNow;
        }

        public bool HasRecentGuardedHit(uint creature, float windowSeconds)
        {
            if (!GetIsObjectValid(creature) || windowSeconds <= 0f)
                return false;

            if (!_recentGuardedHits.TryGetValue(creature, out var lastGuardedHit))
                return false;

            var isRecent = (Clock.UtcNow - lastGuardedHit).TotalSeconds <= windowSeconds;
            if (!isRecent)
                _recentGuardedHits.Remove(creature);

            return isRecent;
        }

        public void NoteDeflection(uint creature, DeflectionSource source)
        {
            _recentDeflections[(creature, source)] = Clock.UtcNow;
        }

        public bool HasRecentDeflection(uint creature, DeflectionSource source, float windowSeconds)
        {
            if (!GetIsObjectValid(creature) || source == DeflectionSource.None || windowSeconds <= 0f)
                return false;

            var key = (creature, source);
            if (!_recentDeflections.TryGetValue(key, out var lastDeflection))
                return false;

            var isRecent = (Clock.UtcNow - lastDeflection).TotalSeconds <= windowSeconds;
            if (!isRecent)
                _recentDeflections.Remove(key);

            return isRecent;
        }

        public void ForgetDefenseMemory(uint creature)
        {
            _recentGuardedHits.Remove(creature);
            foreach (var key in _recentDeflections.Keys.Where(x => x.Creature == creature).ToList())
            {
                _recentDeflections.Remove(key);
            }
        }

        /// <inheritdoc/>
        public void ForgetCreatureState(uint creature, CombatForgetPhase phase)
        {
            switch (phase)
            {
                case CombatForgetPhase.DefenseMemory:
                    ForgetDefenseMemory(creature);
                    break;
            }
        }
    }
}
