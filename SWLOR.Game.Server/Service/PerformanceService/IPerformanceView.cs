namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>
    /// A performer's cue window as the beat loop and the audience tick see it. Every method pushes only the values that
    /// changed, and none of them rebuilds the window.
    /// </summary>
    public interface IPerformanceView
    {
        /// <summary>True while the window is on the performer's screen. A closed window is skipped entirely.</summary>
        bool IsOpen { get; }

        /// <summary>True when the window shows the time left on the beat, so the loop steps inside each beat for it.</summary>
        bool WantsBeatProgress { get; }

        /// <summary>A full beat: the cue strip moved on and the last cue was scored.</summary>
        void ShowBeat(PerformanceSession session, PerformanceBeatOutcome lastOutcome);

        /// <summary>A step inside a beat: how much of the current beat is left, from 1 down toward 0.</summary>
        void ShowBeatProgress(float timeLeft);

        /// <summary>The performer changed routine, set, song or tempo.</summary>
        void ShowSetup(PerformerSetup setup);

        /// <summary>The audience tick counted the performer's watchers.</summary>
        void ShowAudience(PerformerAudienceInfo info);

        /// <summary>The performance ended, so the beat loop stops pushing to this window.</summary>
        void MarkEnded();
    }
}
