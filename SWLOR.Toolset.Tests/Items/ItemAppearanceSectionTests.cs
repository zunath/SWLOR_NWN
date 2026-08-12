using Avalonia.Headless.NUnit;
using Avalonia.Media;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Editors.Items;

namespace SWLOR.Toolset.Tests.Items
{
    /// <summary>
    /// The Appearance tab: which surface a base item's ModelType produces, and that picking a tile
    /// or editing a cell writes the field(s) the design calls for. Every base item is looked up
    /// against a fixed <see cref="BaseItemIconRow"/> stub rather than the real baseitems.2da, and
    /// artwork is a fake exists-set rather than real texture decoding.
    /// </summary>
    [TestFixture]
    public class ItemAppearanceSectionTests
    {
        private static string CorpusPath(string resRef) =>
            Path.Combine(CorpusLocator.ModuleDirectory, "uti", $"{resRef}.uti.json");

        private static ItemValueStore OpenStore(string resRef) =>
            new(UtiDocument.Load(CorpusPath(resRef)).Fields);

        private static bool RunEdit(string _, Action mutation)
        {
            mutation();
            return true;
        }

        private static ItemAppearanceSectionViewModel Open(
            ItemValueStore store, BaseItemIconRow? row, ISet<string> exists) =>
            new(store, RunEdit, _ => row, resRef => exists.Contains(resRef));

        [TestFixture]
        public class ArmorItems
        {
            // BaseItem 16 ("armor") is ModelType 3; ItemClass is irrelevant there - the icon comes
            // from ArmorPart_Torso instead, per ItemIconResolver.
            private static readonly BaseItemIconRow ArmorRow = new(16, 3, "AArCl", "gifp");

            private static ItemAppearanceSectionViewModel OpenArmor() =>
                Open(OpenStore("adren_harness"), ArmorRow, new HashSet<string>());

            [Test]
            public void ArmorBlueprintOffersTheArmorPartsGrid()
            {
                var section = OpenArmor();

                Assert.That(section.Kind, Is.EqualTo(ItemAppearanceKind.ArmorParts));
                Assert.That(section.Armor, Is.Not.Null);
                Assert.That(section.Gallery, Is.Null);
                Assert.That(section.Bottom, Is.Null);
            }

            [Test]
            public void TorsoAndDyeCellsShowTheStoredValues()
            {
                var section = OpenArmor();

                Assert.That(section.Armor!.Torso.Number, Is.EqualTo(156));
                Assert.That(section.Armor.Cloth1.Number, Is.EqualTo(23));
            }

            [Test]
            public void MirrorDefaultsOnBecauseTheCorpusPairsAreSymmetric()
            {
                var section = OpenArmor();

                Assert.That(section.Armor!.MirrorRightFromLeft, Is.True);
            }

            [Test]
            public void EnsureSelectionFillsAFreshArmorWithThePlainBodyBaseline()
            {
                // The swap-to-armor case: a miscellaneous blueprint carries no ArmorPart_* fields
                // at all, and without defaults every Appearance box arrives empty.
                var store = OpenStore("ark_dragon_troph");
                var section = Open(store, ArmorRow, new HashSet<string>());

                section.EnsureSelection();

                Assert.That(section.Armor!.Torso.Number, Is.EqualTo(1));
                Assert.That(section.Armor.Neck.Number, Is.EqualTo(1));
                Assert.That(section.Armor.Belt.Number, Is.EqualTo(0), "belt defaults to none");
                Assert.That(section.Armor.Robe.Number, Is.EqualTo(0), "robe defaults to none");
                Assert.That(section.Armor.LeftShoulder.Number, Is.EqualTo(0), "shoulders default to none");
                Assert.That(section.Armor.LeftBicep.Number, Is.EqualTo(1));
                Assert.That(section.Armor.RightFoot.Number, Is.EqualTo(1));
                Assert.That(section.Armor.Cloth1.Number, Is.EqualTo(0));
                Assert.That(store.GetInteger(BehaviorFieldStorage.Field, "ArmorPart_Torso"), Is.EqualTo(1));
            }

            [Test]
            public void EnsureSelectionNeverOverwritesStoredArmorFields()
            {
                var store = OpenStore("adren_harness");
                var section = Open(store, ArmorRow, new HashSet<string>());

                section.EnsureSelection();

                Assert.That(section.Armor!.Torso.Number, Is.EqualTo(156), "stored values stand");
                Assert.That(section.Armor.Cloth1.Number, Is.EqualTo(23));
            }

