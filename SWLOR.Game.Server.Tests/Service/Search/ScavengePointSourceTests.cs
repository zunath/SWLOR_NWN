using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service.Search;

/// <summary>
/// Pins the event handlers of a scavenge site, which run in the engine and cannot be played here: who may open a site, and when closing
/// one removes it.
/// </summary>
public class ScavengePointSourceTests
{
    private static string Source(params string[] relative) =>
        File.ReadAllText(Path.Combine(new[] { TestRepository.FindRoot(), "SWLOR.Game.Server" }.Concat(relative).ToArray()));

    private static string Method(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, signature);
        var open = source.IndexOf('{', start);
        var depth = 0;
        for (var index = open; index < source.Length; index++)
        {
            if (source[index] == '{')
                depth++;
            else if (source[index] == '}' && --depth == 0)
                return source[start..(index + 1)];
        }

        throw new InvalidOperationException(signature);
    }

    [Test]
    public void ASiteOpenedForItsScavenger_IsHeldForThemUntilTheyOpenIt()
    {
        var onOpened = Method(Source("Feature", "ScavengePoint.cs"), "public static void OnOpened()");
        var world = Source("Service", "ScavengeService", "NwnScavengeWorld.cs");

        world.Should().Contain("SetLocalObject(site, OpeningVariable, player);");
        onOpened.Should().Contain("GetLocalObject(placeable, NwnScavengeWorld.OpeningVariable)");
        onOpened.Should().Contain("if (holder == user)");
        onOpened.Should().Contain("DeleteLocalObject(placeable, NwnScavengeWorld.OpeningVariable);");
        onOpened.Should().Contain("ScavengeLifecycle.SiteHeldMessage");
        onOpened.Should().NotContain("GetLocalBool(placeable, NwnScavengeWorld.OpeningVariable)", "the hold is on a player, not a flag anyone can use");
    }

    [Test]
    public void AHoldThatIsNeverClaimedLapses()
    {
        var world = Source("Service", "ScavengeService", "NwnScavengeWorld.cs");

        world.Should().Contain("DeleteLocalObject(site, OpeningVariable);");
        world.Should().Contain("(float)animationSeconds + OpeningHoldSeconds");
    }

    [Test]
    public void ClosingASiteWhoseBoardIsBeingPlayed_NeverRemovesIt_HoweverManyViewersWereTurnedAway()
    {
        var onClosed = Method(Source("Feature", "ScavengePoint.cs"), "public static void OnClosed()");

        onClosed.Should().Contain("ScavengeSessions.IsInUse(placeable)");
        onClosed.IndexOf("ScavengeSessions.IsInUse(placeable)", StringComparison.Ordinal)
            .Should().BeLessThan(onClosed.IndexOf("GetFirstItemInInventory(placeable)", StringComparison.Ordinal), "the guard comes before the empty-inventory removal");
    }

    [Test]
    public void ASiteIsOnlyOpenedToAPlayerWhenNobodyIsSearchingIt_AndItWasNotSearchedBefore()
    {
        var onOpened = Method(Source("Feature", "ScavengePoint.cs"), "public static void OnOpened()");

        onOpened.IndexOf("ScavengeSessions.GetActiveAt(placeable)", StringComparison.Ordinal)
            .Should().BeLessThan(onOpened.IndexOf("NwnScavengeWorld.SearchedVariable", StringComparison.Ordinal));
        onOpened.IndexOf("NwnScavengeWorld.SearchedVariable", StringComparison.Ordinal)
            .Should().BeLessThan(onOpened.IndexOf("ScavengeSessions.Start(request)", StringComparison.Ordinal));
    }
}
