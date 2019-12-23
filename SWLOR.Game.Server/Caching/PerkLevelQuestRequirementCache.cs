using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PerkLevelQuestRequirementCache: CacheBase<PerkLevelQuestRequirement>
    {
        public PerkLevelQuestRequirementCache() 
            : base("PerkLevelQuestRequirement")
        {
        }

        private const string ByPerkLevelIDIndex = "ByPerkLevelID";

        protected override void OnCacheObjectSet(PerkLevelQuestRequirement entity)
        {
            SetIntoListIndex(ByPerkLevelIDIndex, entity.PerkLevelID.ToString(), entity);
        }

        protected override void OnCacheObjectRemoved(PerkLevelQuestRequirement entity)
        {
            RemoveFromListIndex(ByPerkLevelIDIndex, entity.PerkLevelID.ToString(), entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PerkLevelQuestRequirement GetByID(int id)
        {
            return ByID(id);
        }

        public IEnumerable<PerkLevelQuestRequirement> GetAllByPerkLevelID(int perkLevelID)
        {
            if (!ExistsByIndex(ByPerkLevelIDIndex, perkLevelID.ToString()))
                return new List<PerkLevelQuestRequirement>();

            return GetFromListIndex(ByPerkLevelIDIndex, perkLevelID.ToString());
        }
    }
}