            [Test]
            public void EditingLeftBicepWithMirrorOnWritesBothSidesAndBothXTwins()
            {
                var store = OpenStore("adren_harness");
                var section = Open(store, ArmorRow, new HashSet<string>());

                section.Armor!.LeftBicep.Number = 8;

                Assert.That(section.Armor.LeftBicep.Number, Is.EqualTo(8));
                Assert.That(section.Armor.RightBicep.Number, Is.EqualTo(8), "mirror carries the edit across");
                Assert.That(store.GetInteger(BehaviorFieldStorage.Field, "ArmorPart_LBicep"), Is.EqualTo(8));
                Assert.That(store.GetInteger(BehaviorFieldStorage.Field, "ArmorPart_RBicep"), Is.EqualTo(8));
                Assert.That(store.GetInteger(BehaviorFieldStorage.Field, "xArmorPart_LBice"), Is.EqualTo(8),
                    "the truncated left twin must follow the primary field");
                Assert.That(store.GetInteger(BehaviorFieldStorage.Field, "xArmorPart_RBice"), Is.EqualTo(8),
                    "the truncated right twin must follow too, since mirroring wrote the right side");
            }

            [Test]
            public void TogglingMirrorOffThenEditingLeftOnlyWritesLeft()
            {
                var store = OpenStore("adren_harness");
                var section = Open(store, ArmorRow, new HashSet<string>());

                section.Armor!.MirrorRightFromLeft = false;
                section.Armor.LeftBicep.Number = 9;

                Assert.That(store.GetInteger(BehaviorFieldStorage.Field, "ArmorPart_LBicep"), Is.EqualTo(9));
                Assert.That(store.GetInteger(BehaviorFieldStorage.Field, "ArmorPart_RBicep"), Is.EqualTo(7),
                    "the right side must be untouched once mirroring is off");
                Assert.That(section.Armor.RightBicep.Number, Is.EqualTo(7));
            }

            [Test]
            public void RightCellsStayVisibleButAreReadOnlyWhileMirrored()
            {
                var section = OpenArmor();

                Assert.That(section.Armor!.RightBicep.IsReadOnly, Is.True, "mirrored by default: right cannot diverge");
                Assert.That(section.Armor.RightBicep.IsEnabled, Is.False);

                section.Armor.MirrorRightFromLeft = false;

                Assert.That(section.Armor.RightBicep.IsReadOnly, Is.False);
                Assert.That(section.Armor.RightBicep.IsEnabled, Is.True);
            }

            [Test]
            public void TurningMirrorBackOnWritesRightFromLeftImmediately()
            {
                var store = OpenStore("adren_harness");
                var section = Open(store, ArmorRow, new HashSet<string>());

                section.Armor!.MirrorRightFromLeft = false;
                section.Armor.LeftBicep.Number = 9;
                Assert.That(section.Armor.RightBicep.Number, Is.EqualTo(7), "unmirrored: right stays put");

                section.Armor.MirrorRightFromLeft = true;

                Assert.That(section.Armor.RightBicep.Number, Is.EqualTo(9), "re-mirroring writes right from left");
                Assert.That(store.GetInteger(BehaviorFieldStorage.Field, "ArmorPart_RBicep"), Is.EqualTo(9));
                Assert.That(section.Armor.RightBicep.IsReadOnly, Is.True);
            }

            [Test]
            public void FailedMirrorEditRestoresTheToggleAndRightSideEditing()
            {
                var store = OpenStore("adren_harness");
                ItemAppearanceValues.Write(store, ItemAppearanceFieldNames.Bicep.RightField, 9);
                var armor = new ArmorPartsViewModel(
                    store,
                    (label, mutation) =>
                    {
                        if (label == "Mirror right from left")
                            return false;
                        mutation();
                        return true;
                    });

                armor.MirrorRightFromLeft.Should().BeFalse();

                armor.MirrorRightFromLeft = true;

                armor.MirrorRightFromLeft.Should().BeFalse("the transaction was rejected");
                armor.RightBicep.IsReadOnly.Should().BeFalse();
                ItemAppearanceValues.Read(store.Item, ItemAppearanceFieldNames.Bicep.RightField).Should().Be(9);
            }

            [Test]
            public void ReloadRecomputesMirrorStateAfterExternalChanges()
            {
                var store = OpenStore("adren_harness");
                ItemAppearanceValues.Write(store, ItemAppearanceFieldNames.Bicep.RightField, 9);
                var armor = new ArmorPartsViewModel(store, RunEdit);
                armor.MirrorRightFromLeft.Should().BeFalse();

                ItemAppearanceValues.Write(
                    store,
                    ItemAppearanceFieldNames.Bicep.RightField,
                    ItemAppearanceValues.Read(store.Item, ItemAppearanceFieldNames.Bicep.LeftField)!.Value);
                armor.ReloadFromDocument();

                armor.MirrorRightFromLeft.Should().BeTrue();
                armor.RightBicep.IsReadOnly.Should().BeTrue();
            }

