using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Associate.ValueObjects;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Component.Associate.Repository
{
    /// <summary>
    /// Repository implementation for IncubationJob entity operations.
    /// </summary>
    public class IncubationJobRepository : IIncubationJobRepository
    {
        private readonly IDatabaseService _db;

        public IncubationJobRepository(IDatabaseService db)
        {
            _db = db;
        }

        /// <inheritdoc/>
        public IncubationJob GetById(string id)
        {
            return _db.Get<IncubationJob>(id);
        }

        /// <inheritdoc/>
        public IEnumerable<IncubationJob> GetByParentPropertyId(string parentPropertyId)
        {
            var query = new DBQuery<IncubationJob>()
                .AddFieldSearch(nameof(IncubationJob.ParentPropertyId), parentPropertyId, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<IncubationJob> GetByPlayerId(string playerId)
        {
            var query = new DBQuery<IncubationJob>()
                .AddFieldSearch(nameof(IncubationJob.PlayerId), playerId, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<IncubationJob> GetAll()
        {
            var query = new DBQuery<IncubationJob>();
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public void Save(IncubationJob incubationJob)
        {
            _db.Set(incubationJob);
        }

        /// <inheritdoc/>
        public void Delete(string id)
        {
            _db.Delete<IncubationJob>(id);
        }

        /// <inheritdoc/>
        public bool Exists(string id)
        {
            return _db.Exists<IncubationJob>(id);
        }
    }
}
