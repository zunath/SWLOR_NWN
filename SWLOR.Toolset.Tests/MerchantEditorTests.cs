using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Merchants;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors.Merchants;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

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
        public async Task DocumentSavePersistsBuilderValuesAndHiddenSwlorDefaultsTogether()
        {
            var moduleRoot = Path.Combine(
                Path.GetTempPath(),
                "swlor-merchant-save-contract-" + Guid.NewGuid().ToString("N"),
                "Module");
            var directory = Path.Combine(moduleRoot, "utm");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "probe_store.utm.json");
            var source = JsonGffDocument.Parse(
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Utm, "probe_store", "Probe Store"));
            source.Root.SetString("Comment", GffFieldType.CExoString, "legacy note");
            source.Root.SetInt("IdentifyPrice", GffFieldType.Int, 25);
            source.Root.SetInt("BlackMarket", GffFieldType.Byte, 0);
            source.Root.SetInt("MaxBuyPrice", GffFieldType.Int, 500);
            source.Root.SetInt("StoreGold", GffFieldType.Int, 1000);
            source.Root.SetString("OnOpenStore", GffFieldType.ResRef, "legacy_open");
            source.Root.SetString("OnStoreClosed", GffFieldType.ResRef, "legacy_close");
            new MerchantValueStore(source.Root).AddInventoryItem(
                (int)MerchantInventoryCategory.PotionsScrolls,
                "probe_ring");
            File.WriteAllBytes(path, source.ToBytes());

            var document = new MerchantDocumentViewModel(
                path,
                "probe_store",
                new OutputLogService(),
                new MerchantSavePrompts(),
                loadItem: resRef => new MerchantItemDefinition(
                    resRef,
                    "Probe Ring",
                    100,
                    (int)MerchantInventoryCategory.RingsAmulets));
            try
            {
                document.Editor.DetailRows.Single(row => row.Definition.Name == "LocName").Text =
                    "Builder Merchant";
                document.Editor.DetailRows.Single(row => row.Definition.Name == "Tag").Text =
                    "BUILDER_MERCHANT";
                document.Editor.PricingRows.Single(row => row.Definition.Name == "MarkUp").Number =
                    135;

                (await document.TrySaveAsync()).Should().BeTrue();

                var saved = JsonGffDocument.Load(path).Root;
                new MerchantValueStore(saved).GetLocalizedText("LocName")
                    .Should().Be("Builder Merchant");
                saved.GetStringOrNull("Tag").Should().Be("BUILDER_MERCHANT");
                saved.GetIntOrNull("MarkUp").Should().Be(135);
                saved.GetStringOrNull("Comment").Should().BeEmpty();
                saved.GetIntOrNull("IdentifyPrice").Should().Be(0);
                saved.GetIntOrNull("BlackMarket").Should().Be(1);
                saved.GetIntOrNull("MaxBuyPrice").Should().Be(-1);
                saved.GetIntOrNull("StoreGold").Should().Be(-1);
                saved.GetStringOrNull("OnOpenStore")
                    .Should().Be(MerchantEditorViewModel.OnOpenStoreScript);
                saved.GetStringOrNull("OnStoreClosed")
                    .Should().Be(MerchantEditorViewModel.OnStoreClosedScript);
                var savedStore = new MerchantValueStore(saved);
                savedStore.Inventory((int)MerchantInventoryCategory.PotionsScrolls)
                    .Should().BeEmpty();
                savedStore.Inventory((int)MerchantInventoryCategory.RingsAmulets)
                    .Should().ContainSingle()
                    .Which.GetStringOrNull("InventoryRes").Should().Be("probe_ring");
            }
            finally
            {
                document.OnClose();
                Directory.Delete(Directory.GetParent(moduleRoot)!.FullName, recursive: true);
            }
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
        public void InventoryRowsRequestTheSharedItemPreview()
        {
            var root = NewMerchant();
            new MerchantValueStore(root).AddInventoryItem(
                (int)MerchantInventoryCategory.Armor,
                "probe_armor");
            var requested = new List<string>();
            Action<Avalonia.Media.Imaging.Bitmap>? deliverPreview = null;

            using var editor = new MerchantEditorViewModel(
                root,
                "probe_store",
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                loadItem: resRef => new MerchantItemDefinition(
                    resRef,
                    "Probe Armor",
                    100,
                    (int)MerchantInventoryCategory.Armor),
                requestItemPreview: (resRef, onReady) =>
                {
                    requested.Add(resRef);
                    deliverPreview = onReady;
                });

            requested.Should().Equal("probe_armor");
            deliverPreview.Should().NotBeNull();
            editor.InventoryItems.Should().ContainSingle();
            editor.SelectedInventoryItem.Should().BeSameAs(editor.InventoryItems[0]);
        }

        [Test]
        public void InventoryCategoriesAndSaveFollowBaseItemsStorePanel()
        {
            var root = NewMerchant();
            var store = new MerchantValueStore(root);
            store.AddInventoryItem(
                (int)MerchantInventoryCategory.PotionsScrolls,
                "probe_ring");
            var misplaced = store.Inventory(
                (int)MerchantInventoryCategory.PotionsScrolls).Single();
            misplaced.SetInt("Infinite", GffFieldType.Byte, 0);

            using var editor = new MerchantEditorViewModel(
                root,
                "probe_store",
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                loadItem: resRef => new MerchantItemDefinition(
                    resRef,
                    "Probe Ring",
                    100,
                    (int)MerchantInventoryCategory.RingsAmulets),
                searchItems: (_, _) => new[]
                {
                    new MerchantItemDefinition(
                        "probe_ring",
                        "Probe Ring",
                        100,
                        (int)MerchantInventoryCategory.RingsAmulets),
                    new MerchantItemDefinition(
                        "probe_sword",
                        "Probe Sword",
                        100,
                        (int)MerchantInventoryCategory.Weapons)
                });

            editor.InventoryCategories.Single(category =>
                    category.Index == (int)MerchantInventoryCategory.PotionsScrolls)
                .Count.Should().Be(0);
            var rings = editor.InventoryCategories.Single(category =>
                category.Index == (int)MerchantInventoryCategory.RingsAmulets);
            rings.Count.Should().Be(1);

            editor.SelectedInventoryCategory = rings;
            editor.InventoryItems.Should().ContainSingle()
                .Which.ResRef.Should().Be("probe_ring");
            editor.ItemCandidates.Should().ContainSingle()
                .Which.ResRef.Should().Be("probe_ring");
            editor.NeedsSaveNormalization.Should().BeTrue();

            editor.PrepareForSave().Should().BeTrue();

            store.Inventory((int)MerchantInventoryCategory.PotionsScrolls).Should().BeEmpty();
            var normalized = store.Inventory(
                (int)MerchantInventoryCategory.RingsAmulets).Should().ContainSingle().Subject;
            normalized.GetStringOrNull("InventoryRes").Should().Be("probe_ring");
            normalized.GetIntOrNull("Infinite").Should().Be(0);
            normalized.GetIntOrNull("Repos_PosX").Should().Be(0);
            normalized.GetIntOrNull("Repos_Posy").Should().Be(0);
            editor.NeedsSaveNormalization.Should().BeFalse();
        }

        [Test]
        public void AddingAnItemUsesItsBaseItemsStorePanelInsteadOfTheSelectedPane()
        {
            var root = NewMerchant();
            var store = new MerchantValueStore(root);
            var ring = new MerchantItemDefinition(
                "probe_ring",
                "Probe Ring",
                100,
                (int)MerchantInventoryCategory.RingsAmulets);

            using var editor = new MerchantEditorViewModel(
                root,
                "probe_store",
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                loadItem: _ => ring);

            editor.SelectedInventoryCategory = editor.InventoryCategories.Single(category =>
                category.Index == (int)MerchantInventoryCategory.PotionsScrolls);
            editor.SelectedItemCandidate = ring;

            editor.AddInventoryItemCommand.Execute(null);

            store.Inventory((int)MerchantInventoryCategory.PotionsScrolls).Should().BeEmpty();
            store.Inventory((int)MerchantInventoryCategory.RingsAmulets).Should().ContainSingle()
                .Which.GetStringOrNull("InventoryRes").Should().Be("probe_ring");
            editor.SelectedInventoryCategory!.Index.Should().Be(
                (int)MerchantInventoryCategory.RingsAmulets);
        }

        [Test]
        public void BuyOnlyRulesCannotCollapseToTheNativeBothEmptyDefault()
        {
            var root = NewMerchant();
            var store = new MerchantValueStore(root);

            var switchEmpty = () => store.SwitchBuyingRuleMode(buyOnlySelected: true);
            switchEmpty.Should().Throw<InvalidOperationException>()
                .WithMessage("*at least one base item type*");
            root.GetListOrEmpty(MerchantValueStore.WillNotBuyField).Should().BeEmpty();
            root.GetListOrEmpty(MerchantValueStore.WillOnlyBuyField).Should().BeEmpty();

            store.SetBuyingRule(buyOnlySelected: false, baseItem: 42, selected: true);
            store.SwitchBuyingRuleMode(buyOnlySelected: true);
            var removeLast = () => store.SetBuyingRule(
                buyOnlySelected: true,
                baseItem: 42,
                selected: false);
            removeLast.Should().Throw<InvalidOperationException>()
                .WithMessage("*at least one base item type selected*");
            root.GetListOrEmpty(MerchantValueStore.WillOnlyBuyField)
                .Single().GetIntOrNull("BaseItem").Should().Be(42);
        }

        [Test]
        public void EditorRequiresASelectionBeforeAndDuringBuyOnlyMode()
        {
            var root = NewMerchant();
            var editCount = 0;
            using var editor = new MerchantEditorViewModel(
                root,
                "probe_store",
                (_, mutation) =>
                {
                    mutation();
                    editCount++;
                    return true;
                },
                baseItems: new[] { new BehaviorChoice(42, "Probe Type") });

            editor.BuyOnlySelected = true;
            editor.BuyOnlySelected.Should().BeFalse();
            editor.BuyingRuleError.Should().Contain("at least one");
            editCount.Should().Be(0);

            var rule = editor.BuyingRules.Should().ContainSingle().Subject;
            rule.IsSelected = true;
            editor.BuyOnlySelected = true;
            editor.BuyOnlySelected.Should().BeTrue();
            editor.BuyingRuleError.Should().BeNull();
            root.GetListOrEmpty(MerchantValueStore.WillOnlyBuyField)
                .Single().GetIntOrNull("BaseItem").Should().Be(42);

            rule.IsSelected = false;
            rule.IsSelected.Should().BeTrue();
            editor.BuyingRuleError.Should().Contain("at least one");
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

        private sealed class MerchantSavePrompts : IEditorPromptService
        {
            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string filePath) =>
                Task.FromResult(ExternalChangeChoice.Cancel);

            public Task<UnsavedChangesChoice> ConfirmCloseAsync(string documentTitle) =>
                Task.FromResult(UnsavedChangesChoice.Cancel);

            public Task<bool> ConfirmDestructiveAsync(
                string headline,
                string message,
                string confirmLabel) =>
                Task.FromResult(false);

            public Task<string?> PromptForTextAsync(
                string headline,
                string message,
                string initialValue,
                string confirmLabel) =>
                Task.FromResult<string?>(null);
        }
    }
}
