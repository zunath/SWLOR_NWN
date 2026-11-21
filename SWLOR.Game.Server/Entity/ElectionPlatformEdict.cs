using SWLOR.Game.Server.Service.EdictService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// One Edict a candidate promises on their platform, saved with the <see cref="Election"/>. An Alliance or Embargo
    /// also names the city it is aimed at.
    /// </summary>
    public class ElectionPlatformEdict
    {
        public ElectionPlatformEdict()
        {
            TargetCityId = string.Empty;
        }

        public EdictType Type { get; set; }

        /// <summary>The city an Alliance or Embargo names. Empty for every other Edict.</summary>
        public string TargetCityId { get; set; }
    }
}
