using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class SkillCache: CacheBase<Skill>
    {
        public SkillCache() 
            : base("Skill")
        {
        }

        private const string ByCategoryIDIndex = "ByCategoryID";
        private const string ByContributesToSkillCapIndex = "ByContributesToSkillCap";

        protected override void OnCacheObjectSet(Skill entity)
        {
            SetIntoListIndex(ByCategoryIDIndex, entity.SkillCategoryID.ToString(), entity);
            SetByContributesToSkillCap(entity);
        }

        protected override void OnCacheObjectRemoved(Skill entity)
        {
            RemoveFromListIndex(ByCategoryIDIndex, entity.SkillCategoryID.ToString(), entity);
            RemoveFromListIndex(ByContributesToSkillCapIndex, "Active", entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        private void SetByContributesToSkillCap(Skill entity)
        {
            // No longer contributing to skill cap. Remove it.
            if (!entity.ContributesToSkillCap && ExistsInListIndex(ByContributesToSkillCapIndex, "Active", entity))
            {
                RemoveFromListIndex(ByContributesToSkillCapIndex, "Active", entity);
            }
            // Contributes to skill cap and doesn't exist in the dictionary yet.
            else if (entity.ContributesToSkillCap)
            {
                SetIntoListIndex(ByContributesToSkillCapIndex, "Active", entity);
            }
        }

        public Skill GetByID(int id)
        {
            return ByID(id);
        }

        public IEnumerable<Skill> GetByCategoryIDAndContributesToSkillCap(int skillCategoryID)
        {
            return GetFromListIndex(ByCategoryIDIndex, skillCategoryID.ToString())
                .Where(x => x.ContributesToSkillCap);
        }

        public IEnumerable<Skill> GetAllWhereContributesToSkillCap()
        {
            return GetFromListIndex(ByContributesToSkillCapIndex, "Active");
        }

        public IEnumerable<Skill> GetAllBySkillCategoryIDAndActive(int skillCategoryID)
        {
            return GetFromListIndex(ByCategoryIDIndex, skillCategoryID.ToString())
                .Where(x => x.IsActive);
        }
    }
}
