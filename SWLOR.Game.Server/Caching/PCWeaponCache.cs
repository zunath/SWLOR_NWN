using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCWeaponCache: CacheBase<PCWeapon>
    {
        protected override void OnCacheObjectSet(string @namespace, object id, PCWeapon entity)
        {
        }

        protected override void OnCacheObjectRemoved(string @namespace, object id, PCWeapon entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCWeapon GetByIDOrDefault(Guid playerID)
        {
            if (!Exists(playerID))
                return null;

            return ByID(playerID);
        }
    }
}
