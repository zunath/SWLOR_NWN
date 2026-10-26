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
    public void ASiteOpenedForItsScavenger_IsHeldForThemUntilTheyCloseIt()
    {
        var source = Source("Feature", "ScavengePoint.cs");
        var onOpened = Method(source, "public static void OnOpened()");
        var onClosed = Method(source, "public static void OnClosed()");
        var world = Source("Service", "ScavengeService", "NwnScavengeWorld.cs");

        world.Should().Contain("SetLocalObject(site, OpeningVariable, player);");
        onOpened.Should().Contain("GetLocalObject(placeable, NwnScavengeWorld.OpeningVariable)");
        onOpened.Should().Contain("if (holder == user)");
        onOpened.Should().Contain("ScavengeLifecycle.SiteHeldMessage");
        onOpened.Should().NotContain("DeleteLocalObject(placeable, NwnScavengeWorld.OpeningVariable)", "opening it does not end the hold; the scavenger is still taking what it gave");
        onOpened.Should().NotContain("GetLocalBool(placeable, NwnScavengeWorld.OpeningVariable)", "the hold is on a player, not a flag anyone can use");
        onClosed.Should().Contain("GetLocalObject(placeable, NwnScavengeWorld.OpeningVariable) == user");
        onClosed.Should().Contain("DeleteLocalObject(placeable, NwnScavengeWorld.OpeningVariable);");
    }

    [Test]
    public void AHoldThatIsNeverReleasedLapses()
    {
        var world = Source("Service", "ScavengeService", "NwnScavengeWorld.cs");

        world.Should().Contain("DeleteLocalObject(site, OpeningVariable);");
        world.Should().Contain("(float)animationSeconds + OpeningHoldSeconds");
        world.Should().Contain("GetLocalObject(site, OpeningVariable) != player");
    }

    [Test]
    public void AHoldDoesNotLapseWhileItsScavengerHasTheSiteOpen_AndEndsWhenTheyLeaveOrClose()
    {
        var world = Source("Service", "ScavengeService", "NwnScavengeWorld.cs");
        var onOpened = Method(Source("Feature", "ScavengePoint.cs"), "public static void OnOpened()");
        var onClosed = Method(Source("Feature", "ScavengePoint.cs"), "public static void OnClosed()");
        var lapse = Method(world, "private static void ScheduleHoldLapse(");

        onOpened.Should().Contain("SetLocalBool(placeable, NwnScavengeWorld.HolderOpenedVariable, true);", "opening the site is recorded so the hold outlives the first minute");
        lapse.Should().Contain("GetLocalBool(site, HolderOpenedVariable)");
        lapse.Should().Contain("ScheduleHoldLapse(site, player, OpeningHoldSeconds);", "an open site is checked again, not released");
        lapse.Should().Contain("GetIsObjectValid(player)", "a scavenger who logged out must not hold the site for good");
        lapse.Should().Contain("GetDistanceBetween(player, site) <= ScavengeLifecycle.MaxDistance", "a scavenger who walked away must not hold the site for good");
        lapse.IndexOf("ScheduleHoldLapse(site, player, OpeningHoldSeconds);", StringComparison.Ordinal)
            .Should().BeLessThan(lapse.IndexOf("DeleteLocalObject(site, OpeningVariable);", StringComparison.Ordinal));
        onClosed.Should().Contain("DeleteLocalBool(placeable, NwnScavengeWorld.HolderOpenedVariable);");
        world.Should().Contain("DeleteLocalBool(site, HolderOpenedVariable);", "a new hold starts unopened");
    }

    [Test]
    public void ClosingASiteWhoseBoardIsBeingPlayed_NeverRemovesIt_HoweverManyViewersWereTurnedAway()
    {
        var source = Source("Feature", "ScavengePoint.cs");
        var onClosed = Method(source, "public static void OnClosed()");
        var resolve = Method(source, "public static void ResolveClosedScavengePoint(uint placeable)");

        onClosed.Should().Contain("ResolveClosedScavengePoint(placeable)");
        resolve.Should().Contain("ScavengeSessions.IsInUse(placeable)");
        resolve.IndexOf("ScavengeSessions.IsInUse(placeable)", StringComparison.Ordinal)
            .Should().BeLessThan(resolve.IndexOf("GetFirstItemInInventory(placeable)", StringComparison.Ordinal), "the guard comes before the empty-inventory removal");
    }

    [Test]
    public void FindsKeptOnAScavengersRecord_AreReturnedAtLogin_AndWhenTheyRespawn()
    {
        var source = Source("Feature", "ScavengePoint.cs");
        var login = Method(source, "public static void ReturnPendingSearchFinds()");
        var respawn = Method(source, "public static void ReturnPendingSearchFindsOnRespawn()");

        login.Should().Contain("NwnScavengeWorld.Instance.ReturnPendingFinds(player)");
        respawn.Should().Contain("NwnScavengeWorld.Instance.ReturnPendingFinds(player)");
        source.Should().Contain("[NWNEventHandler(ScriptName.OnModuleRespawn)]");
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