            [Test]
            public void FractionalArmorPartIsRefusedRatherThanTruncated()
            {
                var stored = 7;
                var writes = new List<int>();
                var cell = new ItemFieldCellViewModel(
                    "Left Bicep",
                    () => stored,
                    value =>
                    {
                        writes.Add(value);
                        stored = value;
                        return true;
                    },
                    0,
                    ushort.MaxValue);

                cell.Number = 12.9m;

                writes.Should().BeEmpty();
                cell.Number.Should().Be(7);
            }

            [Test]
            public void DyeCellWriteRoundTrips()
            {
                var store = OpenStore("adren_harness");
                var section = Open(store, ArmorRow, new HashSet<string>());

                section.Armor!.Cloth1.Number = 50;

                Assert.That(section.Armor.Cloth1.Number, Is.EqualTo(50));
                Assert.That(store.GetInteger(BehaviorFieldStorage.Field, "Cloth1Color"), Is.EqualTo(50));
            }

            [Test]
            public void ChoosingAnArmorPresetPreservesIndependentCustomTintOverrides()
            {
                var store = OpenStore("adren_harness");
                var cloth1 = TintMapVariable.GetName("pmh0_chest156", TintMapLayerType.Cloth1);
                var cloth2 = TintMapVariable.GetName("pmh0_chest156", TintMapLayerType.Cloth2);
                store.Locals.SetInt(cloth1, new TintMapColor(12, 34, 56).ToStoredValue());
                store.Locals.SetInt(cloth2, new TintMapColor(65, 43, 21).ToStoredValue());
                var section = Open(store, ArmorRow, new HashSet<string>());

                section.Armor!.Cloth1.Number = 50;

                store.GetInteger(BehaviorFieldStorage.Field, "Cloth1Color").Should().Be(50);
                store.Locals.GetInt(cloth1).Should().NotBeNull(
                    "without global semantic intent the custom value may belong to one material");
                store.Locals.GetInt(cloth2).Should().NotBeNull(
                    "selecting Cloth 1 must not reset another dye channel");
            }

            [Test]
            public void ChoosingAnArmorPresetClearsOnlyOverridesFromThePreviousGlobalCustomTint()
            {
                var store = OpenStore("adren_harness");
                var globalColor = new TintMapColor(12, 34, 56).ToStoredValue();
                var independentColor = new TintMapColor(65, 43, 21).ToStoredValue();
                var inherited = TintMapVariable.GetName("pmh0_chest156", TintMapLayerType.Cloth1);
                var independent = TintMapVariable.GetName("pmh0_bicepl249", TintMapLayerType.Cloth1);
                var otherLayer = TintMapVariable.GetName("pmh0_chest156", TintMapLayerType.Cloth2);
                var state = TintMapVariable.GetItemGlobalColorStateName(TintMapLayerType.Cloth1);
                store.Locals.SetInt(inherited, globalColor);
                store.Locals.SetInt(independent, independentColor);
                store.Locals.SetInt(otherLayer, globalColor);
                store.Locals.SetInt(state, globalColor);
                var section = Open(store, ArmorRow, new HashSet<string>());

                section.Armor!.Cloth1.Number = 50;

                store.GetInteger(BehaviorFieldStorage.Field, "Cloth1Color").Should().Be(50);
                store.Locals.GetInt(inherited).Should().BeNull(
                    "the material that followed the old global custom color now follows the preset");
                store.Locals.GetInt(independent).Should().Be(independentColor,
                    "a separately customized material must retain its own tint");
                store.Locals.GetInt(otherLayer).Should().Be(globalColor,
                    "the preset transition is scoped to one dye channel");
                store.Locals.GetInt(state).Should().BeNull(
                    "the selected preset replaces the old global custom intent");
            }

            [Test]
            public void ChoosingAnArmorPresetClearsUniformLegacyGlobalTintWithoutMarker()
            {
                var store = OpenStore("adren_harness");
                var globalColor = new TintMapColor(12, 34, 56).ToStoredValue();
                var first = TintMapVariable.GetName("pmh0_chest156", TintMapLayerType.Cloth1);
                var second = TintMapVariable.GetName("pmh0_bicepl249", TintMapLayerType.Cloth1);
                store.Locals.SetInt(first, globalColor);
                store.Locals.SetInt(second, globalColor);
                var section = Open(store, ArmorRow, new HashSet<string>());

                section.Armor!.Cloth1.Number = 50;

                store.Locals.GetInt(first).Should().BeNull();
                store.Locals.GetInt(second).Should().BeNull(
                    "a complete uniform pre-marker tint is the legacy global-color representation");
            }

