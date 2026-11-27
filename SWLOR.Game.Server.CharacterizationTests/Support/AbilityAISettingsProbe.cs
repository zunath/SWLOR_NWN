using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Creatures;

namespace SWLOR.Game.Server.CharacterizationTests.Support;

/// <summary>
/// Whether an ability sets each of its AI settings. On the branch the settings live in Creatures; master keeps them on
/// <see cref="AbilityDetail"/>, so the parity run overlays a master version of this file (tools/Refactor/MasterOverlay).
/// </summary>
internal static class AbilityAISettingsProbe
{
    public static bool HasHitPointCostPercent(AbilityDetail ability) => ability.GetAIHitPointCostPercent() != null;

    public static bool HasScore(AbilityDetail ability) => ability.GetAIScore() != null;

    public static bool HasTargetSelector(AbilityDetail ability) => ability.GetAITargetSelector() != null;
}
