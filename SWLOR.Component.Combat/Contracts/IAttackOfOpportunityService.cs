namespace SWLOR.Component.Combat.Contracts;

public interface IAttackOfOpportunityService
{
    /// <summary>
    /// Whenever an attack of opportunity is broadcast, skip the event to disable it.
    /// This should effectively disable AOOs across the board.
    /// </summary>
    void OnAttackOfOpportunity();
}