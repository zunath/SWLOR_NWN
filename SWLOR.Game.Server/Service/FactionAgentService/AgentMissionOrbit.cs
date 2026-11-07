namespace SWLOR.Game.Server.Service.FactionAgentService
{
    /// <summary>A space system an agent sends pilots to, and how many starlane jumps it lies from the agent's dock.</summary>
    public class AgentMissionOrbit
    {
        public string AreaResref { get; }
        public int Hops { get; }

        public AgentMissionOrbit(string areaResref, int hops)
        {
            AreaResref = areaResref;
            Hops = hops;
        }
    }
}
