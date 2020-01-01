using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCPerkCache: CacheBase<PCPerk>
    {
        public PCPerkCache() 
            : base("PCPerk")
        {
        }

        private const string ByPlayerIDIndex = "ByPlayerID";

        protected override void OnCacheObjectSet(PCPerk entity)
        {
            SetIntoListIndex(ByPlayerIDIndex, entity.PlayerID.ToString(), entity);
        }

        protected override void OnCacheObjectRemoved(PCPerk entity)
        {
            RemoveFromListIndex(ByPlayerIDIndex, entity.PlayerID.ToString(), entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCPerk GetByID(Guid id)
        {
            return ByID(id);
        }

        public PCPerk GetByPlayerAndPerkID(Guid playerID, int perkID)
        {
            return GetFromListIndex(ByPlayerIDIndex, playerID.ToString())
                .Single(x => x.PerkID == perkID);
        }

        public PCPerk GetByPlayerAndPerkIDOrDefault(Guid playerID, int perkID)
        {
            if (!ExistsByListIndex(ByPlayerIDIndex, playerID.ToString()))
                return default;

            return GetFromListIndex(ByPlayerIDIndex, playerID.ToString())
                .SingleOrDefault(x => x.PerkID == perkID);
        }

        public IEnumerable<PCPerk> GetAllByPlayerID(Guid playerID)
        {
            if (!ExistsByListIndex(ByPlayerIDIndex, playerID.ToString()))
            {
                return new List<PCPerk>();
            }

            return GetFromListIndex(ByPlayerIDIndex, playerID.ToString());
        }
    }
}
