using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class GuildTaskCache: CacheBase<GuildTask>
    {
        public GuildTaskCache() 
            : base("GuildTask")
        {
        }

        private const string ByCurrentlyOfferedIndex = "ByCurrentlyOffered";
        private const string ByCurrentlyOfferedValue = "Active";
        private const string ByRequiredRankIndex = "ByRequiredRank";

        
        protected override void OnCacheObjectSet(GuildTask entity)
        {
            SetByCurrentlyOffered(entity);
            SetIntoListIndex(ByRequiredRankIndex, entity.RequiredRank.ToString(), entity);
        }

        protected override void OnCacheObjectRemoved(GuildTask entity)
        {
            RemoveByCurrentlyOffered(entity);
            RemoveFromListIndex(ByRequiredRankIndex, entity.RequiredRank.ToString(), entity);
        }

        protected override void OnSubscribeEvents()
        {
        }

        private void SetByCurrentlyOffered(GuildTask entity)
        {
            if (!ExistsInListIndex(ByCurrentlyOfferedIndex, ByCurrentlyOfferedValue, entity) && entity.IsCurrentlyOffered)
                SetIntoListIndex(ByCurrentlyOfferedIndex, ByCurrentlyOfferedValue, entity);
            else if (ExistsInListIndex(ByCurrentlyOfferedIndex, ByCurrentlyOfferedValue, entity) && !entity.IsCurrentlyOffered)
                RemoveFromListIndex(ByCurrentlyOfferedIndex, ByCurrentlyOfferedValue, entity);
        }

        private void RemoveByCurrentlyOffered(GuildTask entity)
        {
            if (ExistsInListIndex(ByCurrentlyOfferedIndex, ByCurrentlyOfferedIndex, entity))
                RemoveFromListIndex(ByCurrentlyOfferedIndex, ByCurrentlyOfferedValue, entity);
        }

        public GuildTask GetByID(int id)
        {
            return ByID(id);
        }

        public IEnumerable<GuildTask> GetAllByCurrentlyOffered()
        {
            if(!ExistsByListIndex(ByCurrentlyOfferedIndex, ByCurrentlyOfferedValue))
                return new List<GuildTask>();

            return GetFromListIndex(ByCurrentlyOfferedIndex, ByCurrentlyOfferedValue);
        }

        public IEnumerable<GuildTask> GetAllByGuildIDAndRequiredRank(int requiredRank, int guildID)
        {
            if (!ExistsByListIndex(ByRequiredRankIndex, requiredRank.ToString()))
                return new List<GuildTask>();

            return GetFromListIndex(ByRequiredRankIndex, requiredRank.ToString()).Where(x => x.GuildID == guildID);
        }
    }
}
