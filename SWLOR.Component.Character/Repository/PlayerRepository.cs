using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Repositories;
using SWLOR.Shared.Domain.Character.Enums;

namespace SWLOR.Component.Character.Repository
{
    /// <summary>
    /// Repository implementation for Player entity operations.
    /// </summary>
    public class PlayerRepository : IPlayerRepository
    {
        private readonly IDatabaseService _db;

        public PlayerRepository(IDatabaseService db)
        {
            _db = db;
        }

        /// <inheritdoc/>
        public Player GetById(string id)
        {
            return _db.Get<Player>(id);
        }

        /// <inheritdoc/>
        public IEnumerable<Player> GetByAreaResref(string areaResref)
        {
            var query = new DBQuery<Player>()
                .AddFieldSearch(nameof(Player.LocationAreaResref), areaResref, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<Player> GetByCharacterType(CharacterType characterType)
        {
            var query = new DBQuery<Player>()
                .AddFieldSearch(nameof(Player.CharacterType), (int)characterType);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<Player> GetByCitizenPropertyId(string citizenPropertyId)
        {
            var query = new DBQuery<Player>()
                .AddFieldSearch(nameof(Player.CitizenPropertyId), citizenPropertyId, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<Player> GetActiveCitizensByPropertyId(string citizenPropertyId)
        {
            var query = new DBQuery<Player>()
                .AddFieldSearch(nameof(Player.CitizenPropertyId), citizenPropertyId, false)
                .AddFieldSearch(nameof(Player.IsDeleted), false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<Player> GetActivePlayers()
        {
            var query = new DBQuery<Player>()
                .AddFieldSearch(nameof(Player.IsDeleted), false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public void Save(Player player)
        {
            _db.Set(player);
        }

        /// <inheritdoc/>
        public void Delete(string id)
        {
            _db.Delete<Player>(id);
        }

        /// <inheritdoc/>
        public bool Exists(string id)
        {
            return _db.Exists<Player>(id);
        }

        /// <inheritdoc/>
        public long GetCountByCitizenPropertyId(string citizenPropertyId)
        {
            var query = new DBQuery<Player>()
                .AddFieldSearch(nameof(Player.CitizenPropertyId), citizenPropertyId, false);
            return _db.SearchCount(query);
        }

        /// <inheritdoc/>
        public IEnumerable<Player> GetAll()
        {
            var query = new DBQuery<Player>();
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public long GetCount()
        {
            var query = new DBQuery<Player>();
            return _db.SearchCount(query);
        }

        /// <inheritdoc/>
        public IEnumerable<Player> GetPlayersWithSearch(string searchText = null)
        {
            var query = new DBQuery<Player>();
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query.AddFieldSearch(nameof(Player.Name), searchText, true);
            }
            return _db.Search(query);
        }
    }
}
