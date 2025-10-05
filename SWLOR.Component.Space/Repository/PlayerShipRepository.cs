using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Component.Space.Repository
{
    /// <summary>
    /// Repository implementation for PlayerShip entity operations.
    /// </summary>
    public class PlayerShipRepository : IPlayerShipRepository
    {
        private readonly IDatabaseService _db;

        public PlayerShipRepository(IDatabaseService db)
        {
            _db = db;
        }

        /// <inheritdoc/>
        public PlayerShip GetById(string id)
        {
            return _db.Get<PlayerShip>(id);
        }

        /// <inheritdoc/>
        public IEnumerable<PlayerShip> GetByOwnerPlayerId(string ownerPlayerId)
        {
            var query = new DBQuery<PlayerShip>()
                .AddFieldSearch(nameof(PlayerShip.OwnerPlayerId), ownerPlayerId, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<PlayerShip> GetByPropertyId(string propertyId)
        {
            var query = new DBQuery<PlayerShip>()
                .AddFieldSearch(nameof(PlayerShip.PropertyId), propertyId, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<PlayerShip> GetByPropertyIds(IEnumerable<string> propertyIds)
        {
            var query = new DBQuery<PlayerShip>()
                .AddFieldSearch(nameof(PlayerShip.PropertyId), propertyIds);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<PlayerShip> GetByPropertyIdsExcludingPlayer(IEnumerable<string> propertyIds, string excludePlayerId)
        {
            var query = new DBQuery<PlayerShip>()
                .AddFieldSearch(nameof(PlayerShip.PropertyId), propertyIds);
            return _db.Search(query).Where(x => x.OwnerPlayerId != excludePlayerId);
        }

        /// <inheritdoc/>
        public void Save(PlayerShip playerShip)
        {
            _db.Set(playerShip);
        }

        /// <inheritdoc/>
        public void Delete(string id)
        {
            _db.Delete<PlayerShip>(id);
        }

        /// <inheritdoc/>
        public bool Exists(string id)
        {
            return _db.Exists<PlayerShip>(id);
        }
    }
}
