using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;

namespace SWLOR.Game.Server.Domain.Core
{
    /// <summary>
    /// The persistence calls the converted services make. Until <c>DB</c> itself is converted (Phase 2, wave C) the
    /// implementation is <c>StaticDBAdapter</c>, which forwards to the static <c>DB</c> class one to one, so a service
    /// that takes this interface behaves exactly as one that called <c>DB</c> directly, and a unit test can hand it an
    /// in-memory fake. When <c>DB</c> converts, this becomes its interface and the adapter goes away.
    /// Members are added here as services need them, with the same signatures as on <c>DB</c>.
    /// </summary>
    public interface IDBService
    {
        /// <summary>Retrieves an object by its Id, or null when none is stored. Same as <c>DB.Get&lt;T&gt;</c>.</summary>
        T Get<T>(string id)
            where T : EntityBase;

        /// <summary>Stores an object under its Id. Same as <c>DB.Set&lt;T&gt;</c>.</summary>
        void Set<T>(T entity)
            where T : EntityBase;

        /// <summary>Determines whether an entity with the Id is stored. Same as <c>DB.Exists&lt;T&gt;</c>.</summary>
        bool Exists<T>(string id)
            where T : EntityBase;

        /// <summary>Deletes an entity by its Id. Same as <c>DB.Delete&lt;T&gt;</c>.</summary>
        void Delete<T>(string id)
            where T : EntityBase;

        /// <summary>Searches for the entities matching a query. Same as <c>DB.Search&lt;T&gt;</c> (the result is lazy).</summary>
        IEnumerable<T> Search<T>(DBQuery<T> query)
            where T : EntityBase;

        /// <summary>Counts the entities matching a query without reading them. Same as <c>DB.SearchCount&lt;T&gt;</c>.</summary>
        long SearchCount<T>(DBQuery<T> query)
            where T : EntityBase;
    }
}
