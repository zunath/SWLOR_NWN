using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Merchants;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors.Merchants;

namespace SWLOR.Toolset.Tests
{
    public class MerchantEditorTests
    {
        [Test]
        public void Editor_ExposesOnlySwlorMerchantFieldsAndEngineInventoryOrder()
        {
            var root = NewMerchant();
            using var editor = new MerchantEditorViewModel(
                root,
                "probe_store",
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                key => key == MerchantChoiceKeys.PaletteCategories
                    ? new[] { new BehaviorChoice(5, "Merchants") }
                    : Array.Empty<BehaviorChoice>());

            editor.DetailRows.Select(row => row.Definition.Name).Should().Equal(
                "LocName", "Tag", "ResRef", "ID");
            editor.DetailRows.Single(row => row.Definition.Name == "Tag").MaxLength.Should().Be(32);
            editor.DetailRows.Single(row => row.Definition.Name == "ResRef").MaxLength.Should().Be(16);
            editor.InventoryCategories.Select(category => category.Name).Should().Equal(
                "Armor",
                "Weapons",
                "Potions/Scrolls",
                "Rings/Amulets",
                "Miscellaneous");

            editor.DetailRows.Concat(editor.PricingRows)
                .Select(row => row.Definition.Name)
                .Should().NotContain(new[]
                {
                    "Comment", "IdentifyPrice", "BlackMarket", "MaxBuyPrice", "StoreGold",
                    "OnOpenStore", "OnStoreClosed"
                });
        }

        [Test]
        public void PrepareForSave_AppliesHiddenSwlorDefaults()
        {
            var root = NewMerchant();
            root.SetString("Comment", GffFieldType.CExoString, "builder note");
            root.SetInt("IdentifyPrice", GffFieldType.Int, 17);
            root.SetInt("BlackMarket", GffFieldType.Byte, 0);
            root.SetInt("MaxBuyPrice", GffFieldType.Int, 500);
            root.SetInt("StoreGold", GffFieldType.Int, 1000);
            root.SetString("OnOpenStore", GffFieldType.ResRef, "legacy_open");
            root.SetString("OnStoreClosed", GffFieldType.ResRef, "legacy_close");

            using var editor = new MerchantEditorViewModel(
                root,
                "probe_store",
                (_, mutation) =>
                {
                    mutation();
                    return true;
                });

            editor.NeedsSaveNormalization.Should().BeTrue();
            editor.PrepareForSave().Should().BeTrue();
            root.GetStringOrNull("Comment").Should().BeEmpty();
            root.GetIntOrNull("IdentifyPrice").Should().Be(0);
            root.GetIntOrNull("BlackMarket").Should().Be(1);
            root.GetIntOrNull("MaxBuyPrice").Should().Be(-1);
            root.GetIntOrNull("StoreGold").Should().Be(-1);
            root.GetStringOrNull("OnOpenStore").Should().Be(MerchantEditorViewModel.OnOpenStoreScript);
            root.GetStringOrNull("OnStoreClosed").Should().Be(MerchantEditorViewModel.OnStoreClosedScript);
            editor.NeedsSaveNormalization.Should().BeFalse();
        }

        [Test]
        public void InventoryAndBuyingRules_WriteTheNativeUtmLists()
        {
            var root = NewMerchant();
            var store = new MerchantValueStore(root);

            store.AddInventoryItem((int)MerchantInventoryCategory.RingsAmulets, "probe_ring");
            var slot = store.Inventory((int)MerchantInventoryCategory.RingsAmulets).Single();
            slot.GetStringOrNull("InventoryRes").Should().Be("probe_ring");
            slot.GetIntOrNull("Infinite").Should().Be(1);

            store.SetBuyingRule(buyOnlySelected: false, baseItem: 42, selected: true);
            root.GetListOrEmpty(MerchantValueStore.WillNotBuyField)
                .Single().GetIntOrNull("BaseItem").Should().Be(42);
            root.GetListOrEmpty(MerchantValueStore.WillOnlyBuyField).Should().BeEmpty();

            store.SwitchBuyingRuleMode(buyOnlySelected: true);
            root.GetListOrEmpty(MerchantValueStore.WillNotBuyField).Should().BeEmpty();
            root.GetListOrEmpty(MerchantValueStore.WillOnlyBuyField)
                .Single().GetIntOrNull("BaseItem").Should().Be(42);
        }

        [Test]
        public void StoreSynchronizer_PreservesPlacementAndExpandsInventoryInCliOrder()
        {
            var merchant = JsonGffDocument.Parse(
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Utm, "probe_store", "Probe Store"));
            var item = JsonGffDocument.Parse(
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Uti, "probe_item", "Probe Item"));
            var values = new MerchantValueStore(merchant.Root);
            values.AddInventoryItem((int)MerchantInventoryCategory.Armor, "probe_item");
            var slot = values.Inventory((int)MerchantInventoryCategory.Armor).Single();
            slot.SetInt("Repos_PosX", GffFieldType.Word, 3);
            slot.SetInt("Repos_Posy", GffFieldType.Word, 4);

            var placed = InstanceFieldMap.CreateInstance(
                ResourceType.Utm,
                merchant,
                "probe_store",
                12.5,
                24.25,
                1.5,
                0,
                1);
            var expected = StoreInstanceSynchronizer.BuildExpected(
                merchant,
                placed,
                "probe_store",
                resRef => resRef == "probe_item" ? item : null);

            expected.GetSingleOrNull("XPosition").Should().Be(12.5f);
            expected.GetSingleOrNull("YPosition").Should().Be(24.25f);
            expected.GetSingleOrNull("ZPosition").Should().Be(1.5f);

            var expanded = expected.GetListOrEmpty("StoreList")[0]
                .GetListOrEmpty("ItemList").Single();
            expanded.GetStringOrNull("TemplateResRef").Should().Be("probe_item");
            expanded.GetIntOrNull("Infinite").Should().Be(1);
            expanded.GetIntOrNull("Repos_PosX").Should().Be(3);
            expanded.GetIntOrNull("Repos_Posy").Should().Be(4);

            var names = expanded.Entries.Select(entry => entry.Key).ToList();
            names.IndexOf("Infinite").Should().BeLessThan(names.IndexOf("LocalizedName"));
            names.IndexOf("Repos_PosX").Should().BeLessThan(names.IndexOf("StackSize"));
            StoreInstanceSynchronizer.IsCurrent(
                merchant,
                expected,
                "probe_store",
                resRef => resRef == "probe_item" ? item : null).Should().BeTrue();

            expected.SetInt("MarkUp", GffFieldType.Int, 175);
            StoreInstanceSynchronizer.IsCurrent(
                merchant,
                expected,
                "probe_store",
                resRef => resRef == "probe_item" ? item : null).Should().BeFalse();
        }

        private static JsonGffStruct NewMerchant() =>
            JsonGffDocument.Parse(
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Utm, "probe_store", "Probe Store")).Root;
    }
}
