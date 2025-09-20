using SWLOR.Game.Server.Service;
using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Shared.Core.Data
{
    /// <summary>
    /// Database service providing Redis-based storage and search capabilities.
    /// Handles CRUD operations, indexing, and complex queries for entities.
    /// </summary>
    public class DatabaseService: IDatabaseService
    {
        public void Set<T>(T entity) where T : EntityBase
        {
            DB.Set(entity);
        }

        public T Get<T>(string id) where T : EntityBase
        {
            return DB.Get<T>(id);
        }

        public string GetRawJson<T>(string id)
        {
            return DB.GetRawJson<T>(id);
        }

        public bool Exists<T>(string id) where T : EntityBase
        {
            return DB.Exists<T>(id);
        }

        public void Delete<T>(string id) where T : EntityBase
        {
            DB.Delete<T>(id);
        }

        public IEnumerable<T> Search<T>(IDBQuery<T> query) where T : EntityBase
        {
            return DB.Search(query);
        }

        public IEnumerable<string> SearchRawJson<T>(IDBQuery<T> query) where T : EntityBase
        {
            return DB.SearchRawJson(query);
        }

        public long SearchCount<T>(IDBQuery<T> query) where T : EntityBase
        {
            return DB.SearchCount(query);
        }
    }
}
