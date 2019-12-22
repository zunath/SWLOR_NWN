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
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Caching
{
    public abstract class CacheBase<T>: ICache<T>
        where T: class, IEntity
    {
        protected CacheBase()
        {
            MessageHub.Instance.Subscribe<OnCacheObjectSet<T>>(msg => CacheObjectSet(msg.Entity));
            MessageHub.Instance.Subscribe<OnCacheObjectDeleted<T>>(msg => CacheObjectRemoved(msg.Entity));
            OnSubscribeEvents();
        }

        private void CacheObjectSet(T entity)
        {
            var clone = (T)entity.Clone();
            var @namespace = typeof(T).Name;
            var id = GetEntityKey(clone);
            var key = $"{@namespace}:{id}";
            var json = JsonConvert.SerializeObject(clone);
            DataService.DB.StringSet(key, json);
            OnCacheObjectSet(@namespace, id, clone);
        }

        private void CacheObjectRemoved(T entity)
        {
            var @namespace = typeof(T).Name;
            var id = GetEntityKey(entity);
            var key = $"{@namespace}:{id}" ;
            DataService.DB.KeyDelete(key);
            OnCacheObjectRemoved(@namespace, id, entity);
        }

        protected T ByID(object id)
        {
            var key = $"{typeof(T).Name}:{id}";
            var json = DataService.DB.StringGet(key);

            return JsonConvert.DeserializeObject<T>(json);
        }

        protected bool Exists(object id)
        {
            var key = $"{typeof(T).Name}:{id}";
            return DataService.DB.KeyExists(key);
        }

        protected void SetIndexByKey(string index, object id)
        {
            var key = $"{typeof(T).Name}:{index}";
            var json = JsonConvert.SerializeObject(id);
            DataService.DB.StringSet(key, json);
        }

        protected bool ExistsByIndex(string index)
        {
            var key = $"{typeof(T).Name}:{index}";
            return DataService.DB.KeyExists(key);
        }

        protected T GetByIndex(string index)
        {
            var key = $"{typeof(T).Name}:{index}";
            var json = DataService.DB.StringGet(key);

            return JsonConvert.DeserializeObject<T>(json);
        }

        protected void RemoveByIndex(string index)
        {
            var key = $"{typeof(T).Name}:{index}";
            DataService.DB.KeyDelete(key);
        }

        protected long AddIndexToList(string listName, object id)
        {
            var key = $"{typeof(T).Name}:{listName}";
            var json = JsonConvert.SerializeObject(id);
            return DataService.DB.ListRightPush(key, json);
        }

        protected IEnumerable<T> GetIndexList(string listName)
        {
            var @namespace = typeof(T).Name;
            var key = $"{@namespace}:{listName}";
            var length = DataService.DB.ListLength(key);

            for (var x = 0; x < length; x++)
            {
                var json = DataService.DB.ListGetByIndex(key, x);
                yield return JsonConvert.DeserializeObject<T>(json);
            }
        }

        protected void RemoveIndexFromList(string listName, long index)
        {
            var key = $"{typeof(T).Name}:{listName}";
            var value = DataService.DB.ListGetByIndex(key, index);
            DataService.DB.ListRemove(key, value);
        }

        protected abstract void OnCacheObjectSet(string @namespace, object id, T entity);
        protected abstract void OnCacheObjectRemoved(string @namespace, object id, T entity);
        protected abstract void OnSubscribeEvents();

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
    }
}
