namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>The Scanner tab's fixed texts (plan section 24).</summary>
    public static class ScannerHudRules
    {
        /// <summary>The line above the signature list.</summary>
        public static string Summary(int signatureCount, bool hasProbeLauncher)
        {
            if (signatureCount > 0)
                return signatureCount == 1 ? "1 signature" : $"{signatureCount} signatures";

            return hasProbeLauncher
                ? "No signals yet. Switch on your Probe Launcher to scan this system."
                : "No signals. Fit a Probe Launcher to scan for signatures.";
        }

        /// <summary>The Sensor Sweep button's text.</summary>
        public static string SweepText(bool hasProbeLauncher, int secondsLeft)
        {
            if (!hasProbeLauncher)
                return "Sensor Sweep (no Probe Launcher)";

            return secondsLeft > 0 ? $"Sensor Sweep ({secondsLeft} s)" : "Sensor Sweep";
        }
    }
}
