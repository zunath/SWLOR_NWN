using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Component.Admin.Repository
{
    /// <summary>
    /// Repository implementation for PlayerBan entity operations.
    /// </summary>
    public class PlayerBanRepository : IPlayerBanRepository
    {
        private readonly IDatabaseService _db;

        public PlayerBanRepository(IDatabaseService db)
        {
            _db = db;
        }

        /// <inheritdoc/>
        public PlayerBan GetById(string id)
        {
            return _db.Get<PlayerBan>(id);
        }

        /// <inheritdoc/>
        public PlayerBan GetByCDKey(string cdKey)
        {
            var query = new DBQuery<PlayerBan>()
                .AddFieldSearch(nameof(PlayerBan.CDKey), cdKey, false);
            return _db.Search(query).FirstOrDefault();
        }

        /// <inheritdoc/>
        public IEnumerable<PlayerBan> GetAll()
        {
            var query = new DBQuery<PlayerBan>();
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public long GetCount()
        {
            var query = new DBQuery<PlayerBan>();
            return _db.SearchCount(query);
        }

        /// <inheritdoc/>
        public void Save(PlayerBan playerBan)
        {
            _db.Set(playerBan);
        }

        /// <inheritdoc/>
        public void Delete(string id)
        {
            _db.Delete<PlayerBan>(id);
        }

        /// <inheritdoc/>
        public bool Exists(string id)
        {
            return _db.Exists<PlayerBan>(id);
        }
    }
}
