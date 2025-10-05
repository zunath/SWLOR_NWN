using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Component.Character.Repository
{
    /// <summary>
    /// Repository implementation for Account entity operations.
    /// </summary>
    public class AccountRepository : IAccountRepository
    {
        private readonly IDatabaseService _db;

        public AccountRepository(IDatabaseService db)
        {
            _db = db;
        }

        /// <inheritdoc/>
        public Account GetById(string id)
        {
            return _db.Get<Account>(id);
        }

        /// <inheritdoc/>
        public IEnumerable<Account> GetAll()
        {
            var query = new DBQuery<Account>();
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public IEnumerable<Account> GetAccountsWithSearch(string searchText = null)
        {
            var query = new DBQuery<Account>();
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query.AddFieldSearch(nameof(Account.Id), searchText, true);
            }
            return _db.Search(query);
        }

        /// <inheritdoc/>
        public long GetCount()
        {
            var query = new DBQuery<Account>();
            return _db.SearchCount(query);
        }

        /// <inheritdoc/>
        public void Save(Account account)
        {
            _db.Set(account);
        }

        /// <inheritdoc/>
        public void Delete(string id)
        {
            _db.Delete<Account>(id);
        }

        /// <inheritdoc/>
        public bool Exists(string id)
        {
            return _db.Exists<Account>(id);
        }
    }
}
