using SWLOR.Component.Admin.Entity;
using SWLOR.Shared.Domain.Admin.Enums;

namespace SWLOR.Shared.Domain.Repositories
{
    /// <summary>
    /// Repository interface for AuthorizedDM entity operations.
    /// </summary>
    public interface IAuthorizedDMRepository
    {
        /// <summary>
        /// Gets an authorized DM by their unique identifier.
        /// </summary>
        /// <param name="id">The authorized DM's unique identifier</param>
        /// <returns>The authorized DM if found, null otherwise</returns>
        AuthorizedDM GetById(string id);

        /// <summary>
        /// Gets an authorized DM by their CD key.
        /// </summary>
        /// <param name="cdKey">The CD key to search for</param>
        /// <returns>The authorized DM if found, null otherwise</returns>
        AuthorizedDM GetByCDKey(string cdKey);

        /// <summary>
        /// Gets all authorized DMs by authorization level.
        /// </summary>
        /// <param name="authorizationLevel">The authorization level to search for</param>
        /// <returns>Collection of authorized DMs with the specified authorization level</returns>
        IEnumerable<AuthorizedDM> GetByAuthorizationLevel(AuthorizationLevel authorizationLevel);

        /// <summary>
        /// Gets all authorized DMs by name.
        /// </summary>
        /// <param name="name">The name to search for</param>
        /// <returns>Collection of authorized DMs with the specified name</returns>
        IEnumerable<AuthorizedDM> GetByName(string name);

        /// <summary>
        /// Gets all authorized DMs.
        /// </summary>
        /// <returns>Collection of all authorized DMs</returns>
        IEnumerable<AuthorizedDM> GetAll();

        /// <summary>
        /// Gets authorized DMs with optional search text.
        /// </summary>
        /// <param name="searchText">Optional search text to filter by name</param>
        /// <returns>Collection of authorized DMs matching the search criteria</returns>
        IEnumerable<AuthorizedDM> GetDMsWithSearch(string searchText = null);

        /// <summary>
        /// Saves an authorized DM entity.
        /// </summary>
        /// <param name="authorizedDM">The authorized DM to save</param>
        void Save(AuthorizedDM authorizedDM);

        /// <summary>
        /// Deletes an authorized DM by their unique identifier.
        /// </summary>
        /// <param name="id">The authorized DM's unique identifier</param>
        void Delete(string id);

        /// <summary>
        /// Checks if an authorized DM exists by their unique identifier.
        /// </summary>
        /// <param name="id">The authorized DM's unique identifier</param>
        /// <returns>True if the authorized DM exists, false otherwise</returns>
        bool Exists(string id);
    }
}
