using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Core;

/// <summary>
/// Unit tests of the cache instance. The module is a scripted engine: a list of item blueprints that the cache spawns one
/// by one (with the item service answering icon, legacy and economy questions from the same script) and the
/// <c>portraits</c> and <c>soundset</c> 2DAs. Nothing needs a server.
/// </summary>
[TestFixture]
public class CacheServiceTests
{
    private const uint Storage = 5000;
    private const uint Invalid = 0x7F000000;

    private FakeNWScript _engine = null!;
    private FakeUtilPlugin _util = null!;
    private IDisposable _engineScope = null!;
    private IDisposable _utilScope = null!;
    private ScriptedProxy _itemScript = null!;
    private readonly Dictionary<string, ItemBlueprint> _blueprints = new();
    private readonly Dictionary<uint, ItemBlueprint> _spawned = new();
    private readonly Dictionary<string, Dictionary<(string Column, int Row), string>> _twoDAs = new();
    private uint _nextItem;
    private CacheService _cache = null!;

    private sealed record ItemBlueprint(
        string Resref,
        string Name,
        string Icon,
        bool HasInventory = false,
        bool Cursed = false,
        bool Plot = false,
        bool Legacy = false,
        bool EconomyRestricted = false);

    [SetUp]
    public void SetUp()
    {
        _blueprints.Clear();
        _spawned.Clear();
        _twoDAs.Clear();
        _nextItem = 7000;

        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetObjectByTag", arguments => (string)arguments[0]! == "TEMP_ITEM_STORAGE" ? Storage : Invalid);
        _engine.SetReturnHandler("CreateItemOnObject", arguments =>
        {
            var blueprint = _blueprints[(string)arguments[0]!];
            var item = ++_nextItem;
            _spawned[item] = blueprint;
            return item;
        });
        _engine.SetReturnHandler("GetName", arguments => _spawned[(uint)arguments[0]!].Name);
        _engine.SetReturnHandler("GetHasInventory", arguments => _spawned[(uint)arguments[0]!].HasInventory);
        _engine.SetReturnHandler("GetItemCursedFlag", arguments => _spawned[(uint)arguments[0]!].Cursed);
        _engine.SetReturnHandler("GetPlotFlag", arguments => _spawned[(uint)arguments[0]!].Plot);
        _engine.SetReturnHandler("Get2DARowCount", arguments =>
            _twoDAs.TryGetValue((string)arguments[0]!, out var rows) ? rows.Keys.Select(key => key.Row).DefaultIfEmpty(-1).Max() + 1 : 0);
        _engine.SetReturnHandler("Get2DAString", arguments =>
            _twoDAs.TryGetValue((string)arguments[0]!, out var rows) &&
            rows.TryGetValue(((string)arguments[1]!, (int)arguments[2]!), out var value) ? value : string.Empty);
        _engine.SetReturnHandler("GetStringByStrRef", arguments => "Name " + arguments[0]);
        _engineScope = NWScriptRuntime.Override(_engine);

        _util = new FakeUtilPlugin();
        _utilScope = UtilPluginRuntime.Override(_util);

        // The item service answers from the spawned item's blueprint, as the real one reads the item.
        var items = ScriptedProxy.Create<IItemService>(out _itemScript);
        _itemScript
            .On("GetIconResref", arguments => _spawned[(uint)arguments[0]!].Icon)
            .On("IsLegacyItem", arguments => _spawned[(uint)arguments[0]!].Legacy)
            .On("IsEconomyRestricted", arguments => _spawned[(uint)arguments[0]!].EconomyRestricted);
        _cache = new CacheService(items);
    }

    [TearDown]
    public void TearDown()
    {
        _utilScope.Dispose();
        _engineScope.Dispose();
    }

    private void Blueprint(ItemBlueprint blueprint) => _blueprints[blueprint.Resref] = blueprint;

    private void ModuleHolds(params string[] resrefs)
    {
        _util.SetReturnSequence("GetFirstResRef", resrefs.Length > 0 ? resrefs[0] : string.Empty);
        _util.SetReturnSequence("GetNextResRef", resrefs.Skip(1).Append(string.Empty).Cast<object?>().ToArray());
    }

    private void TwoDA(string file, int row, string column, string value)
    {
        if (!_twoDAs.TryGetValue(file, out var rows))
            _twoDAs[file] = rows = new Dictionary<(string, int), string>();
        rows[(column, row)] = value;
    }

    private void Portrait(int row, string baseResref, string race)
    {
        TwoDA("portraits", row, "BaseResRef", baseResref);
        TwoDA("portraits", row, "Race", race);
    }

