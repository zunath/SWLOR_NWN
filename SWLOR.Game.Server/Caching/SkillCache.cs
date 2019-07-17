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
            }
            // Contributes to skill cap and doesn't exist in the dictionary yet.
            else if (entity.ContributesToSkillCap)
            {
                ByContributesToSkillCap[entity.ID] = (Skill)entity.Clone();
            }
        }

        public Skill GetByID(int id)
        {
            return (Skill)ByID[id].Clone();
        }

        public IEnumerable<Skill> GetByCategoryIDAndContributesToSkillCap(int skillCategoryID)
        {
            var list = new List<Skill>();
            if (!ByCategoryID.ContainsKey(skillCategoryID))
                return list;

            foreach(var record in ByCategoryID[skillCategoryID].Values.Where(x => x.ContributesToSkillCap))
            {
                list.Add((Skill)record.Clone());
            }

            return list;
        }

        public IEnumerable<Skill> GetAllWhereContributesToSkillCap()
        {
            var list = new List<Skill>();
            foreach (var record in ByContributesToSkillCap.Values)
            {
                list.Add((Skill)record.Clone());
            }

            return list;
        }

        public IEnumerable<Skill> GetAllBySkillCategoryIDAndActive(int skillCategoryID)
        {
            var list = new List<Skill>();
            if (!ByCategoryID.ContainsKey(skillCategoryID))
                return list;

            foreach(var record in ByCategoryID[skillCategoryID].Values.Where(x => x.IsActive))
            {
                list.Add((Skill)record.Clone());
            }

            return list;
        }
    }
}
