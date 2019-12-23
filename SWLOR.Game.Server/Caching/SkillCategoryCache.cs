using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class SkillCategoryCache: CacheBase<SkillCategory>
    {
        public SkillCategoryCache() 
            : base("SkillCategory")
        {
        }

        private const string ByActiveIndex = "ByActive";
        
        protected override void OnCacheObjectSet(SkillCategory entity)
        {
            SetByActive(entity);
        }

        protected override void OnCacheObjectRemoved(SkillCategory entity)
        {
            RemoveFromListIndex(ByActiveIndex, "Active", entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        private void SetByActive(SkillCategory entity)
        {
            // Exclude inactive / remove if swapped to inactive.
            if (!entity.IsActive && ExistsInListIndex(ByActiveIndex, "Active", entity))
            {
                RemoveFromListIndex(ByActiveIndex, "Active", entity);
                return;
            }

            // Exclude zero
            if (entity.ID <= 0) return;

            SetIntoListIndex(ByActiveIndex, "Active", entity);
        }

        public SkillCategory GetByID(int id)
        {
            return ByID(id);
        }

        public IEnumerable<SkillCategory> GetAllActive()
        {
            return GetFromListIndex(ByActiveIndex, "Active");
        }

    }
}
