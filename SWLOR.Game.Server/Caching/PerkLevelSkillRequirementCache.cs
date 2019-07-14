using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PerkLevelSkillRequirementCache: CacheBase<PerkLevelSkillRequirement>
    {
        protected override void OnCacheObjectSet(PerkLevelSkillRequirement entity)
        {
        }

        protected override void OnCacheObjectRemoved(PerkLevelSkillRequirement entity)
        {
        }
    }
}
