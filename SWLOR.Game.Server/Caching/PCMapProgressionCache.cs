using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class PCMapProgressionCache: CacheBase<PCMapProgression>
    {
        public PCMapProgressionCache() 
            : base("PCMapProgression")
        {
        }

        private const string ByPlayerIDAndAreaResrefIndex = "ByPlayerIDAndAreaResref";

        protected override void OnCacheObjectSet(PCMapProgression entity)
        {
            SetIntoIndex($"{ByPlayerIDAndAreaResrefIndex}:{entity.PlayerID}", $"{entity.AreaResref}", entity);
        }

        protected override void OnCacheObjectRemoved(PCMapProgression entity)
        {
            RemoveFromIndex($"{ByPlayerIDAndAreaResrefIndex}:{entity.PlayerID}", $"{entity.AreaResref}");
        }

        protected override void OnSubscribeEvents()
        {
        }

        public PCMapProgression GetByID(Guid id)
        {
            return ByID(id);
        }

        public PCMapProgression GetByPlayerIDAndAreaResrefOrDefault(Guid playerID, string areaResref)
        {
            if (!ExistsByIndex($"{ByPlayerIDAndAreaResrefIndex}:{playerID.ToString()}", $"{areaResref}"))
                return default;

            return GetFromIndex($"{ByPlayerIDAndAreaResrefIndex}:{playerID.ToString()}", $"{areaResref}");
        }
    }
}
