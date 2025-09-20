using System;

using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Event;

namespace SWLOR.Game.Server.Feature
{
    public static class PersistentHitPoints
    {
        /// <summary>
        /// When a player leaves the server, save their persistent HP.
        /// </summary>
        [ScriptHandler(ScriptName.OnModuleExit)]
        public static void SaveHP()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null) return;
            dbPlayer.HP = GetCurrentHitPoints(player);

            DB.Set(dbPlayer);
        }

        /// <summary>
        /// When a player enters the server, load their persistent HP.
        /// </summary>
        [ScriptHandler(ScriptName.OnModuleEnter)]
        public static void LoadHP()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null) return;
            if (dbPlayer.MaxHP <= 0) return; // Check whether MaxHP is initialized

            int hp = GetCurrentHitPoints(player);
            int damage;
            if (dbPlayer.HP < 0)
            {
                damage = hp + Math.Abs(dbPlayer.HP);
            }
            else
            {
                damage = hp - dbPlayer.HP;
            }

            if (damage != 0)
            {
                ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), player);
            }
        }
    }
}
