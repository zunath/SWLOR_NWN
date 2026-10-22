using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// Declares which window crafts a skill's recipes. A minigame adds one implementation when it lands; skills with none keep
/// using the original crafting window. Implementations are found by reflection and need a public parameterless constructor.
/// </summary>
public interface ICraftMinigameRoute
{
    SkillType Skill { get; }

    /// <summary>The window the recipe window opens, with a <c>CraftPayload</c>.</summary>
    GuiWindowType Window { get; }

    CraftMinigameType Minigame { get; }

    /// <summary>The factor on the base XP of every craft of the skill, success and failure alike. 1 leaves the XP chart's value.</summary>
    double XpMultiplier => 1.0;
}
