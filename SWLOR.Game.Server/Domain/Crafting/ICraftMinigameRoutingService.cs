using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Domain.Crafting;

/// <summary>
/// Chooses the window that crafts a skill's recipes. Every crafting skill registers an <see cref="ICraftMinigameRoute"/>. Static
/// callers reach the container's instance through <c>GameServices.CraftMinigameRouting</c>.
/// </summary>
public interface ICraftMinigameRoutingService
{
    /// <summary>The minigame route of a crafting skill. A skill with no minigame is an error, because no other window crafts recipes.</summary>
    CraftMinigameRoute GetRoute(SkillType skill);

    /// <summary>The minigame route of a skill, or false for a skill that crafts nothing.</summary>
    bool TryGetRoute(SkillType skill, out CraftMinigameRoute route);

    GuiWindowType GetWindow(SkillType skill);

    /// <summary>Every window that crafts items, each minigame's, and the research Experiment, which commits the worn gear.</summary>
    IReadOnlyList<GuiWindowType> GetCraftWindows();

    /// <summary>True when the player has any crafting minigame window open, or the research Experiment.</summary>
    bool IsAnyCraftWindowOpen(uint player);

    /// <summary>Builds the lookup from route declarations. Two routes for one skill is an error.</summary>
    IReadOnlyDictionary<SkillType, CraftMinigameRoute> BuildRoutes(IEnumerable<ICraftMinigameRoute> declarations);
}
