namespace SWLOR.Shared.Abstractions.Contracts;

public interface ISoundSetCacheService
{
    Dictionary<int, string> GetSoundSets();
}
