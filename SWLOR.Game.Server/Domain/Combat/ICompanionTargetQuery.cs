namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// What the enmity tables read from a player's droid or beast companion: whether a creature is a registered companion,
    /// the target its master authorized and whether an attack command is allowed. Combat owns the interface and the companion
    /// target authority implements it (<c>ICompanionTargetAuthorityService : ICompanionTargetQuery</c>).
    /// </summary>
    public interface ICompanionTargetQuery
    {
        bool IsRegisteredCompanion(uint creature);

        uint PeekAuthorizedTarget(uint companion);

        bool CanIssueAttackCommand(uint companion, uint target);
    }
}
