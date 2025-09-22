namespace SWLOR.Shared.Core.Contracts
{
    public interface ISoundSetCacheService
    {
        Dictionary<int, string> GetSoundSets();
    }
}
