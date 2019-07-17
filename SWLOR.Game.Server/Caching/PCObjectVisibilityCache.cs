using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCObjectVisibilityCache: CacheBase<PCObjectVisibility>
    {
        private Dictionary<Guid, Dictionary<string, PCObjectVisibility>> ByPlayer { get; } = new Dictionary<Guid, Dictionary<string, PCObjectVisibility>>();

        protected override void OnCacheObjectSet(PCObjectVisibility entity)
        {
            SetEntityIntoDictionary(entity.PlayerID, entity.VisibilityObjectID, entity, ByPlayer);
        }

        protected override void OnCacheObjectRemoved(PCObjectVisibility entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity.VisibilityObjectID, ByPlayer);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCObjectVisibility GetByID(Guid id)
        {
            return (PCObjectVisibility)ByID[id].Clone();
        }

        public PCObjectVisibility GetByPlayerIDAndVisibilityObjectIDOrDefault(Guid playerID, string visibilityObjectID)
        {
            return GetEntityFromDictionaryOrDefault(playerID, visibilityObjectID, ByPlayer);
        }

        public IEnumerable<PCObjectVisibility> GetAllByPlayerID(Guid playerID)
        {
            if(!ByPlayer.ContainsKey(playerID))
                return new List<PCObjectVisibility>();

            var list = new List<PCObjectVisibility>();
            foreach (var record in ByPlayer[playerID].Values)
            {
                list.Add((PCObjectVisibility)record.Clone());
            }

            return list;
        }

        public IEnumerable<PCObjectVisibility> GetAllByPlayerIDsAndVisibilityObjectID(IEnumerable<Guid> playerIDs, string visibilityObjectID)
        {
            var list = new List<PCObjectVisibility>();

            foreach (var playerID in playerIDs)
            {
                if (!ByPlayer.ContainsKey(playerID)) continue;

                var results = ByPlayer[playerID].Where(x => x.Key == visibilityObjectID).Select(s => (PCObjectVisibility)s.Value.Clone());
                list.AddRange(results);
            }

            return list;
        }
    }
}
