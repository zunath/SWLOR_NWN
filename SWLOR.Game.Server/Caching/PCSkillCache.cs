using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCSkillCache: CacheBase<PCSkill>
    {
        protected override void OnCacheObjectSet(PCSkill entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCSkill entity)
        {
        }
    }
}
