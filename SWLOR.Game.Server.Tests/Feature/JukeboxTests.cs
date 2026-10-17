using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.MusicService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Feature;

public class JukeboxTests
{
    private static readonly HashSet<int> JukeboxAppearances = [6937, 7639, 31806, 31807, 31808, 31809, 31810];

    [TestCase(PropertyType.House)]
    [TestCase(PropertyType.Apartment)]
    [TestCase(PropertyType.Starship)]
    [TestCase(PropertyType.Cantina)]
    public void InteriorJukebox_UsesCurrentInteriorOwnerWithoutFollowingTheCity(PropertyType interiorType)
    {
        var jukebox = new WorldProperty
        {
            PropertyType = PropertyType.Structure,
            OwnerPlayerId = "previous-owner",
            ParentPropertyId = "interior"
        };
        var interior = new WorldProperty
        {
            PropertyType = interiorType,
            OwnerPlayerId = "house-owner",
            ParentPropertyId = "building"
        };
        var building = new WorldProperty
        {
            PropertyType = PropertyType.Structure,
            OwnerPlayerId = "mayor",
            ParentPropertyId = "city"
        };
        var city = new WorldProperty { PropertyType = PropertyType.City, OwnerPlayerId = "mayor" };
        var properties = new Dictionary<string, WorldProperty>
        {
            [jukebox.Id] = jukebox, ["interior"] = interior, ["building"] = building, ["city"] = city
        };
        var reads = new List<string>();

        ResolveOwner(jukebox.Id, id => { reads.Add(id); return properties[id]; }).Should().Be("house-owner");
        reads.Should().Equal(jukebox.Id, "interior");

        interior.OwnerPlayerId = "new-house-owner";
        ResolveOwner(jukebox.Id, id => properties[id]).Should().Be("new-house-owner");
    }

    [Test]
    public void CityJukebox_UsesCurrentMayorInsteadOfItsRecordedOwner()
    {
        var jukebox = new WorldProperty
        {
            PropertyType = PropertyType.Structure,
            OwnerPlayerId = "previous-mayor",
            ParentPropertyId = "city"
        };
        var city = new WorldProperty
        {
            PropertyType = PropertyType.City,
            OwnerPlayerId = "current-mayor",
            ParentPropertyId = "area-resref"
        };
        var properties = new Dictionary<string, WorldProperty> { [jukebox.Id] = jukebox, ["city"] = city };

        ResolveOwner(jukebox.Id, id => properties[id]).Should().Be("current-mayor");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void UnownedWorldJukebox_HasNoCollectionOwner(string propertyId)
    {
        ResolveOwner(propertyId, _ => throw new AssertionException("An unowned jukebox must not query properties."))
            .Should().BeEmpty();
    }

    [Test]
    public void MissingJukeboxProperty_HasNoCollectionOwner()
    {
        ResolveOwner("missing-property", _ => null!).Should().BeEmpty();
    }

    [Test]
    public void UnownedInterior_DoesNotFallBackToTheMayor()
    {
        var jukebox = new WorldProperty { PropertyType = PropertyType.Structure, ParentPropertyId = "interior" };
        var interior = new WorldProperty { PropertyType = PropertyType.House, ParentPropertyId = "building" };
        var properties = new Dictionary<string, WorldProperty> { [jukebox.Id] = jukebox, ["interior"] = interior };

        ResolveOwner(jukebox.Id, id => properties[id]).Should().BeEmpty();
    }

    [TestCase("utp")]
    [TestCase("git")]
    public void AllJukeboxes_AreIndestructibleCollectionPickers(string resourceType)
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;
        root.Should().NotBeNull();
        var count = 0;

        foreach (var path in Directory.EnumerateFiles(Path.Combine(root!.FullName, "Module", resourceType), "*.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var objects = resourceType == "utp"
                ? new[] { document.RootElement }
                : document.RootElement.TryGetProperty("Placeable List", out var placeables)
                    ? placeables.GetProperty("value").EnumerateArray().ToArray()
                    : Array.Empty<JsonElement>();
            foreach (var placeable in objects.Where(p => JukeboxAppearances.Contains(p.GetProperty("Appearance").GetProperty("value").GetInt32())))
            {
                var context = $"{Path.GetFileName(path)}: {placeable.GetProperty("Tag").GetProperty("value").GetString()}";
                placeable.GetProperty("OnUsed").GetProperty("value").GetString().Should().Be(ScriptName.OnJukeboxUsed, context);
                placeable.GetProperty("Static").GetProperty("value").GetInt32().Should().Be(0, context);
                placeable.GetProperty("Useable").GetProperty("value").GetInt32().Should().Be(1, context);
                placeable.GetProperty("Plot").GetProperty("value").GetInt32().Should().Be(1, context);
                count++;
            }
        }

        count.Should().BeGreaterThan(0);
    }

    [TestCase(MusicPickerMode.Jukebox, false, 1)]
    [TestCase(MusicPickerMode.Jukebox, true, 1)]
    [TestCase(MusicPickerMode.Personal, false, 1)]
    [TestCase(MusicPickerMode.Personal, true, 2)]
    public void UncollectedSongs_CannotBeRevealedOnAJukebox(MusicPickerMode mode, bool showUncollected, int expectedCount)
    {
        var model = new MusicPickerViewModel();
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        typeof(MusicPickerViewModel).GetField("_mode", flags)!.SetValue(model, mode);
        typeof(MusicPickerViewModel).GetField("_entries", flags)!.SetValue(model, new[]
        {
            new MusicPickerEntry(1, "Collected song", "Cantina", true),
            new MusicPickerEntry(2, "Locked song", "Cantina", false)
        });

        model.ShowUncollected = showUncollected;
        model.SongNames.Should().HaveCount(expectedCount);
        model.SongNames.Should().Contain("Collected song");
        if (mode == MusicPickerMode.Jukebox)
        {
            model.PlayEnabled.Should().OnlyContain(enabled => enabled);
            model.SearchText = "Locked";
            model.SongNames.Should().BeEmpty("search cannot reveal a song the user cannot play");
        }
    }

    [Test]
    public void JukeboxLayout_HasNoUncollectedSongToggle()
    {
        using var validation = GuiLayoutValidator.BeginValidationOnlyBuild();
        var definition = new MusicPickerDefinition();
        var window = definition.BuildWindow();
        window.LayoutFindings.Should().BeEmpty();
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var builder = typeof(MusicPickerDefinition).GetField("_builder", flags)!.GetValue(definition)!;
        var authored = (GuiWindow<MusicPickerViewModel>)builder.GetType().GetField("_activeWindow", flags)!.GetValue(builder)!;
        Walk(authored.PartialViews[MusicPickerViewModel.AreaContentPartial])
            .Should().NotContain(widget => widget is GuiCheckBox<MusicPickerViewModel>);
        Walk(authored.PartialViews[MusicPickerViewModel.PersonalContentPartial])
            .Should().ContainSingle(widget => widget is GuiCheckBox<MusicPickerViewModel>);
    }

    private static string ResolveOwner(string propertyId, Func<string, WorldProperty> getProperty) =>
        (string)typeof(Server.Service.Music)
            .GetMethod("ResolveJukeboxOwnerId", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { propertyId, getProperty })!;

    private static IEnumerable<IGuiWidget> Walk(IGuiWidget widget) =>
        new[] { widget }.Concat(widget.Elements.SelectMany(Walk));
}
