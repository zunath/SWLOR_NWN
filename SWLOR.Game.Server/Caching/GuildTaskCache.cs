using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class GuildTaskCache: CacheBase<GuildTask>
    {
        private Dictionary<int, GuildTask> ByCurrentlyOffered { get; } = new Dictionary<int, GuildTask>();
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
            if (!ByCurrentlyOffered.ContainsKey(entity.ID) && entity.IsCurrentlyOffered)
                ByCurrentlyOffered[entity.ID] = (GuildTask)entity.Clone();
            else if (ByCurrentlyOffered.ContainsKey(entity.ID) && !entity.IsCurrentlyOffered)
                ByCurrentlyOffered.Remove(entity.ID);
        }

        private void RemoveByCurrentlyOffered(GuildTask entity)
        {
            if (ByCurrentlyOffered.ContainsKey(entity.ID))
                ByCurrentlyOffered.Remove(entity.ID);
        }

        public GuildTask GetByID(int id)
        {
            return (GuildTask)ByID[id].Clone();
        }

        public IEnumerable<GuildTask> GetAllByCurrentlyOffered()
        {
            var list = new List<GuildTask>();
            foreach (var task in ByCurrentlyOffered.Values)
            {
                list.Add( (GuildTask) task.Clone());
            }

            return list;
        }

        public IEnumerable<GuildTask> GetAllByGuildIDAndRequiredRank(int requiredRank, int guildID)
        {
            var list = new List<GuildTask>();
            if (!ByRequiredRank.ContainsKey(requiredRank))
                return list;

            var results = ByRequiredRank[requiredRank].Values.Where(x => x.GuildID == guildID);
            foreach (var result in results)
            {
                list.Add( (GuildTask)result.Clone());
            }

            return list;
        }
    }
}
