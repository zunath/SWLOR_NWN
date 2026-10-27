using System.Collections;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using static SWLOR.Game.Server.Tests.Service.DMPossessionHarness;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Runs the DM Settings window's load and change handlers against the engine model of DM possession. The window
/// keeps the DM's avatar, so its settings must follow whatever creature the DM controls when a change arrives.
/// </summary>
public class DMSettingsWindowTests
{
    private const uint DM = 1;
    private const uint Creature = 2;

    private DMPossessionHarness _harness = null!;

    [OneTimeSetUp]
    public void CompileProductionSettings()
    {
        _harness = Compile($$"""
            {{Type("Enumeration/EmoteStyle.cs", "EmoteStyle")}}
            {{Type("Enumeration/DMCommsFilter.cs", "DMCommsFilter")}}

            public sealed class Player { public string Id; public EmoteStyle EmoteStyle = EmoteStyle.Regular; }
            public sealed class DMPreferences
            {
                public string Id;
                public EmoteStyle EmoteStyle = EmoteStyle.Regular;
                public DMCommsFilter CommsFilter = DMCommsFilter.AllParties;
            }

            public static class DB
            {
                public static readonly Dictionary<string, object> Records = new();
                public static T Get<T>(string id) where T : class => Records.TryGetValue(id, out var record) ? record as T : null;
                public static void Set(Player player) => Records[player.Id] = player;
                public static void Set(DMPreferences preferences) => Records[preferences.Id] = preferences;
            }

            public static class DMPresence
            {
                {{InstanceMethod("Domain/Administration/DMPresenceService.cs", "GetControlledCreature")}}
                {{InstanceMethod("Domain/Administration/DMPresenceService.cs", "GetDM")}}
                public static readonly List<(uint Actor, string Message)> Sent = new();
                public static void SendMessage(uint actor, string message) => Sent.Add((actor, message));
            }

            public static class DMPreference
            {
                private const string IdPrefix = "DM:";
                // The service's injected database and presence, as this harness models them.
                private static readonly Database _db = new();
                private static readonly Presence _presence = new();
                private sealed class Database
                {
                    public T Get<T>(string id) where T : class => DB.Get<T>(id);
                    public void Set(DMPreferences preferences) => DB.Set(preferences);
                }
                private sealed class Presence
                {
                    public uint GetControlledCreature(uint actor) => DMPresence.GetControlledCreature(actor);
                }
                private static string GetPCPublicCDKey(uint obj)
                {
                    var player = ReceivingPlayer(obj);
                    return player == OBJECT_INVALID ? string.Empty : "KEY" + player;
                }
                {{InstanceMethod("Domain/Administration/DMPreferenceService.cs", "Get")}}
                {{InstanceMethod("Domain/Administration/DMPreferenceService.cs", "Update")}}
                {{InstanceMethod("Domain/Administration/DMPreferenceService.cs", "GetId")}}
            }

            public static class Communication
            {
                {{Method("Service/Communication.cs", "SetEmoteStyle")}}
            }

            public static class ArmorDisplay
            {
                public static readonly Dictionary<uint, (bool Helmet, bool Cloak)> Worn = new();
                public static bool SaveFails;
                public static (bool ShowHelmet, bool ShowCloak) GetDMArmorDisplay(uint creature) =>
                    Worn.TryGetValue(creature, out var worn) ? worn : (true, true);
                public static bool SetDMArmorDisplay(uint creature, bool showHelmet, bool showCloak)
                {
                    // Like production, the worn items change even when the avatar's preference cannot be saved.
                    Worn[creature] = (showHelmet, showCloak);
                    return !SaveFails;
                }
            }

            public static class ColorToken { public static string Red(string text) => text; }

            public abstract class ViewModelBase
            {
                protected virtual void OnClientPropertyUpdated(string propertyName) { }
            }

            public sealed class Settings : ViewModelBase
            {
                public const int RegularEmoteIndex = 0;
                public const int NovelEmoteIndex = 1;
                public const int AllPartiesIndex = 0;
                public const int OwnPartyIndex = 1;
                private uint _dm;
                public int EmoteStyleIndex { get; set; }
                public int CommsFilterIndex { get; set; }
                public bool ShowHelmet { get; set; }
                public bool ShowCloak { get; set; }
                public string ArmorDisplayText { get; set; }

                public Settings(uint player)
                {
                    _dm = DMPresence.GetDM(player);
                    LoadSettings();
                }

                public void ClientChanged(string propertyName, object value)
                {
                    GetType().GetProperty(propertyName)!.SetValue(this, value);
                    OnClientPropertyUpdated(propertyName);
                }

                public void Reload() => LoadSettings();

                {{Method("Feature/GuiDefinition/ViewModel/DMSettingsViewModel.cs", "LoadSettings")}}
                {{Method("Feature/GuiDefinition/ViewModel/DMSettingsViewModel.cs", "OnClientPropertyUpdated")}}
            }

            public static class Driver
            {
                public static object Open(uint player) => new Settings(player);
                public static void Change(object settings, string propertyName, object value) =>
                    ((Settings)settings).ClientChanged(propertyName, value);
                public static void Reload(object settings) => ((Settings)settings).Reload();
                public static object Read(object settings, string propertyName) =>
                    typeof(Settings).GetProperty(propertyName)!.GetValue(settings);
                public static void Wear(uint creature, bool helmet, bool cloak) => ArmorDisplay.Worn[creature] = (helmet, cloak);
                public static void FailArmorDisplaySaves() => ArmorDisplay.SaveFails = true;
                public static int MessagesSent() => DMPresence.Sent.Count;
                public static (bool, bool) Worn(uint creature) => ArmorDisplay.GetDMArmorDisplay(creature);
                public static string Saved(uint player, string field)
                {
                    var preferences = (DMPreferences)DB.Records["DM:KEY" + player];
                    return field == "EmoteStyle" ? preferences.EmoteStyle.ToString() : preferences.CommsFilter.ToString();
                }
            }
            """);
    }