    private void SoundSet(int row, string strRef, string resref)
    {
        TwoDA("soundset", row, "STRREF", strRef);
        TwoDA("soundset", row, "RESREF", resref);
    }

    // ---- items

    [Test]
    public void The_boot_spawns_every_blueprint_once_and_remembers_name_icon_and_searchability()
    {
        Blueprint(new ItemBlueprint("blaster", "Blaster Pistol", "icon_blaster"));
        Blueprint(new ItemBlueprint("vest", "Padded Vest", "icon_vest"));
        ModuleHolds("blaster", "vest");

        _cache.CacheData();

        _util.CallsTo("GetFirstResRef").Should().ContainSingle().Which.Arguments[0].Should().Be(ResRefType.Item);
        _engine.CountOf("CreateItemOnObject").Should().Be(2);
        _engine.CallsTo("CreateItemOnObject").Select(call => call.Arguments[1]).Should().OnlyContain(target => (uint)target! == Storage);
        _engine.CountOf("DestroyObject").Should().Be(2, "each spawned item is destroyed once it has been read");

        _cache.GetItemNameByResref("blaster").Should().Be("Blaster Pistol");
        _cache.GetItemIconByResref("vest").Should().Be("icon_vest");
        _cache.IsItemSearchableByResref("blaster").Should().BeTrue();
        _engine.CountOf("CreateItemOnObject").Should().Be(2, "everything was cached at boot, so nothing is spawned again");
    }

    [Test]
    public void A_module_with_no_blueprints_caches_nothing()
    {
        ModuleHolds();

        _cache.CacheData();

        _engine.CountOf("CreateItemOnObject").Should().Be(0);
    }

    [TestCase("container", true, false, false, false, false)]
    [TestCase("cursed", false, true, false, false, false)]
    [TestCase("plot", false, false, true, false, false)]
    [TestCase("legacy", false, false, false, true, false)]
    [TestCase("restricted", false, false, false, false, true)]
    public void An_item_that_cannot_be_traded_is_not_searchable(
        string resref, bool hasInventory, bool cursed, bool plot, bool legacy, bool restricted)
    {
        Blueprint(new ItemBlueprint(resref, "Odd Thing", "icon", hasInventory, cursed, plot, legacy, restricted));
        Blueprint(new ItemBlueprint("fine", "Fine Thing", "icon"));
        ModuleHolds(resref, "fine");
        _cache.CacheData();

        _cache.IsItemSearchableByResref(resref).Should().BeFalse();
        _cache.IsItemSearchableByResref("fine").Should().BeTrue();
        _cache.SearchItemsByName("Thing", 10).Select(match => match.Resref).Should().Equal("fine");
    }

    [Test]
    public void A_resref_the_boot_did_not_see_is_spawned_once_and_then_remembered()
    {
        Blueprint(new ItemBlueprint("late", "Late Arrival", "icon_late"));
        ModuleHolds();
        _cache.CacheData();

        _cache.GetItemNameByResref("late").Should().Be("Late Arrival");
        _cache.GetItemNameByResref("late").Should().Be("Late Arrival");
        _cache.GetItemIconByResref("late").Should().Be("icon_late");
        _cache.IsItemSearchableByResref("late").Should().BeTrue();

        _engine.CountOf("CreateItemOnObject").Should().Be(1);
    }

    [Test]
    public void Asking_for_the_icon_or_the_searchability_of_an_unseen_resref_spawns_it()
    {
        Blueprint(new ItemBlueprint("icon_first", "First", "icon_one"));
        Blueprint(new ItemBlueprint("search_first", "Second", "icon_two"));
        ModuleHolds();
        _cache.CacheData();

        _cache.GetItemIconByResref("icon_first").Should().Be("icon_one");
        _cache.IsItemSearchableByResref("search_first").Should().BeTrue();

        _engine.CallsTo("CreateItemOnObject").Select(call => call.Arguments[0]).Should().Equal("icon_first", "search_first");
    }