            [Test]
            public void ChoosingAnArmorPresetPreservesMixedLegacyPerMaterialTints()
            {
                var store = OpenStore("adren_harness");
                var first = TintMapVariable.GetName("pmh0_chest156", TintMapLayerType.Cloth1);
                var second = TintMapVariable.GetName("pmh0_bicepl249", TintMapLayerType.Cloth1);
                var firstColor = new TintMapColor(12, 34, 56).ToStoredValue();
                var secondColor = new TintMapColor(65, 43, 21).ToStoredValue();
                store.Locals.SetInt(first, firstColor);
                store.Locals.SetInt(second, secondColor);
                var section = Open(store, ArmorRow, new HashSet<string>());

                section.Armor!.Cloth1.Number = 50;

                store.Locals.GetInt(first).Should().Be(firstColor);
                store.Locals.GetInt(second).Should().Be(secondColor,
                    "mixed legacy values are independent material edits, not global intent");
            }

            [Test]
            public void OutOfRangeDyeValueIsClampedToThePalette()
            {
                var store = OpenStore("adren_harness");
                var section = Open(store, ArmorRow, new HashSet<string>());

                section.Armor!.Cloth1.Number = 999;

                Assert.That(section.Armor.Cloth1.Number, Is.EqualTo(section.Armor.Cloth1.Maximum),
                    "a value past the palette's last colour lands on it rather than being stored raw");
                Assert.That(
                    store.GetInteger(BehaviorFieldStorage.Field, "Cloth1Color"),
                    Is.EqualTo(section.Armor.Cloth1.Maximum));
            }

            [Test]
            public void WithoutPaletteArtworkTheDyeCellFallsBackToIndexEntry()
            {
                // This fixture has no ArmorDyeSwatchService, so there are no colours to draw - the
                // template shows the numeric box instead of an unusable grid of blank chips.
                var section = Open(OpenStore("adren_harness"), ArmorRow, new HashSet<string>());

                Assert.That(section.Armor!.Cloth1.HasPalette, Is.False);
                Assert.That(section.Armor.Cloth1.Swatches, Is.Empty);
                Assert.That(section.Armor.Cloth1.Maximum, Is.EqualTo(175), "NWN's dye range stands in");
            }

            [Test]
            public void PalettePickerCanForbidNumericFallbackForCreatureColors()
            {
                var cell = new ItemDyeCellViewModel(
                    "Skin",
                    () => 7,
                    _ => true,
                    Array.Empty<(byte, byte, byte)>(),
                    allowsNumericFallback: false);

                cell.HasPalette.Should().BeFalse();
                cell.HasNumericFallback.Should().BeFalse(
                    "a creature color must never turn into a raw palette-number field");
                cell.IsPaletteUnavailable.Should().BeTrue();
            }

            [Test]
            public void AFractionalDyeIndexIsRefusedRatherThanTruncated()
            {
                var written = new List<int>();
                var stored = 7;
                var cell = new ItemDyeCellViewModel(
                    "Cloth 1",
                    () => stored,
                    value => { written.Add(value); stored = value; return true; },
                    Array.Empty<(byte, byte, byte)>());

                cell.HasPalette.Should().BeFalse("this is the numeric fallback path");

                cell.Number = 12.9m;

                written.Should().BeEmpty("a dye index is a whole number - 12.9 is not silently stored as 12");
                cell.Number.Should().Be(7, "the box goes back to what the document holds");
            }

            [Test]
            public void ExtremelyLargeDyeInputClampsWithoutOverflowing()
            {
                var stored = 7;
                var cell = new ItemDyeCellViewModel(
                    "Cloth 1",
                    () => stored,
                    value => { stored = value; return true; },
                    Array.Empty<(byte, byte, byte)>());

                cell.Number = decimal.MaxValue;

                stored.Should().Be(cell.Maximum);
                cell.Number.Should().Be(cell.Maximum);
            }

