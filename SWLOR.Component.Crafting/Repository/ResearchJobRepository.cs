using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Component.Crafting.Repository
{
    /// <summary>
    /// Repository implementation for ResearchJob entity operations.
    /// </summary>
    public class ResearchJobRepository : IResearchJobRepository
    {
        private readonly IDatabaseService _db;

        public ResearchJobRepository(IDatabaseService db)
        {
            _db = db;
        }

        /// <inheritdoc/>
        public ResearchJob GetById(string id)
        {
            return _db.Get<ResearchJob>(id);
        }

        /// <inheritdoc/>
        public IEnumerable<ResearchJob> GetByParentPropertyId(string parentPropertyId)
        {
            var query = new DBQuery<ResearchJob>()
                .AddFieldSearch(nameof(ResearchJob.ParentPropertyId), parentPropertyId, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<ResearchJob> GetByPlayerId(string playerId)
        {
            var query = new DBQuery<ResearchJob>()
                .AddFieldSearch(nameof(ResearchJob.PlayerId), playerId, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<ResearchJob> GetAll()
        {
            var query = new DBQuery<ResearchJob>();
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public void Save(ResearchJob researchJob)
        {
            _db.Set(researchJob);
        }

        /// <inheritdoc/>
        public void Delete(string id)
        {
            _db.Delete<ResearchJob>(id);
        }

        /// <inheritdoc/>
        public bool Exists(string id)
        {
            return _db.Exists<ResearchJob>(id);
        }
    }
}
