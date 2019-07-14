using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PerkLevelQuestRequirementCache: CacheBase<PerkLevelQuestRequirement>
    {
        protected override void OnCacheObjectSet(PerkLevelQuestRequirement entity)
        {
        }

        protected override void OnCacheObjectRemoved(PerkLevelQuestRequirement entity)
        {
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PerkLevelQuestRequirement GetByID(int id)
        {
            return ByID[id];
        }
    }
}
