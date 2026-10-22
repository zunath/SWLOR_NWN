using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// Chooses the window that crafts a skill's recipes. Every skill uses the original crafting window until its minigame registers
/// an <see cref="ICraftMinigameRoute"/>.
/// </summary>
public static class CraftMinigameRouting
{
    /// <summary>The original crafting window, which every skill without a minigame still uses.</summary>
    public const GuiWindowType LegacyWindow = GuiWindowType.Craft;

    private static IReadOnlyDictionary<SkillType, CraftMinigameRoute> _routes;

    public static CraftMinigameRoute GetRoute(SkillType skill)
    {
        _routes ??= BuildRoutes(DiscoverRoutes());
        return _routes.TryGetValue(skill, out var route) ? route : Legacy(skill);
    }

    public static GuiWindowType GetWindow(SkillType skill) => GetRoute(skill).Window;

    /// <summary>
    /// The window of the research Experiment. It crafts no item, but it spends the Fabrication CP of the gear worn when it opened, so
    /// it commits that gear like a crafting window does.
    /// </summary>
    public const GuiWindowType ResearchExperimentWindow = GuiWindowType.ResearchExperiment;

    /// <summary>Every window that crafts items, the original one and each minigame's, and the research Experiment, which commits the worn gear.</summary>
    public static IReadOnlyList<GuiWindowType> GetCraftWindows()
    {
        _routes ??= BuildRoutes(DiscoverRoutes());
        return _routes.Values.Select(route => route.Window)
            .Append(LegacyWindow)
            .Append(ResearchExperimentWindow)
            .Distinct()
            .ToList();
    }

    /// <summary>True when the player has any crafting window open, the original or a minigame's, or the research Experiment.</summary>
    public static bool IsAnyCraftWindowOpen(uint player) =>
        GetCraftWindows().Any(window => Gui.IsWindowOpen(player, window));

    /// <summary>Builds the lookup from route declarations. Two routes for one skill is an error.</summary>
    public static IReadOnlyDictionary<SkillType, CraftMinigameRoute> BuildRoutes(IEnumerable<ICraftMinigameRoute> declarations)
    {
        var routes = new Dictionary<SkillType, CraftMinigameRoute>();
        foreach (var declaration in declarations)
        {
            if (routes.ContainsKey(declaration.Skill))
                throw new InvalidOperationException($"More than one crafting window is registered for {declaration.Skill}.");

            routes[declaration.Skill] = new CraftMinigameRoute(declaration.Skill, declaration.Window, declaration.Minigame);
        }

        return routes;
    }

    public static CraftMinigameRoute Legacy(SkillType skill) => new(skill, LegacyWindow, null);

    private static IEnumerable<ICraftMinigameRoute> DiscoverRoutes() =>
        typeof(ICraftMinigameRoute).Assembly.GetTypes()
            .Where(type => typeof(ICraftMinigameRoute).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .Select(type => (ICraftMinigameRoute)Activator.CreateInstance(type))
            .ToList();
}
