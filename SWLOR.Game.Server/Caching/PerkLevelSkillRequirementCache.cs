using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PerkLevelSkillRequirementCache: CacheBase<PerkLevelSkillRequirement>
    {
        private Dictionary<int, Dictionary<int, PerkLevelSkillRequirement>> ByPerkLevelID { get; } = new Dictionary<int, Dictionary<int, PerkLevelSkillRequirement>>();

        protected override void OnCacheObjectSet(PerkLevelSkillRequirement entity)
        {
            SetEntityIntoDictionary(entity.PerkLevelID, entity.ID, entity, ByPerkLevelID);
        }

        protected override void OnCacheObjectRemoved(PerkLevelSkillRequirement entity)
        {
            RemoveEntityFromDictionary(entity.PerkLevelID, entity.ID, ByPerkLevelID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PerkLevelSkillRequirement GetByID(int id)
        {
            return ByID[id];
        }

        public IEnumerable<PerkLevelSkillRequirement> GetAllByPerkLevelID(int perkLevelID)
        {
            if(!ByPerkLevelID.ContainsKey(perkLevelID))
                return new List<PerkLevelSkillRequirement>();

            return ByPerkLevelID[perkLevelID].Values;
        }
    }
}
