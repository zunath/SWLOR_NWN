using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCSkillPoolCache: CacheBase<PCSkillPool>
    {
        public PCSkillPoolCache() 
            : base("PCSkillPool")
        {
        }

        private Dictionary<Guid, Dictionary<int, PCSkillPool>> ByPlayerIDAndSkillCategoryID { get; } = new Dictionary<Guid, Dictionary<int, PCSkillPool>>();

        protected override void OnCacheObjectSet(PCSkillPool entity)
        {
            //SetEntityIntoDictionary(entity.PlayerID, entity.SkillCategoryID, entity, ByPlayerIDAndSkillCategoryID);
        }

        protected override void OnCacheObjectRemoved(PCSkillPool entity)
        {
            //RemoveEntityFromDictionary(entity.PlayerID, entity.SkillCategoryID, ByPlayerIDAndSkillCategoryID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCSkillPool GetByID(Guid id)
        {
            return ByID(id);
        }

        public PCSkillPool GetByPlayerIDAndSkillCategoryID(Guid playerID, int skillCategoryID)
        {
            return null;
            //return GetEntityFromDictionary(playerID, skillCategoryID, ByPlayerIDAndSkillCategoryID);
        }

        public PCSkillPool GetByPlayerIDAndSkillCategoryIDOrDefault(Guid playerID, int skillCategoryID)
        {
            return null;
            //return GetEntityFromDictionaryOrDefault(playerID, skillCategoryID, ByPlayerIDAndSkillCategoryID);
        }

        public IEnumerable<PCSkillPool> GetByPlayerIDWithLevelsUndistributed(Guid playerID)
        {
            if(!ByPlayerIDAndSkillCategoryID.ContainsKey(playerID))
                return new List<PCSkillPool>();

            var list = new List<PCSkillPool>();
            foreach (var record in ByPlayerIDAndSkillCategoryID[playerID].Values.Where(x => x.Levels > 0))
            {
                list.Add((PCSkillPool)record.Clone());
            }

            return list;
        }
    }
}
