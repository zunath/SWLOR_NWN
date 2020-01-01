using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.Caching
{
    public class PerkFeatCache: CacheBase<PerkFeat>
    {
        public PerkFeatCache() 
            : base("PerkFeat")
        {
        }

        private const string ByPerkIDAndLevelUnlockedIndex = "ByPerkIDAndLevelUnlocked";
        private const string ByFeatIDIndex = "ByFeatID";

        protected override void OnCacheObjectSet(PerkFeat entity)
        {
            SetIntoListIndex(ByPerkIDAndLevelUnlockedIndex, entity.PerkID.ToString(), entity);
            SetIntoIndex(ByFeatIDIndex, entity.FeatID.ToString(), entity);
        }

        protected override void OnCacheObjectRemoved(PerkFeat entity)
        {
            RemoveFromListIndex(ByPerkIDAndLevelUnlockedIndex, entity.PerkID.ToString(), entity);
            RemoveFromIndex(ByFeatIDIndex, entity.FeatID.ToString());
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PerkFeat GetByID(int id)
        {
            return ByID(id);
        }

        public PerkFeat GetByPerkIDAndLevelUnlocked(int perkID, int levelUnlocked)
        {
            return GetFromListIndex(ByPerkIDAndLevelUnlockedIndex, perkID.ToString()).Single(x => x.PerkLevelUnlocked == levelUnlocked);
        }

        public PerkFeat GetByPerkIDAndLevelUnlockedOrDefault(int perkID, int levelUnlocked)
        {
            if (!ExistsByListIndex(ByPerkIDAndLevelUnlockedIndex, perkID.ToString()))
                return default;

            return GetFromListIndex(ByPerkIDAndLevelUnlockedIndex, perkID.ToString()).Single(x => x.PerkLevelUnlocked == levelUnlocked);
        }

        public PerkFeat GetByFeatID(int featID)
        {
            return GetFromIndex(ByFeatIDIndex, featID.ToString());
        }
         
        public PerkFeat GetByFeatIDOrDefault(int featID)
        {
            if (!ExistsByIndex(ByFeatIDIndex, featID.ToString()))
            {
                return default;
            }

            return GetFromIndex(ByFeatIDIndex, featID.ToString());
        }

        public IEnumerable<PerkFeat> GetAllByIDs(IEnumerable<Feat> perkIDs)
        {
            var list = new List<PerkFeat>();
            foreach (var perkID in perkIDs)
            {
                if (ExistsByIndex(ByFeatIDIndex, ((int)perkID).ToString()))
                {
                    list.Add(GetFromIndex(ByFeatIDIndex, ((int) perkID).ToString()));
                }
            }

            return list;
        }

        public IEnumerable<PerkFeat> GetAllByPerkID(int perkID)
        {
            if(!ExistsByListIndex(ByPerkIDAndLevelUnlockedIndex, perkID.ToString()))
                return new List<PerkFeat>();

            return GetFromListIndex(ByPerkIDAndLevelUnlockedIndex, perkID.ToString());
        }
    }
}
