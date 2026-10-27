using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.World;

/// <summary>
/// Unit tests of the planet instance: the engine is replaced by a recording fake that describes the areas the test
/// asks about, so no server is needed.
/// </summary>
[TestFixture]
public class PlanetServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint SomeArea = 4242;
    private const uint OtherArea = 4243;
    private const string PlanetTypeIdVariable = "PLANET_TYPE_ID";

    private FakeNWScript _engine = null!;
    private IDisposable _scope = null!;
    private PlanetService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != ObjectInvalid);
        _scope = NWScriptRuntime.Override(_engine);
        _service = new PlanetService();
    }

    [TearDown]
    public void TearDown()
    {
        _scope.Dispose();
    }

    private static IEnumerable<PlanetType> ActivePlanets() =>
        Enum.GetValues(typeof(PlanetType)).Cast<PlanetType>()
            .Where(type => type.GetAttribute<PlanetType, PlanetAttribute>().IsActive);

    /// <summary>Makes the engine describe one area; the module's area list is just that area.</summary>
    private void TheAreaIs(string name, string resref, bool isSpace = false, int storedPlanetId = 0, uint area = SomeArea)
    {
        _engine.SetReturnSequence("GetFirstArea", area);
        _engine.SetReturn("GetName", name);
        _engine.SetReturn("GetResRef", resref);
        _engine.SetReturn("GetLocalBool", isSpace);
        _engine.SetReturn("GetLocalInt", storedPlanetId);
    }

    private IEnumerable<(uint Area, string Variable, int Value)> StoredPlanetIds() =>
        _engine.CallsTo("SetLocalInt").Select(call => ((uint)call.Arguments[0]!, (string)call.Arguments[1]!, (int)call.Arguments[2]!));

    // ---- the cache

    [Test]
    public void Before_the_cache_is_filled_no_planet_is_registered()
    {
        _service.GetAllPlanets().Should().BeEmpty();
        var get = () => _service.GetPlanetByType(PlanetType.Viscara);

        get.Should().Throw<KeyNotFoundException>();
    }

    [Test]
    public void CacheData_registers_every_active_planet_and_no_other()
    {
        _service.CacheData();

        _service.GetAllPlanets().Keys.Should().BeEquivalentTo(ActivePlanets());
        _service.GetAllPlanets().Keys.Should().NotContain(PlanetType.Invalid);
    }

    [Test]
    public void GetPlanetByType_answers_with_the_attribute_of_the_planet()
    {
        _service.CacheData();

        var viscara = _service.GetPlanetByType(PlanetType.Viscara);

        viscara.Name.Should().Be("Viscara");
        viscara.Prefix.Should().Be("Viscara - ");
        viscara.IsActive.Should().BeTrue();
    }

    [Test]
    public void GetPlanetByType_fails_for_the_invalid_planet()
    {
        _service.CacheData();

        var get = () => _service.GetPlanetByType(PlanetType.Invalid);

        get.Should().Throw<KeyNotFoundException>();
    }

    [Test]
    public void GetAllPlanets_hands_out_a_copy()
    {
        _service.CacheData();

        _service.GetAllPlanets().Clear();

        _service.GetAllPlanets().Should().NotBeEmpty();
    }

    [Test]
    public void CacheData_can_run_again_without_changing_the_cache()
    {
        _service.CacheData();
        var before = _service.GetAllPlanets().Keys.ToList();

        _service.CacheData();

        _service.GetAllPlanets().Keys.Should().BeEquivalentTo(before);
    }

    [Test]
    public void Each_instance_keeps_its_own_cache()
    {
        _service.CacheData();

        new PlanetService().GetAllPlanets().Should().BeEmpty("state lives on the instance, not in static fields");
    }

    // ---- stamping the areas at module load

    [Test]
    public void CacheData_stamps_the_planet_on_every_area_that_belongs_to_one()
    {
        TheAreaIs("Tatooine - Mos Eisley", "mos_eisley");

        _service.CacheData();

        StoredPlanetIds().Should().Equal((SomeArea, PlanetTypeIdVariable, (int)PlanetType.Tatooine));
    }

    [Test]
    public void CacheData_stamps_nothing_on_an_area_no_planet_claims()
    {
        TheAreaIs("The Void", "the_void");

        _service.CacheData();

        StoredPlanetIds().Should().BeEmpty();
    }

    [Test]
    public void CacheData_walks_every_area_of_the_module()
    {
        _engine.SetReturnSequence("GetFirstArea", SomeArea);
        _engine.SetReturnSequence("GetNextArea", OtherArea);
        _engine.SetReturnHandler("GetName", arguments => (uint)arguments[0]! == SomeArea ? "Viscara - Port" : "Korriban - Tomb");
        _engine.SetReturn("GetResRef", string.Empty);

        _service.CacheData();

        StoredPlanetIds().Should().Equal(
            (SomeArea, PlanetTypeIdVariable, (int)PlanetType.Viscara),
            (OtherArea, PlanetTypeIdVariable, (int)PlanetType.Korriban));
    }

    // ---- the planet of an area

    [Test]
    public void GetPlanetType_is_invalid_for_an_object_that_is_not_valid()
    {
        _service.GetPlanetType(ObjectInvalid).Should().Be(PlanetType.Invalid);
        _engine.CallsTo("GetName").Should().BeEmpty();
    }

    [TestCase("Viscara - Port", PlanetType.Viscara)]
    [TestCase("Tatooine - Mos Eisley", PlanetType.Tatooine)]
    [TestCase("Mon Cala - Coral Isles - Inner", PlanetType.MonCala)]
    [TestCase("Hutlar - Frozen Wastes", PlanetType.Hutlar)]
    [TestCase("CZ-220 - Main Deck", PlanetType.CZ220)]
    [TestCase("Korriban - Tomb", PlanetType.Korriban)]
    [TestCase("Dathomir - Swamp", PlanetType.Dathomir)]
    [TestCase("Dantooine - Ruins", PlanetType.Dantooine)]
    [TestCase("Smuggler's Moon - Docks", PlanetType.SmugglersMoon)]
    [TestCase("Smuggler's Moon Station - Corridors", PlanetType.SmugglersMoonStation)]
    public void GetPlanetType_reads_the_planet_from_the_prefix_of_the_area_name(string name, PlanetType expected)
    {
        TheAreaIs(name, "any_resref_at_all");

        _service.GetPlanetType(SomeArea).Should().Be(expected);
    }

    [Test]
    public void GetPlanetType_remembers_the_planet_it_resolved_on_the_area()
    {
        TheAreaIs("Tatooine - Mos Eisley", "mos_eisley");

        _service.GetPlanetType(SomeArea);

        StoredPlanetIds().Should().Equal((SomeArea, PlanetTypeIdVariable, (int)PlanetType.Tatooine));
    }

    [Test]
    public void GetPlanetType_fills_the_cache_itself_when_the_module_handler_has_not_run()
    {
        _service.GetAllPlanets().Should().BeEmpty();
        TheAreaIs("Viscara - Port", "port");

        _service.GetPlanetType(SomeArea);

        _service.GetAllPlanets().Should().NotBeEmpty();
    }

    [Test]
    public void GetPlanetType_trusts_the_planet_stored_on_the_area()
    {
        TheAreaIs("Nothing recognizable", "nothing", storedPlanetId: (int)PlanetType.Dathomir);

        _service.GetPlanetType(SomeArea).Should().Be(PlanetType.Dathomir);
        _engine.CallsTo("GetName").Should().BeEmpty("the stored id answers without resolving the name");
        StoredPlanetIds().Should().BeEmpty();
    }

    [Test]
    public void GetPlanetType_ignores_a_stored_id_that_names_no_registered_planet()
    {
        TheAreaIs("Korriban - Tomb", "tomb", storedPlanetId: 1024);

        _service.GetPlanetType(SomeArea).Should().Be(PlanetType.Korriban);
    }

    [TestCase("canyon_001", PlanetType.Tatooine)]
    [TestCase("tat_cantina", PlanetType.Tatooine)]
    [TestCase("v_something", PlanetType.Viscara)]
    [TestCase("hutlar_frozen_wa", PlanetType.Hutlar)]
    public void GetPlanetType_falls_back_to_the_resref_when_the_name_names_no_planet(string resref, PlanetType expected)
    {
        TheAreaIs("Somewhere", resref);

        _service.GetPlanetType(SomeArea).Should().Be(expected);
    }

    [Test]
    public void GetPlanetType_never_uses_the_resref_of_an_area_flagged_as_space()
    {
        TheAreaIs("Somewhere", "tat_cantina", isSpace: true);

        _service.GetPlanetType(SomeArea).Should().Be(PlanetType.Invalid);
    }

    [Test]
    public void GetPlanetType_never_uses_the_resref_of_an_area_whose_name_starts_with_Space()
    {
        TheAreaIs("Space - Tatooine Orbit", "tat_orbit");

        _service.GetPlanetType(SomeArea).Should().Be(PlanetType.Invalid);
        StoredPlanetIds().Should().BeEmpty();
    }

    [Test]
    public void GetPlanetType_is_invalid_when_neither_the_name_nor_the_resref_names_a_planet()
    {
        TheAreaIs("Somewhere", "nowhere");

        _service.GetPlanetType(SomeArea).Should().Be(PlanetType.Invalid);
    }

    // ---- the planet of a resref

    [TestCase("area", PlanetType.Viscara)]
    [TestCase("AREA", PlanetType.Viscara)]
    [TestCase("canyon_001", PlanetType.Tatooine)]
    [TestCase("veles_exterior", PlanetType.Viscara)]
    [TestCase("viscarawildlands", PlanetType.Viscara)]
    public void GetPlanetTypeByAreaResref_knows_the_exact_resrefs_in_any_case(string resref, PlanetType expected)
    {
        _service.GetPlanetTypeByAreaResref(resref).Should().Be(expected);
    }

    [TestCase("anc_something", PlanetType.Tatooine)]
    [TestCase("ANCHOR_dock", PlanetType.Tatooine)]
    [TestCase("ar_scor_k01", PlanetType.Korriban)]
    [TestCase("czs220_deck", PlanetType.CZ220)]
    [TestCase("dan_ruins", PlanetType.Dantooine)]
    [TestCase("dathomir_swamp", PlanetType.Dathomir)]
    [TestCase("hutlar_ice", PlanetType.Hutlar)]
    [TestCase("moncala_reef", PlanetType.MonCala)]
    [TestCase("nanostation_a", PlanetType.CZ220)]
    [TestCase("sol_hutlar_a", PlanetType.Hutlar)]
    [TestCase("sol_swamp_a", PlanetType.Viscara)]
    [TestCase("veles_sewers", PlanetType.Viscara)]
    [TestCase("ziyhut_hall", PlanetType.Hutlar)]
    public void GetPlanetTypeByAreaResref_matches_the_resref_prefixes(string resref, PlanetType expected)
    {
        _service.GetPlanetTypeByAreaResref(resref).Should().Be(expected);
    }

    [TestCase("tatooineorbit")]
    [TestCase("VISCARAORBIT")]
    [TestCase("space_korriban")]
    [TestCase("prefab_space")]
    [TestCase("hutlar_orbit")]
    public void GetPlanetTypeByAreaResref_is_invalid_for_the_space_areas_even_when_a_prefix_would_match(string resref)
    {
        _service.GetPlanetTypeByAreaResref(resref).Should().Be(PlanetType.Invalid);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    [TestCase("unknown_area")]
    public void GetPlanetTypeByAreaResref_is_invalid_for_blank_or_unknown_resrefs(string resref)
    {
        _service.GetPlanetTypeByAreaResref(resref).Should().Be(PlanetType.Invalid);
    }

    [Test]
    public void GetPlanetTypeByAreaResref_needs_no_cache_and_no_engine()
    {
        var fresh = new PlanetService();

        fresh.GetPlanetTypeByAreaResref("tat_cantina").Should().Be(PlanetType.Tatooine);
        _engine.Calls.Should().BeEmpty();
    }
}
