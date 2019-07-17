using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PerkLevelQuestRequirementCache: CacheBase<PerkLevelQuestRequirement>
    {
        private Dictionary<int, Dictionary<int, PerkLevelQuestRequirement>> ByPerkLevelID { get; } = new Dictionary<int, Dictionary<int, PerkLevelQuestRequirement>>();

        protected override void OnCacheObjectSet(PerkLevelQuestRequirement entity)
        {
            SetEntityIntoDictionary(entity.PerkLevelID, entity.ID, entity, ByPerkLevelID);
        }

        protected override void OnCacheObjectRemoved(PerkLevelQuestRequirement entity)
        {
            RemoveEntityFromDictionary(entity.PerkLevelID, entity.ID, ByPerkLevelID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PerkLevelQuestRequirement GetByID(int id)
        {
            return (PerkLevelQuestRequirement)ByID[id].Clone();
        }

        public IEnumerable<PerkLevelQuestRequirement> GetAllByPerkLevelID(int perkLevelID)
        {
            if (!ByPerkLevelID.ContainsKey(perkLevelID))
                return new List<PerkLevelQuestRequirement>();

            var list = new List<PerkLevelQuestRequirement>();
            foreach (var record in ByPerkLevelID[perkLevelID].Values)
            {
                list.Add((PerkLevelQuestRequirement)record.Clone());
            }

            return list;
        }
    }
}
