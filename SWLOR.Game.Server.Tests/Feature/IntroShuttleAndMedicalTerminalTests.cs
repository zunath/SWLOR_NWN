using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;

namespace SWLOR.Game.Server.Tests.Feature;

public sealed class IntroShuttleAndMedicalTerminalTests
{
    private const int FemaleRodianPortraitId = 4385;
    private const string MedicalPortraitResref = "p_256x128_medic1";

    [Test]
    public void EveryFemaleRodianMusicianUsesTheRenderableRodianPortrait()
    {
        var root = FindRepositoryRoot().FullName;
        var blueprint = JObject.Parse(File.ReadAllText(Path.Combine(
            root,
            "Module",
            "utc",
            "femrodianmusic1.utc.json")));
        GetInt(blueprint, "PortraitId").Should().Be(FemaleRodianPortraitId);

        var musicians = EnumerateResources(root, "Creature List")
            .Where(resource => GetString(resource, "TemplateResRef") == "femrodianmusic1")
            .ToArray();

        musicians.Should().NotBeEmpty();
        musicians.Should().OnlyContain(resource =>
            GetInt(resource, "PortraitId") == FemaleRodianPortraitId);

        var introMusician = musicians.Single(resource =>
            GetString(resource, "Conversation") == "start_musician");
        GetString(introMusician, "Tag").Should().Be("FemaleRodianMusician");
    }

    [Test]
    public void EveryMedicalRegistrationTerminalUsesTheSharedPortraitMenu()
    {
        var root = FindRepositoryRoot().FullName;
        var terminals = EnumerateResources(root, "Placeable List")
            .Where(resource => GetLocalizedString(resource, "LocName") ==
                               "Medical Registration Terminal")
            .ToArray();

        terminals.Should().NotBeEmpty();
        terminals.Should().OnlyContain(resource =>
            GetString(resource, "TemplateResRef") == "cloning_reg" &&
            GetConversationMenu(resource) == nameof(MedicalRegistrationDialog));

        var blueprint = JObject.Parse(File.ReadAllText(Path.Combine(
            root,
            "Module",
            "utp",
            "cloning_reg.utp.json")));
        GetConversationMenu(blueprint).Should().Be(nameof(MedicalRegistrationDialog));

        var menu = new MedicalRegistrationDialog().Build();
        menu.PortraitResref.Should().Be(MedicalPortraitResref);
    }

    [Test]
    public void ShuttleStatusHeader_RendersAsOneCompactNuiTextBlock()
    {
        var method = typeof(ShuttleStatusDialog).GetMethod(
            "BuildStatusHeader",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method.Should().NotBeNull();

        var header = (string)method!.Invoke(null, new object[] { "Dantooine", "5 minutes" })!;
        var blocks = ConversationMarkup.ParseLegacyColors(header, ConversationTextStyle.Normal);

        blocks.Should().ContainSingle(
            "the status labels and values should remain in one flowing styled block");
        blocks[0].Text.Should().Be("Destination: Dantooine\nArriving in: 5 minutes");
        blocks[0].Style.Should().Be(ConversationTextStyle.Custom);
        blocks[0].Color.Should().NotBeNull();
        blocks[0].Color.Red.Should().Be(0);
        blocks[0].Color.Green.Should().Be(255);
        blocks[0].Color.Blue.Should().Be(255);
    }

    private static IEnumerable<JObject> EnumerateResources(string root, string listName)
    {
        var gitDirectory = Path.Combine(root, "Module", "git");
        foreach (var path in Directory.EnumerateFiles(gitDirectory, "*.git.json"))
        {
            var document = JObject.Parse(File.ReadAllText(path));
            foreach (var resource in document[listName]?["value"]?.Children<JObject>() ?? [])
                yield return resource;
        }
    }

    private static string GetConversationMenu(JObject resource) =>
        resource["VarTable"]?["value"]?
            .Children<JObject>()
            .SingleOrDefault(variable => GetString(variable, "Name") == "CONVERSATION")?
            ["Value"]?["value"]?.Value<string>() ?? string.Empty;

    private static string GetLocalizedString(JObject resource, string field) =>
        resource[field]?["value"]?["0"]?.Value<string>() ?? string.Empty;

    private static string GetString(JObject resource, string field) =>
        resource[field]?["value"]?.Value<string>() ?? string.Empty;

    private static int GetInt(JObject resource, string field) =>
        resource[field]?["value"]?.Value<int>() ?? 0;

    private static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null &&
               !Directory.Exists(Path.Combine(current.FullName, "SWLOR.Game.Server")))
        {
            current = current.Parent;
        }

        return current ?? throw new DirectoryNotFoundException("Unable to locate the repository root.");
    }
}
