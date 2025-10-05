using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Component.Associate.Repository
{
    /// <summary>
    /// Repository implementation for Beast entity operations.
    /// </summary>
    public class BeastRepository : IBeastRepository
    {
        private readonly IDatabaseService _db;

        public BeastRepository(IDatabaseService db)
        {
            _db = db;
        }

        /// <inheritdoc/>
        public Beast GetById(string id)
        {
            return _db.Get<Beast>(id);
        }

        /// <inheritdoc/>
        public IEnumerable<Beast> GetByOwnerPlayerId(string ownerPlayerId)
        {
            var query = new DBQuery<Beast>()
                .AddFieldSearch(nameof(Beast.OwnerPlayerId), ownerPlayerId, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<Beast> GetByName(string name)
        {
            var query = new DBQuery<Beast>()
                .AddFieldSearch(nameof(Beast.Name), name, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<Beast> GetAll()
        {
            var query = new DBQuery<Beast>();
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public long GetCountByOwnerPlayerId(string ownerPlayerId)
        {
            var query = new DBQuery<Beast>()
                .AddFieldSearch(nameof(Beast.OwnerPlayerId), ownerPlayerId, false);
            return _db.SearchCount(query);
        }

        /// <inheritdoc/>
        public void Save(Beast beast)
        {
            _db.Set(beast);
        }

        /// <inheritdoc/>
        public void Delete(string id)
        {
            _db.Delete<Beast>(id);
        }

        /// <inheritdoc/>
        public bool Exists(string id)
        {
            return _db.Exists<Beast>(id);
        }
    }
}