            [Test]
            public void PickingASwatchWritesItsIndexAndKeepsThePickerOpen()
            {
                var store = OpenStore("adren_harness");
                var palette = new[]
                {
                    ((byte)10, (byte)20, (byte)30),
                    ((byte)40, (byte)50, (byte)60),
                    ((byte)70, (byte)80, (byte)90)
                };
                var written = -1;
                var cell = new ItemDyeCellViewModel(
                    "Cloth 1",
                    () => written < 0 ? 0 : written,
                    value => { written = value; return true; },
                    palette);

                cell.HasPalette.Should().BeTrue();
                cell.Swatches.Should().HaveCount(3);
                cell.Swatches[0].IsSelected.Should().BeTrue("index 0 is what the field holds");
                cell.IsPickerOpen = true;

                cell.PickCommand.Execute(cell.Swatches[2]);

                written.Should().Be(2, "the swatch's palette index is what the field stores");
                cell.Number.Should().Be(2);
                cell.Swatches[2].IsSelected.Should().BeTrue();
                cell.Swatches[0].IsSelected.Should().BeFalse();
                cell.SelectedBrush.Should().NotBeNull("the row's chip shows the chosen colour");
                cell.IsPickerOpen.Should().BeTrue(
                    "builders can compare preset colors without repeatedly reopening the popup");

                cell.PickCommand.Execute(cell.Swatches[2]);

                cell.IsPickerOpen.Should().BeTrue(
                    "reselecting the active preset is still an interaction inside the popup");
            }

            [Test]
            public void ReselectingTheCurrentPresetStillClearsAnExternalCustomOverride()
            {
                var stored = 1;
                var hasOverride = true;
                var writes = 0;
                var cell = new ItemDyeCellViewModel(
                    "Cloth 1",
                    () => stored,
                    value =>
                    {
                        writes++;
                        stored = value;
                        hasOverride = false;
                        return true;
                    },
                    new[]
                    {
                        ((byte)10, (byte)20, (byte)30),
                        ((byte)40, (byte)50, (byte)60)
                    },
                    hasExternalOverride: () => hasOverride);

                cell.PickCommand.Execute(cell.Swatches[1]);

                writes.Should().Be(1,
                    "the matching TM_* override must be cleared even when its underlying preset is reselected");
                hasOverride.Should().BeFalse();
                cell.Number.Should().Be(1);
            }

            [AvaloniaTest]
            public void CustomRgbIsAChoiceInsideThePresetPickerAndTheCurrentPresetClearsIt()
            {
                var stored = 1;
                Color? custom = null;
                var cell = new ItemDyeCellViewModel(
                    "Skin",
                    () => stored,
                    value =>
                    {
                        stored = value;
                        custom = null;
                        return true;
                    },
                    new[]
                    {
                        ((byte)10, (byte)20, (byte)30),
                        ((byte)40, (byte)50, (byte)60)
                    },
                    readCustom: () => custom,
                    writeCustom: value =>
                    {
                        custom = value;
                        return true;
                    });

                cell.HasCustomOption.Should().BeTrue();
                cell.IsPickerOpen = true;
                cell.CustomColor = Color.FromRgb(70, 80, 90);
                cell.IsUsingCustomColor.Should().BeTrue();
                cell.IsPickerOpen.Should().BeTrue(
                    "editing custom RGB must leave the shared preset/custom popup open");
                cell.DisplayBrush.Should().BeOfType<SolidColorBrush>()
                    .Which.Color.Should().Be(Color.FromRgb(70, 80, 90));

                cell.PickCommand.Execute(cell.Swatches[1]);

                custom.Should().BeNull(
                    "reselecting the underlying preset must authoritatively replace Custom");
                cell.IsUsingCustomColor.Should().BeFalse();
                cell.DisplayBrush.Should().BeSameAs(cell.SelectedBrush);
                cell.IsPickerOpen.Should().BeTrue(
                    "switching from a custom color to a preset must not dismiss the popup");
            }

            [AvaloniaTest]
            public void PaletteLessCustomColorCanRestoreItsStoredPresetWithoutClosing()
            {
                var stored = 7;
                Color? custom = null;
                var cell = new ItemDyeCellViewModel(
                    "Skin",
                    () => stored,
                    value =>
                    {
                        stored = value;
                        custom = null;
                        return true;
                    },
                    Array.Empty<(byte, byte, byte)>(),
                    allowsNumericFallback: false,
                    readCustom: () => custom,
                    writeCustom: value =>
                    {
                        custom = value;
                        return true;
                    });
                cell.IsPickerOpen = true;
                cell.CustomColor = Color.FromRgb(70, 80, 90);

                cell.CanRestorePreset.Should().BeTrue(
                    "there is no swatch to clear Custom when palette artwork is unavailable");
                cell.RestorePresetCommand.Execute(null);

                custom.Should().BeNull();
                stored.Should().Be(7, "restoring Custom keeps the existing NWN palette choice");
                cell.IsUsingCustomColor.Should().BeFalse();
                cell.CanRestorePreset.Should().BeFalse();
                cell.IsPickerOpen.Should().BeTrue(
                    "restoring a preset is another color choice inside the popup");
            }

        }

