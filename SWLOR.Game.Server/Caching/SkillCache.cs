using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class SkillCache: CacheBase<Skill>
    {
        private Dictionary<int, Dictionary<int, Skill>> ByCategoryID { get; } = new Dictionary<int, Dictionary<int, Skill>>();

        protected override void OnCacheObjectSet(Skill entity)
        {
            SetEntityIntoDictionary(entity.SkillCategoryID, entity.ID, entity, ByCategoryID);
        }

        protected override void OnCacheObjectRemoved(Skill entity)
        {
            RemoveEntityFromDictionary(entity.SkillCategoryID, entity.ID, ByCategoryID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public Skill GetByID(int id)
        {
            return ByID[id];
        }

        public IEnumerable<Skill> GetByCategoryIDAndContributesToSkillCap(int skillCategoryID)
        {
            return ByCategoryID[skillCategoryID].Values.Where(x => x.ContributesToSkillCap);
        }
    }
}
