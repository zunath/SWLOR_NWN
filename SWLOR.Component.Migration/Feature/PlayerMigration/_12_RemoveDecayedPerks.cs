using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Data.Entity;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration
{
    public class _12_RemoveDecayedPerks: PlayerMigrationBase
    {
        private readonly IDatabaseService _db;
        private readonly IPerkService _perkService;
        
        public _12_RemoveDecayedPerks(IDatabaseService db, IPerkService perkService)
        {
            _db = db;
            _perkService = perkService;
        }
        
        public override int Version => 12;
        public override void Migrate(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            foreach (var (perkType, level) in dbPlayer.Perks)
            {
                var effectiveLevel = _perkService.GetPlayerEffectivePerkLevel(player, perkType);

                if (level != effectiveLevel)
                {
                    RefundPerk(player, perkType);
                }
            }
        }
    }
}
