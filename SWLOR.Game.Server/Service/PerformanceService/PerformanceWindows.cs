using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>
    /// The engine side of the four Performance windows: opening and closing them, finding their view models, and reading
    /// the names and numbers they show. The windows only show state; input stays on the hot bar feats.
    /// The logic lives in <see cref="IPerformanceWindowsService"/>; every member here forwards to the container's instance
    /// and the module-load handler stays here. This is a further facade of the <c>Performance</c> service
    /// (<c>ConvertedServices.json</c>).
    /// </summary>
    public static class PerformanceWindows
    {
        private static IPerformanceWindowsService Instance => FacadeAccessor.Resolve<IPerformanceWindowsService>();

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void ReserveGuiIds()
        {
            Instance.ReserveGuiIds();
        }

        /// <summary>
        /// Shows one beat of the cue strip when no cue window is open. Screen text, not floating text: floating text is
        /// also written to the chat log, once per beat. Each line replaces the last and lasts just past the next beat.
        /// </summary>
        public static void ShowFallbackStrip(uint performer, string text, float beatSeconds)
        {
            Instance.ShowFallbackStrip(performer, text, beatSeconds);
        }

        public static void ClearFallbackStrip(uint performer)
        {
            Instance.ClearFallbackStrip(performer);
        }

        public static GuiWindowType GetCueWindowType(PerformanceKind kind)
        {
            return Instance.GetCueWindowType(kind);
        }

        /// <summary>Opens the performer's cue window and attaches it to the session. Returns false when it could not be opened.</summary>
        public static bool OpenCueWindow(PerformanceSession session)
        {
            return Instance.OpenCueWindow(session);
        }

        /// <summary>Detaches the cue window and, unless the player logged out, closes it.</summary>
        public static void CloseCueWindow(PerformanceSession session, PerformanceEndReason reason)
        {
            Instance.CloseCueWindow(session, reason);
        }

        /// <summary>
        /// The Options button: shows or hides the Perform window. A performer who closed their cue window gets it back too,
        /// so they are not left with floating text and no way to reopen it.
        /// </summary>
        public static void ToggleOptions(PerformanceSession session)
        {
            Instance.ToggleOptions(session);
        }

        /// <summary>Tells an open cue window that the performer changed routine, set, song, tempo or mode.</summary>
        public static void RefreshSetup(PerformanceSession session)
        {
            Instance.RefreshSetup(session);
        }

        /// <summary>
        /// The audience summary between ticks, after the performer changed something. It reads the performer alone, so a
        /// partner's riders and Harmony come back with the next audience tick.
        /// </summary>
        public static string BuildAudienceGets(PerformanceSession session)
        {
            return Instance.BuildAudienceGets(session);
        }

        /// <summary>What a performer chose for the show, as the cue windows word it.</summary>
        public static PerformerSetup BuildSetup(PerformanceSession session)
        {
            return Instance.BuildSetup(session);
        }

        /// <summary>The performer's own XP bonus and debt clearing for their audience, as the Perform window quotes them.</summary>
        public static PerformanceAudienceBonus GetAudienceBonus(uint performer, PerformanceKind kind)
        {
            return Instance.GetAudienceBonus(performer, kind);
        }

        /// <summary>
        /// What the audience tick tells a performer's cue window: who follows them, the venue, and for a musician the band.
        /// Built only for an open window, so a closed one costs nothing.
        /// </summary>
        public static PerformerAudienceInfo BuildAudienceInfo(PerformanceAreaShow show, PerformerSnapshot performer)
        {
            return Instance.BuildAudienceInfo(show, performer);
        }

        public static bool IsWatchingOpen(uint watcher)
        {
            return Instance.IsWatchingOpen(watcher);
        }

        /// <summary>Opens the Watching window for a watcher. Returns false when it could not be opened.</summary>
        public static bool OpenWatching(uint watcher)
        {
            return Instance.OpenWatching(watcher);
        }

        public static void ShowWatching(uint watcher, PerformanceWatchView view)
        {
            Instance.ShowWatching(watcher, view);
        }

        public static void CloseWatching(uint watcher)
        {
            Instance.CloseWatching(watcher);
        }
    }
}