        [TestFixture]
        public class CompositeItems
        {
            // BaseItem 512 ("lightsaber") is ModelType 2 with ItemClass "WSwGlsbr" - read directly out
            // of SWLOR_Haks/sw_2da/baseitems.2da rather than assumed.
            private static readonly BaseItemIconRow LightsaberRow = new(512, 2, "WSwGlsbr", "iwswglsbr");

            private static ISet<string> ExistingLightsaberTextures() => new HashSet<string>
            {
                "iWSwGlsbr_b_032", "iWSwGlsbr_m_011", "iWSwGlsbr_t_014", "iWSwGlsbr_t_021"
            };

            [Test]
            public void CompositeBlueprintOffersThreeLayerGalleries()
            {
                var section = Open(OpenStore("bobsaber"), LightsaberRow, ExistingLightsaberTextures());

                Assert.That(section.Kind, Is.EqualTo(ItemAppearanceKind.Composite));
                Assert.That(section.Bottom, Is.Not.Null);
                Assert.That(section.Middle, Is.Not.Null);
                Assert.That(section.Top, Is.Not.Null);
                Assert.That(section.Gallery, Is.Null);
                Assert.That(section.Armor, Is.Null);
            }

            [Test]
            public void BottomSelectionReflectsTheStoredPartWithAModelColorCaption()
            {
                var section = Open(OpenStore("bobsaber"), LightsaberRow, ExistingLightsaberTextures());

                Assert.That(section.Bottom!.Selected, Is.Not.Null);
                Assert.That(section.Bottom.Selected!.Value, Is.EqualTo(32));
                Assert.That(section.Bottom.Selected.Display, Is.EqualTo("3-2"));
            }

            [Test]
            public void MiddleAndTopSelectionsAlsoReflectTheStoredParts()
            {
                var section = Open(OpenStore("bobsaber"), LightsaberRow, ExistingLightsaberTextures());

                Assert.That(section.Middle!.Selected!.Display, Is.EqualTo("1-1"));
                Assert.That(section.Top!.Selected!.Display, Is.EqualTo("1-4"));
            }

            [Test]
            public void ChoosingTheTopOptionWritesModelPart3()
            {
                var store = OpenStore("bobsaber");
                var section = Open(store, LightsaberRow, ExistingLightsaberTextures());

                var option21 = section.Top!.Options.Single(o => o.Value == 21);
                section.Top.Selected = option21;

                Assert.That(store.GetInteger(BehaviorFieldStorage.Field, "ModelPart3"), Is.EqualTo(21));
                Assert.That(section.Top.Selected!.Value, Is.EqualTo(21));
            }

            [Test]
            public void CompositeGalleryIncludesPartZeroAndStoresExtendedPartNumbersSafely()
            {
                var store = OpenStore("bobsaber");
                var textures = ExistingLightsaberTextures();
                textures.Add("iWSwGlsbr_t_000");
                textures.Add("iWSwGlsbr_t_259");
                var section = Open(store, LightsaberRow, textures);

                section.Top!.Options.Should().Contain(option => option.Value == 0);
                var extended = section.Top.Options.Single(option => option.Value == 259);

                section.Top.Selected = extended;

                store.GetInteger(BehaviorFieldStorage.Field, "ModelPart3").Should().Be(byte.MaxValue);
                store.GetInteger(BehaviorFieldStorage.Field, "xModelPart3").Should().Be(259);
                ItemAppearanceValues.Read(store.Item, "ModelPart3").Should().Be(259);
            }
        }

        [TestFixture]
        public class SimpleItems
        {
            // BaseItem 516 ("ess2") is ModelType 0 with ItemClass "it_ess2".
            private static readonly BaseItemIconRow Ess2Row = new(516, 0, "it_ess2", "iinvalid_2x2");

            private static ISet<string> ExistingEss2Textures() => new HashSet<string>
            {
                "iit_ess2_001", "iit_ess2_003", "iit_ess2_005"
            };

            [Test]
            public void SimpleBlueprintOffersOneGalleryOfExactlyTheExistingIcons()
            {
                var section = Open(OpenStore("abdamaryllia"), Ess2Row, ExistingEss2Textures());

                Assert.That(section.Kind, Is.EqualTo(ItemAppearanceKind.Gallery));
                Assert.That(section.Gallery, Is.Not.Null);
                Assert.That(
                    section.Gallery!.Options.Select(option => option.Display),
                    Is.EquivalentTo(new[] { "001", "003", "005" }));
            }

