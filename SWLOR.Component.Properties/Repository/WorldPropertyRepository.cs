using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Properties.Entities;
using SWLOR.Shared.Domain.Properties.Enums;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Component.Properties.Repository
{
    /// <summary>
    /// Repository implementation for WorldProperty entity operations.
    /// </summary>
    public class WorldPropertyRepository : IWorldPropertyRepository
    {
        private readonly IDatabaseService _db;

        public WorldPropertyRepository(IDatabaseService db)
        {
            _db = db;
        }

        /// <inheritdoc/>
        public WorldProperty GetById(string id)
        {
            return _db.Get<WorldProperty>(id);
        }

        /// <inheritdoc/>
        public IEnumerable<WorldProperty> GetByOwnerPlayerId(string ownerPlayerId)
        {
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.OwnerPlayerId), ownerPlayerId, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<WorldProperty> GetByPropertyType(PropertyType propertyType)
        {
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)propertyType);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<WorldProperty> GetByParentPropertyId(string parentPropertyId)
        {
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.ParentPropertyId), parentPropertyId, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<WorldProperty> GetByStructureType(StructureType structureType)
        {
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.StructureType), (int)structureType);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<WorldProperty> GetPubliclyAccessible()
        {
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.IsPubliclyAccessible), true);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<WorldProperty> GetQueuedForDeletion()
        {
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.IsQueuedForDeletion), true);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<WorldProperty> GetByPropertyIds(IEnumerable<string> propertyIds)
        {
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.Id), propertyIds);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public long GetCountByPropertyType(PropertyType propertyType)
        {
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)propertyType);
            return _db.SearchCount(query);
        }

        /// <inheritdoc/>
        public IEnumerable<WorldProperty> GetPropertiesWithLeases()
        {
            // Currently only apartments have leases, but this can be extended for other property types
            var propertyTypesWithLeases = new[] { PropertyType.Apartment };
            var results = new List<WorldProperty>();
            
            foreach (var propertyType in propertyTypesWithLeases)
            {
                var query = new DBQuery<WorldProperty>()
                    .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)propertyType);
                results.AddRange(_db.Search(query));
            }
            
            return results;
        }

        /// <inheritdoc/>
        public IEnumerable<WorldProperty> GetActiveCities()
        {
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.City)
                .AddFieldSearch(nameof(WorldProperty.IsQueuedForDeletion), false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<WorldProperty> GetStructuresByParentPropertyId(string parentPropertyId)
        {
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.ParentPropertyId), parentPropertyId, false)
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Structure);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public long GetActiveStructureCountByParentAndType(string parentPropertyId, StructureType structureType)
        {
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.ParentPropertyId), parentPropertyId, false)
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Structure)
                .AddFieldSearch(nameof(WorldProperty.StructureType), (int)structureType)
                .AddFieldSearch(nameof(WorldProperty.IsQueuedForDeletion), false);
            return _db.SearchCount(query);
        }

        /// <inheritdoc/>
        public IEnumerable<WorldProperty> GetAll()
        {
            var query = new DBQuery<WorldProperty>();
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public void Save(WorldProperty worldProperty)
        {
            _db.Set(worldProperty);
        }

        /// <inheritdoc/>
        public void Delete(string id)
        {
            _db.Delete<WorldProperty>(id);
        }

        /// <inheritdoc/>
        public bool Exists(string id)
        {
            return _db.Exists<WorldProperty>(id);
        }
    }
}
