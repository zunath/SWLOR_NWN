using SWLOR.Game.Server.Service.EdictService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// One Edict a city holds, kept on <see cref="WorldProperty.Edicts"/> under its <see cref="EdictType"/>. The
    /// city-wide conditions (Disrepair, No confidence, Neglect) are not stored here: they belong to the city.
    /// </summary>
    public class CityEdict
    {
        public CityEdict()
        {
            TargetCityId = string.Empty;
        }

        public EdictState State { get; set; }

        /// <summary>When the mayor enacted it. A pending Alliance keeps the time it was queued.</summary>
        public DateTime EnactedUtc { get; set; }

        /// <summary>
        /// When the 7-day lock ends and the mayor may repeal it. Null while a pending Alliance waits for the other
        /// mayor, since its lock starts when both enact.
        /// </summary>
        public DateTime? LockEndsUtc { get; set; }

        /// <summary>The city an Alliance or Embargo names. Empty for every other Edict.</summary>
        public string TargetCityId { get; set; }

        /// <summary>True when the Edict came from the sitting mayor's election platform, so repealing it breaks a promise.</summary>
        public bool FromPlatform { get; set; }

        /// <summary>The city level is below the Edict's minimum, or its slot was lost. Cleared when the level returns.</summary>
        public bool IsPausedByCityLevel { get; set; }

        /// <summary>
        /// A situation outcome pauses this Edict until this time. The Edict is still billed while it is paused, since
        /// the pause is the outcome. Null when no situation pauses it.
        /// </summary>
        public DateTime? SituationPauseEndsUtc { get; set; }
    }
}
