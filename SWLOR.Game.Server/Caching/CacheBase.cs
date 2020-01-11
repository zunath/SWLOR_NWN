using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using SWLOR.Game.Server.Caching.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;
using Index = SWLOR.Game.Server.Data.Index;

namespace SWLOR.Game.Server.Caching
{
    public abstract class CacheBase<T>: ICache<T>
        where T: class, IEntity
    {
        private readonly string _setName;

        protected CacheBase(string setName)
        {
            _setName = setName;

            MessageHub.Instance.Subscribe<OnCacheObjectSet<T>>(msg => CacheObjectSet(msg.Entity));
            MessageHub.Instance.Subscribe<OnCacheObjectDeleted<T>>(msg => CacheObjectRemoved(msg.Entity));
            OnSubscribeEvents();
        }

        /// <summary>
        /// Retrieves index details for this entity type.
        /// </summary>
        /// <returns>The Index details for this entity type.</returns>
        private Index GetIndexDetails()
        {
            var key = $"{_setName}:Index";
            var index = 
                NWNXRedis.Exists(key) ?
                JsonConvert.DeserializeObject<Index>(NWNXRedis.Get(key)) :
                new Index();

            return index;
        }

        /// <summary>
        /// Stores index details for this entity type.
        /// </summary>
        /// <param name="index">The index to store.</param>
        private void SetIndexDetails(Index index)
        {
            var key = $"{_setName}:Index";
            var json = JsonConvert.SerializeObject(index);
            NWNXRedis.Set(key, json);
        }

        /// <summary>
        /// Fires when an entity is added or updated in the cache.
        /// </summary>
        /// <param name="entity">The entity which was added or updated.</param>
        private void CacheObjectSet(T entity)
        {
            // Update the entity data.
            var id = GetEntityKey(entity);
            var key = $"{_setName}:{id}";
            var json = JsonConvert.SerializeObject(entity);
            NWNXRedis.Set(key, json);

            // Update the index data.
            var index = GetIndexDetails();

            if (!index.IDs.Contains(id))
            {
                index.IDs.Add(id);
                SetIndexDetails(index);
            }

            // Publish event notifying that a new object has been set into the cache.
            OnCacheObjectSet(entity);
        }

        /// <summary>
        /// Fires when an entity is removed from the cache.
        /// </summary>
        /// <param name="entity">The entity which was removed from the cache.</param>
        private void CacheObjectRemoved(T entity)
        {
            // Remove the entity data.
            var id = GetEntityKey(entity);
            var key = $"{_setName}:{id}" ;

            NWNXRedis.Delete(key);

            // Update the index data.
            var index = GetIndexDetails();
            index.IDs.Remove(id);
            SetIndexDetails(index);

            // Publish event notifying that an object has been removed from the cache.
            OnCacheObjectRemoved(entity);
        }

        /// <summary>
        /// Retrieves an entity by its ID.
        /// </summary>
        /// <param name="id">The ID of the entity.</param>
        /// <returns>An entity with a matching ID.</returns>
        protected T ByID(object id)
        {
            var key = $"{_setName}:{id}";
            var json = NWNXRedis.Get(key);

            return JsonConvert.DeserializeObject<T>(json);
        }

        /// <summary>
        /// Returns all of the entities stored in the cache for this object type.
        /// This should be used sparingly and only on small data sets.
        /// </summary>
        /// <returns>An enumerable of entities</returns>
        public IEnumerable<T> GetAll()
        {
            var index = GetIndexDetails();
            
            foreach(var id in index.IDs)
            {
                yield return JsonConvert.DeserializeObject<T>(NWNXRedis.Get($"{_setName}:{id}"));
            }
        }

        /// <summary>
        /// Returns whether an entity exists in the cache.
        /// </summary>
        /// <param name="id">The ID of the entity to check.</param>
        /// <returns>true if it exists, false otherwise</returns>
        protected bool Exists(object id)
        {
            var key = $"{_setName}:{id}";
            return NWNXRedis.Exists(key);
        }

        /// <summary>
        /// Retrieves the unique key for an entity. This is denoted with the KeyAttribute attribute.
        /// </summary>
        /// <param name="entity">The entity whose ID we want to retrieve.</param>
        /// <returns></returns>
        private static string GetEntityKey(IEntity entity)
        {
            // Locate a Key attribute on this type.
            var properties = entity.GetType().GetProperties();
            PropertyInfo propertyWithKey = null;

            foreach (var prop in properties)
            {
                var autoGenKey = prop.GetCustomAttributes(typeof(KeyAttribute), false).FirstOrDefault();
                if (autoGenKey != null)
                {
                    propertyWithKey = prop;
                    break;
                }
            }

            if (propertyWithKey == null)
            {
                throw new NullReferenceException("Unable to find a Key attribute on the entity provided (" + entity.GetType() + "). Make sure you add the Key attribute to the entity.");
            }

            string key = propertyWithKey.GetValue(entity).ToString();
            return key;
        }

        /// <summary>
        /// Stores an entity into a given index.
        /// </summary>
        /// <param name="indexName">The name of the index</param>
        /// <param name="indexValue">The value of the index.</param>
        /// <param name="entity">The entity to store</param>
        protected void SetIntoIndex(string indexName, string indexValue, T entity)
        {
            var index = GetIndexDetails();
            var id = GetEntityKey(entity);
            var key = $"{indexName}:{indexValue}";
            index.SecondaryIndexes[key] = id;

            SetIndexDetails(index);
        }

        /// <summary>
        /// Stores an entity into a given list index.
        /// </summary>
        /// <param name="indexName">The name of the list index.</param>
        /// <param name="indexValue">The value of the list index.</param>
        /// <param name="entity">The entity to store</param>
        protected void SetIntoListIndex(string indexName, string indexValue, T entity)
        {
            var index = GetIndexDetails();
            var id = GetEntityKey(entity);
            var key = $"{indexName}:{indexValue}";

            if (!index.SecondaryListIndexes.ContainsKey(key))
            {
                index.SecondaryListIndexes[key] = new List<object>();
            }

            var list = index.SecondaryListIndexes[key];

            if (!list.Contains(id))
            {
                list.Add(id);
                SetIndexDetails(index);
            }
        }

        /// <summary>
        /// Removes an entity located at an index.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <param name="indexValue">The value of the index.</param>
        protected void RemoveFromIndex(string indexName, string indexValue)
        {
            var index = GetIndexDetails();
            var key = $"{indexName}:{indexValue}";
            index.SecondaryIndexes.Remove(key);
            SetIndexDetails(index);
        }

        /// <summary>
        /// Removes an entity located at a list index.
        /// </summary>
        /// <param name="indexName">The name of the list index.</param>
        /// <param name="indexValue">The value of the list index.</param>
        /// <param name="entity">The entity to remove.</param>
        protected void RemoveFromListIndex(string indexName, string indexValue, T entity)
        {
            var index = GetIndexDetails();
            var key = $"{indexName}:{indexValue}";
            var id = GetEntityKey(entity);

            var list = index.SecondaryListIndexes[key];
            list.Remove(id);
            SetIndexDetails(index);
        }

        /// <summary>
        /// Retrieves an entity from a given index.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <param name="indexValue">The value of the index.</param>
        /// <returns>An entity stored at a given index.</returns>
        protected T GetFromIndex(string indexName, string indexValue)
        {
            var index = GetIndexDetails();
            var key = $"{indexName}:{indexValue}";
            var id = index.SecondaryIndexes[key];

            return ByID(id);
        }

        /// <summary>
        /// Retrieves a list of entities from a list index.
        /// </summary>
        /// <param name="indexName">The name of the list index.</param>
        /// <param name="indexValue">The value of the list index.</param>
        /// <returns>An enumerable of entities stored at the provided list index.</returns>
        protected IEnumerable<T> GetFromListIndex(string indexName, string indexValue)
        {
            var index = GetIndexDetails();
            var key = $"{indexName}:{indexValue}";

            var list = index.SecondaryListIndexes[key];
            
            foreach (var id in list)
            {
                var entityKey = $"{_setName}:{id}";
                yield return JsonConvert.DeserializeObject<T>(NWNXRedis.Get(entityKey));
            }
        }

        /// <summary>
        /// Returns whether an object exists in an index.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <param name="indexValue">The value of the index.</param>
        /// <returns>true if an object exists in this index, false otherwise</returns>
        protected bool ExistsByIndex(string indexName, string indexValue)
        {
            var index = GetIndexDetails();
            var key = $"{indexName}:{indexValue}";
            return index.SecondaryIndexes.ContainsKey(key);
        }

        /// <summary>
        /// Returns whether any object exists in a list index.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <param name="indexValue">The value of the index.</param>
        /// <returns>true if any object exists in this list index, false otherwise</returns>
        protected bool ExistsByListIndex(string indexName, string indexValue)
        {
            var index = GetIndexDetails();
            var key = $"{indexName}:{indexValue}";
            return index.SecondaryListIndexes.ContainsKey(key);
        }

        /// <summary>
        /// Returns whether an object exists within a list index.
        /// </summary>
        /// <param name="indexName">The name of the index.</param>
        /// <param name="indexValue">The value of the index.</param>
        /// <param name="entity">The entity to locate.</param>
        /// <returns>true if object exists, false otherwise</returns>
        protected bool ExistsInListIndex(string indexName, string indexValue, T entity)
        {
            var id = GetEntityKey(entity);
            var index = GetIndexDetails();
            var key = $"{indexName}:{indexValue}";

            if (!index.SecondaryListIndexes.ContainsKey(key))
                return false;

            return index.SecondaryListIndexes[key].Contains(id);
        }

        protected abstract void OnCacheObjectSet(T entity);
        protected abstract void OnCacheObjectRemoved(T entity);
        protected abstract void OnSubscribeEvents();

    }
}
