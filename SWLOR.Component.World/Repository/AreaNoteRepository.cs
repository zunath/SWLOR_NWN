using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Component.World.Repository
{
    /// <summary>
    /// Repository implementation for AreaNote entity operations.
    /// </summary>
    public class AreaNoteRepository : IAreaNoteRepository
    {
        private readonly IDatabaseService _db;

        public AreaNoteRepository(IDatabaseService db)
        {
            _db = db;
        }

        /// <inheritdoc/>
        public AreaNote GetById(string id)
        {
            return _db.Get<AreaNote>(id);
        }

        /// <inheritdoc/>
        public IEnumerable<AreaNote> GetByAreaResref(string areaResref)
        {
            var query = new DBQuery<AreaNote>()
                .AddFieldSearch(nameof(AreaNote.AreaResref), areaResref, false)
                .OrderBy(nameof(AreaNote.AreaResref));
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public void Save(AreaNote areaNote)
        {
            _db.Set(areaNote);
        }

        /// <inheritdoc/>
        public void Delete(string id)
        {
            _db.Delete<AreaNote>(id);
        }

        /// <inheritdoc/>
        public bool Exists(string id)
        {
            return _db.Exists<AreaNote>(id);
        }
    }
}
