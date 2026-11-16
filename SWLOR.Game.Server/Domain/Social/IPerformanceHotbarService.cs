using SWLOR.Game.Server.Domain.Social;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// A performer's hot bar and mode feats: the feats a performance adds, the bar it swaps in, and the restore of the
    /// player's own bar when the performance ends or a crash left it swapped. A part of the Performance service.
    /// </summary>
    public interface IPerformanceHotbarService
    {
        /// <summary>True for the cue, Flourish and Options feats of a performance's hot bar.</summary>
        bool IsModeFeat(FeatType feat);

        /// <summary>True for the Dance and Play Instrument toggle feats, which double as the Stop button.</summary>
        bool IsToggleFeat(FeatType feat);

        /// <summary>The toggle feat of the performance's perk level, the one the Stop button holds.</summary>
        FeatType GetToggleFeat(PerformanceKind kind, int level);

        /// <summary>The cue a cue feat stands for.</summary>
        bool TryGetCue(FeatType feat, out PerformanceCue cue);

        /// <summary>Saves the player's hot bar for the restore; false when the player has no record to save it in.</summary>
        bool TrySaveHotbar(uint player);

        /// <summary>Gives the player the cue, Flourish and Options feats of the kind.</summary>
        void AddModeFeats(uint player, PerformanceKind kind);

        /// <summary>Swaps in the performance's hot bar. Options and Stop keep their slots in both modes.</summary>
        void ApplyModeQuickbar(uint player, PerformanceKind kind, FeatType toggleFeat, bool roleplay);

        /// <summary>Takes every mode feat off the player.</summary>
        void RemoveModeFeats(uint player);

        /// <summary>Puts the player's saved hot bar back and removes the mode feats.</summary>
        void RestorePlayer(uint player);

        /// <summary>Restores the hot bar and feats after a crash, a failed cleanup, or a stale mode button.</summary>
        void RecoverInterruptedPerformance(uint player, bool atLogin = false);
    }
}
