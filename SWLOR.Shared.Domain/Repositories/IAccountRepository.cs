using SWLOR.Shared.Domain.Entities;

namespace SWLOR.Shared.Domain.Repositories
{
    /// <summary>
    /// Repository interface for Account entity operations.
    /// </summary>
    public interface IAccountRepository
    {
        /// <summary>
        /// Gets an account by its unique identifier.
        /// </summary>
        /// <param name="id">The account's unique identifier</param>
        /// <returns>The account if found, null otherwise</returns>
        Account GetById(string id);

        /// <summary>
        /// Gets all accounts.
        /// </summary>
        /// <returns>Collection of all accounts</returns>
        IEnumerable<Account> GetAll();

        /// <summary>
        /// Gets accounts with optional search text.
        /// </summary>
        /// <param name="searchText">Optional search text to filter by ID</param>
        /// <returns>Collection of accounts matching the search criteria</returns>
        IEnumerable<Account> GetAccountsWithSearch(string searchText = null);

        /// <summary>
        /// Gets the count of all accounts.
        /// </summary>
        /// <returns>The count of all accounts</returns>
        long GetCount();

        /// <summary>
        /// Saves an account entity.
        /// </summary>
        /// <param name="account">The account to save</param>
        void Save(Account account);

        /// <summary>
        /// Deletes an account by its unique identifier.
        /// </summary>
        /// <param name="id">The account's unique identifier</param>
        void Delete(string id);

        /// <summary>
        /// Checks if an account exists by its unique identifier.
        /// </summary>
        /// <param name="id">The account's unique identifier</param>
        /// <returns>True if the account exists, false otherwise</returns>
        bool Exists(string id);
    }
}
