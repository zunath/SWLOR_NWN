using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Properties.Entities;
using SWLOR.Shared.Domain.Properties.Enums;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Component.Properties.Repository
{
    /// <summary>
    /// Repository implementation for Election entity operations.
    /// </summary>
    public class ElectionRepository : IElectionRepository
    {
        private readonly IDatabaseService _db;

        public ElectionRepository(IDatabaseService db)
        {
            _db = db;
        }

        /// <inheritdoc/>
        public Election GetById(string id)
        {
            return _db.Get<Election>(id);
        }

        /// <inheritdoc/>
        public IEnumerable<Election> GetByPropertyId(string propertyId)
        {
            var query = new DBQuery<Election>()
                .AddFieldSearch(nameof(Election.PropertyId), propertyId, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<Election> GetByStage(ElectionStageType stage)
        {
            var query = new DBQuery<Election>()
                .AddFieldSearch(nameof(Election.Stage), (int)stage);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<Election> GetByPropertyIdAndStage(string propertyId, ElectionStageType stage)
        {
            var query = new DBQuery<Election>()
                .AddFieldSearch(nameof(Election.PropertyId), propertyId, false)
                .AddFieldSearch(nameof(Election.Stage), (int)stage);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public Election GetSingleByPropertyId(string propertyId)
        {
            var query = new DBQuery<Election>()
                .AddFieldSearch(nameof(Election.PropertyId), propertyId, false);
            return _db.Search(query).FirstOrDefault();
        }

        /// <inheritdoc/>
        public void Save(Election election)
        {
            _db.Set(election);
        }

        /// <inheritdoc/>
        public void Delete(string id)
        {
            _db.Delete<Election>(id);
        }

        /// <inheritdoc/>
        public bool Exists(string id)
        {
            return _db.Exists<Election>(id);
        }
    }
}
