using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using StackExchange.Redis;
using SWLOR.Game.Server.Caching.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Service;
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

        private Index GetIndexDetails()
        {
            var key = $"{_setName}:Index";
            var index = 
                DataService.DB.KeyExists(key) ?
                JsonConvert.DeserializeObject<Index>(DataService.DB.StringGet(key)) :
                new Index();

            return index;
        }

        private void SetIndexDetails(Index index)
        {
            var key = $"{_setName}:Index";
            var json = JsonConvert.SerializeObject(index);
            DataService.DB.StringSet(key, json);
        }

        private void CacheObjectSet(T entity)
        {
            // Update the entity data.
            var id = GetEntityKey(entity);
            var key = $"{_setName}:{id}";
            var json = JsonConvert.SerializeObject(entity);
            DataService.DB.StringSet(key, json);

            // Update the index data.
            var index = GetIndexDetails();
            index.IDs.Add(id);
            SetIndexDetails(index);

            // Publish event notifying that a new object has been set into the cache.
            OnCacheObjectSet(entity);
        }

        private void CacheObjectRemoved(T entity)
        {
            // Remove the entity data.
            var id = GetEntityKey(entity);
            var key = $"{_setName}:{id}" ;
            DataService.DB.KeyDelete(key);

            // Update the index data.
            var index = GetIndexDetails();
            index.IDs.Remove(id);
            SetIndexDetails(index);

            // Publish event notifying that an object has been removed from the cache.
            OnCacheObjectRemoved(entity);
        }

        protected T ByID(object id)
        {
            var key = $"{_setName}:{id}";
            var json = DataService.DB.StringGet(key);

            return JsonConvert.DeserializeObject<T>(json);
        }

        protected IEnumerable<T> GetAll()
        {
            var index = GetIndexDetails();
            List<RedisKey> redisKeys = new List<RedisKey>();

            foreach(var id in index.IDs)
            {
                redisKeys.Add($"{_setName}:{id}");
            }

            var data = DataService.DB.StringGet(redisKeys.ToArray());
            foreach (var record in data)
            {
                yield return JsonConvert.DeserializeObject<T>(record);
            }
        }

        protected bool Exists(object id)
        {
            var key = $"{_setName}:{id}";
            return DataService.DB.KeyExists(key);
        }

        private static object GetEntityKey(IEntity entity)
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

            return propertyWithKey.GetValue(entity);
        }

        protected void SetIntoIndex(string indexName, string indexValue, T entity)
        {
            var index = GetIndexDetails();
            var id = GetEntityKey(entity);
            var key = $"{indexName}:{indexValue}";
            index.SecondaryIndexes[key] = id;

            SetIndexDetails(index);
        }

        protected void SetIntoListIndex(string indexName, string indexValue, T entity)
        {
            var index = GetIndexDetails();
            var id = GetEntityKey(entity);
            var key = $"{indexName}:{indexValue}";

            if (!index.SecondaryIndexes.ContainsKey(key))
            {
                index.SecondaryIndexes[key] = new List<object>();
            }

            var list = (List<object>)index.SecondaryIndexes[key];
            list.Add(id);
            SetIndexDetails(index);
        }

        protected void RemoveFromIndex(string indexName, string indexValue)
        {
            var index = GetIndexDetails();
            var key = $"{indexName}:{indexValue}";
            index.SecondaryIndexes.Remove(key);
            SetIndexDetails(index);
        }

        protected void RemoveFromListIndex(string indexName, string indexValue, T entity)
        {
            var index = GetIndexDetails();
            var key = $"{indexName}:{indexValue}";
            var id = GetEntityKey(entity);

            var list = (List<object>) index.SecondaryIndexes[key];
            list.Remove(id);
            SetIndexDetails(index);
        }

        protected T GetFromIndex(string indexName, string indexValue)
        {
            var index = GetIndexDetails();
            var key = $"{indexName}:{indexValue}";
            var id = index.SecondaryIndexes[key];

            return ByID(id);
        }

        protected IEnumerable<T> GetFromListIndex(string indexName, string indexValue)
        {
            var index = GetIndexDetails();
            var key = $"{indexName}:{indexValue}";

            var list = (List<object>) index.SecondaryIndexes[key];
            var redisKeys = new List<RedisKey>();

            foreach (var id in list)
            {
                redisKeys.Add($"{_setName}:{id}");
            }

            var results = DataService.DB.StringGet(redisKeys.ToArray());

            foreach(var result in results)
            {
                yield return JsonConvert.DeserializeObject<T>(result);
            }
        }

        protected bool ExistsByIndex(string indexName, string indexValue)
        {
            var index = GetIndexDetails();
            var key = $"{indexName}:{indexValue}";
            return index.SecondaryIndexes.ContainsKey(key);
        }

        protected abstract void OnCacheObjectSet(T entity);
        protected abstract void OnCacheObjectRemoved(T entity);
        protected abstract void OnSubscribeEvents();

    }
}
