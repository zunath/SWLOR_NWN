namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// What a performer is told when a performance starts, changes mode and ends: the text that was
    /// <see cref="PerformanceService"/>'s own, moved here unchanged.
    /// </summary>
    public static class PerformanceMessageText
    {
        public static string GetStartMessage(PerformanceKind kind, bool roleplay, PerformanceInstrumentFamily family)
        {
            var action = kind == PerformanceKind.Dance ? "dance" : family == PerformanceInstrumentFamily.Vocals ? "sing" : "play";
            if (roleplay)
            {
                return $"You begin to {action} in Roleplay mode. {RoleplayKeepGoingText}";
            }

            return kind == PerformanceKind.Dance
                ? "You begin to dance. Use the step buttons on your hot bar to match the cues, or switch to Roleplay mode under Options."
                : $"You begin to {action}. Use the note buttons on your hot bar to match the cues, or switch to Roleplay mode under Options.";
        }

        public static string GetModeMessage(PerformanceKind kind, bool roleplay)
        {
            if (roleplay)
                return $"Roleplay mode: no cues, and your audience banks Inspired at the Rough rate. {RoleplayKeepGoingText}";

            return kind == PerformanceKind.Dance
                ? "Active play: use the step buttons on your hot bar to match the cues."
                : "Active play: use the note buttons on your hot bar to match the cues.";
        }

        private static string RoleplayKeepGoingText =>
            $"Talk or emote at least once every {RoleplayIdleMinutes} minutes to keep performing.";

        private static int RoleplayIdleMinutes => (int)(PerformanceMath.RoleplayIdleTimeoutSeconds / 60);

        public static string GetEndMessage(PerformanceEndReason reason, bool roleplay)
        {
            if (reason == PerformanceEndReason.Idle && roleplay)
                return $"Your performance ends because you were quiet for {RoleplayIdleMinutes} minutes.";

            return reason switch
            {
                PerformanceEndReason.Stopped => "You finish your performance.",
                PerformanceEndReason.Combat => "Your performance ends because you are in combat.",
                PerformanceEndReason.Moved => "Your performance ends because you moved.",
                PerformanceEndReason.Idle => "Your performance ends because you stopped following the beat.",
                PerformanceEndReason.InstrumentRemoved => "Your performance ends because your instrument was removed.",
                _ => string.Empty
            };
        }
    }
}
