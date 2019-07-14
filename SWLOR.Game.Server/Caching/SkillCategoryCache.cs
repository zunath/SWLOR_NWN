using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class SkillCategoryCache: CacheBase<SkillCategory>
    {
        protected override void OnCacheObjectSet(SkillCategory entity)
        {
        }

        protected override void OnCacheObjectRemoved(SkillCategory entity)
        {
        }

        public SkillCategory GetByID(int id)
        {
            return ByID[id];
        }

    }
}
