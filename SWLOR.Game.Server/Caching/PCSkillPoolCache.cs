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

        private const string ByPlayerIDAndSkillCategoryIDIndex = "ByPlayerIDAndSkillCategory";
        
        protected override void OnCacheObjectSet(PCSkillPool entity)
        {
            SetIntoListIndex(ByPlayerIDAndSkillCategoryIDIndex, entity.PlayerID.ToString(), entity);
        }

        protected override void OnCacheObjectRemoved(PCSkillPool entity)
        {
            RemoveFromListIndex(ByPlayerIDAndSkillCategoryIDIndex, entity.PlayerID.ToString(), entity);
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
            return GetFromListIndex(ByPlayerIDAndSkillCategoryIDIndex, playerID.ToString())
                .Single(x => x.SkillCategoryID == skillCategoryID);
        }

        public PCSkillPool GetByPlayerIDAndSkillCategoryIDOrDefault(Guid playerID, int skillCategoryID)
        {
            if (!ExistsByListIndex(ByPlayerIDAndSkillCategoryIDIndex, playerID.ToString()))
                return default;

            return GetFromListIndex(ByPlayerIDAndSkillCategoryIDIndex, playerID.ToString())
                .Single(x => x.SkillCategoryID == skillCategoryID);
        }

        public IEnumerable<PCSkillPool> GetByPlayerIDWithLevelsUndistributed(Guid playerID)
        {
            if(!ExistsByListIndex(ByPlayerIDAndSkillCategoryIDIndex, playerID.ToString()))
                return new List<PCSkillPool>();

            return GetFromListIndex(ByPlayerIDAndSkillCategoryIDIndex, playerID.ToString())
                .Where(x => x.Levels > 0);
        }
    }
}
