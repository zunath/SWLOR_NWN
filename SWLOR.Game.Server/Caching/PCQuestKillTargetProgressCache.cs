using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCQuestKillTargetProgressCache: CacheBase<PCQuestKillTargetProgress>
    {
        public PCQuestKillTargetProgressCache() 
            : base("PCQuestKillTargetProgress")
        {
        }

        private const string ByPlayerIDIndex = "ByPlayerID";
        
        protected override void OnCacheObjectSet(PCQuestKillTargetProgress entity)
        {
            SetIntoListIndex(ByPlayerIDIndex, entity.PlayerID.ToString(), entity);
        }

        protected override void OnCacheObjectRemoved(PCQuestKillTargetProgress entity)
        {
            RemoveFromListIndex(ByPlayerIDIndex, entity.PlayerID.ToString(), entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCQuestKillTargetProgress GetByID(Guid id)
        {
            return ByID(id);
        }

        public IEnumerable<PCQuestKillTargetProgress> GetAllByPlayerIDAndPCQuestStatusID(Guid playerID, Guid pcQuestStatusID)
        {
            if (!ExistsByListIndex(ByPlayerIDIndex, playerID.ToString()))
                return new List<PCQuestKillTargetProgress>();

            return GetFromListIndex(ByPlayerIDIndex, playerID.ToString())
                .Where(x => x.PCQuestStatusID == pcQuestStatusID);
        }

        public IEnumerable<PCQuestKillTargetProgress> GetAllByPlayerIDAndNPCGroupID(Guid playerID, int npcGroupID)
        {
            if(!ExistsByListIndex(ByPlayerIDIndex, playerID.ToString()))
                return new List<PCQuestKillTargetProgress>();

            return GetFromListIndex(ByPlayerIDIndex, playerID.ToString())
                .Where(x => x.NPCGroupID == npcGroupID);
        }
    }
}
