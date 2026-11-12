using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature
{
    public static class PersistentHitPoints
    {
        /// <summary>
        /// When a player leaves the server, save their persistent HP.
        /// </summary>
        [PlayerSession(PlayerReload.Quiesce,
            "Saves the HP to Redis as at logout, so a restart after a failed reload loses nothing; no world effect.",
            Step = nameof(SaveHPFor))]
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void SaveHP()
        {
            SaveHPFor(GetExitingObject());
        }

        /// <summary>
        /// The work of <see cref="SaveHP"/> for <paramref name="player"/>: the logout's handler, and a hot reload's quiescence step for a player who stays online.
        /// </summary>
        public static void SaveHPFor(uint player)
        {
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
        [PlayerSession(PlayerReload.LoginOnly,
            "Sets HP to the value saved at logout; HP stays in the engine.")]
        [NWNEventHandler(ScriptName.OnModuleEnter)]
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
