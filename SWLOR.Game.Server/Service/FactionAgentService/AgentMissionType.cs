namespace SWLOR.Game.Server.Service.FactionAgentService
{
    /// <summary>The kinds of mission a faction agent offers (plan section 29). Held missions store it by number.</summary>
    public enum AgentMissionType
    {
        Invalid = 0,
        Kill = 1,
        SealedCourier = 2,
        MiningQuota = 3,
        ConvoyEscort = 4,
        ScanDown = 5,
        DataHeist = 6,
    }
}
