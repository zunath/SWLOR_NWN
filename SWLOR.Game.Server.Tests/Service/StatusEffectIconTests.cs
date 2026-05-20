using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class StatusEffectIconTests
{
    [Test]
    public void PainSuppressant_UsesGeneratedCustomStatusIcons()
    {
        var rank1 = new PainSuppressant1StatusEffect();
        var rank2 = new PainSuppressant2StatusEffect();

        rank1.Icon.Should().Be(EffectIconType.PainSuppressant1StatusEffect);
        rank2.Icon.Should().Be(EffectIconType.PainSuppressant2StatusEffect);
        ((int)rank1.Icon).Should().Be(315);
        ((int)rank2.Icon).Should().Be(316);
    }

    [Test]
    public void PainSuppressantCustomIconRows_DoNotReplaceFoodOrDash()
    {
        var root = FindRepositoryRoot();
        var rows = Read2da(new FileInfo(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "swlor2_2da",
            "effecticons.2da")));

        rows[130]["Label"].Should().Be("FOOD");
        rows[130]["Icon"].Should().Be("ife_aether_curre");
        rows[131]["Label"].Should().Be("DASH");
        rows[131]["Icon"].Should().Be("ife_sprint");
        rows[315]["Label"].Should().Be("PainSuppressant1");
        rows[315]["Icon"].Should().Be("ief_painsup1");
        rows[316]["Label"].Should().Be("PainSuppressant2");
        rows[316]["Icon"].Should().Be("ief_painsup2");
    }

    [Test]
    public void EffectIconDocumentation_AllowsEffectIconRowsPast255()
    {
        var root = FindRepositoryRoot();
        var effectFunctions = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.NWN.API",
            "NWScript",
            "EffectFunctions.cs"));
        var nwscript = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.NWN.API",
            "NWN",
            "nwscript-8193.37.nss"));

        effectFunctions.Should().Contain("support effecticons.2da rows past 255");
        effectFunctions.Should().Contain("Older clients are simply not sent icons past row 255");
        effectFunctions.Should().NotContain("nIconID is < 1 or > 255");

        nwscript.Should().Contain("effecticons.2da rows past 255 are supported");
        nwscript.Should().Contain("Older clients are simply not sent icons past row 255");
        nwscript.Should().NotContain("nIconID is < 1 or > 255");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("repository root should be discoverable from the test directory");
        return directory!;
    }

    private static Dictionary<int, Dictionary<string, string>> Read2da(FileInfo file)
    {
        var lines = File.ReadAllLines(file.FullName)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        var headers = lines[1].Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
        var rows = new Dictionary<int, Dictionary<string, string>>();

        foreach (var line in lines.Skip(2))
        {
            var parts = line.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < headers.Length + 1 || !int.TryParse(parts[0], out var rowNumber))
                continue;

            var row = new Dictionary<string, string>();
            for (var index = 0; index < headers.Length; index++)
            {
                row[headers[index]] = parts[index + 1];
            }

            rows[rowNumber] = row;
        }

        return rows;
    }
}