    [SetUp]
    public void Reset()
    {
        _harness.Reset();
        ((IDictionary)_harness.GetField("DB", "Records")).Clear();
        ((IDictionary)_harness.GetField("ArmorDisplay", "Worn")).Clear();
        ((IList)_harness.GetField("DMPresence", "Sent")).Clear();
        _harness.SetField("ArmorDisplay", "SaveFails", false);
    }

    [Test]
    public void AvatarChangesSaveToTheDMsPreferences()
    {
        _harness.AddPlayer(DM, isDM: true);
        var settings = Open(DM);

        Change(settings, nameof(DMSettingsViewModel.EmoteStyleIndex), DMSettingsViewModel.NovelEmoteIndex);
        Change(settings, nameof(DMSettingsViewModel.CommsFilterIndex), DMSettingsViewModel.OwnPartyIndex);

        Saved(DM, "EmoteStyle").Should().Be("Novel");
        Saved(DM, "CommsFilter").Should().Be("OwnParty");
        Read(settings, nameof(DMSettingsViewModel.ArmorDisplayText)).Should().Be("Helmet and Cloak: your avatar");
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void WindowOpenedOnTheAvatarFollowsTheDMIntoPossession(int possessionType)
    {
        _harness.AddPlayer(DM, isDM: true);
        var settings = Open(DM);
        _harness.Possess(DM, Creature, possessionType);
        _harness.Invoke("Driver", "Wear", Creature, true, true);
        _harness.Invoke("Driver", "Reload", settings);

        Read(settings, nameof(DMSettingsViewModel.ArmorDisplayText)).Should().Be("Helmet and Cloak: possessed creature");
        Change(settings, nameof(DMSettingsViewModel.ShowHelmet), false);
        Change(settings, nameof(DMSettingsViewModel.EmoteStyleIndex), DMSettingsViewModel.NovelEmoteIndex);

        _harness.Invoke<(bool, bool)>("Driver", "Worn", Creature).Should().Be((false, true), "the possessed creature's helmet is hidden");
        _harness.Invoke<(bool, bool)>("Driver", "Worn", DM).Should().Be((true, true), "the avatar is left alone");
        Saved(DM, "EmoteStyle").Should().Be("Novel", "the possessed creature saves to the same DM key");
    }

    [Test]
    public void LoadShowsSavedSettingsAndWornItems()
    {
        _harness.AddPlayer(DM, isDM: true);
        Change(Open(DM), nameof(DMSettingsViewModel.CommsFilterIndex), DMSettingsViewModel.OwnPartyIndex);
        _harness.Invoke("Driver", "Wear", DM, false, true);

        var reopened = Open(DM);

        Read(reopened, nameof(DMSettingsViewModel.CommsFilterIndex)).Should().Be(DMSettingsViewModel.OwnPartyIndex);
        Read(reopened, nameof(DMSettingsViewModel.EmoteStyleIndex)).Should().Be(DMSettingsViewModel.RegularEmoteIndex);
        Read(reopened, nameof(DMSettingsViewModel.ShowHelmet)).Should().Be(false);
        Read(reopened, nameof(DMSettingsViewModel.ShowCloak)).Should().Be(true);
    }

    [Test]
    public void FailedHelmetAndCloakSaveIsReported()
    {
        _harness.AddPlayer(DM, isDM: true);
        var settings = Open(DM);
        _harness.Invoke("Driver", "FailArmorDisplaySaves");

        Change(settings, nameof(DMSettingsViewModel.ShowCloak), false);

        _harness.Invoke<int>("Driver", "MessagesSent").Should().Be(1, "the DM learns the choice will not last");
    }

    [Test]
    public void ToolbarFitsBeforeThePortraitAtTheLargestUIScale()
    {
        // A 1280x1024 screen at the largest UI scale is about 914 UI pixels wide, and the portrait takes about 80.
        (DMToolbarViewModel.ToolbarX + DMToolbarViewModel.AdminToolbarWidth).Should().BeLessThanOrEqualTo(914f - 80f);
    }

    [Test]
    public void SettingsWindowStaysOpenAcrossAreaChanges()
    {
        using var validationBuild = GuiLayoutValidator.BeginValidationOnlyBuild();

        new DMSettingsDefinition().BuildWindow().KeepsOpenOnAreaChange.Should().BeTrue();
    }

    private object Open(uint player) => _harness.Invoke("Driver", "Open", player);

    private void Change(object settings, string propertyName, object value) =>
        _harness.Invoke("Driver", "Change", settings, propertyName, value);

    private object Read(object settings, string propertyName) => _harness.Invoke("Driver", "Read", settings, propertyName);

    private string Saved(uint player, string field) => _harness.Invoke<string>("Driver", "Saved", player, field);
}
