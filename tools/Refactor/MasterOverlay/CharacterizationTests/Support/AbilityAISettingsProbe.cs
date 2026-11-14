using SWLOR.Game.Server.Domain.Abilities;

namespace SWLOR.Game.Server.CharacterizationTests.Support;

/// <summary>
/// Master version of the branch's AbilityAISettingsProbe (see MasterCopy.ps1, "master overlay"): master keeps the AI settings on
/// <see cref="AbilityDetail"/>.
/// </summary>
internal static class AbilityAISettingsProbe
{
    public static bool HasHitPointCostPercent(AbilityDetail ability) => ability.AIHitPointCostPercent != null;

    public static bool HasScore(AbilityDetail ability) => ability.AIScore != null;

    public static bool HasTargetSelector(AbilityDetail ability) => ability.AITargetSelector != null;
}
