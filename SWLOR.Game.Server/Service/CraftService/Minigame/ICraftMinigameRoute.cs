using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// Declares which window crafts a skill's recipes. Each crafting skill has one implementation. Implementations are found by
/// reflection and need a public parameterless constructor.
/// </summary>
public interface ICraftMinigameRoute
{
    SkillType Skill { get; }

    /// <summary>The window the recipe window opens, with a <c>CraftPayload</c>.</summary>
    GuiWindowType Window { get; }

    CraftMinigameType Minigame { get; }
}
