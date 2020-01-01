using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Caching
{
    public class PCBaseStructureItemCache: CacheBase<PCBaseStructureItem>
    {
        public PCBaseStructureItemCache() 
            : base("PCBaseStructureItem")
        {
        }

        private const string ByItemGlobalIDIndex = "ByItemGlobalID";
        private const string ByPCBaseStructureIDIndex = "ByPCBaseStructureID";
        private const string CountsByPCBaseStructureIDIndex = "CountsByPCBaseStructureID";

        protected override void OnCacheObjectSet(PCBaseStructureItem entity)
        {
            SetIntoIndex(ByItemGlobalIDIndex, entity.ItemGlobalID, entity);
            SetIntoListIndex(ByPCBaseStructureIDIndex, entity.PCBaseStructureID.ToString(), entity);
            SetCountsByPCBaseStructureID(entity);
        }

        protected override void OnCacheObjectRemoved(PCBaseStructureItem entity)
        {
            RemoveFromIndex(ByItemGlobalIDIndex, entity.ItemGlobalID);
            RemoveFromListIndex(ByPCBaseStructureIDIndex, entity.PCBaseStructureID.ToString(), entity);
            RemoveCountsByPCBaseStructureID(entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        private void SetCountsByPCBaseStructureID(PCBaseStructureItem entity)
        {
            var key = CountsByPCBaseStructureIDIndex + ":" + entity.PCBaseStructureID;
            if (!NWNXRedis.Exists(key))
                
                NWNXRedis.Set(key, JsonConvert.SerializeObject(0));

            var count = JsonConvert.DeserializeObject<int>(NWNXRedis.Get(key)) + 1;
            NWNXRedis.Set( key, JsonConvert.SerializeObject(count));
        }

        private void RemoveCountsByPCBaseStructureID(PCBaseStructureItem entity)
        {
            var key = CountsByPCBaseStructureIDIndex + ":" + entity.PCBaseStructureID;

            if (!NWNXRedis.Exists(key))
                NWNXRedis.Set(key, JsonConvert.SerializeObject(0));

            var count = JsonConvert.DeserializeObject<int>(NWNXRedis.Get(key)) - 1;
            NWNXRedis.Set(key, JsonConvert.SerializeObject(count));
        }

        public PCBaseStructureItem GetByID(Guid id)
        {
            return ByID(id);
        }

        public PCBaseStructureItem GetByItemGlobalID(string itemGlobalID)
        {
            return GetFromIndex(ByItemGlobalIDIndex, itemGlobalID);
        }

        public PCBaseStructureItem GetByPCBaseStructureIDAndItemGlobalIDOrDefault(Guid pcBaseStructureID, string itemGlobalID)
        {
            if (!ExistsByListIndex(ByPCBaseStructureIDIndex, pcBaseStructureID.ToString()))
                return default;

            return GetFromListIndex(ByPCBaseStructureIDIndex, pcBaseStructureID.ToString())
                .SingleOrDefault(x => x.ItemGlobalID == itemGlobalID);
        }

        public int GetNumberOfItemsContainedBy(Guid pcBaseStructureID)
        {
            var key = CountsByPCBaseStructureIDIndex + ":" + pcBaseStructureID;

            if (!NWNXRedis.Exists(key))
                return 0;

            return JsonConvert.DeserializeObject<int>(key);
        }

        public IEnumerable<PCBaseStructureItem> GetAllByPCBaseStructureID(Guid pcBaseStructureID)
        {
            if(!ExistsByListIndex(ByPCBaseStructureIDIndex, pcBaseStructureID.ToString()))
                return new List<PCBaseStructureItem>();

            return GetFromListIndex(ByPCBaseStructureIDIndex, pcBaseStructureID.ToString());
        }
    }
}