            [Test]
            public void SelectedOptionTracksTheStoredModelPart1()
            {
                var section = Open(OpenStore("abdamaryllia"), Ess2Row, ExistingEss2Textures());

                Assert.That(section.Gallery!.Selected, Is.Not.Null);
                Assert.That(section.Gallery.Selected!.Display, Is.EqualTo("005"));
            }

            [Test]
            public void SelectingATileWritesModelPart1()
            {
                var store = OpenStore("abdamaryllia");
                var section = Open(store, Ess2Row, ExistingEss2Textures());

                var option001 = section.Gallery!.Options.Single(o => o.Display == "001");
                section.Gallery.Selected = option001;

                Assert.That(store.GetInteger(BehaviorFieldStorage.Field, "ModelPart1"), Is.EqualTo(1));
                Assert.That(section.Gallery.Selected!.Value, Is.EqualTo(1));
            }

            [Test]
            public void ExtendedModelWithoutAUniqueIconIsStillSelectableAndPreviewable()
            {
                var scratch = Path.Combine(
                    Path.GetTempPath(),
                    "swlor-item-parts-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(scratch);
                try
                {
                    File.WriteAllBytes(Path.Combine(scratch, "helm_309.mdl"), Array.Empty<byte>());
                    var resources = new ResourceIndex(
                        baseLayer: null,
                        hakLayersInOrder: new[] { new ResourceIndex.HakLayer("fixture", scratch) });
                    var catalog = new ArmorPartCatalog(resources);
                    var row = new BaseItemIconRow(17, 1, "helm", "ihelm");
                    var section = new ItemAppearanceSectionViewModel(
                        OpenStore("abdamaryllia"),
                        RunEdit,
                        _ => row,
                        _ => false,
                        armorPartModels: catalog);

                    var part = section.Gallery!.Options.Single(option => option.Value == 309);
                    part.Choice.ModelResRef.Should().Be("helm_309",
                        "a model thumbnail can represent a part whose inventory icon falls back");
                }
                finally
                {
                    Directory.Delete(scratch, recursive: true);
                }
            }
        }

        [TestFixture]
        public class ProbingBounds
        {
            [Test]
            public void RebuildingAGalleryItemNeverProbesMoreThanSixteenHundredTimes()
            {
                var calls = 0;
                var row = new BaseItemIconRow(516, 0, "it_ess2", "iinvalid_2x2");
                var store = OpenStore("abdamaryllia");

                _ = new ItemAppearanceSectionViewModel(store, RunEdit, _ => row, _ =>
                {
                    calls++;
                    return false;
                });

                Assert.That(calls, Is.LessThanOrEqualTo(1600));
            }

            [Test]
            public void RebuildingACompositeItemNeverProbesMoreThanSixteenHundredTimes()
            {
                var calls = 0;
                var row = new BaseItemIconRow(512, 2, "WSwGlsbr", "iwswglsbr");
                var store = OpenStore("bobsaber");

                _ = new ItemAppearanceSectionViewModel(store, RunEdit, _ => row, _ =>
                {
                    calls++;
                    return false;
                });

                Assert.That(calls, Is.LessThanOrEqualTo(1600));
            }
        }

        [TestFixture]
        public class EnsureSelectionAfterABaseTypeChange
        {
            [Test]
            public void GalleryPicksTheFirstOfferedTileWhenNothingIsSelectedAfterASwitch()
            {
                var store = OpenStore("abdamaryllia");
                var ess2Row = new BaseItemIconRow(516, 0, "it_ess2", "iinvalid_2x2");
                var ess3Row = new BaseItemIconRow(517, 0, "it_ess3", "iinvalid_2x2");
                BaseItemIconRow currentRow = ess2Row;
                ISet<string> currentTextures = new HashSet<string> { "iit_ess2_001", "iit_ess2_003", "iit_ess2_005" };

                var section = new ItemAppearanceSectionViewModel(
                    store, RunEdit, _ => currentRow, resRef => currentTextures.Contains(resRef));

                // Stored ModelPart1 is 5 - matches the ess2 gallery's "005" tile.
                Assert.That(section.Gallery!.Selected!.Display, Is.EqualTo("005"));

                // Simulate the base-type change: a different row, a different set of textures whose
                // only part number (10) never matches the still-stored ModelPart1 (5).
                currentRow = ess3Row;
                currentTextures = new HashSet<string> { "iit_ess3_010" };
                section.Rebuild();

                Assert.That(section.Gallery!.Selected, Is.Null, "nothing offered by the new row matches the old stored part");

                section.EnsureSelection();

                Assert.That(section.Gallery!.Selected, Is.Not.Null);
                Assert.That(section.Gallery.Selected!.Display, Is.EqualTo("010"));
                Assert.That(store.GetInteger(BehaviorFieldStorage.Field, "ModelPart1"), Is.EqualTo(10),
                    "EnsureSelection writes through the same path a manual tile pick would");
            }

