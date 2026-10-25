using System.IO;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

public class HoloComHologramFactionTests
{
    // The engine treats a reputation of 10 or less as hostile.
    private const int HostileReputation = 10;

    [Test]
    public void HologramFaction_IsTheModuleNeutralFaction()
    {
        var factions = (JArray)ReadReputationFile()["FactionList"]!["value"]!;

        factions.Count.Should().BeGreaterThan(HoloCom.HologramFactionId);
        ((string)factions[HoloCom.HologramFactionId]["FactionName"]!["value"]!).Should().Be("Neutral");
    }

    [Test]
    public void HologramFaction_IsNeitherHostileToNorHatedByAnyFaction()
    {
        var reputation = ReadReputationFile();
        var factionCount = ((JArray)reputation["FactionList"]!["value"]!).Count;
        // FactionRep is how FactionID2 feels about FactionID1.
        var entries = ((JArray)reputation["RepList"]!["value"]!)
            .Select(entry => (
                Subject: (int)entry["FactionID1"]!["value"]!,
                Observer: (int)entry["FactionID2"]!["value"]!,
                Rep: (int)entry["FactionRep"]!["value"]!))
            .ToList();

        var id = HoloCom.HologramFactionId;
        for (var other = 0; other < factionCount; other++)
        {
            if (other == id)
                continue;

            entries.Should().Contain(entry => entry.Subject == id && entry.Observer == other && entry.Rep > HostileReputation,
                $"faction {other} must not be hostile to the hologram faction");
            entries.Should().Contain(entry => entry.Subject == other && entry.Observer == id && entry.Rep > HostileReputation,
                $"the hologram faction must not be hostile to faction {other}");
        }
    }

    private static JObject ReadReputationFile()
    {
        return JObject.Parse(File.ReadAllText(Path.Combine(FindRepositoryRoot().FullName, "Module", "fac", "repute.fac.json")));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the repository root.");
    }
}
