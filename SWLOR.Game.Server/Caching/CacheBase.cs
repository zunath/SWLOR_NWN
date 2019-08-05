using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SWLOR.Game.Server.Caching.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Caching
{
    public abstract class CacheBase<T>: ICache<T>
        where T: class, IEntity
    {
        protected IEnumerable<T> All => ByID.Values;

        protected Dictionary<object, T> ByID { get; } = new Dictionary<object, T>();

        protected CacheBase()
        {
            MessageHub.Instance.Subscribe<OnCacheObjectSet<T>>(msg => CacheObjectSet(msg.Entity));
            MessageHub.Instance.Subscribe<OnCacheObjectDeleted<T>>(msg => CacheObjectRemoved(msg.Entity));
            OnSubscribeEvents();
        }

        private void CacheObjectSet(T entity)
        {
            var clone = (T)entity.Clone();
            var key = GetEntityKey(clone);
            ByID[key] = clone;
            OnCacheObjectSet(clone);
        }

        private void CacheObjectRemoved(T entity)
        {
            var key = GetEntityKey(entity);
            ByID.Remove(key);
            OnCacheObjectRemoved(entity);
        }

        protected abstract void OnCacheObjectSet(T entity);
        protected abstract void OnCacheObjectRemoved(T entity);
        protected abstract void OnSubscribeEvents();

        public IEnumerable<T> GetAll()
        {
            var list = new List<T>();
            foreach (T obj in All)
            {
                list.Add( (T)obj.Clone());
            }

            return list;
        }

        private static object GetEntityKey(IEntity entity)
        {
            // Locate a Key or ExplicitKey attribute on this type. These are Dapper attributes which determine if the key
            // is auto-generated (Key) or manually set (ExplicitKey) on the entity.
            // We will reuse these attributes for identifying cache items.
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

                var explicitKey = prop.GetCustomAttributes(typeof(ExplicitKeyAttribute), false).FirstOrDefault();
                if (explicitKey != null)
                {
                    propertyWithKey = prop;
                    break;
                }
            }

            if (propertyWithKey == null)
            {
                throw new NullReferenceException("Unable to find a Key or ExplicitKey attribute on the entity provided (" + entity.GetType() + "). Make sure you add the Key attribute for a database-generated IDs or an ExplicitKey attribute for manually-set IDs.");
            }

            return propertyWithKey.GetValue(entity);
        }

        /// <summary>
        /// Handles setting an entity into a dictionary with a new value based on the organization criteria specified.
        /// Should be called in the OnCacheObjectSet event.
        /// </summary>
        /// <typeparam name="TPrimary">The type of the first organization key</typeparam>
        /// <typeparam name="TSecondary">The type of the second organization key</typeparam>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <param name="primaryIndexID">The first organization key</param>
        /// <param name="secondaryIndexID">The second organization key</param>
        /// <param name="entity">The entity to set</param>
        /// <param name="dictionary">The dictionary to modify</param>
        protected void SetEntityIntoDictionary<TPrimary, TSecondary, TEntity>(
            TPrimary primaryIndexID, 
            TSecondary secondaryIndexID, 
            TEntity entity, 
            Dictionary<TPrimary, Dictionary<TSecondary, TEntity>> dictionary)
            where TEntity: class, IEntity
        {
            if(!dictionary.ContainsKey(primaryIndexID))
            {
                dictionary[primaryIndexID] = new Dictionary<TSecondary, TEntity>();
            }

            var clone = (TEntity)entity.Clone();
            dictionary[primaryIndexID][secondaryIndexID] = clone;
        }

        /// <summary>
        /// Handles removing an entity from a dictionary based on the organization criteria specified.
        /// Should be called in the OnCacheObjectRemoved event.
        /// </summary>
        /// <typeparam name="TPrimary">The type of the first organization key</typeparam>
        /// <typeparam name="TSecondary">The type of the second organization key</typeparam>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <param name="primaryIndexID">The first organization key</param>
        /// <param name="secondaryIndexID">The second organization key</param>
        /// <param name="dictionary">The dictionary to modify</param>
        protected void RemoveEntityFromDictionary<TPrimary, TSecondary, TEntity>(
            TPrimary primaryIndexID,
            TSecondary secondaryIndexID,
            Dictionary<TPrimary, Dictionary<TSecondary, TEntity>> dictionary)
        where TEntity: class, IEntity
        {
            if (!dictionary.ContainsKey(primaryIndexID))
            {
                dictionary.Add(primaryIndexID, new Dictionary<TSecondary, TEntity>());
            }

            var list = dictionary[primaryIndexID];
            list.Remove(secondaryIndexID);
        }

        /// <summary>
        /// Retrieves an entity from a dictionary of dictionaries.
        /// If entity does not exist, a KeyNotFound exception will be thrown.
        /// </summary>
        /// <typeparam name="TPrimary">The type of the primary index</typeparam>
        /// <typeparam name="TSecondary">The type of the secondary index</typeparam>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <param name="primaryIndexID">The primary index</param>
        /// <param name="secondaryIndexID">The secondary index</param>
        /// <param name="dictionary">The dictionary to read from</param>
        /// <returns></returns>
        protected TEntity GetEntityFromDictionary<TPrimary, TSecondary, TEntity>(
            TPrimary primaryIndexID, 
            TSecondary secondaryIndexID,
            Dictionary<TPrimary, Dictionary<TSecondary, TEntity>> dictionary)
            where TEntity: class, IEntity
        {
            if (!dictionary.ContainsKey(primaryIndexID))
            {
                dictionary.Add(primaryIndexID, new Dictionary<TSecondary, TEntity>());
            }

            return (TEntity)dictionary[primaryIndexID][secondaryIndexID].Clone();
        }

        /// <summary>
        /// Retrieves an entity from a dictionary of dictionaries.
        /// If entity does not exist, the default value for that type will be returned.
        /// </summary>
        /// <typeparam name="TPrimary">The type of the primary index</typeparam>
        /// <typeparam name="TSecondary">The type of the secondary index</typeparam>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <param name="primaryIndexID">The primary index</param>
        /// <param name="secondaryIndexID">The secondary index</param>
        /// <param name="dictionary">The dictionary to read from</param>
        /// <returns></returns>
        protected TEntity GetEntityFromDictionaryOrDefault<TPrimary, TSecondary, TEntity>(
            TPrimary primaryIndexID,
            TSecondary secondaryIndexID,
            Dictionary<TPrimary, Dictionary<TSecondary, TEntity>> dictionary)
            where TEntity: class, IEntity
        {
            if (!dictionary.ContainsKey(primaryIndexID))
            {
                dictionary.Add(primaryIndexID, new Dictionary<TSecondary, TEntity>());
            }

            if (dictionary[primaryIndexID].ContainsKey(secondaryIndexID))
            {
                return (TEntity)dictionary[primaryIndexID][secondaryIndexID].Clone();
            }
            else
            {
                return default;
            }
        }
    }
}
