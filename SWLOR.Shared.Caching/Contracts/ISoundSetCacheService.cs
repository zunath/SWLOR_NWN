namespace SWLOR.Shared.Caching.Contracts
{
    public interface ISoundSetCacheService
    {
        void LoadCache();
        Dictionary<int, string> GetSoundSets();
    }
}
