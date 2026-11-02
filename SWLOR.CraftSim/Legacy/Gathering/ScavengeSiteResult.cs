namespace SWLOR.CraftSim.Legacy.Gathering
{
    /// <summary>What one scavenged site paid out.</summary>
    public readonly record struct ScavengeSiteResult(int Attempts, int Successes, int Xp);
}
