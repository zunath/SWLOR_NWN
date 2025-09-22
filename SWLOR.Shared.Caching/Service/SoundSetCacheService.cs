using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Shared.Caching.Service
{
    /// <summary>
    /// This class is responsible for loading and retrieving sound set data which lives for the lifespan of the server.
    /// Nothing in here will be permanently stored, it's simply here to make queries quicker.
    /// </summary>
    public class SoundSetCacheService : ISoundSetCacheService
    {
        private Dictionary<int, string> SoundSets { get; set; } = new();

        [ScriptHandler<OnModuleCacheBefore>]
        public void CacheSoundSets()
        {
            const string SoundSets2DA = "soundset";
            var soundSetCount = Get2DARowCount(SoundSets2DA);
            for (var row = 0; row < soundSetCount; row++)
            {
                var strRef = Get2DAString(SoundSets2DA, "STRREF", row);
                var resref = Get2DAString(SoundSets2DA, "RESREF", row);

                if (!string.IsNullOrWhiteSpace(strRef) &&
                    !string.IsNullOrWhiteSpace(resref))
                {
                    SoundSets.Add(row, GetStringByStrRef(Convert.ToInt32(strRef)));
                }
            }

            SoundSets = SoundSets.OrderBy(o => o.Value).ToDictionary(x => x.Key, y => y.Value);
        }

        public Dictionary<int, string> GetSoundSets()
        {
            return SoundSets.ToDictionary(x => x.Key, y => y.Value);
        }
    }
}
