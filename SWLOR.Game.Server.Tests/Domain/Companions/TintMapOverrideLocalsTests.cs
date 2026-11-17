using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.Companions;

/// <summary>The tint locals stored on items and droids and how they move between objects.</summary>
[TestFixture]
public class TintMapOverrideLocalsTests
{
    private const uint Droid = 10;
    private const uint Source = 20;
    private const uint Target = 21;

    private TintMapWorld _world = null!;

    [SetUp]
    public void SetUp()
    {
        _world = new TintMapWorld();
        _world.Add(Droid, ObjectType.Creature);
        _world.Add(Source, ObjectType.Item);
        _world.Add(Target, ObjectType.Item);
    }

    [TearDown]
    public void TearDown() => _world.Dispose();

    [Test]
    public void RestoreDroidOverrides_WritesOnlyPositiveNamedColors()
    {
        TintMapOverrideLocals.RestoreDroidOverrides(Droid, new Dictionary<string, int>
        {
            ["TM_a"] = 5, [" "] = 7, ["TM_b"] = 0, ["TM_c"] = -3
        });

        _world.LocalsOf(Droid).Should().BeEquivalentTo(new Dictionary<string, int> { ["TM_a"] = 5 });
    }

    [Test]
    public void RestoreDroidOverrides_IgnoresAnInvalidDroidAndAMissingList()
    {
        TintMapOverrideLocals.RestoreDroidOverrides(999, new Dictionary<string, int> { ["TM_a"] = 5 });
        TintMapOverrideLocals.RestoreDroidOverrides(Droid, null!);

        _world.LocalsOf(999).Should().BeEmpty();
        _world.LocalsOf(Droid).Should().BeEmpty();
    }

    [Test]
    public void GetItemTintOverrides_ReturnsOnlyTheTintLocals()
    {
        var material = TintMapVariable.GetName("pmh0_chest001", TintMapLayerType.Cloth1);
        var global = TintMapVariable.GetItemGlobalColorStateName(TintMapLayerType.Cloth1);
        _world.SetInt(Source, material, 12);
        _world.SetInt(Source, global, new TintMapColor(1, 2, 3).ToStoredValue());
        _world.SetInt(Source, "UNRELATED", 99);

        var overrides = TintMapOverrideLocals.GetItemTintOverrides(Source);

        overrides.Keys.Should().BeEquivalentTo(new[] { material, global });
        overrides[material].Should().Be(12);
    }

    [Test]
    public void ReplaceItemTintOverrides_ClearsTheTargetAndCopiesTheSource()
    {
        var kept = TintMapVariable.GetName("old_material", TintMapLayerType.Metal1);
        var copied = TintMapVariable.GetName("new_material", TintMapLayerType.Cloth2);
        _world.SetInt(Target, kept, 3);
        _world.SetInt(Target, "UNRELATED", 4);
        _world.SetInt(Source, copied, 9);

        TintMapOverrideLocals.ReplaceItemTintOverrides(Source, Target);

        _world.Int(Target, kept).Should().Be(0);
        _world.Int(Target, copied).Should().Be(9);
        _world.Int(Target, "UNRELATED").Should().Be(4);
    }

    [Test]
    public void ReplaceItemTintOverrides_DoesNothingWhenEitherItemIsInvalid()
    {
        var name = TintMapVariable.GetName("material", TintMapLayerType.Cloth1);
        _world.SetInt(Source, name, 9);

        TintMapOverrideLocals.ReplaceItemTintOverrides(Source, 999);
        TintMapOverrideLocals.ReplaceItemTintOverrides(999, Source);

        _world.Int(Source, name).Should().Be(9);
    }
}
