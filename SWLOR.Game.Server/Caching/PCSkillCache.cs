using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCSkillCache: CacheBase<PCSkill>
    {
        private Dictionary<Guid, Dictionary<Guid, PCSkill>> ByPlayerID { get; } = new Dictionary<Guid, Dictionary<Guid, PCSkill>>();
        private Dictionary<Guid, Dictionary<int, PCSkill>> ByPlayerIDAndSkillID { get; } = new Dictionary<Guid, Dictionary<int, PCSkill>>();

        protected override void OnCacheObjectSet(PCSkill entity)
        {
            SetEntityIntoDictionary(entity.PlayerID, entity.ID, entity, ByPlayerID);
            SetEntityIntoDictionary(entity.PlayerID, entity.SkillID, entity, ByPlayerIDAndSkillID);
        }

        protected override void OnCacheObjectRemoved(PCSkill entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity.ID, ByPlayerID);
            RemoveEntityFromDictionary(entity.PlayerID, entity.SkillID, ByPlayerIDAndSkillID);
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

        public PCSkill GetByPlayerIDAndSkillID(Guid playerID, int skillID)
        {
            return GetEntityFromDictionary(playerID, skillID, ByPlayerIDAndSkillID);
        }
    }
}
