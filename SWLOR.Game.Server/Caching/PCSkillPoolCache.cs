using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCSkillPoolCache: CacheBase<PCSkillPool>
    {
        protected override void OnCacheObjectSet(PCSkillPool entity)
        {
        }

        protected override void OnCacheObjectRemoved(PCSkillPool entity)
        {
        }
    }
}
