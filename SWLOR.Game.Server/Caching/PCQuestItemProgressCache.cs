using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCQuestItemProgressCache: CacheBase<PCQuestItemProgress>
    {
        public PCQuestItemProgressCache() 
            : base("PCQuestItemProgress")
        {
        }

        private const string ByQuestStatusIDIndex = "ByQuestStatusID";
        
        protected override void OnCacheObjectSet(PCQuestItemProgress entity)
        {
            SetIntoListIndex(ByQuestStatusIDIndex, entity.ID.ToString(), entity);
        }

        protected override void OnCacheObjectRemoved(PCQuestItemProgress entity)
        {
            RemoveFromListIndex(ByQuestStatusIDIndex, entity.ID.ToString(), entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCQuestItemProgress GetByID(Guid id)
        {
            return ByID(id);
        }

        public int GetCountByPCQuestStatusID(Guid pcQuestStatusID)
        {
            return GetFromListIndex(ByQuestStatusIDIndex, pcQuestStatusID.ToString()).Count();
        }

        public PCQuestItemProgress GetByPCQuestStatusIDAndResrefOrDefault(Guid pcQuestStatusID, string resref)
        {
            if (!ExistsByListIndex(ByQuestStatusIDIndex, pcQuestStatusID.ToString()))
                return default;

            return GetFromListIndex(ByQuestStatusIDIndex, pcQuestStatusID.ToString())
                .SingleOrDefault(x => x.Resref == resref);
        }

        public IEnumerable<PCQuestItemProgress> GetAllByPCQuestStatusID(Guid pcQuestStatusID)
        {
            if (!ExistsByListIndex( ByQuestStatusIDIndex, pcQuestStatusID.ToString()))
                return new List<PCQuestItemProgress>();

            return GetFromListIndex(ByQuestStatusIDIndex, pcQuestStatusID.ToString());
        }
    }
}
