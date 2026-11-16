using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Companions;

/// <summary>The tint-map material table and the render tables the 2DAs fill at boot.</summary>
[TestFixture]
public class TintMapCatalogServiceTests
{
    private TintMapWorld _world = null!;
    private AppearanceRenderStateService _render = null!;
    private TintMapStateService _state = null!;
    private TintMapCatalogService _service = null!;
    private readonly Dictionary<string, List<Dictionary<string, string>>> _tables = new();

    [SetUp]
    public void SetUp()
    {
        _world = new TintMapWorld();
        _tables.Clear();
        _world.Engine.SetReturnHandler("Get2DARowCount", arguments =>
            _tables.TryGetValue((string)arguments[0]!, out var rows) ? rows.Count : 0);
        _world.Engine.SetReturnHandler("Get2DAString", arguments =>
            _tables.TryGetValue((string)arguments[0]!, out var rows) && rows[(int)arguments[2]!].TryGetValue((string)arguments[1]!, out var value)
                ? value
                : string.Empty);
        _render = new AppearanceRenderStateService();
        _state = new TintMapStateService();
        _service = new TintMapCatalogService(_render, _state);
    }

    [TearDown]
    public void TearDown() => _world.Dispose();

    private void Table(string name, params (string Column, string Value)[][] rows) =>
        _tables[name] = rows.Select(row => row.ToDictionary(cell => cell.Column, cell => cell.Value)).ToList();

    [Test]
    public void LoadMaterials_ReadsBothTablesAndSkipsIncompleteRows()
    {
        Table("tintmap",
            new[] { ("MODEL", "model_a"), ("MATERIAL", "mat_a"), ("LAYERS", "2,4") },
            new[] { ("MODEL", "model_b"), ("MATERIAL", ""), ("LAYERS", "2") },
            new[] { ("MODEL", "model_c"), ("MATERIAL", "mat_c"), ("LAYERS", "999") });
        Table("cloaktint", new[] { ("MODEL", "cloak_a"), ("MATERIAL", "mat_cloak"), ("LAYERS", "0") });

        _service.LoadMaterials();

        var material = _state.GetMaterials("model_a").Should().ContainSingle().Subject;
        material.Resref.Should().Be("mat_a");
        material.Layers.Should().Equal((TintMapLayerType)2, (TintMapLayerType)4);
        _state.GetMaterials("model_b").Should().BeEmpty();
        _state.GetMaterials("model_c").Should().BeEmpty("a layer value outside the enum names no layer");
        _state.GetMaterials("cloak_a").Should().ContainSingle().Which.Resref.Should().Be("mat_cloak");
    }

    [Test]
    public void LoadNeckCatalog_HandsTheRowsToTheRenderState()
    {
        Table("neckrender", new[] { ("BODY", "pmh0"), ("SOURCE", "pmh0_neck001"), ("RENDER", "pmh0_neck1001") });

        _service.LoadNeckCatalog();

        _render.GetNeckCatalog().Should().NotBeNull();
    }

    [Test]
    public void LoadShoulderCatalog_HandsTheRowsToTheRenderState()
    {
        Table("shoulderrender", new[] { ("BODY", "pmh0"), ("SOURCE", "pmh0_shor001"), ("RENDER", "pmh0_shor1001") });

        _service.LoadShoulderCatalog();

        _render.GetShoulderCatalog().Should().NotBeNull();
    }

    [Test]
    public void LoadPartSkinCatalog_ReadsTheLayoutAndRenderTables()
    {
        Table("partlayout", new[] { ("BODY", "pfh0"), ("LAYOUT", "layout_a") });
        Table("partrender", new[] { ("LAYOUT", "layout_a"), ("SOURCE", "pfh0_chest001"), ("RENDER", "pfh0_chest1001") });

        _service.LoadPartSkinCatalog();

        _render.GetPartSkinCatalog().Should().NotBeNull();
    }

    [Test]
    public void LoadRobeCatalog_KeepsReservedPhenotypesAndRendersEachRobeRow()
    {
        Table("phenotype");
        var phenotype = Enumerable.Range(0, 34).Select(_ => new[] { ("Label", "Normal"), ("DefaultPhenoType", "0") }).ToList();
        phenotype.Add(new[] { ("Label", "RobeRgb_robe001"), ("DefaultPhenoType", "2") });
        Table("phenotype", phenotype.ToArray());
        Table("roberender", new[] { ("MODEL", "pmh0_robe001"), ("PHENOTYPE", "34"), ("BASEPHENOTYPE", "2") });

        _service.LoadRobeCatalog();

        var catalog = _render.GetRobeCatalog();
        catalog.Supports("pmh0_robe001").Should().BeTrue();
        catalog.GetBasePhenotype(34).Should().Be(2);
    }

    [Test]
    public void LoadRobeCatalog_RejectsAMalformedRenderRow()
    {
        Table("phenotype");
        Table("roberender", new[] { ("MODEL", "pmh0_robe001"), ("PHENOTYPE", "x"), ("BASEPHENOTYPE", "2") });

        var load = () => _service.LoadRobeCatalog();

        load.Should().Throw<InvalidOperationException>().WithMessage("*row 0*");
    }
}
