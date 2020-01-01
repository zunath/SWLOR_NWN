using System;
using System.Collections.Generic;
using System.Linq;
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
            SetIntoListIndex(ByPerkLevelIDIndex, "Active", entity);
        }

        protected override void OnCacheObjectRemoved(PerkLevelSkillRequirement entity)
        {
            RemoveFromListIndex(ByPerkLevelIDIndex, "Active", entity);
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
            if(!ExistsByIndex(ByPerkLevelIDIndex, "Active"))
                return new List<PerkLevelSkillRequirement>();

            return GetFromListIndex(ByPerkLevelIDIndex, "Active");
        }

        public IEnumerable<PerkLevelSkillRequirement> GetAllByPerkLevelID(IEnumerable<int> perkLevelIDs)
        {
            var result = new List<PerkLevelSkillRequirement>();

            foreach (var perkLevelID in perkLevelIDs)
            {
                var records = GetAllByPerkLevelID(perkLevelID);

                Console.WriteLine("count by perkLevelID = " + records.Count());

                result.AddRange(records);
            }

            return result;
        }
    }
}
