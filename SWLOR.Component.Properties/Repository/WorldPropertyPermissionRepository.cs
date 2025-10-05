using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Properties.Entities;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Component.Properties.Repository
{
    /// <summary>
    /// Repository implementation for WorldPropertyPermission entity operations.
    /// </summary>
    public class WorldPropertyPermissionRepository : IWorldPropertyPermissionRepository
    {
        private readonly IDatabaseService _db;

        public WorldPropertyPermissionRepository(IDatabaseService db)
        {
            _db = db;
        }

        /// <inheritdoc/>
        public WorldPropertyPermission GetById(string id)
        {
            return _db.Get<WorldPropertyPermission>(id);
        }

        /// <inheritdoc/>
        public IEnumerable<WorldPropertyPermission> GetByPropertyId(string propertyId)
        {
            var query = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), propertyId, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<WorldPropertyPermission> GetByPlayerId(string playerId)
        {
            var query = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<WorldPropertyPermission> GetByPropertyIdAndPlayerId(string propertyId, string playerId)
        {
            var query = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), propertyId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public WorldPropertyPermission GetSingleByPropertyIdAndPlayerId(string propertyId, string playerId)
        {
            var query = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), propertyId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false);
            return _db.Search(query).FirstOrDefault();
        }

        /// <inheritdoc/>
        public IEnumerable<WorldPropertyPermission> GetByPropertyIds(IEnumerable<string> propertyIds)
        {
            var query = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), propertyIds);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<WorldPropertyPermission> GetByPlayerIds(IEnumerable<string> playerIds)
        {
            var query = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerIds);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<WorldPropertyPermission> GetByPlayerIdAndPropertyIds(string playerId, IEnumerable<string> propertyIds)
        {
            var query = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), propertyIds);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public void Save(WorldPropertyPermission worldPropertyPermission)
        {
            _db.Set(worldPropertyPermission);
        }

        /// <inheritdoc/>
        public void Delete(string id)
        {
            _db.Delete<WorldPropertyPermission>(id);
        }

        /// <inheritdoc/>
        public bool Exists(string id)
        {
            return _db.Exists<WorldPropertyPermission>(id);
        }
    }
}
