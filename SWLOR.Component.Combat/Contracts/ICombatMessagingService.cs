namespace SWLOR.Component.Combat.Contracts;

internal interface ICombatMessagingService
{
    /// <summary>
    /// Builds a combat log message based on the provided information.
    /// </summary>
    /// <param name="attacker">The id of the attacker</param>
    /// <param name="defender">The id of the defender</param>
    /// <param name="attackResultType">The type of result. 1, 7 = Hit, 3 = Critical, 4 = Miss</param>
    /// <param name="chanceToHit">The percent chance to hit</param>
    /// <returns></returns>
    string BuildCombatLogMessage(
        uint attacker,
        uint defender,
        int attackResultType,
        int chanceToHit);

    void SendParalysisMessage(uint creature);
}