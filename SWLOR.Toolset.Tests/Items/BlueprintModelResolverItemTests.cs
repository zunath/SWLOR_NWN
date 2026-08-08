using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Plt;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests.Items
{
    /// <summary>
    /// Coverage for <see cref="BlueprintModelResolver"/>'s <see cref="ResourceType.Uti"/> case: which
    /// model shape each baseitems.2da ModelType resolves to. Uses a fake <see cref="BaseItemIconRow"/>
    /// lookup and fake exists-functions rather than the real 2DA/hak corpus - the naming itself
    /// (<c>{ItemClass}_b/_m/_t_{part:D3}</c> for a composite, <c>{ItemClass}_{part:D3}</c> for a
    /// ground model) was separately verified against real MDL resources in the sw_weapon/sw_item/
    /// sw_pt_helm hak directories (wswls_b_015.mdl, it_torch_006.mdl, helm_001.mdl).
    /// </summary>
    [TestFixture]
    public class BlueprintModelResolverItemTests
    {
        private static JsonGffStruct ItemRoot(int baseItem, int part1 = 0, int part2 = 0, int part3 = 0) =>
            JsonGffDocument.Parse(Encoding.UTF8.GetBytes(
                $$"""
                {
                  "__data_type": "UTI ",
                  "BaseItem": { "type": "int", "value": {{baseItem}} },
                  "ModelPart1": { "type": "byte", "value": {{part1}} },
                  "ModelPart2": { "type": "byte", "value": {{part2}} },
                  "ModelPart3": { "type": "byte", "value": {{part3}} }
                }
                """)).Root;

        [Test]
        public void CompositeBaseItem_ResolvesToBottomMiddleTopParts()
        {
            // BaseItem 512 ("lightsaber"), ItemClass "WSwGlsbr" - the same row ItemAppearanceSectionTests
            // uses. wswglsbr_b_032.mdl / _m_011.mdl / _t_014.mdl all exist in sw_weapon.
            var row = new BaseItemIconRow(512, 2, "WSwGlsbr", "iwswglsbr");
            var root = ItemRoot(512, part1: 32, part2: 11, part3: 14);

            var result = BlueprintModelResolver.Resolve(
                ResourceType.Uti, root, null, null, null, baseItems: _ => row);

            result.Kind.Should().Be(BlueprintModelKind.ItemComposite);
            var parts = result.Parts.ToDictionary(p => p.PartType, p => p.ModelResRef);
            parts.Should().HaveCount(3);
            parts["bottom"].Should().Be("WSwGlsbr_b_032");
            parts["middle"].Should().Be("WSwGlsbr_m_011");
            parts["top"].Should().Be("WSwGlsbr_t_014");
        }

        [Test]
        public void CompositeBaseItemUsesExtendedPartFields()
        {
            var row = new BaseItemIconRow(512, 2, "WSwGlsbr", "iwswglsbr");
            var root = JsonGffDocument.Parse(Encoding.UTF8.GetBytes(
                """
                {
                  "__data_type": "UTI ",
                  "BaseItem": { "type": "int", "value": 512 },
                  "ModelPart1": { "type": "byte", "value": 255 },
                  "ModelPart2": { "type": "byte", "value": 1 },
                  "ModelPart3": { "type": "byte", "value": 1 },
                  "xModelPart1": { "type": "word", "value": 259 }
                }
                """)).Root;

            var result = BlueprintModelResolver.Resolve(
                ResourceType.Uti, root, null, null, null, baseItems: _ => row);

            result.Parts.Single(part => part.PartType == "bottom").ModelResRef
                .Should().Be("WSwGlsbr_b_259");
        }

        [Test]
        public void SimpleBaseItem_ModelType0_ResolvesToGroundModelWhenItExists()
        {
            // it_torch_006.mdl exists in sw_item.
            var row = new BaseItemIconRow(15, 0, "it_torch", "iit_torch");
            var root = ItemRoot(15, part1: 6);

            var result = BlueprintModelResolver.Resolve(
                ResourceType.Uti, root, null, null, null,
                baseItems: _ => row, partModelExists: resRef => resRef == "it_torch_006");

            result.Kind.Should().Be(BlueprintModelKind.Simple);
            result.ModelResRef.Should().Be("it_torch_006");
        }

        [Test]
        public void SimpleBaseItem_FallsBackToTheLootBagWhenTheGroundModelIsMissing()
        {
            // NWN drops any item without a ground model of its own as the loot bag; so does the preview.
            var row = new BaseItemIconRow(15, 0, "it_torch", "iit_torch");
            var root = ItemRoot(15, part1: 250);

            var result = BlueprintModelResolver.Resolve(
                ResourceType.Uti, root, null, null, null,
                baseItems: _ => row, partModelExists: resRef => resRef == "it_bag");

            result.Kind.Should().Be(BlueprintModelKind.Simple);
            result.ModelResRef.Should().Be("it_bag");
        }

        [Test]
        public void SimpleBaseItem_YieldsNoneOnlyWhenEvenTheLootBagIsMissing()
        {
            var row = new BaseItemIconRow(15, 0, "it_torch", "iit_torch");
            var root = ItemRoot(15, part1: 250);

            var result = BlueprintModelResolver.Resolve(
                ResourceType.Uti, root, null, null, null,
                baseItems: _ => row, partModelExists: _ => false);

            result.Kind.Should().Be(BlueprintModelKind.None);
        }

        [Test]
        public void CompositeBaseItem_FallsBackToTheLootBagWhenNoPartResolves()
        {
            var row = new BaseItemIconRow(512, 2, "WSwGlsbr", "iwswglsbr");
            var root = ItemRoot(512, part1: 250, part2: 250, part3: 250);

            var result = BlueprintModelResolver.Resolve(
                ResourceType.Uti, root, null, null, null,
                baseItems: _ => row, partModelExists: resRef => resRef == "it_bag");

            result.Kind.Should().Be(BlueprintModelKind.Simple);
            result.ModelResRef.Should().Be("it_bag");
        }

        [Test]
        public void LayeredPartBaseItem_ModelType1_UsesTheSameSingleGroundPattern()
        {
            // helm_001.mdl exists in sw_pt_helm.
            var row = new BaseItemIconRow(17, 1, "helm", "ihelm");
            var root = ItemRoot(17, part1: 1);

            var result = BlueprintModelResolver.Resolve(
                ResourceType.Uti, root, null, null, null,
                baseItems: _ => row, partModelExists: resRef => resRef == "helm_001");

            result.Kind.Should().Be(BlueprintModelKind.Simple);
            result.ModelResRef.Should().Be("helm_001");
        }

        [Test]
        public void ArmorBaseItem_ModelType3_DressesAMaleMannequinWithTheArmorsParts()
        {
            var row = new BaseItemIconRow(16, 3, "AArCl", "gifp");
            var root = JsonGffDocument.Parse(Encoding.UTF8.GetBytes(
                """
                {
                  "__data_type": "UTI ",
                  "BaseItem": { "type": "int", "value": 16 },
                  "ArmorPart_Torso": { "type": "byte", "value": 156 },
                  "ArmorPart_LShoul": { "type": "byte", "value": 0 },
                  "ArmorPart_RShoul": { "type": "byte", "value": 7 },
                  "ArmorPart_Robe": { "type": "byte", "value": 12 },
                  "Cloth1Color": { "type": "byte", "value": 23 }
                }
                """)).Root;

            var result = BlueprintModelResolver.Resolve(
                ResourceType.Uti, root, null, null, null, baseItems: _ => row);

            result.Kind.Should().Be(BlueprintModelKind.Segmented);
            result.SkeletonResRef.Should().Be("pmh0");
            var parts = result.Parts.ToDictionary(p => p.PartType, p => p.ModelResRef);
            parts["chest"].Should().Be("pmh0_chest156", "the armor part dresses the slot");
            parts["bicepl"].Should().EndWith("001", "an armor-less slot falls back to the bare body");
            parts.Should().NotContainKey("shol", "shoulder 0 means no piece and there is no bare-body shoulder");
            parts["shor"].Should().Be("pmh0_shor007");
            parts["robe"].Should().Be("pmh0_robe012");
            parts["head"].Should().Be("pmh0_head001");
            result.LayerColorIndices[PltLayers.Cloth1].Should().Be(23);
        }

        [Test]
        public void ArmorBaseItem_FemaleFlagSwapsTheMannequinBody()
        {
            var row = new BaseItemIconRow(16, 3, "AArCl", "gifp");
            var root = ItemRoot(16);

            var result = BlueprintModelResolver.Resolve(
                ResourceType.Uti, root, null, null, null, baseItems: _ => row,
                armorPreviewFemale: true);

            result.Kind.Should().Be(BlueprintModelKind.Segmented);
            result.SkeletonResRef.Should().Be("pfh0");
        }

        [Test]
        public void ArmorMannequinUsesExtendedBodyPartFields()
        {
            var row = new BaseItemIconRow(16, 3, "AArCl", "gifp");
            var root = JsonGffDocument.Parse(Encoding.UTF8.GetBytes(
                """
                {
                  "__data_type": "UTI ",
                  "BaseItem": { "type": "int", "value": 16 },
                  "ArmorPart_LBicep": { "type": "byte", "value": 255 },
                  "xArmorPart_LBice": { "type": "word", "value": 270 }
                }
                """)).Root;

            var result = BlueprintModelResolver.Resolve(
                ResourceType.Uti, root, null, null, null, baseItems: _ => row);

            result.Parts.Single(part => part.PartType == "bicepl").ModelResRef
                .Should().Be("pmh0_bicepl270");
        }

        [Test]
        public void UnrecognisedModelType_FallsBackToTheLootBagWithoutThrowing()
        {
            var row = new BaseItemIconRow(999, 7, "xx", null);
            var root = ItemRoot(999);

            var result = BlueprintModelResolver.Resolve(
                ResourceType.Uti, root, null, null, null, baseItems: _ => row);

            result.Kind.Should().Be(BlueprintModelKind.Simple);
            result.ModelResRef.Should().Be("it_bag");
        }

        [Test]
        public void NoBaseItemsLookup_YieldsNoneWithoutThrowing()
        {
            var root = ItemRoot(15);

            var result = BlueprintModelResolver.Resolve(ResourceType.Uti, root, null, null, null);

            result.Kind.Should().Be(BlueprintModelKind.None);
        }

        [Test]
        public void UnknownBaseItemId_YieldsNone()
        {
            var root = ItemRoot(4242);

            var result = BlueprintModelResolver.Resolve(
                ResourceType.Uti, root, null, null, null, baseItems: _ => null);

            result.Kind.Should().Be(BlueprintModelKind.None);
        }
    }
}
