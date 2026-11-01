namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The Afterburner (W11k, plan Appendix B): it adds speed while it is on and pays capacitor each cycle; an
    /// Interdictor's hold shuts it down. Pure logic with no engine calls.
    /// </summary>
    public static class ShipAfterburnerRules
    {
        /// <summary>The speed multiplier with the strongest running Afterburner's percent (0 when none runs).</summary>
        public static double ApplySpeed(double multiplier, int afterburnerPercent) =>
            afterburnerPercent <= 0 ? multiplier : multiplier * (100 + afterburnerPercent) / 100.0;

        /// <summary>Why an Afterburner cannot run, or an empty string.</summary>
        public static string GetError(bool interdicted) =>
            interdicted ? ShipElectronicWarfareRules.InterdictedMessage : string.Empty;
    }
}
