using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class SkillCache: CacheBase<Skill>
    {
        private Dictionary<int, Dictionary<int, Skill>> ByCategoryID { get; } = new Dictionary<int, Dictionary<int, Skill>>();
        private Dictionary<int, Skill> ByContributesToSkillCap { get; } = new Dictionary<int, Skill>();

        protected override void OnCacheObjectSet(Skill entity)
        {
            SetEntityIntoDictionary(entity.SkillCategoryID, entity.ID, entity, ByCategoryID);
            SetByContributesToSkillCap(entity);
        }

        protected override void OnCacheObjectRemoved(Skill entity)
        {
            RemoveEntityFromDictionary(entity.SkillCategoryID, entity.ID, ByCategoryID);
            ByContributesToSkillCap.Remove(entity.ID);
        }

        protected override void OnSubscribeEvents()
        {
        }

        private void SetByContributesToSkillCap(Skill entity)
        {
            // No longer contributing to skill cap. Remove it.
            if (!entity.ContributesToSkillCap && ByContributesToSkillCap.ContainsKey(entity.ID))
            {
                ByContributesToSkillCap.Remove(entity.ID);
                return;
            }

            ByContributesToSkillCap[entity.ID] = entity;
        }

        public Skill GetByID(int id)
        {
            return ByID[id];
        }

        public IEnumerable<Skill> GetByCategoryIDAndContributesToSkillCap(int skillCategoryID)
        {
            return ByCategoryID[skillCategoryID].Values.Where(x => x.ContributesToSkillCap);
        }

        public IEnumerable<Skill> GetAllWhereContributesToSkillCap()
        {
            return ByContributesToSkillCap.Values;
        }
    }
}
