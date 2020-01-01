using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCObjectVisibilityCache: CacheBase<PCObjectVisibility>
    {
        public PCObjectVisibilityCache() 
            : base("PCObjectVisibility")
        {
        }

        private const string ByPlayerIDIndex = "ByPlayerID";
        
        protected override void OnCacheObjectSet(PCObjectVisibility entity)
        {
            SetIntoListIndex(ByPlayerIDIndex, entity.VisibilityObjectID, entity);
        }

        protected override void OnCacheObjectRemoved(PCObjectVisibility entity)
        {
            RemoveFromListIndex(ByPlayerIDIndex, entity.VisibilityObjectID, entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCObjectVisibility GetByID(Guid id)
        {
            return ByID(id);
        }

        public PCObjectVisibility GetByPlayerIDAndVisibilityObjectIDOrDefault(Guid playerID, string visibilityObjectID)
        {
            if (!ExistsByListIndex(ByPlayerIDIndex, playerID.ToString()))
                return default;

            return GetFromListIndex(ByPlayerIDIndex, playerID.ToString())
                .SingleOrDefault(x => x.VisibilityObjectID == visibilityObjectID);
        }

        public IEnumerable<PCObjectVisibility> GetAllByPlayerID(Guid playerID)
        {
            if(!ExistsByListIndex(ByPlayerIDIndex, playerID.ToString()))
                return new List<PCObjectVisibility>();

            return GetFromListIndex(ByPlayerIDIndex, playerID.ToString());
        }

        public IEnumerable<PCObjectVisibility> GetAllByPlayerIDsAndVisibilityObjectID(IEnumerable<Guid> playerIDs, string visibilityObjectID)
        {
            var list = new List<PCObjectVisibility>();

            foreach (var playerID in playerIDs)
            {
                if (!ExistsByListIndex(ByPlayerIDIndex, playerID.ToString())) continue;

                var results = GetFromListIndex(ByPlayerIDIndex, playerID.ToString())
                    .Where(x => x.VisibilityObjectID == visibilityObjectID)
                    .Select(s => s);

                list.AddRange(results);
            }

            return list;
        }
    }
}
