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

    /// <summary>
    /// What the minigame multiplies a craft's base XP by, for both a finished item and a failed one. 1 unless CraftSim's XP-rate gate
    /// showed the minigame pays less XP per hour than the old window and the plan scaled it up.
    /// </summary>
    double XpMultiplier => 1.0;
}
