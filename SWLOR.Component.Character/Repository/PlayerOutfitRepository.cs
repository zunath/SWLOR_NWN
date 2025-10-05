using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Component.Character.Repository
{
    /// <summary>
    /// Repository implementation for PlayerOutfit entity operations.
    /// </summary>
    public class PlayerOutfitRepository : IPlayerOutfitRepository
    {
        private readonly IDatabaseService _db;

        public PlayerOutfitRepository(IDatabaseService db)
        {
            _db = db;
        }

        /// <inheritdoc/>
        public PlayerOutfit GetById(string id)
        {
            return _db.Get<PlayerOutfit>(id);
        }

        /// <inheritdoc/>
        public IEnumerable<PlayerOutfit> GetByPlayerId(string playerId)
        {
            var query = new DBQuery<PlayerOutfit>()
                .AddFieldSearch(nameof(PlayerOutfit.PlayerId), playerId, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<PlayerOutfit> GetByName(string name)
        {
            var query = new DBQuery<PlayerOutfit>()
                .AddFieldSearch(nameof(PlayerOutfit.Name), name, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<PlayerOutfit> GetAll()
        {
            var query = new DBQuery<PlayerOutfit>();
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public long GetCountByPlayerId(string playerId)
        {
            var query = new DBQuery<PlayerOutfit>()
                .AddFieldSearch(nameof(PlayerOutfit.PlayerId), playerId, false);
            return _db.SearchCount(query);
        }

        /// <inheritdoc/>
        public void Save(PlayerOutfit playerOutfit)
        {
            _db.Set(playerOutfit);
        }

        /// <inheritdoc/>
        public void Delete(string id)
        {
            _db.Delete<PlayerOutfit>(id);
        }

        /// <inheritdoc/>
        public bool Exists(string id)
        {
            return _db.Exists<PlayerOutfit>(id);
        }
    }
}
