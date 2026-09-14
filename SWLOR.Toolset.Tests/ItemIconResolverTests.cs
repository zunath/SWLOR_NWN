using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Render.Icons;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The inventory-icon naming rules for item blueprints. Hermetic on purpose - the patterns
    /// themselves were derived from the real corpus (see <see cref="BlueprintIconCoverageTests"/>, which
    /// checks them against every uti and every hak), so what is pinned here is the rule per ModelType and
    /// the order candidates are offered in.
    /// </summary>
    public class ItemIconResolverTests
    {
        private static JsonGffStruct Item(int baseItem, int part1 = 0, int part2 = 0, int part3 = 0, int torso = 0)
        {
            var root = new JsonGffStruct();
            SetInt(root, "BaseItem", baseItem);
            SetInt(root, "ModelPart1", part1);
            SetInt(root, "ModelPart2", part2);
            SetInt(root, "ModelPart3", part3);
            SetInt(root, "ArmorPart_Torso", torso);
            return root;
        }

        private static void SetInt(JsonGffStruct root, string name, int value)
        {
            var field = JsonGffField.CreateScalar(GffFieldType.Int, Encoding.ASCII.GetBytes("0"));
            field.SetInteger(value);
            root.Add(name, field);
        }

        private static Func<int, BaseItemIconRow?> Table(int modelType, string itemClass, string? defaultIcon = "igeneric") =>
            id => id == 7 ? new BaseItemIconRow(7, modelType, itemClass, defaultIcon) : null;

        private static IReadOnlyList<string> FirstLayers(IReadOnlyList<IconLayerStack> stacks) =>
            stacks.Select(stack => stack.Layers[0]).ToList();

        [Test]
        public void Simple_Items_Are_Named_ItemClass_And_Part_Number()
        {
            var stacks = ItemIconResolver.Resolve(Item(7, part1: 51), Table(0, "it_belt"));

            FirstLayers(stacks).Should().StartWith(new[] { "iit_belt_051" });
        }

        [Test]
        public void Simple_Items_Also_Offer_The_Unseparated_Spelling()
        {
            var stacks = ItemIconResolver.Resolve(Item(7, part1: 51), Table(0, "it_belt"));

            FirstLayers(stacks).Should().Contain("iit_belt051");
        }

        [Test]
        public void Part_Items_Offer_Both_The_Plain_And_Sized_Spellings()
        {
            // Helmets are ihelm_223; cloaks are the same ModelType but icloak_m_001 - hence both.
            var stacks = ItemIconResolver.Resolve(Item(7, part1: 117), Table(1, "cloak"));

            FirstLayers(stacks).Should().ContainInOrder("icloak_117", "icloak_m_117");
        }

        [Test]
        public void Composite_Weapons_Produce_One_Three_Layer_Stack()
        {
            var stacks = ItemIconResolver.Resolve(
                Item(7, part1: 28, part2: 28, part3: 153), Table(2, "WSwGs"));

            stacks[0].Layers.Should().Equal("iWSwGs_b_028", "iWSwGs_m_028", "iWSwGs_t_153");
        }

        [Test]
        public void ExtendedModelPartOverridesTheLegacyByteForCompositeIcons()
        {
            var root = Item(7, part1: 28, part2: 28, part3: 255);
            SetInt(root, "xModelPart3", 259);

            var stacks = ItemIconResolver.Resolve(root, Table(2, "WSwGs"));

            stacks[0].Layers.Should().Equal("iWSwGs_b_028", "iWSwGs_m_028", "iWSwGs_t_259");
        }

        [Test]
        public void Armor_Uses_The_Body_Part_Icons_Keyed_On_Torso_Not_Its_Own_ItemClass()
        {
            // iAArCl_### exists nowhere in the base game or any hak; armor icons are ip{m,f}_chest###.
            var stacks = ItemIconResolver.Resolve(Item(7, torso: 156), Table(3, "AArCl"));

            FirstLayers(stacks).Should().ContainInOrder("ipm_chest156", "ipf_chest156");
        }

        [Test]
        public void ExtendedTorsoPartControlsTheArmorIcon()
        {
            var root = Item(7, torso: 255);
            SetInt(root, "xArmorPart_Torso", 270);

            var stacks = ItemIconResolver.Resolve(root, Table(3, "AArCl"));

            FirstLayers(stacks).Should().ContainInOrder("ipm_chest270", "ipf_chest270");
        }

        [Test]
        public void The_Base_Items_Default_Icon_Is_Always_The_Last_Resort()
        {
            var stacks = ItemIconResolver.Resolve(Item(7, part1: 3), Table(0, "it_belt", "iit_belt"));

            stacks.Should().HaveCountGreaterThan(1);
            stacks[^1].Layers.Should().Equal("iit_belt");
        }

        [Test]
        public void A_Row_With_No_Default_Icon_Contributes_No_Fallback_Candidate()
        {
            var stacks = ItemIconResolver.Resolve(Item(7, part1: 3), Table(0, "it_belt", defaultIcon: null));

            FirstLayers(stacks).Should().OnlyContain(layer => layer.StartsWith("iit_belt_") || layer.StartsWith("iit_belt0"));
        }

        [Test]
        public void An_Unresolvable_Base_Item_Yields_No_Candidates()
        {
            var stacks = ItemIconResolver.Resolve(Item(999), Table(0, "it_belt"));

            stacks.Should().BeEmpty(because: "the caller falls back to the item type symbol");
        }

        [Test]
        public void A_Missing_BaseItem_Field_Yields_No_Candidates()
        {
            ItemIconResolver.Resolve(new JsonGffStruct(), Table(0, "it_belt")).Should().BeEmpty();
        }

        [Test]
        public void An_Unknown_ModelType_Still_Offers_Both_Common_Shapes()
        {
            var stacks = ItemIconResolver.Resolve(Item(7, part1: 12), Table(-1, "it_thing"));

            FirstLayers(stacks).Should().ContainInOrder("iit_thing_012", "iit_thing_b_012");
        }
    }
}
