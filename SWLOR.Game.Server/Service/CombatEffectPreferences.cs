using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Tracks who chose to reduce allied ability bursts. Held in memory because every burst in the
    /// game consults it during combat.
    /// </summary>
    public static class CombatEffectPreferences
    {
        private static readonly HashSet<uint> _reducingPlayers = new();

        public static bool AnyPlayerReducing => _reducingPlayers.Count > 0;

        public static bool IsReducing(uint player) => _reducingPlayers.Contains(player);

        public static void SetReducing(uint player, bool isReducing)
        {
            if (isReducing)
                _reducingPlayers.Add(player);
            else
                _reducingPlayers.Remove(player);
        }

        /// <summary>A viewer who opted in still sees bursts from their own abilities and from enemies.</summary>
        public static bool ShouldHideBurst(bool isReducing, bool isOwnAbility, bool casterIsEnemy)
        {
            return isReducing && !isOwnAbility && !casterIsEnemy;
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void LoadOnEnter()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            SetReducing(player, dbPlayer?.Settings?.ReduceAlliedCombatEffects == true);
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ForgetOnExit()
        {
            _reducingPlayers.Remove(GetExitingObject());
        }
    }
}
