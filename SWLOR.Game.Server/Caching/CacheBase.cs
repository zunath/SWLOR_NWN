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
        protected HashSet<T> All { get; } = new HashSet<T>();
        protected Dictionary<object, T> ByID { get; } = new Dictionary<object, T>();

        protected CacheBase()
        {
            MessageHub.Instance.Subscribe<OnCacheObjectSet<T>>(msg => CacheObjectSet(msg.Entity));
            MessageHub.Instance.Subscribe<OnCacheObjectDeleted<T>>(msg => CacheObjectRemoved(msg.Entity));
            OnSubscribeEvents();
        }

        private void CacheObjectSet(T entity)
        {
            if (!All.Contains(entity))
                All.Add(entity);

            var key = GetEntityKey(entity);
            ByID[key] = entity;
            OnCacheObjectSet(entity);
        }

        private void CacheObjectRemoved(T entity)
        {
            All.Remove(entity);

            var key = GetEntityKey(entity);
            ByID.Remove(key);
            OnCacheObjectRemoved(entity);
        }

        protected abstract void OnCacheObjectSet(T entity);
        protected abstract void OnCacheObjectRemoved(T entity);
        protected abstract void OnSubscribeEvents();

        public IEnumerable<T> GetAll()
        {
            return All;
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
        /// <param name="primaryOrganizationID">The first organization key</param>
        /// <param name="secondaryOrganizationID">The second organization key</param>
        /// <param name="entity">The entity to set</param>
        /// <param name="dictionary">The dictionary to modify</param>
        protected void SetEntityIntoDictionary<TPrimary, TSecondary, TEntity>(
            TPrimary primaryOrganizationID, 
            TSecondary secondaryOrganizationID, 
            TEntity entity, 
            Dictionary<TPrimary, Dictionary<TSecondary, TEntity>> dictionary)
            where TEntity: class, IEntity
        {
            if(!dictionary.ContainsKey(primaryOrganizationID))
            {
                dictionary.Add(primaryOrganizationID, new Dictionary<TSecondary, TEntity>());
            }

            var list = dictionary[primaryOrganizationID];
            list[secondaryOrganizationID] = entity;
        }

        /// <summary>
        /// Handles removing an entity from a dictionary based on the organization criteria specified.
        /// Should be called in the OnCacheObjectRemoved event.
        /// </summary>
        /// <typeparam name="TPrimary">The type of the first organization key</typeparam>
        /// <typeparam name="TSecondary">The type of the second organization key</typeparam>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <param name="primaryOrganizationID">The first organization key</param>
        /// <param name="secondaryOrganizationID">The second organization key</param>
        /// <param name="dictionary">The dictionary to modify</param>
        protected void RemoveEntityFromDictionary<TPrimary, TSecondary, TEntity>(
            TPrimary primaryOrganizationID,
            TSecondary secondaryOrganizationID,
            Dictionary<TPrimary, Dictionary<TSecondary, TEntity>> dictionary)
        where TEntity: class, IEntity
        {
            if (!dictionary.ContainsKey(primaryOrganizationID))
            {
                dictionary.Add(primaryOrganizationID, new Dictionary<TSecondary, TEntity>());
            }

            var list = dictionary[primaryOrganizationID];
            list.Remove(secondaryOrganizationID);
        }

        /// <summary>
        /// Handles setting an entity into a dictionary with a new value based on the organization criteria specified.
        /// Should be called in the OnCacheObjectSet event.
        /// </summary>
        /// <typeparam name="TPrimary">The type of the first organization key</typeparam>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <param name="primaryOrganizationID">The first organization key</param>
        /// <param name="entity">The entity to set</param>
        /// <param name="dictionary">The dictionary to modify</param>
        protected void SetEntityIntoDictionary<TPrimary, TEntity>(
            TPrimary primaryOrganizationID,
            TEntity entity,
            Dictionary<TPrimary, List<TEntity>> dictionary)
        {
            if (!dictionary.ContainsKey(primaryOrganizationID))
            {
                dictionary.Add(primaryOrganizationID, new List<TEntity>());
            }

            var list = dictionary[primaryOrganizationID];

            // We only add the entity if it doesn't already exist.
            // Because objects are pass by reference, any changes made to them
            // will apply automatically.
            if (!list.Contains(entity))
                list.Add(entity);
        }

        /// <summary>
        /// Handles removing an entity from a dictionary based on the organization criteria specified.
        /// Should be called in the OnCacheObjectRemoved event.
        /// </summary>
        /// <typeparam name="TPrimary">The type of the first organization key</typeparam>
        /// <typeparam name="TEntity">The type of the entity</typeparam>
        /// <param name="primaryOrganizationID">The first organization key</param>
        /// <param name="entity">The entity to remove</param>
        /// <param name="dictionary">The dictionary to modify</param>
        protected void RemoveEntityFromDictionary<TPrimary, TEntity>(
            TPrimary primaryOrganizationID,
            TEntity entity,
            Dictionary<TPrimary, List<TEntity>> dictionary)
        {
            if (!dictionary.ContainsKey(primaryOrganizationID))
            {
                dictionary.Add(primaryOrganizationID, new List<TEntity>());
            }

            var list = dictionary[primaryOrganizationID];
            list.Remove(entity);
        }
    }
}
