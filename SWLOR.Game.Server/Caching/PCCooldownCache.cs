using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCCooldownCache: CacheBase<PCCooldown>
    {
        public PCCooldownCache() 
            : base("PCCooldown")
        {
        }

        private const string ByPlayerIDIndex = "ByPlayerID";

        protected override void OnCacheObjectSet(PCCooldown entity)
        {
            SetIntoListIndex(ByPlayerIDIndex, entity.PlayerID.ToString(), entity);
        }

        protected override void OnCacheObjectRemoved(PCCooldown entity)
        {
            RemoveFromListIndex(ByPlayerIDIndex, entity.PlayerID.ToString(), entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCCooldown GetByID(Guid id)
        {
            return ByID(id);
        }

        public PCCooldown GetByPlayerAndCooldownCategoryIDOrDefault(Guid playerID, int cooldownCategoryID)
        {
            if (!ExistsByIndex(ByPlayerIDIndex, playerID.ToString()))
                return default;

            return GetFromListIndex(ByPlayerIDIndex, playerID.ToString()).SingleOrDefault(x => x.CooldownCategoryID == cooldownCategoryID);
        }

        public PCCooldown GetByPlayerAndCooldownCategoryID(Guid playerID, int cooldownCategoryID)
        {
            return GetFromListIndex(ByPlayerIDIndex, playerID.ToString()).Single(x => x.CooldownCategoryID == cooldownCategoryID);
        }

        public IEnumerable<PCCooldown> GetAllByPlayerID(Guid playerID)
        {
            if (!ExistsByIndex(ByPlayerIDIndex, playerID.ToString()))
            {
                return new List<PCCooldown>();
            }

            return GetFromListIndex(ByPlayerIDIndex, playerID.ToString());
        }
    }
}
