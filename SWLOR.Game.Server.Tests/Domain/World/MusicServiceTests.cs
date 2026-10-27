using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.MusicService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.World;

/// <summary>
/// Unit tests of the music instance: the engine (NWScript and the player plugin) is replaced by recording fakes with
/// real local variables, and the database by an in-memory one. The key items of a player are answered from the stored
/// player (the static key item service reads the database), the way the real service does.
/// </summary>
[TestFixture]
public class MusicServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Player = 5001;
    private const uint Enemy = 5002;
    private const uint Dm = 5003;
    private const uint Area = 7001;
    private const uint OtherArea = 7002;
    private const uint Jukebox = 7101;
    private const int DayTrack = 11;
    private const int NightTrack = 12;
    private const int BattleTrack = 13;

    private static readonly MusicSong Starter = new MusicSongCatalog().Songs.First(song => song.KeyItem == KeyItemType.SongExplore1);
    private static readonly MusicSong SecondStarter = new MusicSongCatalog().Songs.First(song => song.KeyItem == KeyItemType.SongCantina1);
    private static readonly MusicSong Loot = new MusicSongCatalog().Songs.First(song => song.Acquisition == MusicAcquisitionType.Loot);
    private static readonly MusicSong Boss = new MusicSongCatalog().Songs.First(song => song.Acquisition == MusicAcquisitionType.BossDrop);

    private FakeNWScript _engine = null!;
    private LocalVariables _locals = null!;
    private FakePlayerPlugin _plugin = null!;
    private InMemoryDBService _db = null!;
    private MusicWithKeyItems _service = null!;
    private IDisposable _engineScope = null!;
    private IDisposable _pluginScope = null!;

    private readonly HashSet<uint> _players = new();
    private readonly HashSet<uint> _dms = new();
    private readonly HashSet<uint> _possessed = new();
    private readonly HashSet<uint> _creatures = new();
    private readonly HashSet<uint> _inCombat = new();
    private readonly Dictionary<uint, uint> _areaOf = new();
    private readonly Dictionary<uint, string> _tags = new();

    [OneTimeSetUp]
    public void LoadKeyItems()
    {
        // The song names come from the key item the song is tracked by; the module-cache handler fills that table.
        KeyItem.LoadData();
    }

    [SetUp]
    public void SetUp()
    {
        _players.Clear();
        _dms.Clear();
        _possessed.Clear();
        _creatures.Clear();
        _inCombat.Clear();
        _areaOf.Clear();
        _tags.Clear();

        _engine = new FakeNWScript();
        _locals = new LocalVariables(_engine);
        _plugin = new FakePlayerPlugin();
        _db = new InMemoryDBService();
        _service = new MusicWithKeyItems(_db);

        _engine.SetReturnHandler("GetObjectUUID", arguments => "uuid-" + (uint)arguments[0]!);
        _engine.SetReturnHandler("GetIsPC", arguments => _players.Contains((uint)arguments[0]!));
        _engine.SetReturnHandler("GetIsDM", arguments => _dms.Contains((uint)arguments[0]!));
        _engine.SetReturnHandler("GetIsDMPossessed", arguments => _possessed.Contains((uint)arguments[0]!));
        _engine.SetReturnHandler("GetObjectType", arguments => _creatures.Contains((uint)arguments[0]!) ? ObjectType.Creature : ObjectType.Placeable);
        _engine.SetReturnHandler("GetArea", arguments => _areaOf.GetValueOrDefault((uint)arguments[0]!, ObjectInvalid));
        _engine.SetReturnHandler("GetIsInCombat", arguments => _inCombat.Contains((uint)arguments[0]!));
        _engine.SetReturnHandler("GetTag", arguments => _tags.GetValueOrDefault((uint)arguments[0]!, string.Empty));
        _engine.SetReturn("MusicBackgroundGetDayTrack", DayTrack);
        _engine.SetReturn("MusicBackgroundGetNightTrack", NightTrack);
        _engine.SetReturn("MusicBackgroundGetBattleTrack", BattleTrack);

        _engineScope = NWScriptRuntime.Override(_engine);
        _pluginScope = PlayerPluginRuntime.Override(_plugin);

        AddPlayer(Player, Area);
    }

    [TearDown]
    public void TearDown()
    {
        _pluginScope.Dispose();
        _engineScope.Dispose();
    }

    /// <summary>
    /// The music instance with the key items of a player answered from the stored player (the real lookups read the
    /// database through the static key item service, and giving one also refreshes the key item window).
    /// </summary>
    private sealed class MusicWithKeyItems : MusicService
    {
        private readonly InMemoryDBService _store;

        public MusicWithKeyItems(InMemoryDBService db)
            : base(db)
        {
            _store = db;
        }

        public List<KeyItemType> Given { get; } = new();

        protected override bool HasKeyItem(uint player, KeyItemType keyItem) =>
            _store.Get<Entity.Player>("uuid-" + player).KeyItems.ContainsKey(keyItem);

        protected override void GiveKeyItem(uint player, KeyItemType keyItem)
        {
            var stored = _store.Get<Entity.Player>("uuid-" + player);
            stored.KeyItems[keyItem] = DateTime.UtcNow;
            _store.Set(stored);
            Given.Add(keyItem);
        }
    }

    private void AddPlayer(uint creature, uint area)
    {
        _players.Add(creature);
        _creatures.Add(creature);
        _areaOf[creature] = area;
        _db.With(new Entity.Player { Id = "uuid-" + creature });
    }

    private Entity.Player Stored(uint creature = Player) => _db.Get<Entity.Player>("uuid-" + creature);

    private void Collect(MusicSong song, uint creature = Player) => Stored(creature).KeyItems[song.KeyItem] = DateTime.UtcNow;

    private static string Render(string member, params object?[] arguments) => $"{member}({string.Join(", ", arguments)})";

    private List<string> PluginCalls(params string[] members) =>
        _plugin.Calls.Where(call => members.Length == 0 || members.Contains(call.Member))
            .Select(call => Render(call.Member, call.Arguments.ToArray())).ToList();

    private void SetInt(uint target, string name, int value) => ((INWScript)_engine).SetLocalInt(target, name, value);

    [Test]
    public void The_constructor_builds_the_song_tables_without_touching_the_engine()
    {
        var service = new MusicService(new InMemoryDBService());

        service.GetCollectibleSongs().Should().HaveCount(new MusicSongCatalog().Songs.Count);
        _engine.Calls.Should().BeEmpty();
        _plugin.Calls.Should().BeEmpty();
    }

    // ---- the ambientmusic.2da song list

    [Test]
    public void The_song_list_is_read_from_the_ambient_music_table_and_skips_rows_without_a_name()
    {
        var rows = new Dictionary<int, (string Description, string DisplayName)>
        {
            [0] = ("", ""),
            [1] = ("", "Cantina Band"),
            [2] = ("4242", "Ignored when the description names a string"),
            [3] = ("   ", "Hangar Hum"),
            [4] = ("", "   ")
        };
        _engine.SetReturnHandler("Get2DARowCount", arguments => (string)arguments[0]! == "ambientmusic" ? rows.Count : 0);
        _engine.SetReturnHandler("Get2DAString", arguments =>
        {
            var row = rows[(int)arguments[2]!];
            return (string)arguments[1]! == "Description" ? row.Description : row.DisplayName;
        });
        _engine.SetReturnHandler("GetStringByStrRef", arguments => (int)arguments[0]! == 4242 ? "Spaceport Theme" : "?");

        _service.LoadSongList();

        _service.GetAllSongs().Select(song => (song.ID, song.DisplayName)).Should().BeEquivalentTo(new[]
        {
            (1, "Cantina Band"),
            (2, "Spaceport Theme"),
            (3, "Hangar Hum")
        });
    }

    [Test]
    public void A_track_is_named_by_its_song_then_by_the_table_then_by_its_number()
    {
        _engine.SetReturn("Get2DARowCount", 2);
        _engine.SetReturnHandler("Get2DAString", arguments =>
            (string)arguments[1]! == "DisplayName" ? (int)arguments[2]! == 0 ? "Silence" : "Table name for the song" : string.Empty);
        _service.LoadSongList();

        _service.GetTrackName(0).Should().Be("Silence", "a track that is no collectible song keeps the name of the table");
        _service.GetTrackName(Starter.TrackId).Should().Be(KeyItem.GetKeyItem(Starter.KeyItem).Name,
            "a collectible song is named by its key item, whatever the table says");
        _service.GetTrackName(100000).Should().Be("Track 100000");
        _service.GetSongName(Loot).Should().Be(KeyItem.GetKeyItem(Loot.KeyItem).Name);
    }

    // ---- the collectible songs

    [Test]
    public void The_collectible_songs_are_the_catalog_found_by_key_item_and_by_track()
    {
        var catalog = new MusicSongCatalog().Songs;

        _service.GetCollectibleSongs().Select(song => song.KeyItem).Should().Equal(catalog.Select(song => song.KeyItem));
        _service.TryGetCollectibleSong(Loot.KeyItem, out var byKeyItem).Should().BeTrue();
        byKeyItem.TrackId.Should().Be(Loot.TrackId);
        _service.TryGetCollectibleSong(KeyItemType.Invalid, out var missing).Should().BeFalse();
        missing.Should().BeNull();
        _service.TryGetCollectibleSongByTrackId(Boss.TrackId, out var byTrack).Should().BeTrue();
        byTrack.KeyItem.Should().Be(Boss.KeyItem);
        _service.TryGetCollectibleSongByTrackId(-1, out _).Should().BeFalse();
    }

    [Test]
    public void A_song_category_is_named_by_its_attribute()
    {
        _service.GetCategoryName(MusicCategoryType.Cantina).Should().Be("Cantina");
        _service.GetCategoryName(MusicCategoryType.Theme).Should().Be("Themes");
    }

    [Test]
    public void The_music_player_and_the_song_discs_are_music_items()
    {
        const uint player = 8001;
        const uint disc = 8002;
        const uint otherDisc = 8003;
        const uint sword = 8004;
        _tags[player] = Music.MusicPlayerTag;
        _tags[disc] = "KEY_ITEM";
        _tags[otherDisc] = "KEY_ITEM";
        _tags[sword] = "SWORD";
        SetInt(disc, "KEY_ITEM_ID", (int)Loot.KeyItem);
        SetInt(otherDisc, "KEY_ITEM_ID", (int)KeyItemType.Invalid);

        _service.IsMusicItem(player).Should().BeTrue("the Portable Music Player is a music item");
        _service.IsMusicItem(disc).Should().BeTrue("a key item disc of a collectible song is a music item");
        _service.IsMusicItem(otherDisc).Should().BeFalse("a key item that is not a song is not");
        _service.IsMusicItem(sword).Should().BeFalse();
    }

    [Test]
    public void The_facade_repeats_the_music_player_tag()
    {
        Music.MusicPlayerTag.Should().Be("MUSIC_PLAYER").And.Be(MusicService.MusicPlayerTag);
    }

    // ---- a player's collection

    [Test]
    public void A_collection_holds_the_tracks_of_the_songs_among_the_key_items()
    {
        Collect(Starter);
        Collect(Boss);
        Stored().KeyItems[KeyItemType.Invalid] = DateTime.UtcNow;

        _service.GetCollectedTrackIds("uuid-" + Player).Should().BeEquivalentTo(new[] { Starter.TrackId, Boss.TrackId });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("nobody")]
    public void An_unknown_player_has_an_empty_collection(string? playerId)
    {
        _service.GetCollectedTrackIds(playerId!).Should().BeEmpty();
    }

    [Test]
    public void Starter_songs_are_granted_once()
    {
        _service.GrantStarterSongs(Player);
        _service.GrantStarterSongs(Player);

        _service.Given.Should().BeEquivalentTo(new[] { Starter.KeyItem, SecondStarter.KeyItem });
        _service.GetCollectedTrackIds("uuid-" + Player).Should().BeEquivalentTo(new[] { Starter.TrackId, SecondStarter.TrackId });
    }

    [Test]
    public void Settings_for_songs_the_player_does_not_own_are_cleared_and_saved()
    {
        Collect(Starter);
        Stored().Settings.PersonalMusicTrackId = Loot.TrackId;
        Stored().Settings.BattleThemeId = Boss.TrackId;

        var result = _service.ClearUncollectedMusicSettings("uuid-" + Player);

        result.Should().BeSameAs(Stored());
        result.Settings.PersonalMusicTrackId.Should().BeNull();
        result.Settings.BattleThemeId.Should().BeNull();
        _db.Saved.Should().ContainSingle().Which.Should().BeSameAs(result);
    }

    [Test]
    public void Only_the_setting_for_a_song_that_is_not_owned_is_cleared()
    {
        Collect(Starter);
        Stored().Settings.PersonalMusicTrackId = Starter.TrackId;
        Stored().Settings.BattleThemeId = Boss.TrackId;

        var result = _service.ClearUncollectedMusicSettings("uuid-" + Player);

        result.Settings.PersonalMusicTrackId.Should().Be(Starter.TrackId);
        result.Settings.BattleThemeId.Should().BeNull();
        _db.Saved.Should().ContainSingle();
    }

    [Test]
    public void Settings_for_owned_songs_are_left_alone_and_not_saved()
    {
        Collect(Starter);
        Collect(Boss);
        Stored().Settings.PersonalMusicTrackId = Starter.TrackId;
        Stored().Settings.BattleThemeId = Boss.TrackId;

        var result = _service.ClearUncollectedMusicSettings("uuid-" + Player);

        result.Settings.PersonalMusicTrackId.Should().Be(Starter.TrackId);
        result.Settings.BattleThemeId.Should().Be(Boss.TrackId);
        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void Clearing_the_settings_of_an_unknown_player_answers_nothing()
    {
        _service.ClearUncollectedMusicSettings("nobody").Should().BeNull();
        _db.Saved.Should().BeEmpty();
    }

    // ---- the jukebox owner

    private WorldProperty StoreProperty(PropertyType type, string? owner = null, string? parent = null)
    {
        var property = new WorldProperty { PropertyType = type, OwnerPlayerId = owner!, ParentPropertyId = parent ?? string.Empty };
        _db.With(property);
        return property;
    }

    private void JukeboxIn(WorldProperty? property)
    {
        _engine.SetReturnHandler("GetLocalString", arguments =>
            (uint)arguments[0]! == Jukebox && (string)arguments[1]! == "PROPERTY_ID" ? property?.Id ?? string.Empty : string.Empty);
    }

    [TestCase(PropertyType.House)]
    [TestCase(PropertyType.Cantina)]
    [TestCase(PropertyType.CityHall)]
    [TestCase(PropertyType.Bank)]
    [TestCase(PropertyType.MedicalCenter)]
    [TestCase(PropertyType.Starport)]
    [TestCase(PropertyType.Lab)]
    public void A_city_interior_jukebox_uses_the_players_collection_despite_the_inherited_mayor_ownership(PropertyType interiorType)
    {
        var city = StoreProperty(PropertyType.City, "founding-mayor");
        var building = StoreProperty(PropertyType.Structure, "founding-mayor", city.Id);
        var interior = StoreProperty(interiorType, "founding-mayor", building.Id);
        var jukebox = StoreProperty(PropertyType.Structure, "founding-mayor", interior.Id);
        JukeboxIn(jukebox);

        _service.GetJukeboxOwnerId(Jukebox).Should().BeEmpty(
            "city-created interiors record the mayor as owner, so the picker must fall back to the user");
        _db.Reads.Should().Equal(jukebox.Id, interior.Id, building.Id, city.Id);

        city.OwnerPlayerId = "current-mayor";
        _service.GetJukeboxOwnerId(Jukebox).Should().BeEmpty("an election must not switch an interior jukebox to either mayor's collection");
    }

    [TestCase(PropertyType.Apartment)]
    [TestCase(PropertyType.Starship)]
    public void A_private_property_jukebox_uses_the_current_owner(PropertyType propertyType)
    {
        var interior = StoreProperty(propertyType, "current-owner");
        var jukebox = StoreProperty(PropertyType.Structure, "previous-owner", interior.Id);
        JukeboxIn(jukebox);

        _service.GetJukeboxOwnerId(Jukebox).Should().Be("current-owner");
        interior.OwnerPlayerId = "new-owner";
        _service.GetJukeboxOwnerId(Jukebox).Should().Be("new-owner");
    }

    [Test]
    public void A_jukebox_placed_in_a_city_uses_the_current_mayor_instead_of_its_recorded_owner()
    {
        var city = StoreProperty(PropertyType.City, "current-mayor", "area-resref");
        var jukebox = StoreProperty(PropertyType.Structure, "previous-mayor", city.Id);
        JukeboxIn(jukebox);

        _service.GetJukeboxOwnerId(Jukebox).Should().Be("current-mayor");
    }

    [Test]
    public void A_city_with_no_owner_leaves_a_directly_placed_jukebox_without_one()
    {
        var city = StoreProperty(PropertyType.City);
        var jukebox = StoreProperty(PropertyType.Structure, parent: city.Id);
        JukeboxIn(jukebox);

        _service.GetJukeboxOwnerId(Jukebox).Should().BeEmpty();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void An_unowned_world_jukebox_has_no_collection_owner_and_reads_no_property(string? propertyId)
    {
        _engine.SetReturnHandler("GetLocalString", _ => propertyId);

        _service.GetJukeboxOwnerId(Jukebox).Should().BeEmpty();
        _db.Reads.Should().BeEmpty("an unowned jukebox must not query properties");
    }

    [Test]
    public void A_jukebox_whose_property_is_missing_has_no_collection_owner()
    {
        _engine.SetReturnHandler("GetLocalString", _ => "missing-property");

        _service.GetJukeboxOwnerId(Jukebox).Should().BeEmpty();
    }

    [Test]
    public void An_unowned_interior_does_not_fall_back_to_the_mayor()
    {
        var city = StoreProperty(PropertyType.City, "mayor");
        var building = StoreProperty(PropertyType.Structure, parent: city.Id);
        var interior = StoreProperty(PropertyType.House, parent: building.Id);
        var jukebox = StoreProperty(PropertyType.Structure, parent: interior.Id);
        JukeboxIn(jukebox);

        _service.GetJukeboxOwnerId(Jukebox).Should().BeEmpty();
    }

    [Test]
    public void A_jukebox_whose_parent_is_missing_uses_the_nearest_property()
    {
        var jukebox = StoreProperty(PropertyType.Structure, "installer", "gone");
        JukeboxIn(jukebox);

        _service.GetJukeboxOwnerId(Jukebox).Should().Be("installer");
    }

    [Test]
    public void A_property_that_is_its_own_ancestor_ends_the_search()
    {
        var jukebox = StoreProperty(PropertyType.Structure, "installer");
        jukebox.ParentPropertyId = jukebox.Id;
        JukeboxIn(jukebox);

        _service.GetJukeboxOwnerId(Jukebox).Should().Be("installer");
    }

    // ---- personal tracks and battle themes

    [Test]
    public void Playing_a_track_for_a_player_saves_it_and_plays_it_for_day_and_night()
    {
        _service.PlayForPlayer(Player, 77);

        Stored().Settings.PersonalMusicTrackId.Should().Be(77);
        _db.Saved.Should().ContainSingle();
        PluginCalls("MusicBackgroundChangeTimeToggle", "MusicBackgroundToggle").Should().Equal(
            Render("MusicBackgroundChangeTimeToggle", Player, 77, false),
            Render("MusicBackgroundChangeTimeToggle", Player, 77, true),
            Render("MusicBackgroundToggle", Player, true));
    }

    [Test]
    public void Playing_a_track_for_an_unknown_player_does_nothing()
    {
        _service.PlayForPlayer(8888, 77);

        _db.Saved.Should().BeEmpty();
        _plugin.Calls.Should().BeEmpty();
    }

    [Test]
    public void Stopping_a_personal_track_restores_the_music_of_the_area()
    {
        Stored().Settings.PersonalMusicTrackId = 77;

        _service.StopForPlayer(Player);

        Stored().Settings.PersonalMusicTrackId.Should().BeNull();
        PluginCalls("MusicBackgroundChangeTimeToggle", "MusicBackgroundToggle").Should().Equal(
            Render("MusicBackgroundChangeTimeToggle", Player, DayTrack, false),
            Render("MusicBackgroundChangeTimeToggle", Player, NightTrack, true),
            Render("MusicBackgroundToggle", Player, true));
    }

    [Test]
    public void Stopping_a_personal_track_in_an_area_whose_music_was_stopped_keeps_the_music_off()
    {
        _service.StopInArea(Area);

        _service.StopForPlayer(Player);

        PluginCalls("MusicBackgroundToggle").Should().Equal(
            Render("MusicBackgroundToggle", Player, true),
            Render("MusicBackgroundToggle", Player, false));
    }

    [Test]
    public void Stopping_the_track_of_an_unknown_player_does_nothing()
    {
        _service.StopForPlayer(8888);

        _db.Saved.Should().BeEmpty();
        _plugin.Calls.Should().BeEmpty();
    }

    [Test]
    public void The_personal_track_and_the_battle_theme_are_read_from_the_stored_player()
    {
        Stored().Settings.PersonalMusicTrackId = 21;
        Stored().Settings.BattleThemeId = 22;

        _service.GetPersonalTrackId(Player).Should().Be(21);
        _service.GetBattleThemeId(Player).Should().Be(22);
        _service.GetPersonalTrackId(8888).Should().BeNull();
        _service.GetBattleThemeId(8888).Should().BeNull();
    }

    [Test]
    public void Selecting_a_battle_theme_saves_it_and_tells_the_client()
    {
        _service.SetBattleTheme(Player, 31);

        Stored().Settings.BattleThemeId.Should().Be(31);
        _locals.Int(Player, "MUSIC_BATTLE_THEME_SET").Should().Be(1);
        PluginCalls("MusicBattleChange").Should().Equal(Render("MusicBattleChange", Player, 31));
    }

    [Test]
    public void Clearing_the_battle_theme_goes_back_to_the_battle_music_of_the_area_and_stops_a_theme_in_progress()
    {
        AddEnemy();
        _service.SetBattleTheme(Player, 31);
        _service.StartBattleTheme(Player, Enemy);
        _service.IsBattleThemePlaying(Player).Should().BeTrue();
        _plugin.ClearCalls();

        _service.SetBattleTheme(Player, null);

        Stored().Settings.BattleThemeId.Should().BeNull();
        _locals.Int(Player, "MUSIC_BATTLE_THEME_SET").Should().Be(0);
        _service.IsBattleThemePlaying(Player).Should().BeFalse();
        PluginCalls().Should().Equal(
            Render("MusicBattleToggle", Player, false),
            Render("MusicBattleChange", Player, BattleTrack));
    }

    [Test]
    public void Selecting_a_battle_theme_for_an_unknown_player_does_nothing()
    {
        _service.SetBattleTheme(8888, 31);

        _db.Saved.Should().BeEmpty();
        _plugin.Calls.Should().BeEmpty();
    }

    // ---- the music of an area

    [Test]
    public void Playing_a_track_in_an_area_changes_both_halves_of_the_day_and_clears_the_stopped_mark()
    {
        _service.StopInArea(Area);
        _locals.ClearCalls();

        _service.PlayInArea(Area, 41);

        _engine.CallsTo("MusicBackgroundChangeDay").Should().ContainSingle().Which.Arguments.Should().Equal(Area, 41);
        _engine.CallsTo("MusicBackgroundChangeNight").Should().ContainSingle().Which.Arguments.Should().Equal(Area, 41);
        _engine.CountOf("MusicBackgroundPlay").Should().Be(1);
        _service.IsAreaMusicStopped(Area).Should().BeFalse();
    }

    [Test]
    public void Stopping_the_music_of_an_area_marks_it_stopped()
    {
        _service.IsAreaMusicStopped(Area).Should().BeFalse();

        _service.StopInArea(Area);

        _engine.CallsTo("MusicBackgroundStop").Should().ContainSingle().Which.Arguments.Should().Equal(Area);
        _service.IsAreaMusicStopped(Area).Should().BeTrue();
        _service.IsAreaMusicStopped(OtherArea).Should().BeFalse();
    }

    // ---- entering an area

    private void Enter(uint creature, uint area = Area)
    {
        _engine.SetReturn("GetEnteringObject", creature);
        _engine.SetReturn("OBJECT_SELF", area);
        _service.ApplyPlayerMusic();
    }

    [Test]
    public void Entering_an_area_grants_the_starter_songs_and_leaves_a_player_without_settings_alone()
    {
        Enter(Player);

        _service.Given.Should().BeEquivalentTo(new[] { Starter.KeyItem, SecondStarter.KeyItem });
        _locals.Int(Player, "MUSIC_BATTLE_THEME_SET").Should().Be(0);
        _plugin.Calls.Should().BeEmpty();
    }

    [Test]
    public void Entering_an_area_puts_the_personal_track_back_and_matches_the_battle_music_of_the_area()
    {
        Collect(Loot);
        Stored().Settings.PersonalMusicTrackId = Loot.TrackId;

        Enter(Player);

        PluginCalls().Should().Equal(
            Render("MusicBackgroundChangeTimeToggle", Player, Loot.TrackId, false),
            Render("MusicBackgroundChangeTimeToggle", Player, Loot.TrackId, true),
            Render("MusicBackgroundToggle", Player, true),
            Render("MusicBattleChange", Player, BattleTrack));
        _locals.Int(Player, "MUSIC_BATTLE_THEME_SET").Should().Be(0);
    }

    [Test]
    public void Entering_an_area_keeps_the_selected_battle_theme()
    {
        Collect(Boss);
        Stored().Settings.BattleThemeId = Boss.TrackId;

        Enter(Player);

        PluginCalls("MusicBattleChange").Should().Equal(Render("MusicBattleChange", Player, Boss.TrackId));
        _locals.Int(Player, "MUSIC_BATTLE_THEME_SET").Should().Be(1);
    }

    [Test]
    public void Entering_an_area_drops_settings_for_songs_that_are_not_collected()
    {
        Stored().Settings.PersonalMusicTrackId = Loot.TrackId;
        Stored().Settings.BattleThemeId = Boss.TrackId;

        Enter(Player);

        Stored().Settings.PersonalMusicTrackId.Should().BeNull();
        Stored().Settings.BattleThemeId.Should().BeNull();
        _plugin.Calls.Should().BeEmpty();
    }

    [Test]
    public void Entering_an_area_ends_a_battle_theme_that_was_playing_in_the_last_one()
    {
        Collect(Boss);
        Stored().Settings.BattleThemeId = Boss.TrackId;
        Enter(Player);
        _creatures.Add(Enemy);
        _areaOf[Enemy] = Area;
        _service.StartBattleTheme(Player, Enemy);
        _service.IsBattleThemePlaying(Player).Should().BeTrue();
        _plugin.ClearCalls();

        Enter(Player, OtherArea);

        _service.IsBattleThemePlaying(Player).Should().BeFalse();
        PluginCalls("MusicBattleToggle").Should().Equal(Render("MusicBattleToggle", Player, false));
    }

    [Test]
    public void Entering_an_area_does_nothing_for_dms_possessed_creatures_creatures_and_players_without_a_record()
    {
        const uint possessed = 5101;
        const uint monster = 5102;
        const uint stranger = 5103;
        _dms.Add(Dm);
        _players.Add(Dm);
        _players.Add(possessed);
        _possessed.Add(possessed);
        _players.Add(stranger);

        foreach (var creature in new[] { Dm, possessed, monster, stranger })
            Enter(creature);

        _db.Saved.Should().BeEmpty();
        _service.Given.Should().BeEmpty();
        _plugin.Calls.Should().BeEmpty();
    }

    // ---- battle themes

    private void GivePlayerABattleTheme(uint creature = Player)
    {
        var song = Boss;
        Collect(song, creature);
        Stored(creature).Settings.BattleThemeId = song.TrackId;
        Enter(creature);
        _plugin.ClearCalls();
    }

    private void AddEnemy(uint area = Area)
    {
        _creatures.Add(Enemy);
        _areaOf[Enemy] = area;
    }

    [Test]
    public void A_player_with_a_battle_theme_hears_it_when_attacked()
    {
        GivePlayerABattleTheme();
        AddEnemy();
        _engine.SetReturn("OBJECT_SELF", Player);
        _engine.SetReturn("GetLastAttacker", Enemy);

        _service.StartBattleThemeOnAttack();

        _service.IsBattleThemePlaying(Player).Should().BeTrue();
        PluginCalls("MusicBattleToggle").Should().Equal(Render("MusicBattleToggle", Player, true));
    }

    [Test]
    public void A_player_with_a_battle_theme_hears_it_when_damaged()
    {
        GivePlayerABattleTheme();
        AddEnemy();
        _engine.SetReturn("OBJECT_SELF", Player);
        _engine.SetReturn("GetLastDamager", Enemy);

        _service.StartBattleThemeOnDamage();

        _service.IsBattleThemePlaying(Player).Should().BeTrue();
    }

    [Test]
    public void The_theme_starts_once_and_for_either_combatant_who_has_one()
    {
        GivePlayerABattleTheme();
        AddEnemy();

        _service.StartBattleTheme(Enemy, Player);
        _service.StartBattleTheme(Player, Enemy);

        PluginCalls("MusicBattleToggle").Should().ContainSingle("a theme that is already playing is not started again");
    }

    [Test]
    public void A_player_without_a_battle_theme_hears_nothing_when_a_fight_starts()
    {
        AddEnemy();

        _service.StartBattleTheme(Player, Enemy);

        _service.IsBattleThemePlaying(Player).Should().BeFalse();
        _plugin.Calls.Should().BeEmpty();
    }

    [Test]
    public void No_theme_starts_for_a_fight_with_oneself_a_non_creature_or_another_area()
    {
        GivePlayerABattleTheme();
        AddEnemy(OtherArea);

        _service.StartBattleTheme(Player, Player);
        _service.StartBattleTheme(Player, Enemy);
        _service.StartBattleTheme(Player, Jukebox);
        _service.StartBattleTheme(Jukebox, Player);

        _plugin.Calls.Should().BeEmpty();
    }

    [Test]
    public void No_theme_starts_for_a_dm_or_a_possessed_creature()
    {
        GivePlayerABattleTheme();
        AddEnemy();
        _dms.Add(Player);

        _service.StartBattleTheme(Player, Enemy);
        _dms.Remove(Player);
        _possessed.Add(Player);
        _service.StartBattleTheme(Player, Enemy);

        _plugin.Calls.Should().BeEmpty();
    }

    [Test]
    public void The_theme_goes_on_while_the_player_is_in_combat_and_just_after_it()
    {
        GivePlayerABattleTheme();
        AddEnemy();
        _service.StartBattleTheme(Player, Enemy);
        _plugin.ClearCalls();
        _engine.SetReturn("OBJECT_SELF", Player);

        _inCombat.Add(Player);
        _service.StopBattleThemeAfterCombat();
        _inCombat.Remove(Player);
        _service.StopBattleThemeAfterCombat();

        _service.IsBattleThemePlaying(Player).Should().BeTrue("the player fought a moment ago");
        _plugin.Calls.Should().BeEmpty();
    }

    [Test]
    public void The_theme_stops_after_combat_when_no_fight_was_recorded_and_the_music_of_a_stopped_area_stays_off()
    {
        GivePlayerABattleTheme();
        AddEnemy();
        _service.StartBattleTheme(Player, Enemy);
        _service.StopInArea(Area);
        _plugin.ClearCalls();
        _engine.SetReturn("OBJECT_SELF", Player);
        _engine.SetReturn("GetExitingObject", Player);

        _service.ForgetBattleThemeCombat();
        _service.StopBattleThemeAfterCombat();

        _service.IsBattleThemePlaying(Player).Should().BeFalse();
        PluginCalls("MusicBattleToggle", "MusicBackgroundToggle").Should().Equal(
            Render("MusicBattleToggle", Player, false),
            Render("MusicBackgroundToggle", Player, false));
    }

    [Test]
    public void Stopping_the_theme_leaves_the_background_music_on_when_the_player_plays_a_track_of_their_own()
    {
        GivePlayerABattleTheme();
        Collect(Loot);
        Stored().Settings.PersonalMusicTrackId = Loot.TrackId;
        AddEnemy();
        _service.StartBattleTheme(Player, Enemy);
        _service.StopInArea(Area);
        _plugin.ClearCalls();
        _engine.SetReturn("OBJECT_SELF", Player);
        _engine.SetReturn("GetExitingObject", Player);
        _service.ForgetBattleThemeCombat();

        _service.StopBattleThemeAfterCombat();

        PluginCalls("MusicBattleToggle", "MusicBackgroundToggle").Should().Equal(Render("MusicBattleToggle", Player, false));
    }

    [Test]
    public void Nothing_stops_for_a_player_whose_theme_is_not_playing()
    {
        _engine.SetReturn("OBJECT_SELF", Player);

        _service.StopBattleThemeAfterCombat();

        _plugin.Calls.Should().BeEmpty();
    }
}
