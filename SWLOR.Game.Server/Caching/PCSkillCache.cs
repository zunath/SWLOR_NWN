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
            return (PCSkill)ByID[id].Clone();
        }

        public IEnumerable<PCSkill> GetAllByPlayerID(Guid playerID)
        {
            if (!ByPlayerID.ContainsKey(playerID))
            {
                return new List<PCSkill>();
            }

            var list = new List<PCSkill>();
            foreach (var record in ByPlayerID[playerID].Values)
            {
                list.Add((PCSkill)record.Clone());
            }

            return list;
        }

        public PCSkill GetByPlayerIDAndSkillID(Guid playerID, int skillID)
        {
            return GetEntityFromDictionary(playerID, skillID, ByPlayerIDAndSkillID);
        }

        public PCSkill GetByPlayerIDAndSkillIDOrDefault(Guid playerID, int skillID)
        {
            return GetEntityFromDictionaryOrDefault(playerID, skillID, ByPlayerIDAndSkillID);
        }

        public IEnumerable<PCSkill> GetAllByPlayerIDAndSkillIDs(Guid playerID, IEnumerable<int> skillIDs)
        {
            var list = new List<PCSkill>();
            foreach(var skillID in skillIDs)
            {
                list.Add(GetEntityFromDictionary(playerID, skillID, ByPlayerIDAndSkillID));
            }

            return list;
        }
    }
}
