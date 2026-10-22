using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>Covers the routing from a crafting skill to the window that crafts it.</summary>
public class CraftMinigameRoutingTests
{
    private sealed record Declaration(SkillType Skill, GuiWindowType Window, CraftMinigameType Minigame) : ICraftMinigameRoute;

    private static IEnumerable<SkillType> CraftingSkills() =>
        Enum.GetValues<SkillType>().Where(skill =>
        {
            var attribute = skill.GetAttribute<SkillType, SkillAttribute>();
            return attribute is { IsActive: true, IsShownInCraftMenu: true };
        });

    private static IReadOnlyDictionary<SkillType, ICraftMinigameRoute> RegisteredRoutes() =>
        typeof(ICraftMinigameRoute).Assembly.GetTypes()
            .Where(type => typeof(ICraftMinigameRoute).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .Select(type => (ICraftMinigameRoute)Activator.CreateInstance(type)!)
            .ToDictionary(route => route.Skill);

    [Test]
    public void EveryCraftingSkillUsesTheOriginalWindowUntilItsMinigameLands()
    {
        var skills = CraftingSkills().ToList();
        skills.Should().NotBeEmpty();
        var registered = RegisteredRoutes();

        foreach (var skill in skills.Where(skill => !registered.ContainsKey(skill)))
        {
            var route = CraftMinigameRouting.GetRoute(skill);

            route.Window.Should().Be(GuiWindowType.Craft, $"{skill} has no minigame yet");
            route.IsLegacy.Should().BeTrue();
            route.Minigame.Should().BeNull();
            CraftMinigameRouting.GetWindow(skill).Should().Be(GuiWindowType.Craft);
        }
    }

    [Test]
    public void ASkillWithAMinigameOpensItsOwnWindow()
    {
        foreach (var (skill, declaration) in RegisteredRoutes())
        {
            var route = CraftMinigameRouting.GetRoute(skill);

            route.IsLegacy.Should().BeFalse();
            route.Window.Should().Be(declaration.Window);
            route.Minigame.Should().Be(declaration.Minigame);
            CraftMinigameRouting.GetWindow(skill).Should().Be(declaration.Window);
        }
    }

    [Test]
    public void EspionageCraftsInTheReactionWindow()
    {
        var route = CraftMinigameRouting.GetRoute(SkillType.Espionage);

        route.Window.Should().Be(GuiWindowType.Reaction);
        route.Minigame.Should().Be(CraftMinigameType.Reaction);
        route.IsLegacy.Should().BeFalse();
    }

    [Test]
    public void SmitheryCraftsInThePlasmaForge()
    {
        var route = CraftMinigameRouting.GetRoute(SkillType.Smithery);

        route.Window.Should().Be(GuiWindowType.Forge);
        route.Minigame.Should().Be(CraftMinigameType.Forge);
        route.IsLegacy.Should().BeFalse();
    }

    [Test]
    public void AgricultureCooksInTheKitchenWindow()
    {
        var route = CraftMinigameRouting.GetRoute(SkillType.Agriculture);

        route.Window.Should().Be(GuiWindowType.Kitchen);
        route.Minigame.Should().Be(CraftMinigameType.Kitchen);
        route.IsLegacy.Should().BeFalse();
    }

    [Test]
    public void TheCraftWindowsAreTheOriginalAndEveryRoutedMinigameWindow()
    {
        var windows = CraftMinigameRouting.GetCraftWindows();

        windows.Should().Contain(GuiWindowType.Craft);
        windows.Should().Contain(RegisteredRoutes().Values.Select(route => route.Window));
        windows.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void EquippingIsBlockedWhileAnyRoutedCraftWindowIsOpen()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        directory.Should().NotBeNull();

        var source = File.ReadAllText(Path.Combine(directory!.FullName, "SWLOR.Game.Server", "Service", "Item.cs"));

        source.Should().Contain("CraftMinigameRouting.IsAnyCraftWindowOpen(creature)");
        source.Should().NotContain("Gui.IsWindowOpen(creature, GuiWindowType.Craft)",
            "a minigame's window must block equipping like the original one");
    }

    [Test]
    public void ARegisteredRouteReplacesTheOriginalWindowForItsSkillOnly()
    {
        var routes = CraftMinigameRouting.BuildRoutes(new ICraftMinigameRoute[]
        {
            new Declaration(SkillType.Smithery, GuiWindowType.DebugEnmity, CraftMinigameType.Forge)
        });

        routes[SkillType.Smithery].Window.Should().Be(GuiWindowType.DebugEnmity);
        routes[SkillType.Smithery].Minigame.Should().Be(CraftMinigameType.Forge);
        routes[SkillType.Smithery].IsLegacy.Should().BeFalse();
        routes.Should().NotContainKey(SkillType.Engineering);
    }

    [Test]
    public void TwoWindowsForOneSkillIsAnError()
    {
        var act = () => CraftMinigameRouting.BuildRoutes(new ICraftMinigameRoute[]
        {
            new Declaration(SkillType.Smithery, GuiWindowType.DebugEnmity, CraftMinigameType.Forge),
            new Declaration(SkillType.Smithery, GuiWindowType.DebugNuiGallery, CraftMinigameType.Forge)
        });

        act.Should().Throw<InvalidOperationException>().WithMessage("*Smithery*");
    }

    [Test]
    public void EveryRouteInTheGameNamesADistinctSkillAndAnExistingWindow()
    {
        var declarations = typeof(ICraftMinigameRoute).Assembly.GetTypes()
            .Where(type => typeof(ICraftMinigameRoute).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .Select(type => (ICraftMinigameRoute)Activator.CreateInstance(type)!)
            .ToList();

        var routes = CraftMinigameRouting.BuildRoutes(declarations);

        routes.Values.Select(route => route.Window).Should().OnlyContain(window => Enum.IsDefined(window));
    }
}
