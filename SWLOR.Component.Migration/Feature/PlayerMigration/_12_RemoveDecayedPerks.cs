using SWLOR.Component.Migration.Model;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Entities;

namespace SWLOR.Component.Migration.Feature.PlayerMigration
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
