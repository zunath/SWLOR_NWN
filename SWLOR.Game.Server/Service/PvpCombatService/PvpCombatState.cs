using System;

namespace SWLOR.Game.Server.Service.PvpCombatService
{
    /// <summary>One player's running PvP combat.</summary>
    public sealed class PvpCombatState
    {
        public DateTime StartedAt { get; set; }
        public DateTime LastActivityAt { get; set; }

        /// <summary>The healing-reduction step shown on the status bar: -1 for the countdown before the first step.</summary>
        public int ShownHealingStep { get; set; } = int.MinValue;

        /// <summary>The damage-increase step last announced in the combat log, or -1 before the first.</summary>
        public int AnnouncedDamageStep { get; set; } = -1;

        public PvpCombatState(DateTime startedAt)
        {
            StartedAt = startedAt;
            LastActivityAt = startedAt;
        }
    }
}