            [Test]
            public void EnsuredCompositeDefaultsResolveToARealIcon()
            {
                var store = OpenStore("bobsaber");
                var vibrobladeRow = new BaseItemIconRow(0, 2, "WSwVibro", "iwswvibro");
                var textures = new HashSet<string>
                {
                    "iWSwVibro_b_001", "iWSwVibro_m_001", "iWSwVibro_t_001"
                };

                var section = new ItemAppearanceSectionViewModel(
                    store, RunEdit, _ => vibrobladeRow, resRef => textures.Contains(resRef));
                section.EnsureSelection();

                // Defaulted parts must compose a real inventory icon - non-null part numbers that
                // resolve to nothing would leave the preview empty after a base-type switch.
                var stacks = SWLOR.Toolset.Domain.Render.Icons.ItemIconResolver.Resolve(
                    store.Item, _ => vibrobladeRow);
                Assert.That(
                    stacks.Any(stack => stack.Layers.All(textures.Contains)),
                    Is.True,
                    "the ensured defaults must resolve to existing icon artwork");
            }

            [Test]
            public void CompositePicksTheFirstOfferedPartForEachLayerWhenNothingIsSelected()
            {
                var store = OpenStore("bobsaber");
                var lightsaberRow = new BaseItemIconRow(512, 2, "WSwGlsbr", "iwswglsbr");
                var vibrobladeRow = new BaseItemIconRow(0, 2, "WSwVibro", "iwswvibro");
                BaseItemIconRow currentRow = lightsaberRow;
                ISet<string> currentTextures = new HashSet<string>
                {
                    "iWSwGlsbr_b_032", "iWSwGlsbr_m_011", "iWSwGlsbr_t_014"
                };

                var section = new ItemAppearanceSectionViewModel(
                    store, RunEdit, _ => currentRow, resRef => currentTextures.Contains(resRef));

                Assert.That(section.Bottom!.Selected, Is.Not.Null);

                // A different composite base type whose only offered parts (1) never match the
                // still-stored ModelPart1/2/3 (32/11/14).
                currentRow = vibrobladeRow;
                currentTextures = new HashSet<string>
                {
                    "iWSwVibro_b_001", "iWSwVibro_m_001", "iWSwVibro_t_001"
                };
                section.Rebuild();

                Assert.That(section.Bottom!.Selected, Is.Null);
                Assert.That(section.Middle!.Selected, Is.Null);
                Assert.That(section.Top!.Selected, Is.Null);

                section.EnsureSelection();

                Assert.That(section.Bottom!.Selected!.Value, Is.EqualTo(1));
                Assert.That(section.Middle!.Selected!.Value, Is.EqualTo(1));
                Assert.That(section.Top!.Selected!.Value, Is.EqualTo(1));
                Assert.That(store.GetInteger(BehaviorFieldStorage.Field, "ModelPart1"), Is.EqualTo(1));
                Assert.That(store.GetInteger(BehaviorFieldStorage.Field, "ModelPart2"), Is.EqualTo(1));
                Assert.That(store.GetInteger(BehaviorFieldStorage.Field, "ModelPart3"), Is.EqualTo(1));
            }

            [Test]
            public void CompositeDefaultsAreCommittedAsOneEditAndOnePreviewRefresh()
            {
                var store = OpenStore("bobsaber");
                var row = new BaseItemIconRow(0, 2, "WSwVibro", "iwswvibro");
                var textures = new HashSet<string>
                {
                    "iWSwVibro_b_001", "iWSwVibro_m_001", "iWSwVibro_t_001"
                };
                var edits = 0;
                var previews = 0;
                var section = new ItemAppearanceSectionViewModel(
                    store,
                    (_, mutation) =>
                    {
                        edits++;
                        mutation();
                        return true;
                    },
                    _ => row,
                    textures.Contains,
                    appearanceChanged: () => previews++);

                section.EnsureSelection();

                edits.Should().Be(1);
                previews.Should().Be(1);
            }
        }

        [Test]
        public void UnknownBaseItemOffersNothing()
        {
            var section = Open(OpenStore("adren_harness"), null, new HashSet<string>());

            Assert.That(section.Kind, Is.EqualTo(ItemAppearanceKind.None));
            Assert.That(section.Gallery, Is.Null);
            Assert.That(section.Bottom, Is.Null);
            Assert.That(section.Middle, Is.Null);
            Assert.That(section.Top, Is.Null);
            Assert.That(section.Armor, Is.Null);
        }
    }
}
