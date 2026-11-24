namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// Where a companion stands and what it is told: the follow action, the mode position, the reset of its engagements and
    /// progress, the check for an order in progress and the responses it sends its master.
    /// </summary>
    public interface ICompanionPositioningService
    {
        void ClearEngagements(CompanionControlState state);

        void ResetProgress(CompanionControlState state);

        bool IsExplicitOrderInProgress(uint companion);

        void MaintainModePosition(uint companion);

        void IssueFollowAction(uint companion, bool clearActions = true);

        void SendResponse(uint companion, string response);
    }
}
