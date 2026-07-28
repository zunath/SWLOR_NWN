using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.GameData.Lookups;
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

                Assert.That(section.Armor!.Torso.Value, Is.EqualTo("156"));
                Assert.That(section.Armor.Cloth1.Value, Is.EqualTo("23"));
            }

            [Test]
            public void MirrorDefaultsOnBecauseTheCorpusPairsAreSymmetric()
            {
                var section = OpenArmor();

                Assert.That(section.Armor!.MirrorRightFromLeft, Is.True);
            }

            [Test]
            public void EditingLeftBicepWithMirrorOnWritesBothSidesAndBothXTwins()
            {
                var store = OpenStore("adren_harness");
                var section = Open(store, ArmorRow, new HashSet<string>());

                section.Armor!.LeftBicep.Value = "8";

                Assert.That(section.Armor.LeftBicep.Value, Is.EqualTo("8"));
                Assert.That(section.Armor.RightBicep.Value, Is.EqualTo("8"), "mirror carries the edit across");
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
                section.Armor.LeftBicep.Value = "9";

                Assert.That(store.GetInteger(BehaviorFieldStorage.Field, "ArmorPart_LBicep"), Is.EqualTo(9));
                Assert.That(store.GetInteger(BehaviorFieldStorage.Field, "ArmorPart_RBicep"), Is.EqualTo(7),
                    "the right side must be untouched once mirroring is off");
                Assert.That(section.Armor.RightBicep.Value, Is.EqualTo("7"));
            }

            [Test]
            public void MirrorOffExposesTheRightCells()
            {
                var section = OpenArmor();

                Assert.That(section.Armor!.ShowsRightCells, Is.False, "mirrored by default: right is redundant");

                section.Armor.MirrorRightFromLeft = false;

                Assert.That(section.Armor.ShowsRightCells, Is.True);
            }

            [Test]
            public void DyeCellWriteRoundTrips()
            {
                var store = OpenStore("adren_harness");
                var section = Open(store, ArmorRow, new HashSet<string>());

                section.Armor!.Cloth1.Value = "50";

                Assert.That(section.Armor.Cloth1.Value, Is.EqualTo("50"));
                Assert.That(store.GetInteger(BehaviorFieldStorage.Field, "Cloth1Color"), Is.EqualTo(50));
            }

            [Test]
            public void OutOfRangeDyeValueIsRefused()
            {
                var store = OpenStore("adren_harness");
                var section = Open(store, ArmorRow, new HashSet<string>());

                section.Armor!.Cloth1.Value = "176";

                Assert.That(section.Armor.Cloth1.Value, Is.EqualTo("23"), "the refused edit restores what is stored");
                Assert.That(store.GetInteger(BehaviorFieldStorage.Field, "Cloth1Color"), Is.EqualTo(23));
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
