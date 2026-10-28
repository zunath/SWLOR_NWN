namespace SWLOR.Game.Server.Service.FactionAgentService
{
    /// <summary>
    /// Where an offer sits on an agent's board: a standard offer at the dock's tier, a priority offer a tier higher for
    /// Trusted pilots, or an intel lead opened by agent intel.
    /// </summary>
    public enum AgentMissionSlot
    {
        Standard = 0,
        Priority = 1,
        Intel = 2,
    }
}
