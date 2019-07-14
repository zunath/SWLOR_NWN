using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class SkillCache: CacheBase<Skill>
    {
        protected override void OnCacheObjectSet(Skill entity)
        {
        }

        protected override void OnCacheObjectRemoved(Skill entity)
        {
        }
    }
}
