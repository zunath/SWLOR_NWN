using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Crafting.Minigame;

/// <summary>The window that crafts a skill's recipes.</summary>
/// <param name="Skill">The crafting skill.</param>
/// <param name="Window">The window the recipe window opens.</param>
/// <param name="Minigame">The minigame the window plays.</param>
public sealed record CraftMinigameRoute(SkillType Skill, GuiWindowType Window, CraftMinigameType Minigame);