    [Test]
    public void The_search_is_case_insensitive_ordered_by_name_and_capped()
    {
        Blueprint(new ItemBlueprint("zed", "Zeta Blaster", "icon"));
        Blueprint(new ItemBlueprint("alp", "alpha BLASTER", "icon"));
        Blueprint(new ItemBlueprint("mid", "Mid Blaster", "icon"));
        Blueprint(new ItemBlueprint("other", "Padded Vest", "icon"));
        Blueprint(new ItemBlueprint("bare", "", "icon"));
        ModuleHolds("zed", "alp", "mid", "other", "bare");
        _cache.CacheData();

        _cache.SearchItemsByName("blaster", 10).Should().Equal(
            ("alp", "alpha BLASTER"), ("mid", "Mid Blaster"), ("zed", "Zeta Blaster"));
        _cache.SearchItemsByName("BLASTER", 2).Should().Equal(("alp", "alpha BLASTER"), ("mid", "Mid Blaster"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void A_blank_search_finds_nothing(string? search)
    {
        Blueprint(new ItemBlueprint("blaster", "Blaster", "icon"));
        ModuleHolds("blaster");
        _cache.CacheData();

        _cache.SearchItemsByName(search!, 10).Should().BeEmpty();
    }

    // ---- portraits

    [Test]
    public void Portraits_get_internal_ids_in_row_order_and_skip_rows_without_a_race_or_a_resref()
    {
        Portrait(0, "", "H");
        Portrait(1, "human_m", "H");
        Portrait(2, "human_f", "");
        Portrait(3, "twilek_f", "T");
        ModuleHolds();

        _cache.CacheData();

        _cache.PortraitCount.Should().Be(2);
        _cache.GetPortraitByInternalId(1).Should().Be(1);
        _cache.GetPortraitByInternalId(2).Should().Be(3);
        _cache.GetPortraitInternalId(3).Should().Be(2);
        _cache.GetPortraitResrefByInternalId(1).Should().Be("po_human_m");
        _cache.GetPortraitInternalIdByResref("po_twilek_f").Should().Be(2);
        _cache.GetPortraitInternalIdByResref("po_human_f").Should().Be(-1, "a portrait the 2DA does not list has no internal id");
    }

    [Test]
    public void An_unknown_portrait_id_is_a_lookup_failure_as_it_always_was()
    {
        ModuleHolds();
        _cache.CacheData();

        var byInternal = () => _cache.GetPortraitByInternalId(1);
        var byPortrait = () => _cache.GetPortraitInternalId(1);
        var byResref = () => _cache.GetPortraitResrefByInternalId(1);

        byInternal.Should().Throw<KeyNotFoundException>();
        byPortrait.Should().Throw<KeyNotFoundException>();
        byResref.Should().Throw<KeyNotFoundException>();
    }

    // ---- sound sets

    [Test]
    public void Sound_sets_are_listed_by_name_and_rows_with_a_blank_strref_or_resref_are_skipped()
    {
        SoundSet(0, "30", "c_viper");
        SoundSet(1, "10", "c_spider");
        SoundSet(2, "", "c_lonely");
        SoundSet(3, "20", "");
        SoundSet(4, "50", "bit_one");
        ModuleHolds();

        _cache.CacheData();

        _cache.GetSoundSets().Should().Equal(new Dictionary<int, string>
        {
            [1] = "Name 10",
            [0] = "Name 30",
            [4] = "Name 50"
        });
        _cache.GetSoundSets().Keys.Should().Equal(new[] { 1, 0, 4 }, "the list is ordered by display name");
    }

    [Test]
    public void The_sound_sets_are_handed_out_as_a_copy()
    {
        SoundSet(0, "30", "c_viper");
        ModuleHolds();
        _cache.CacheData();

        _cache.GetSoundSets().Clear();

        _cache.GetSoundSets().Should().ContainKey(0);
    }

    [TestCase("c_viper", "c_viper_bat1", Description = "a sound set with its own preview sound")]
    [TestCase("  C_VIPER  ", "c_viper_bat1", Description = "the custom table ignores case and padding")]
    [TestCase("wookie", "p_zaalbar_bat1", Description = "a custom preview that does not follow the suffix rule")]
    [TestCase(" c_spider ", "c_spider_bat1", Description = "a padded resref keeps the suffix behaviour")]
    [TestCase("C_Spider", "c_spider_bat1", Description = "the suffixed name is lower case")]
    [TestCase("12345678901", "12345678901_bat1", Description = "eleven characters is the longest that fits a resref with the suffix")]
    [TestCase("123456789012", "", Description = "a longer resref has no preview")]
    [TestCase(" **** ", "", Description = "a padded placeholder has no preview")]
    [TestCase("****", "", Description = "a placeholder has no preview")]
    public void The_preview_sound_of_a_sound_set_follows_the_resref_rules(string resref, string expected)
    {
        SoundSet(0, "10", resref);
        ModuleHolds();

        _cache.CacheData();

        _cache.GetSoundSetPreviewSoundResref(0).Should().Be(expected);
    }

    [Test]
    public void A_sound_set_that_is_not_listed_has_no_preview()
    {
        SoundSet(0, "10", "c_viper");
        ModuleHolds();
        _cache.CacheData();

        _cache.GetSoundSetPreviewSoundResref(99).Should().BeEmpty();
    }
}
