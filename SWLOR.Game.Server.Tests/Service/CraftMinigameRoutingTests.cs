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

    [Test]
    public void EveryCraftingSkillUsesTheOriginalWindowUntilItsMinigameLands()
    {
        var skills = CraftingSkills().ToList();
        skills.Should().NotBeEmpty();

        foreach (var skill in skills)
        {
            var route = CraftMinigameRouting.GetRoute(skill);

            route.Window.Should().Be(GuiWindowType.Craft, $"{skill} has no minigame yet");
            route.IsLegacy.Should().BeTrue();
            route.Minigame.Should().BeNull();
            CraftMinigameRouting.GetWindow(skill).Should().Be(GuiWindowType.Craft);
        }
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
