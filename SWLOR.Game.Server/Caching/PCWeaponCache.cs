using System;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCWeaponCache: CacheBase<PCWeapon>
    {
        public PCWeaponCache() 
            : base("PCWeapon")
        {
        }

        protected override void OnCacheObjectSet(PCWeapon entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCWeapon entity)
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
