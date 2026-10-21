using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

[TestFixture]
public class HeadTintChannelsTests
{
    private static TintMapMaterialSelection Selection(string model, bool item, params TintMapLayerType[] layers) =>
        new(model, new TintMapMaterialDefinition("Test", "test", layers), 1, 1, item, AppearanceArmor.Invalid);

    [Test]
    public void NoHeadKeepsOnlyNativeCreatureChannels()
    {
        HeadTintChannels.GetLayers(new[] {
            Selection("pmh0_chest303", false, TintMapLayerType.Cloth1),
            Selection("helm_001", true, TintMapLayerType.Metal2)
        }).Should().Equal(TintMapLayerType.Skin, TintMapLayerType.Hair,
            TintMapLayerType.Tattoo1, TintMapLayerType.Tattoo2);
    }

    [Test]
    public void MultipleHeadMaterialsExposeOnlyTheirUnionInStableOrder()
    {
        HeadTintChannels.GetLayers(new[] {
            Selection("pfh0_head311", false, TintMapLayerType.Metal1, TintMapLayerType.Cloth1),
            Selection("PFH0_HEAD311", false, TintMapLayerType.Cloth1, TintMapLayerType.Cloth2, TintMapLayerType.Leather1),
            Selection("pmh0_chest303", true, TintMapLayerType.Metal2)
        }).Should().Equal(TintMapLayerType.Skin, TintMapLayerType.Hair,
            TintMapLayerType.Tattoo1, TintMapLayerType.Tattoo2,
            TintMapLayerType.Cloth1, TintMapLayerType.Cloth2, TintMapLayerType.Leather1, TintMapLayerType.Metal1);
    }

    [Test]
    public void HeadSwitchRetainsSemanticChannelInsteadOfOldListIndex()
    {
        var oldLayers = HeadTintChannels.GetLayers(new[] {Selection("pfh0_head311", false,
            TintMapLayerType.Cloth1, TintMapLayerType.Metal1)});
        var next = HeadTintChannels.GetLayers(new[] {Selection("pmh0_head323", false, TintMapLayerType.Metal1)});
        HeadTintChannels.GetSelectedIndex(oldLayers, TintMapLayerType.Metal1).Should().Be(5);
        HeadTintChannels.GetSelectedIndex(next, TintMapLayerType.Metal1).Should().Be(4);
        HeadTintChannels.GetSelectedIndex(next, TintMapLayerType.Cloth1).Should().Be(0);
    }

    [Test]
    public void ItemHeadModelsCannotExposeEquipmentChannelsInAppearance()
    {
        HeadTintChannels.GetLayers(new[] {
            Selection("pmh0_head327", true, TintMapLayerType.Cloth1),
            Selection("pmh0_neck001", false, TintMapLayerType.Leather2)
        }).Should().Equal(HeadTintChannels.GetLayers(Array.Empty<TintMapMaterialSelection>()));
    }

    [TestCase(TintMapLayerType.Skin, "Skin Color", "gui_pal_skin")]
    [TestCase(TintMapLayerType.Hair, "Hair Color", "gui_pal_hair01")]
    [TestCase(TintMapLayerType.Tattoo1, "Tattoo 1 Color", "gui_pal_tattoo")]
    [TestCase(TintMapLayerType.Tattoo2, "Tattoo 2 Color", "gui_pal_tattoo")]
    [TestCase(TintMapLayerType.Cloth1, "Cloth 1", "gui_pal_tattoo")]
    [TestCase(TintMapLayerType.Cloth2, "Cloth 2", "gui_pal_tattoo")]
    [TestCase(TintMapLayerType.Leather1, "Leather 1", "gui_pal_tattoo")]
    [TestCase(TintMapLayerType.Leather2, "Leather 2", "gui_pal_tattoo")]
    [TestCase(TintMapLayerType.Metal1, "Metal 1", "gui_pal_armor01")]
    [TestCase(TintMapLayerType.Metal2, "Metal 2", "gui_pal_armor01")]
    public void EditorMapsEachVisibleChannelToItsSemanticLayerAndPalette(
        TintMapLayerType layer, string label, string palette)
    {
        var layers = HeadTintChannels.GetLayers(new[] {
            Selection("pfh0_head327", false, Enum.GetValues<TintMapLayerType>())
        });
        var editor = new AppearanceEditorViewModel { IsAppearanceSelected = true };
        typeof(AppearanceEditorViewModel).GetField("_appearanceColorLayers",
            BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, layers);
        editor.SelectedColorCategoryIndex = HeadTintChannels.GetSelectedIndex(layers, layer);

        var arguments = new object[] { default(TintMapLayerType) };
        typeof(AppearanceEditorViewModel).GetMethod("TryGetSelectedTintLayer",
            BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, arguments).Should().Be(true);
        arguments[0].Should().Be(layer);
        editor.ColorSheetResref.Should().Be(palette);
        HeadTintChannels.GetLabel(layer).Should().Be(label);
    }

    [TestCase(-1)]
    [TestCase(4)]
    public void EditorRejectsChannelIndicesNoLongerAvailableOnTheHead(int index)
    {
        var editor = new AppearanceEditorViewModel { IsAppearanceSelected = true };
        editor.SelectedColorCategoryIndex = index;
        typeof(AppearanceEditorViewModel).GetMethod("TryGetSelectedTintLayer",
            BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor,
                new object[] { default(TintMapLayerType) }).Should().Be(false);
    }

    [Test]
    public void SharedHeadStateNamesAreSeparateFromNativeAndItemColorState()
    {
        foreach (var layer in Enum.GetValues<TintMapLayerType>())
        {
            if (TintMapVariable.IsCreatureColorLayer(layer))
            {
                var create = () => TintMapVariable.GetHeadColorStateName(layer);
                create.Should().Throw<ArgumentOutOfRangeException>();
                TintMapVariable.IsHeadColorStateName($"TMH_{(int)layer}").Should().BeFalse();
            }
            else
            {
                var name = TintMapVariable.GetHeadColorStateName(layer);
                TintMapVariable.IsHeadColorStateName(name).Should().BeTrue();
                TintMapVariable.IsCreatureColorStateName(name).Should().BeFalse();
                TintMapVariable.IsItemGlobalColorStateName(name).Should().BeFalse();
            }
        }
        foreach (var invalid in new[] { null, "", "TMH_99", "TMH_-1", "TMH_lips", "TMG_7", "TM_lips_7" })
            TintMapVariable.IsHeadColorStateName(invalid).Should().BeFalse();
    }
}
