using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Caching
{
    public abstract class CacheBase<T>
        where T: class, IEntity
    {
        protected Dictionary<object, T> ByID { get; } = new Dictionary<object, T>();

        protected CacheBase()
        {
            MessageHub.Instance.Subscribe<OnCacheObjectSet<T>>(msg => CacheObjectSet(msg.Entity));
            MessageHub.Instance.Subscribe<OnCacheObjectDeleted<T>>(msg => CacheObjectRemoved(msg.Entity));
        }

        private void CacheObjectSet(T entity)
        {
            var key = GetEntityKey(entity);
            ByID[key] = entity;
            OnCacheObjectSet(entity);
        }

        private void CacheObjectRemoved(T entity)
        {
            var key = GetEntityKey(entity);
            ByID.Remove(key);
            OnCacheObjectRemoved(entity);
        }

        protected abstract void OnCacheObjectSet(T entity);
        protected abstract void OnCacheObjectRemoved(T entity);


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

    }
}
