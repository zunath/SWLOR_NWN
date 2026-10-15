using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SkillService
{
    /// <summary>
    /// Combines feedback after independently calculated awards, without delaying XP itself.
    /// Each source keeps its own message even when several creatures die in the same frame.
    /// </summary>
    public sealed class SkillXPMessageQueue
    {
        private readonly Dictionary<(uint Player, uint Source, SkillType Skill), int> _pendingXP = new();
        private readonly Action<Action> _schedule;
        private readonly Action<uint, SkillType, int> _send;

        public SkillXPMessageQueue(Action<Action> schedule, Action<uint, SkillType, int> send)
        {
            _schedule = schedule;
            _send = send;
        }

        public void Add(uint player, uint source, SkillType skill, int xp)
        {
            if (xp <= 0)
                return;

            var key = (player, source, skill);
            if (_pendingXP.TryGetValue(key, out var pendingXP))
            {
                _pendingXP[key] = pendingXP + xp;
                return;
            }

            _pendingXP[key] = xp;
            _schedule(() =>
            {
                var totalXP = _pendingXP[key];
                _pendingXP.Remove(key);
                _send(player, skill, totalXP);
            });
        }
    }
}
