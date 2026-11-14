using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// The engine side of the four Performance windows: opening and closing them, finding their view models, and reading
    /// the names and numbers they show. Static callers reach
    /// the container's instance of this interface through <c>GameServices.PerformanceWindows</c>; the event handler is on <see cref="PerformanceWindowsService"/>.
    /// </summary>
    public interface IPerformanceWindowsService
    {
        /// <summary>Reserves the GUI id of the window-less cue strip when the module loads.</summary>
        void ReserveGuiIds();

        /// <summary>
        /// Shows one beat of the cue strip when no cue window is open. Screen text, not floating text: floating text is
        /// also written to the chat log, once per beat. Each line replaces the last and lasts just past the next beat.
        /// </summary>
        void ShowFallbackStrip(uint performer, string text, float beatSeconds);

        /// <summary>Removes the window-less cue strip.</summary>
        void ClearFallbackStrip(uint performer);

        /// <summary>The cue window a kind of performance uses.</summary>
        GuiWindowType GetCueWindowType(PerformanceKind kind);

        /// <summary>Opens the performer's cue window and attaches it to the session. Returns false when it could not be opened.</summary>
        bool OpenCueWindow(PerformanceSession session);

        /// <summary>Detaches the cue window and, unless the player logged out, closes it.</summary>
        void CloseCueWindow(PerformanceSession session, PerformanceEndReason reason);

        /// <summary>
        /// The Options button: shows or hides the Perform window. A performer who closed their cue window gets it back too,
        /// so they are not left with floating text and no way to reopen it.
        /// </summary>
        void ToggleOptions(PerformanceSession session);

        /// <summary>Tells an open cue window that the performer changed routine, set, song, tempo or mode.</summary>
        void RefreshSetup(PerformanceSession session);

        /// <summary>
        /// The audience summary between ticks, after the performer changed something. It reads the performer alone, so a
        /// partner's riders and Harmony come back with the next audience tick.
        /// </summary>
        string BuildAudienceGets(PerformanceSession session);

        /// <summary>What a performer chose for the show, as the cue windows word it.</summary>
        PerformerSetup BuildSetup(PerformanceSession session);

        /// <summary>The performer's own XP bonus and debt clearing for their audience, as the Perform window quotes them.</summary>
        PerformanceAudienceBonus GetAudienceBonus(uint performer, PerformanceKind kind);

        /// <summary>
        /// What the audience tick tells a performer's cue window: who follows them, the venue, and for a musician the band.
        /// Built only for an open window, so a closed one costs nothing.
        /// </summary>
        PerformerAudienceInfo BuildAudienceInfo(PerformanceAreaShow show, PerformerSnapshot performer);

        /// <summary>True when the watcher has the Watching window open.</summary>
        bool IsWatchingOpen(uint watcher);

        /// <summary>Opens the Watching window for a watcher. Returns false when it could not be opened.</summary>
        bool OpenWatching(uint watcher);

        /// <summary>Shows a view in the watcher's open Watching window.</summary>
        void ShowWatching(uint watcher, PerformanceWatchView view);

        /// <summary>Closes the watcher's Watching window when it is open.</summary>
        void CloseWatching(uint watcher);
    }
}
