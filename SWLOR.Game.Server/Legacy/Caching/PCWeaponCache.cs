using System;
using SWLOR.Game.Server.Legacy.Data.Entity;

namespace SWLOR.Game.Server.Legacy.Caching
{
    public class PCWeaponCache: CacheBase<PCWeapon>
    {
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
            if (!ByID.ContainsKey(playerID))
                return null;

            return (PCWeapon)ByID[playerID].Clone();
        }
    }
}
