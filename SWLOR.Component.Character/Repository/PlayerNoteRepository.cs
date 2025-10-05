using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Component.Character.Repository
{
    /// <summary>
    /// Repository implementation for PlayerNote entity operations.
    /// </summary>
    public class PlayerNoteRepository : IPlayerNoteRepository
    {
        private readonly IDatabaseService _db;

        public PlayerNoteRepository(IDatabaseService db)
        {
            _db = db;
        }

        /// <inheritdoc/>
        public PlayerNote GetById(string id)
        {
            return _db.Get<PlayerNote>(id);
        }

        /// <inheritdoc/>
        public IEnumerable<PlayerNote> GetByPlayerId(string playerId)
        {
            var query = new DBQuery<PlayerNote>()
                .AddFieldSearch(nameof(PlayerNote.PlayerId), playerId, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<PlayerNote> GetPlayerNotesByPlayerId(string playerId)
        {
            var query = new DBQuery<PlayerNote>()
                .AddFieldSearch(nameof(PlayerNote.PlayerId), playerId, false)
                .AddFieldSearch(nameof(PlayerNote.IsDMNote), false)
                .OrderBy(nameof(PlayerNote.Name));
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<PlayerNote> GetByName(string name)
        {
            var query = new DBQuery<PlayerNote>()
                .AddFieldSearch(nameof(PlayerNote.Name), name, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<PlayerNote> GetDMNotes()
        {
            var query = new DBQuery<PlayerNote>()
                .AddFieldSearch(nameof(PlayerNote.IsDMNote), true);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<PlayerNote> GetDMNotesByCreatorName(string creatorName)
        {
            var query = new DBQuery<PlayerNote>()
                .AddFieldSearch(nameof(PlayerNote.IsDMNote), true)
                .AddFieldSearch(nameof(PlayerNote.DMCreatorName), creatorName, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<PlayerNote> GetDMNotesByCreatorCDKey(string creatorCDKey)
        {
            var query = new DBQuery<PlayerNote>()
                .AddFieldSearch(nameof(PlayerNote.IsDMNote), true)
                .AddFieldSearch(nameof(PlayerNote.DMCreatorCDKey), creatorCDKey, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<PlayerNote> GetDMNotesByPlayerId(string playerId)
        {
            var query = new DBQuery<PlayerNote>()
                .AddFieldSearch(nameof(PlayerNote.PlayerId), playerId, false)
                .AddFieldSearch(nameof(PlayerNote.IsDMNote), true);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public void Save(PlayerNote playerNote)
        {
            _db.Set(playerNote);
        }

        /// <inheritdoc/>
        public void Delete(string id)
        {
            _db.Delete<PlayerNote>(id);
        }

        /// <inheritdoc/>
        public IEnumerable<PlayerNote> GetAll()
        {
            var query = new DBQuery<PlayerNote>();
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public long GetCount()
        {
            var query = new DBQuery<PlayerNote>();
            return _db.SearchCount(query);
        }

        /// <inheritdoc/>
        public bool Exists(string id)
        {
            return _db.Exists<PlayerNote>(id);
        }
    }
}
