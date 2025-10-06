using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Component.Combat.Feature
{
    public class PersistentHitPoints
    {
        private readonly IDatabaseService _db;

        public PersistentHitPoints(
            IDatabaseService db,
            IEventAggregator eventAggregator)
        {
            _db = db;

            // Subscribe to events
            eventAggregator.Subscribe<OnModuleExit>(e => SaveHP());
            eventAggregator.Subscribe<OnModuleEnter>(e => LoadHP());
        }
        
        /// <summary>
        /// When a player leaves the server, save their persistent HP.
        /// </summary>
        public void SaveHP()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            if (dbPlayer == null) return;
            dbPlayer.HP = GetCurrentHitPoints(player);

            _db.Set(dbPlayer);
        }

        /// <summary>
        /// When a player enters the server, load their persistent HP.
        /// </summary>
        public void LoadHP()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
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
