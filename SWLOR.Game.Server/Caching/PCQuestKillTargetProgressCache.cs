using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCQuestKillTargetProgressCache: CacheBase<PCQuestKillTargetProgress>
    {
        private Dictionary<Guid, Dictionary<Guid, PCQuestKillTargetProgress>> ByPlayerID { get; } = new Dictionary<Guid, Dictionary<Guid, PCQuestKillTargetProgress>>();

        protected override void OnCacheObjectSet(PCQuestKillTargetProgress entity)
        {
            SetEntityIntoDictionary(entity.PlayerID, entity.ID, entity, ByPlayerID);
        }

        protected override void OnCacheObjectRemoved(PCQuestKillTargetProgress entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity.ID, ByPlayerID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCQuestKillTargetProgress GetByID(Guid id)
        {
            return (PCQuestKillTargetProgress)ByID[id].Clone();
        }

        public IEnumerable<PCQuestKillTargetProgress> GetAllByPlayerIDAndPCQuestStatusID(Guid playerID, Guid pcQuestStatusID)
        {
            if (!ByPlayerID.ContainsKey(playerID))
                return new List<PCQuestKillTargetProgress>();

            var list = new List<PCQuestKillTargetProgress>();

            foreach (var record in ByPlayerID[playerID].Values.Where(x => x.PCQuestStatusID == pcQuestStatusID))
            {
                list.Add((PCQuestKillTargetProgress)record.Clone());
            }

            return list;
        }

        public IEnumerable<PCQuestKillTargetProgress> GetAllByPlayerIDAndNPCGroupID(Guid playerID, int npcGroupID)
        {
            if(!ByPlayerID.ContainsKey(playerID))
                return new List<PCQuestKillTargetProgress>();

            var list = new List<PCQuestKillTargetProgress>();
            foreach(var record in ByPlayerID[playerID].Values.Where(x => x.NPCGroupID == npcGroupID))
            {
                list.Add((PCQuestKillTargetProgress)record.Clone());
            }

            return list;
        }
    }
}
