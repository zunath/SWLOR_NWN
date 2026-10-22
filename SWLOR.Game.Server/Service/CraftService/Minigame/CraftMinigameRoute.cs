using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>The window that crafts a skill's recipes.</summary>
/// <param name="Skill">The crafting skill.</param>
/// <param name="Window">The window the recipe window opens.</param>
/// <param name="Minigame">The minigame the window plays, or null for the original crafting window.</param>
/// <param name="XpMultiplier">What the minigame multiplies a craft's base XP by; 1 for the original window.</param>
public sealed record CraftMinigameRoute(SkillType Skill, GuiWindowType Window, CraftMinigameType? Minigame, double XpMultiplier = 1.0)
{
    public bool IsLegacy => Minigame == null;
}
