namespace SWLOR.Shared.Abstractions.Contracts
{
    /// <summary>
    /// Interface for database operations using Redis with JSON storage and search capabilities.
    /// </summary>
    public interface IDatabaseService
    {
        /// <summary>
        /// Stores a specific object in the database by its Id.
        /// </summary>
        /// <typeparam name="T">The type of data to store</typeparam>
        /// <param name="entity">The data to store.</param>
        void Set<T>(T entity) where T : EntityBase;

        /// <summary>
        /// Retrieves a specific object in the database by an arbitrary key.
        /// </summary>
        /// <typeparam name="T">The type of data to retrieve</typeparam>
        /// <param name="id">The arbitrary key the data is stored under</param>
        /// <returns>The object stored in the database under the specified key</returns>
        T Get<T>(string id) where T : EntityBase;

        /// <summary>
        /// Retrieves the raw JSON of the object in the database by a given prefix and key.
        /// This can be useful for data migrations and one-time actions at server load.
        /// Do not use this for normal game play as it is slow.
        /// </summary>
        /// <typeparam name="T">The type of entity to retrieve</typeparam>
        /// <param name="id">The arbitrary key the data is stored under</param>
        /// <returns>The raw json stored in the database under the specified key</returns>
        string GetRawJson<T>(string id);

        /// <summary>
        /// Returns true if an entry with the specified key exists.
        /// Returns false if not.
        /// </summary>
        /// <typeparam name="T">The type of entity to check</typeparam>
        /// <param name="id">The key of the entity.</param>
        /// <returns>true if found, false otherwise.</returns>
        bool Exists<T>(string id) where T : EntityBase;

        /// <summary>
        /// Deletes an entry by a specified key.
        /// </summary>
        /// <typeparam name="T">The type of entity to delete.</typeparam>
        /// <param name="id">The key of the entity</param>
        void Delete<T>(string id) where T : EntityBase;

        /// <summary>
        /// Searches the Redis DB for records matching the query criteria.
        /// </summary>
        /// <typeparam name="T">The type of entity to retrieve.</typeparam>
        /// <param name="query">The query to run.</param>
        /// <returns>An enumerable of entities matching the criteria.</returns>
        IEnumerable<T> Search<T>(IDBQuery<T> query) where T : EntityBase;

        /// <summary>
        /// Searches the Redis DB for raw JSON records matching the query criteria.
        /// </summary>
        /// <typeparam name="T">The type of entity to retrieve.</typeparam>
        /// <param name="query">The query to run.</param>
        /// <returns>An enumerable of raw json values matching the criteria.</returns>
        IEnumerable<string> SearchRawJson<T>(IDBQuery<T> query) where T : EntityBase;

        /// <summary>
        /// Searches the Redis DB for the number of records matching the query criteria.
        /// This only retrieves the number of records. Use Search() if you need the actual results.
        /// </summary>
        /// <typeparam name="T">The type of entity to retrieve.</typeparam>
        /// <param name="query">The query to run.</param>
        /// <returns>The number of records matching the query criteria.</returns>
        long SearchCount<T>(IDBQuery<T> query) where T : EntityBase;
    }
}
