namespace SWLOR.Shared.Caching.Contracts
{
    public interface ISoundSetCacheService
    {
        Dictionary<int, string> GetSoundSets();
    }
}
