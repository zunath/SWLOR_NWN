using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PerkLevelSkillRequirementCache: CacheBase<PerkLevelSkillRequirement>
    {
        public PerkLevelSkillRequirementCache() 
            : base("PerkLevelSkillRequirement")
        {
        }

        private const string ByPerkLevelIDIndex = "ByPerkLevelID";

        protected override void OnCacheObjectSet(PerkLevelSkillRequirement entity)
        {
            SetIntoListIndex(ByPerkLevelIDIndex, entity.PerkLevelID.ToString(), entity);
        }

        protected override void OnCacheObjectRemoved(PerkLevelSkillRequirement entity)
        {
            RemoveFromListIndex(ByPerkLevelIDIndex, entity.PerkLevelID.ToString(), entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PerkLevelSkillRequirement GetByID(int id)
        {
            return ByID(id);
        }

        public IEnumerable<PerkLevelSkillRequirement> GetAllByPerkLevelID(int perkLevelID)
        {
            if(!ExistsByIndex(ByPerkLevelIDIndex, perkLevelID.ToString()))
                return new List<PerkLevelSkillRequirement>();

            return GetFromListIndex(ByPerkLevelIDIndex, perkLevelID.ToString());
        }
    }
}
