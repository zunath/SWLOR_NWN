using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Caching
{
    public class SpaceEncounterCache: CacheBase<SpaceEncounter>
    {
        protected override void OnCacheObjectSet(SpaceEncounter entity)
        {
        }

        protected override void OnCacheObjectRemoved(SpaceEncounter entity)
        {
        }
    }
}
