using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Domain.Crafting.Minigame;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Crafting;

/// <inheritdoc cref="ICraftMinigameRoutingService" />
public class CraftMinigameRoutingService : ICraftMinigameRoutingService
{
    private readonly IGuiService _gui;

    public CraftMinigameRoutingService(IGuiService gui)
    {
        _gui = gui;
    }

    /// <summary>
    /// The window of the research Experiment. It crafts no item, but it spends the Fabrication CP of the gear worn when it opened, so
    /// it commits that gear like a crafting window does.
    /// </summary>
    public const GuiWindowType ResearchExperimentWindow = GuiWindowType.ResearchExperiment;

    // The discovered routes, found on first use.
    private readonly CraftMinigameRouteCache _routeCache = new();

    /// <inheritdoc />
    public CraftMinigameRoute GetRoute(SkillType skill) =>
        TryGetRoute(skill, out var route) ? route : throw new InvalidOperationException($"{skill} has no crafting minigame.");

    /// <inheritdoc />
    public bool TryGetRoute(SkillType skill, out CraftMinigameRoute route)
    {
        _routeCache.Routes ??= BuildRoutes(DiscoverRoutes());
        return _routeCache.Routes.TryGetValue(skill, out route);
    }

    /// <inheritdoc />
    public GuiWindowType GetWindow(SkillType skill) => GetRoute(skill).Window;

    /// <inheritdoc />
    public IReadOnlyList<GuiWindowType> GetCraftWindows()
    {
        _routeCache.Routes ??= BuildRoutes(DiscoverRoutes());
        return _routeCache.Routes.Values.Select(route => route.Window)
            .Append(ResearchExperimentWindow)
            .Distinct()
            .ToList();
    }

    /// <inheritdoc />
    public bool IsAnyCraftWindowOpen(uint player) =>
        GetCraftWindows().Any(window => IsWindowOpen(player, window));

    /// <inheritdoc />
    public IReadOnlyDictionary<SkillType, CraftMinigameRoute> BuildRoutes(IEnumerable<ICraftMinigameRoute> declarations)
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

    /// <summary>Whether the player has the window open. Gui is not converted yet.</summary>
    protected virtual bool IsWindowOpen(uint player, GuiWindowType window) => _gui.IsWindowOpen(player, window);

    private static IEnumerable<ICraftMinigameRoute> DiscoverRoutes() =>
        typeof(ICraftMinigameRoute).Assembly.GetTypes()
            .Where(type => typeof(ICraftMinigameRoute).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .Select(type => (ICraftMinigameRoute)Activator.CreateInstance(type))
            .ToList();
}
