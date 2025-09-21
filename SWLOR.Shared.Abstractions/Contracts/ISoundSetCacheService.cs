using System.Collections.Generic;

namespace SWLOR.Game.Server.Service;

public interface ISoundSetCacheService
{
    Dictionary<int, string> GetSoundSets();
}
