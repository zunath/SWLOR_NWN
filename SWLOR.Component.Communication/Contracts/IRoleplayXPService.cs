namespace SWLOR.Component.Communication.Feature;

public interface IRoleplayXPService
{
    /// <summary>
    /// Once every 30 minutes, the RP system will check all players and distribute RP XP if applicable.
    /// </summary>
    void DistributeRoleplayXP();

    /// <summary>
    /// Adds RP points to a player's RP progression.
    /// If messages are sent too quickly, the message will be treated as spam and RP point will not be granted.
    /// </summary>
    void ProcessRPMessage();
}