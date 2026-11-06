using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// Chooses the window that crafts a skill's recipes. Every crafting skill registers an <see cref="ICraftMinigameRoute"/>.
/// </summary>
public static class CraftMinigameRouting
{
    private static ICraftMinigameRoutingService Instance => FacadeAccessor.Resolve<ICraftMinigameRoutingService>();

    /// <summary>
    /// The window of the research Experiment. It crafts no item, but it spends the Fabrication CP of the gear worn when it opened, so
    /// it commits that gear like a crafting window does.
    /// </summary>
    public const GuiWindowType ResearchExperimentWindow = Domain.Crafting.CraftMinigameRoutingService.ResearchExperimentWindow;

    /// <summary>The minigame route of a crafting skill. A skill with no minigame is an error, because no other window crafts recipes.</summary>
    public static CraftMinigameRoute GetRoute(SkillType skill)
    {
        return Instance.GetRoute(skill);
    }

    /// <summary>The minigame route of a skill, or false for a skill that crafts nothing.</summary>
    public static bool TryGetRoute(SkillType skill, out CraftMinigameRoute route)
    {
        return Instance.TryGetRoute(skill, out route);
    }

    public static GuiWindowType GetWindow(SkillType skill)
    {
        return Instance.GetWindow(skill);
    }

    /// <summary>Every window that crafts items, each minigame's, and the research Experiment, which commits the worn gear.</summary>
    public static IReadOnlyList<GuiWindowType> GetCraftWindows()
    {
        return Instance.GetCraftWindows();
    }

    /// <summary>True when the player has any crafting minigame window open, or the research Experiment.</summary>
    public static bool IsAnyCraftWindowOpen(uint player)
    {
        return Instance.IsAnyCraftWindowOpen(player);
    }

    /// <summary>Builds the lookup from route declarations. Two routes for one skill is an error.</summary>
    public static IReadOnlyDictionary<SkillType, CraftMinigameRoute> BuildRoutes(IEnumerable<ICraftMinigameRoute> declarations)
    {
        return Instance.BuildRoutes(declarations);
    }
}
