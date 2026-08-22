using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AttributeService;

namespace SWLOR.Game.Server.Tests.Feature;

public class CombatUpgradeTlkConsistencyTests
{
    private static readonly char[] WhiteSpaceSeparators = { ' ', '\t', '\r', '\n' };

    private static readonly int[] BackgroundDescriptionTlkIds =
    {
        80891,
        80892,
        80893,
        80894,
        80895,
        80896,
        80897,
        80898,
        80899,
        80900,
        80901,
        81040
    };

    [Test]
    public void AttributeDescriptions_MatchImplementedCombatFormulas()
    {
        AttributeDescription.MightSummary.Should().Be(
            "Improves melee weapon damage, maximum STM, STM regeneration, and carrying capacity.");
        AttributeDescription.PerceptionSummary.Should().Be(
            "Improves melee accuracy, ranged weapon damage, critical hit chance, and detection.");
        AttributeDescription.VitalitySummary.Should().Be(
            "Improves maximum HP, HP regeneration, Physical Defense, and resistance to critical hits.");
        AttributeDescription.WillpowerSummary.Should().Be(
            "Improves Force attack, Force Defense, maximum FP, FP regeneration, First Aid, and detection.");
        AttributeDescription.AgilitySummary.Should().Be(
            "Improves ranged accuracy, Evasion, Stealth, and ship combat effectiveness.");
        AttributeDescription.SocialSummary.Should().Be(
            "Improves XP gain and Leadership capabilities.");

        AttributeDescription.MightDetails.Should().Contain("Increases maximum STM.");
        AttributeDescription.PerceptionDetails.Should().Contain("Improves critical hit chance.");
        AttributeDescription.PerceptionDetails.Should().NotContain("First Aid");
        AttributeDescription.VitalityDetails.Should().Contain("Reduces enemy critical hit chance against you.");
        AttributeDescription.WillpowerDetails.Should().Contain("Improves detection.");
        AttributeDescription.WillpowerDetails.Should().Contain("First Aid");
        AttributeDescription.AgilityDetails.Should().Contain("Improves Stealth.");

        var allDescriptions = string.Join('\n',
            AttributeDescription.BuildOverview(),
            AttributeDescription.MightDetails,
            AttributeDescription.PerceptionDetails,
            AttributeDescription.VitalityDetails,
            AttributeDescription.WillpowerDetails,
            AttributeDescription.AgilityDetails,
            AttributeDescription.SocialDetails);

        allDescriptions.Should().NotContain("finesse weapons");
        allDescriptions.Should().NotContain("natural HP/FP/STM");
        allDescriptions.Should().NotContain("Agility improves ranged accuracy, evasion, and max stamina");
    }

    [Test]
    public void BaseTlkOverrides_UseCanonicalCombatUpgradeTerminology()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "TlkOverrides.cs"));

        source.Should().Contain("SetTlkOverride(330, \"Increased Agility By\")");
        source.Should().Contain("SetTlkOverride(477, \"Agility Information\")");
        source.Should().Contain("SetTlkOverride(460, AttributeDescription.PerceptionDetails)");
        source.Should().Contain("SetTlkOverride(1460, \"Accuracy and Damage Penalty:");
        source.Should().Contain("Accuracy vs. Racial Group:");
        source.Should().NotContain("Accuracy vs. Monster Type:");
    }

    [Test]
    public void CustomTlk_UsesCombatUpgradeAttributeAndAccuracyNames()
    {
        var entries = ReadCustomTlkEntries();
        var expectedComponentNames = new Dictionary<int, string>
        {
            [80783] = "Accuracy Up",
            [81021] = "Might Up",
            [81022] = "Perception Up",
            [81023] = "Vitality Up",
            [81024] = "Willpower Up",
            [81025] = "Agility Up",
            [81026] = "Social Up",
            [81027] = "Accuracy Up"
        };

        foreach (var (id, expected) in expectedComponentNames)
            entries[id].Should().Be(expected, $"custom TLK {id} is displayed by iprp_compbon.2da");

        foreach (var id in BackgroundDescriptionTlkIds)
        {
            entries[id].Should().Contain("Accuracy");
            entries[id].Should().NotContain("Base Attack Bonus");
        }

        entries[81040].Should().Contain("Mando'a language");
        entries[81316].Should().Be(
            "Standard characters cannot train the Force skill or use Force powers, lightsabers, or saberstaffs. They can train Devices and access Standard-only Devices and Espionage perks.");
        entries[81319].Should().Be(
            "Force Sensitive characters can train the Force skill and access Force powers, lightsaber perks, and saberstaff perks. They cannot train Devices or access Standard-only Devices and Espionage perks.");
    }

    [Test]
    public void Production2das_CustomTlkReferencesResolveOrUseTheDocumentedBlankBiography()
    {
        const int customTlkOffset = 16777216;
        const int intentionallyBlankBiographyId = 80831;
        var root = FindRepositoryRoot();
        var entries = ReadCustomTlkEntries();
        var twoDaDirectory = Path.Combine(root.FullName, "SWLOR_Haks", "sw_2da");
        var missingReferences = new Dictionary<int, HashSet<string>>();

        foreach (var path in Directory.GetFiles(twoDaDirectory, "*.2da"))
        {
            if (Path.GetFileName(path).EndsWith(" past.2da", StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var token in File.ReadAllText(path).Split(WhiteSpaceSeparators, StringSplitOptions.RemoveEmptyEntries))
            {
                if (token.Length != 8 || token[0] != '1' ||
                    !int.TryParse(token, out var strRef) || strRef < customTlkOffset)
                    continue;

                var id = strRef - customTlkOffset;
                if (entries.ContainsKey(id))
                    continue;

                if (!missingReferences.TryGetValue(id, out var files))
                {
                    files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    missingReferences[id] = files;
                }

                files.Add(Path.GetFileName(path));
            }
        }

        missingReferences.Keys.Should().Equal(intentionallyBlankBiographyId);
        missingReferences[intentionallyBlankBiographyId].Should().Equal("racialtypes.2da");
        entries[80830].Should().Contain("should be left empty");
        entries[80832].Should().Contain("should be left empty");
    }

    private static IReadOnlyDictionary<int, string> ReadCustomTlkEntries()
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(root.FullName, "SWLOR_Haks", "sw_tlk", "sw_tlk.tlk.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));

        return document.RootElement
            .GetProperty("entries")
            .EnumerateArray()
            .ToDictionary(
                entry => entry.GetProperty("id").GetInt32(),
                entry => entry.GetProperty("text").GetString() ?? string.Empty);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(
                    directory.FullName,
                    "SWLOR_Haks",
                    "sw_tlk",
                    "sw_tlk.tlk.json")))
                return directory;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR repository root.");
    }
}
