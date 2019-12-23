using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PerkLevelCache: CacheBase<PerkLevel>
    {
        public PerkLevelCache() 
            : base("PerkLevel")
        {
        }

        private const string ByPerkIDAndLevelIndex = "ByPerkIDAndLevel";
        private const string ByPerkIDIndex = "ByPerkID";

        protected override void OnCacheObjectSet(PerkLevel entity)
        {
            SetIntoIndex($"{ByPerkIDAndLevelIndex}:{entity.PerkID}", entity.Level.ToString(), entity);
            SetIntoListIndex(ByPerkIDIndex, entity.PerkID.ToString(), entity);
        }

        protected override void OnCacheObjectRemoved(PerkLevel entity)
        {
            RemoveFromIndex($"{ByPerkIDAndLevelIndex}:{entity.PerkID}", entity.Level.ToString());
            RemoveFromListIndex(ByPerkIDIndex, entity.PerkID.ToString(), entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PerkLevel GetByID(int id)
        {
            return ByID(id);
        }

        public PerkLevel GetByPerkIDAndLevel(int perkID, int level)
        {
            return GetFromIndex($"{ByPerkIDAndLevelIndex}:{perkID}", $"{level}");
        }

        public IEnumerable<PerkLevel> GetAllByPerkID(int perkID)
        {
            if (!ExistsByListIndex(ByPerkIDIndex, perkID.ToString()))
                return new List<PerkLevel>();

            return GetFromListIndex(ByPerkIDIndex, perkID.ToString());
        }

        public IEnumerable<PerkLevel> GetAllAtOrBelowPerkIDAndLevel(int perkID, int level)
        {
            if(!ExistsByIndex($"{ByPerkIDAndLevelIndex}:{perkID}", $"{level}"))
                return new List<PerkLevel>();

            return GetAll().Where(x => x.PerkID == perkID && x.Level <= level); // May need to index this.
        }
    }
}
