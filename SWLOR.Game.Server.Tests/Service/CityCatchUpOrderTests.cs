using System;
using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Work that builds up while nobody watches is settled before a change takes effect, so the change applies only from then on.
/// </summary>
[TestFixture]
public class CityCatchUpOrderTests
{
    [Test]
    public void APredatorRaid_LetsEachCorralsFarmhandFinishItsEarlierWorkFirst()
    {
        var corrals = ReadServer("Service", "Corrals.cs");
        var method = Between(corrals, "public static int ApplyPredatorPenalty(", "public static void NotifyOwnersSafe(");

        Index(method, "Farmhands.CatchUp(corralId);").Should().BeLessThan(Index(method, "LivestockRules.ApplyPenalty(animal, now);"),
            "a collection due before the raid is made before the penalty stops products");
    }

    [Test]
    public void AnElectionThatClearsAPause_BillsTheEdictsItLetsResume()
    {
        var property = ReadServer("Service", "Property.cs");
        var conclude = Between(property, "private static void ConcludeElection(", "private static void BroadcastElectionResult(");

        var billedBefore = Index(conclude, "var billedBefore = Edict.GetBilledTypes(city);");
        billedBefore.Should().BeLessThan(Index(conclude, "city.NoConfidenceSinceUtc = null;"));
        billedBefore.Should().BeLessThan(Index(conclude, "city.NeglectSinceUtc = null;"));
        Index(conclude, "IndustrialCharter.SettleCity(city);").Should().BeLessThan(Index(conclude, "city.NeglectSinceUtc = null;"));

        var enact = Index(conclude, "Edict.EnactPlatform(city, winnerPlatform, now)");
        var resumed = Index(conclude, "Edict.AddResumedBilling(city, billedBefore, now);");
        resumed.Should().BeGreaterThan(enact, "a platform replaces the Edicts and bills its own part-week; without one the resumed Edicts pay");
        conclude.LastIndexOf("if (platformEnactment == null)", resumed, StringComparison.Ordinal).Should().BeGreaterThan(enact,
            "only a city whose Edicts were not replaced by a platform bills the resumed ones");
    }

    private static string ReadServer(params string[] parts)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the repository root holds SWLOR.Game.Server.sln");
        var path = Path.Combine(directory!.FullName, "SWLOR.Game.Server");
        foreach (var part in parts)
            path = Path.Combine(path, part);

        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    private static int Index(string source, string text)
    {
        var index = source.IndexOf(text, StringComparison.Ordinal);
        index.Should().BeGreaterThanOrEqualTo(0, $"the source should contain: {text}");
        return index;
    }

    private static string Between(string source, string start, string end)
    {
        var from = Index(source, start);
        var to = source.IndexOf(end, from + start.Length, StringComparison.Ordinal);
        to.Should().BeGreaterThan(from, $"the source should contain {end} after {start}");
        return source[from..to];
    }
}
