using SWLOR.Component.Admin.Entity;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Component.Admin.Repository
{
    /// <summary>
    /// Repository implementation for DMCreature entity operations.
    /// </summary>
    public class DMCreatureRepository : IDMCreatureRepository
    {
        private readonly IDatabaseService _db;

        public DMCreatureRepository(IDatabaseService db)
        {
            _db = db;
        }

        /// <inheritdoc/>
        public DMCreature GetById(string id)
        {
            return _db.Get<DMCreature>(id);
        }

        /// <inheritdoc/>
        public IEnumerable<DMCreature> GetByName(string name)
        {
            var query = new DBQuery<DMCreature>()
                .AddFieldSearch(nameof(DMCreature.Name), name, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<DMCreature> GetByTag(string tag)
        {
            var query = new DBQuery<DMCreature>()
                .AddFieldSearch(nameof(DMCreature.Tag), tag, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<DMCreature> GetAllOrderedByName()
        {
            var query = new DBQuery<DMCreature>()
                .OrderBy(nameof(DMCreature.Name));
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<DMCreature> GetAll()
        {
            var query = new DBQuery<DMCreature>();
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public void Save(DMCreature dmCreature)
        {
            _db.Set(dmCreature);
        }

        /// <inheritdoc/>
        public void Delete(string id)
        {
            _db.Delete<DMCreature>(id);
        }

        /// <inheritdoc/>
        public bool Exists(string id)
        {
            return _db.Exists<DMCreature>(id);
        }
    }
}
