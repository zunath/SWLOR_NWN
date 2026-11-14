namespace SWLOR.Game.Server.Service.PropertyService
{
    /// <summary>The next step an election is due for at a given time.</summary>
    public enum ElectionStageAction
    {
        None = 0,
        CloseRegistration = 1,
        CloseVoting = 2,
    }
}
