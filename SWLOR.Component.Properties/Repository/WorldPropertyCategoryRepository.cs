using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Properties.Entities;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Component.Properties.Repository
{
    /// <summary>
    /// Repository implementation for WorldPropertyCategory entity operations.
    /// </summary>
    public class WorldPropertyCategoryRepository : IWorldPropertyCategoryRepository
    {
        private readonly IDatabaseService _db;

        public WorldPropertyCategoryRepository(IDatabaseService db)
        {
            _db = db;
        }

        /// <inheritdoc/>
        public WorldPropertyCategory GetById(string id)
        {
            return _db.Get<WorldPropertyCategory>(id);
        }

        /// <inheritdoc/>
        public IEnumerable<WorldPropertyCategory> GetByPropertyId(string propertyId)
        {
            var query = new DBQuery<WorldPropertyCategory>()
                .AddFieldSearch(nameof(WorldPropertyCategory.ParentPropertyId), propertyId, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<WorldPropertyCategory> GetByParentCategoryId(string parentCategoryId)
        {
            var query = new DBQuery<WorldPropertyCategory>()
                .AddFieldSearch(nameof(WorldPropertyCategory.ParentPropertyId), parentCategoryId, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public void Save(WorldPropertyCategory worldPropertyCategory)
        {
            _db.Set(worldPropertyCategory);
        }

        /// <inheritdoc/>
        public void Delete(string id)
        {
            _db.Delete<WorldPropertyCategory>(id);
        }

        /// <inheritdoc/>
        public bool Exists(string id)
        {
            return _db.Exists<WorldPropertyCategory>(id);
        }
    }
}
