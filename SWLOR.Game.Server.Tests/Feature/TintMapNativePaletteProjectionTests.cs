using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Service.DroidService;

namespace SWLOR.Game.Server.Tests.Feature;

[TestFixture]
public class TintMapNativePaletteProjectionTests
{
    [Test]
    public void BothNativeMetalLayersUseTheSameArmorPaletteWithIndependentRowCoordinates()
    {
        for (var colorId = 0; colorId < TintMapMaterialRegistry.PaletteColorCount; colorId++)
        {
            var color = TintMapPaletteColors.GetColor(TintMapLayerType.Metal1, colorId);
            TintMapPaletteColors.GetColor(TintMapLayerType.Metal2, colorId).Should().Be(color,
                "native constructors select pal_armor01 for both metal channels");
            TintMapPaletteColors.GetClosestColorId(TintMapLayerType.Metal2, color).Should()
                .Be(TintMapPaletteColors.GetClosestColorId(TintMapLayerType.Metal1, color));
        }
        TintMapMaterialRegistry.GetLayer(TintMapLayerType.Metal1).PaletteBaseRow.Should().Be(352);
        TintMapMaterialRegistry.GetLayer(TintMapLayerType.Metal2).PaletteBaseRow.Should().Be(528,
            "the independent metal2 shader coordinate remains compatible with saved palette IDs");
    }

    [TestCase(0)]
    [TestCase(31)]
    [TestCase(174)]
    [TestCase(255)]
    public void AuthoredColorsWithoutCustomTintRequireNoProjection(int authored)
    {
        TintMapNativePaletteProjection.Resolve(authored, 0, 0, null, 255)
            .Should().Be(new TintMapNativePaletteProjection.Update(authored, 0, 0));
    }

    [Test]
    public void RepeatedCustomColorChangesKeepTheOriginalPresetForReset()
    {
        var first = TintMapNativePaletteProjection.Resolve(31, 0, 0, 5);
        var repeated = TintMapNativePaletteProjection.Resolve(first.Color, first.Baseline, first.LastApplied, 5);
        repeated.Should().Be(first);

        var edited = TintMapNativePaletteProjection.Resolve(repeated.Color, repeated.Baseline, repeated.LastApplied, 77);
        TintMapNativePaletteProjection.GetBaseline(edited.Color, edited.Baseline, edited.LastApplied)
            .Should().Be(31, "the preset UI must not read the projected custom color as the original");
        TintMapNativePaletteProjection.Resolve(edited.Color, edited.Baseline, edited.LastApplied, null)
            .Should().Be(new TintMapNativePaletteProjection.Update(31, 0, 0));
    }

    [TestCase(255, 255)]
    [TestCase(0, 255)]
    [TestCase(0, 0)]
    [TestCase(74, 74)]
    public void PerPartResetPreservesExplicitPresetsOrGlobalInheritance(int original, int inherited)
    {
        var projected = TintMapNativePaletteProjection.Resolve(original, 0, 0, 0, inherited);
        var repeated = TintMapNativePaletteProjection.Resolve(0, projected.Baseline, projected.LastApplied, 0, 255);
        repeated.Baseline.Should().Be(inherited + 1,
            "a projected palette zero must not be mistaken for a newly missing legacy field");
        TintMapNativePaletteProjection.Resolve(repeated.Color, repeated.Baseline, repeated.LastApplied, null, 255)
            .Should().Be(new TintMapNativePaletteProjection.Update(inherited, 0, 0));
    }

    [Test]
    public void ExternalPresetEditBecomesTheNewBaselineWhileCustomTintRemainsActive()
    {
        var projected = TintMapNativePaletteProjection.Resolve(174, 0, 0, 5);
        TintMapNativePaletteProjection.GetBaseline(89, projected.Baseline, projected.LastApplied)
            .Should().Be(89);
        var refreshed = TintMapNativePaletteProjection.Resolve(89, projected.Baseline, projected.LastApplied, 5);
        refreshed.Should().Be(new TintMapNativePaletteProjection.Update(5, 90, 6));
        TintMapNativePaletteProjection.Resolve(refreshed.Color, refreshed.Baseline, refreshed.LastApplied, null)
            .Should().Be(new TintMapNativePaletteProjection.Update(89, 0, 0));
    }

    [Test]
    public void ExternalClearToInheritanceIsRetainedAfterReset()
    {
        var projected = TintMapNativePaletteProjection.Resolve(174, 0, 0, 5);
        var cleared = TintMapNativePaletteProjection.Resolve(255, projected.Baseline, projected.LastApplied, 5, 255);
        TintMapNativePaletteProjection.Resolve(cleared.Color, cleared.Baseline, cleared.LastApplied, null)
            .Should().Be(new TintMapNativePaletteProjection.Update(255, 0, 0));
    }

    [Test]
    public void DroidProjectionStateSurvivesPersistenceWithoutReplacingTheAuthoredPreset()
    {
        var projection = TintMapNativePaletteProjection.Resolve(2, 0, 0, 7);
        var droid = new ConstructedDroid();
        droid.TintOverrides[TintMapNativePaletteProjection.BaselineName(0)] = projection.Baseline;
        droid.TintOverrides[TintMapNativePaletteProjection.LastAppliedName(0)] = projection.LastApplied;
        var restored = JsonConvert.DeserializeObject<ConstructedDroid>(JsonConvert.SerializeObject(droid))!;
        TintMapNativePaletteProjection.Resolve(7,
                restored.TintOverrides[TintMapNativePaletteProjection.BaselineName(0)],
                restored.TintOverrides[TintMapNativePaletteProjection.LastAppliedName(0)], null)
            .Should().Be(new TintMapNativePaletteProjection.Update(2, 0, 0));
    }

    [TestCase("TMP_B_114", true)]
    [TestCase("TMP_L_119", true)]
    [TestCase("TMP_B_8", true)]
    [TestCase("TMP_B_120", false)]
    [TestCase("TMP_B_-1", false)]
    [TestCase("TM_pfh0_robe187_4", false)]
    [TestCase("TMC_0", false)]
    [TestCase("TMP_B_wrong", false)]
    public void ProjectionStateHasAnIndependentBoundedPersistenceNamespace(string name, bool expected)
    {
        TintMapNativePaletteProjection.IsStateName(name).Should().Be(expected);
    }
}
