using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Domain.Crafting;

/// <summary>The route from a crafting skill to its window, with the open windows answered by the test.</summary>
[TestFixture]
public class CraftMinigameRoutingServiceTests
{
    private sealed record Declaration(SkillType Skill, GuiWindowType Window, CraftMinigameType Minigame) : ICraftMinigameRoute;

    private sealed class TestRouting : CraftMinigameRoutingService
    {
        public HashSet<GuiWindowType> Open { get; } = new();

        protected override bool IsWindowOpen(uint player, GuiWindowType window) => Open.Contains(window);
    }

    private static IReadOnlyList<ICraftMinigameRoute> Registered() =>
        typeof(ICraftMinigameRoute).Assembly.GetTypes()
            .Where(type => typeof(ICraftMinigameRoute).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .Select(type => (ICraftMinigameRoute)Activator.CreateInstance(type)!)
            .ToList();

    [Test]
    public void Every_registered_route_is_found_by_its_skill()
    {
        var service = new TestRouting();

        foreach (var declaration in Registered())
        {
            service.TryGetRoute(declaration.Skill, out var route).Should().BeTrue();
            route.Window.Should().Be(declaration.Window);
            route.Minigame.Should().Be(declaration.Minigame);
            service.GetRoute(declaration.Skill).Should().BeEquivalentTo(route);
            service.GetWindow(declaration.Skill).Should().Be(declaration.Window);
        }
    }

    [Test]
    public void A_skill_that_crafts_nothing_has_no_route_and_asking_for_it_is_an_error()
    {
        var service = new TestRouting();

        service.TryGetRoute(SkillType.Invalid, out _).Should().BeFalse();
        service.Invoking(s => s.GetRoute(SkillType.Invalid)).Should().Throw<InvalidOperationException>()
            .WithMessage("*has no crafting minigame*");
        service.Invoking(s => s.GetWindow(SkillType.Invalid)).Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void The_craft_windows_are_every_route_window_and_the_research_experiment_once_each()
    {
        var windows = new TestRouting().GetCraftWindows();

        windows.Should().OnlyHaveUniqueItems();
        windows.Should().Contain(CraftMinigameRoutingService.ResearchExperimentWindow);
        windows.Should().Contain(Registered().Select(route => route.Window).Distinct());
        windows.Count.Should().Be(Registered().Select(route => route.Window).Append(CraftMinigameRoutingService.ResearchExperimentWindow).Distinct().Count());
    }

    [Test]
    public void IsAnyCraftWindowOpen_is_true_for_a_crafting_window_or_the_research_experiment_only()
    {
        var service = new TestRouting();
        service.IsAnyCraftWindowOpen(1).Should().BeFalse();

        service.Open.Add(Registered()[0].Window);
        service.IsAnyCraftWindowOpen(1).Should().BeTrue();

        service.Open.Clear();
        service.Open.Add(CraftMinigameRoutingService.ResearchExperimentWindow);
        service.IsAnyCraftWindowOpen(1).Should().BeTrue();
    }

    [Test]
    public void BuildRoutes_maps_each_declaration_and_refuses_two_windows_for_one_skill()
    {
        var service = new TestRouting();
        var one = new Declaration(SkillType.Smithery, GuiWindowType.ResearchExperiment, CraftMinigameType.Forge);
        var two = new Declaration(SkillType.Engineering, GuiWindowType.Research, CraftMinigameType.Circuit);

        var routes = service.BuildRoutes(new ICraftMinigameRoute[] { one, two });

        routes.Keys.Should().BeEquivalentTo(new[] { SkillType.Smithery, SkillType.Engineering });
        routes[SkillType.Smithery].Window.Should().Be(GuiWindowType.ResearchExperiment);
        service.Invoking(s => s.BuildRoutes(new ICraftMinigameRoute[] { one, one }))
            .Should().Throw<InvalidOperationException>().WithMessage("*More than one crafting window*");
    }

    [Test]
    public void Each_instance_finds_its_routes_on_first_use_and_keeps_them()
    {
        var first = new TestRouting();
        var second = new TestRouting();

        first.GetCraftWindows().Should().Equal(first.GetCraftWindows());
        second.GetCraftWindows().Should().BeEquivalentTo(first.GetCraftWindows());
    }
}
