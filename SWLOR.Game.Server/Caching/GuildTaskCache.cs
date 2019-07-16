using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class GuildTaskCache: CacheBase<GuildTask>
    {
        private List<GuildTask> ByCurrentlyOffered { get; } = new List<GuildTask>();
        private Dictionary<int, Dictionary<int, GuildTask>> ByRequiredRank { get; } = new Dictionary<int, Dictionary<int, GuildTask>>();

        protected override void OnCacheObjectSet(GuildTask entity)
        {
            SetByCurrentlyOffered(entity);
            SetEntityIntoDictionary(entity.RequiredRank, entity.ID, entity, ByRequiredRank);
        }

        protected override void OnCacheObjectRemoved(GuildTask entity)
        {
            RemoveByCurrentlyOffered(entity);
            RemoveEntityFromDictionary(entity.RequiredRank, entity.RequiredRank, ByRequiredRank);
        }

        protected override void OnSubscribeEvents()
        {
        }

        private void SetByCurrentlyOffered(GuildTask entity)
        {
            if(!ByCurrentlyOffered.Contains(entity) && entity.IsCurrentlyOffered)
                ByCurrentlyOffered.Add(entity);
            else if (ByCurrentlyOffered.Contains(entity) && !entity.IsCurrentlyOffered)
                ByCurrentlyOffered.Remove(entity);
        }

        private void RemoveByCurrentlyOffered(GuildTask entity)
        {
            if (ByCurrentlyOffered.Contains(entity))
                ByCurrentlyOffered.Remove(entity);
        }

        public GuildTask GetByID(int id)
        {
            return ByID[id];
        }

        public IEnumerable<GuildTask> GetAllByCurrentlyOffered()
        {
            return ByCurrentlyOffered;
        }

        public IEnumerable<GuildTask> GetAllByGuildIDAndRequiredRank(int requiredRank, int guildID)
        {
            return ByRequiredRank[requiredRank].Values.Where(x => x.GuildID == guildID);
        }
    }
}
