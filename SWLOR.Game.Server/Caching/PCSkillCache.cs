using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCSkillCache: CacheBase<PCSkill>
    {
        private Dictionary<Guid, Dictionary<Guid, PCSkill>> ByPlayerID { get; } = new Dictionary<Guid, Dictionary<Guid, PCSkill>>();

        protected override void OnCacheObjectSet(PCSkill entity)
        {
            SetEntityIntoDictionary(entity.PlayerID, entity.ID, entity, ByPlayerID);
        }

        protected override void OnCacheObjectRemoved(PCSkill entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity.ID, ByPlayerID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCSkill GetByID(Guid id)
        {
            return ByID[id];
        }

        public IEnumerable<PCSkill> GetAllByPlayerID(Guid playerID)
        {
            if (!ByPlayerID.ContainsKey(playerID))
            {
                ByPlayerID[playerID] = new Dictionary<Guid, PCSkill>();
            }

            var list = ByPlayerID[playerID].Values;
            return list;
        }
    }
}
