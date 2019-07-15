using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCSkillPoolCache: CacheBase<PCSkillPool>
    {
        private Dictionary<Guid, Dictionary<int, PCSkillPool>> ByPlayerIDAndSkillCategoryID { get; } = new Dictionary<Guid, Dictionary<int, PCSkillPool>>();

        protected override void OnCacheObjectSet(PCSkillPool entity)
        {
            SetEntityIntoDictionary(entity.PlayerID, entity.SkillCategoryID, entity, ByPlayerIDAndSkillCategoryID);
        }

        protected override void OnCacheObjectRemoved(PCSkillPool entity)
        {
            RemoveEntityFromDictionary(entity.PlayerID, entity.SkillCategoryID, ByPlayerIDAndSkillCategoryID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCSkillPool GetByID(Guid id)
        {
            return ByID[id];
        }

        public PCSkillPool GetByPlayerIDAndSkillCategoryID(Guid playerID, int skillCategoryID)
        {
            return GetEntityFromDictionary(playerID, skillCategoryID, ByPlayerIDAndSkillCategoryID);
        }


    }
}
