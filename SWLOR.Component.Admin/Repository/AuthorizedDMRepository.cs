using SWLOR.Component.Admin.Entity;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Admin.Enums;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Component.Admin.Repository
{
    /// <summary>
    /// Repository implementation for AuthorizedDM entity operations.
    /// </summary>
    public class AuthorizedDMRepository : IAuthorizedDMRepository
    {
        private readonly IDatabaseService _db;

        public AuthorizedDMRepository(IDatabaseService db)
        {
            _db = db;
        }

        /// <inheritdoc/>
        public AuthorizedDM GetById(string id)
        {
            return _db.Get<AuthorizedDM>(id);
        }

        /// <inheritdoc/>
        public AuthorizedDM GetByCDKey(string cdKey)
        {
            var query = new DBQuery<AuthorizedDM>()
                .AddFieldSearch(nameof(AuthorizedDM.CDKey), cdKey, false);
            return _db.Search(query).FirstOrDefault();
        }

        /// <inheritdoc/>
        public IEnumerable<AuthorizedDM> GetByAuthorizationLevel(AuthorizationLevel authorizationLevel)
        {
            var query = new DBQuery<AuthorizedDM>()
                .AddFieldSearch(nameof(AuthorizedDM.Authorization), (int)authorizationLevel);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<AuthorizedDM> GetByName(string name)
        {
            var query = new DBQuery<AuthorizedDM>()
                .AddFieldSearch(nameof(AuthorizedDM.Name), name, false);
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<AuthorizedDM> GetAll()
        {
            var query = new DBQuery<AuthorizedDM>();
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<AuthorizedDM> GetDMsWithSearch(string searchText = null)
        {
            var query = new DBQuery<AuthorizedDM>();
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query.AddFieldSearch(nameof(AuthorizedDM.Name), searchText, true);
            }
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public void Save(AuthorizedDM authorizedDM)
        {
            _db.Set(authorizedDM);
        }

        /// <inheritdoc/>
        public void Delete(string id)
        {
            _db.Delete<AuthorizedDM>(id);
        }

        /// <inheritdoc/>
        public bool Exists(string id)
        {
            return _db.Exists<AuthorizedDM>(id);
        }
    }
}
