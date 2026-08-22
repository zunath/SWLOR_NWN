using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Merchants;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors.Items;
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
            editor.DetailRows.Single(row => row.Definition.Name == "ResRef").MaxLength
                .Should().Be(NwnResRef.MaxLength);
            editor.InventoryCategories.Select(category => category.Name).Should().Equal(
                "Armor",
                "Weapons",
                "Potions/Scrolls",
                "Rings/Amulets",
                "Miscellaneous");
            editor.SelectedTabIndex.Should().Be(0);
            editor.ArePlacedInstancesLoaded.Should().BeFalse();
            editor.IsLoadingInstances.Should().BeFalse();
            editor.InstanceSummary.Should().Be(
                "Instance status loads only when this tab is opened.");

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
                document.Editor.ArePlacedInstancesLoaded = true;
                document.Editor.PlacedInstances.Add(new MerchantInstancePlacement(
                    "Probe Area", "probe_area", "PROBE_STORE", 0, 0, 0));
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
                document.Editor.ArePlacedInstancesLoaded.Should().BeFalse();
                document.Editor.PlacedInstancesNeedRefresh.Should().BeTrue();
                document.Editor.PlacedInstances.Should().BeEmpty(
                    "saving should invalidate status without starting another module scan");
            }
            finally
            {
                document.OnClose();
                Directory.Delete(Directory.GetParent(moduleRoot)!.FullName, recursive: true);
            }
        }

        [Test]
        public async Task InstanceUpdateBlocksDocumentActionsUntilSynchronizationFinishes()
        {
            var moduleRoot = Path.Combine(
                Path.GetTempPath(),
                "swlor-merchant-busy-state-" + Guid.NewGuid().ToString("N"),
                "Module");
            var directory = Path.Combine(moduleRoot, "utm");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "probe_store.utm.json");
            File.WriteAllBytes(
                path,
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Utm, "probe_store", "Probe Store"));

            var document = new MerchantDocumentViewModel(
                path,
                "probe_store",
                new OutputLogService(),
                new MerchantSavePrompts());
            try
            {
                var tagRow = document.Editor.DetailRows.Single(
                    row => row.Definition.Name == "Tag");
                tagRow.Text = "PROBE_STORE_CHANGED";
                document.IsDirty.Should().BeTrue();
                document.CanUndo.Should().BeTrue();

                var busyNotifications = 0;
                document.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(MerchantDocumentViewModel.IsBusy))
                        busyNotifications++;
                };

                document.Editor.IsLoadingInstances = true;

                document.Editor.IsInstanceOperationBusy.Should().BeTrue();
                document.Editor.InstanceOperationStatus.Should().Be(
                    "Scanning placed merchant instances...");
                document.IsBusy.Should().BeFalse(
                    "a read-only instance scan must not lock routine merchant editing");
                document.SaveCommand.CanExecute(null).Should().BeTrue();
                document.RevertCommand.CanExecute(null).Should().BeTrue();
                document.UndoCommand.CanExecute(null).Should().BeTrue();

                document.Editor.IsLoadingInstances = false;
                document.Editor.IsUpdatingInstances = true;

                document.IsBusy.Should().BeTrue();
                document.Editor.IsInstanceOperationBusy.Should().BeTrue();
                document.Editor.InstanceOperationStatus.Should().Be(
                    "Updating placed merchant instances...");
                document.SaveCommand.CanExecute(null).Should().BeFalse();
                document.RevertCommand.CanExecute(null).Should().BeFalse();
                document.UndoCommand.CanExecute(null).Should().BeFalse();
                document.CanUndo.Should().BeFalse();
                tagRow.Text = "MUST_NOT_APPLY";
                tagRow.Text.Should().Be("PROBE_STORE_CHANGED");
                (await document.TrySaveAsync()).Should().BeFalse();
                document.OnClose().Should().BeFalse();

                document.Editor.IsUpdatingInstances = false;

                document.IsBusy.Should().BeFalse();
                document.SaveCommand.CanExecute(null).Should().BeTrue();
                document.RevertCommand.CanExecute(null).Should().BeTrue();
                document.UndoCommand.CanExecute(null).Should().BeTrue();
                document.CanUndo.Should().BeTrue();
                busyNotifications.Should().Be(2);
            }
            finally
            {
                document.Editor.IsUpdatingInstances = false;
                if (document.IsDirty)
                    await document.TrySaveAsync();
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
        public void BulkInventoryRemovalPreservesOrderAndRenumbersEveryAffectedPane()
        {
            var store = new MerchantValueStore(NewMerchant());
            store.AddInventoryItem((int)MerchantInventoryCategory.Armor, "armor_a");
            store.AddInventoryItem((int)MerchantInventoryCategory.Armor, "armor_b");
            store.AddInventoryItem((int)MerchantInventoryCategory.Armor, "armor_c");
            store.AddInventoryItem((int)MerchantInventoryCategory.Weapons, "weapon_a");
            store.AddInventoryItem((int)MerchantInventoryCategory.Weapons, "weapon_b");

            store.RemoveInventoryItems(new[]
            {
                ((int)MerchantInventoryCategory.Armor, 0),
                ((int)MerchantInventoryCategory.Armor, 2),
                ((int)MerchantInventoryCategory.Weapons, 1)
            });

            store.Inventory((int)MerchantInventoryCategory.Armor)
                .Select(item => item.GetStringOrNull("InventoryRes"))
                .Should().Equal("armor_b");
            store.Inventory((int)MerchantInventoryCategory.Weapons)
                .Select(item => item.GetStringOrNull("InventoryRes"))
                .Should().Equal("weapon_a");
            store.Inventory((int)MerchantInventoryCategory.Armor).Single().StructId.Should().Be(0);
            store.Inventory((int)MerchantInventoryCategory.Weapons).Single().StructId.Should().Be(0);
            AssertIndexDerivedPositions(store, (int)MerchantInventoryCategory.Armor);
            AssertIndexDerivedPositions(store, (int)MerchantInventoryCategory.Weapons);
        }

        [Test]
        public void RemovingInventoryItemsKeepsGridPositionsIndexDerivedSoAddsCannotCollide()
        {
            var store = new MerchantValueStore(NewMerchant());
            for (var slot = 0; slot < 7; slot++)
                store.AddInventoryItem((int)MerchantInventoryCategory.Armor, $"armor_{slot}");

            store.RemoveInventoryItem((int)MerchantInventoryCategory.Armor, 0);

            AssertIndexDerivedPositions(store, (int)MerchantInventoryCategory.Armor);

            store.AddInventoryItem((int)MerchantInventoryCategory.Armor, "armor_added");

            var items = store.Inventory((int)MerchantInventoryCategory.Armor);
            items.Select(item => item.GetStringOrNull("InventoryRes")).Should().Equal(
                "armor_1", "armor_2", "armor_3", "armor_4", "armor_5", "armor_6", "armor_added");
            AssertIndexDerivedPositions(store, (int)MerchantInventoryCategory.Armor);

            store.RemoveInventoryItems(new[]
            {
                ((int)MerchantInventoryCategory.Armor, 1),
                ((int)MerchantInventoryCategory.Armor, 4)
            });
            store.AddInventoryItem((int)MerchantInventoryCategory.Armor, "armor_final");

            AssertIndexDerivedPositions(store, (int)MerchantInventoryCategory.Armor);
        }

        /// <summary>
        /// Every slot's Repos position must be the one its list index dictates, which also proves
        /// the pane has no two items sharing a grid cell — the overlap that used to survive a
        /// remove and ship to placed store instances.
        /// </summary>
        private static void AssertIndexDerivedPositions(MerchantValueStore store, int paneIndex)
        {
            var items = store.Inventory(paneIndex);
            for (var index = 0; index < items.Count; index++)
            {
                items[index].GetIntOrNull("Repos_PosX").Should().Be(
                    index % 5 * 2, $"slot {index} must sit in its index-derived column");
                items[index].GetIntOrNull("Repos_Posy").Should().Be(
                    index / 5 * 2, $"slot {index} must sit in its index-derived row");
            }
        }

        [Test]
        public void BulkInventoryRemovalValidatesEverySlotBeforeMutating()
        {
            var store = new MerchantValueStore(NewMerchant());
            store.AddInventoryItem((int)MerchantInventoryCategory.Armor, "armor_a");
            store.AddInventoryItem((int)MerchantInventoryCategory.Armor, "armor_b");

            var remove = () => store.RemoveInventoryItems(new[]
            {
                ((int)MerchantInventoryCategory.Armor, 0),
                ((int)MerchantInventoryCategory.Armor, 20)
            });

            remove.Should().Throw<ArgumentOutOfRangeException>();
            store.Inventory((int)MerchantInventoryCategory.Armor)
                .Select(item => item.GetStringOrNull("InventoryRes"))
                .Should().Equal("armor_a", "armor_b");
        }

        [Test]
        public void InventoryChecksPersistAcrossFiltersAndRemoveInOneEdit()
        {
            var root = NewMerchant();
            var store = new MerchantValueStore(root);
            store.AddInventoryItem((int)MerchantInventoryCategory.Armor, "armor_alpha");
            store.AddInventoryItem((int)MerchantInventoryCategory.Armor, "armor_beta");
            store.AddInventoryItem((int)MerchantInventoryCategory.Armor, "armor_gamma");
            var editDescriptions = new List<string>();
            var names = new Dictionary<string, string>
            {
                ["armor_alpha"] = "Alpha Armor",
                ["armor_beta"] = "Beta Armor",
                ["armor_gamma"] = "Gamma Armor"
            };

            using var editor = new MerchantEditorViewModel(
                root,
                "probe_store",
                (description, mutation) =>
                {
                    mutation();
                    editDescriptions.Add(description);
                    return true;
                },
                loadItem: resRef => new MerchantItemDefinition(
                    resRef,
                    names[resRef],
                    100,
                    (int)MerchantInventoryCategory.Armor));

            editor.InventorySearchText = "Alpha";
            editor.ShownInventorySelectionState.Should().BeFalse();
            editor.ToggleShownInventorySelectionCommand.Execute(null);
            editor.ShownInventorySelectionState.Should().BeTrue();
            editor.InventorySearchText = "Gamma";
            editor.ShownInventorySelectionState.Should().BeFalse();
            editor.ToggleShownInventorySelectionCommand.Execute(null);
            editor.ShownInventorySelectionState.Should().BeTrue();

            editor.ToggleShownInventorySelectionCommand.Execute(null);
            editor.ShownInventorySelectionState.Should().BeFalse();
            editor.CheckedInventoryItemCount.Should().Be(1);
            editor.ToggleShownInventorySelectionCommand.Execute(null);

            editor.CheckedInventoryItemCount.Should().Be(2);
            editor.CheckedInventorySummary.Should().Be("2 items selected");
            editor.RemoveCheckedInventoryLabel.Should().Be("Remove selected (2)");
            editor.RemoveCheckedInventoryItemsCommand.CanExecute(null).Should().BeTrue();
            editor.InventorySearchText = string.Empty;
            editor.ShownInventorySelectionState.Should().BeNull();

            editor.RemoveCheckedInventoryItemsCommand.Execute(null);

            editDescriptions.Should().Equal("Remove 2 items from merchant inventory");
            store.Inventory((int)MerchantInventoryCategory.Armor)
                .Select(item => item.GetStringOrNull("InventoryRes"))
                .Should().Equal("armor_beta");
            editor.CheckedInventoryItemCount.Should().Be(0);
            editor.RemoveCheckedInventoryItemsCommand.CanExecute(null).Should().BeFalse();
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
        public async Task InventoryAndCandidateRowsShareItemStatsAndOpenTheItemEditor()
        {
            var root = NewMerchant();
            new MerchantValueStore(root).AddInventoryItem(
                (int)MerchantInventoryCategory.Armor,
                "probe_armor");
            var stats = new[]
            {
                new ItemStatSummaryGroup("Defense", new[]
                {
                    new ItemStatSummaryEntry("Defense", "12"),
                    new ItemStatSummaryEntry("Evasion", "4")
                }),
                new ItemStatSummaryGroup("Vitals", new[]
                {
                    new ItemStatSummaryEntry("HP", "25"),
                    new ItemStatSummaryEntry("FP", "10"),
                    new ItemStatSummaryEntry("STM", "15")
                })
            };
            var item = new MerchantItemDefinition(
                "probe_armor",
                "Probe Armor",
                100,
                (int)MerchantInventoryCategory.Armor,
                stats);
            var opened = new List<string>();

            using var editor = new MerchantEditorViewModel(
                root,
                "probe_store",
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                loadItem: _ => item,
                searchItems: (_, _, skip, take, _) =>
                    Task.FromResult<IReadOnlyList<MerchantItemDefinition>>(
                        new[] { item }.Skip(skip).Take(take).ToList()),
                openItem: opened.Add);

            await WaitForItemCandidatesAsync(editor);

            var inventoryRow = editor.InventoryItems.Should().ContainSingle().Subject;
            inventoryRow.StatGroups.Should().BeSameAs(stats);
            inventoryRow.StatSummary.Should().Contain("Defense 12")
                .And.Contain("Evasion 4")
                .And.Contain("HP 25")
                .And.EndWith("+1 more");
            inventoryRow.PrimaryStatSummary.Should().NotContain("STM 15");
            inventoryRow.AdditionalStatSummary.Should().Be("+1 more");
            inventoryRow.HasAdditionalStats.Should().BeTrue();
            var candidateRow = editor.ItemCandidates.Should().ContainSingle().Subject;
            candidateRow.StatSummary.Should().Be(inventoryRow.StatSummary);
            candidateRow.PrimaryStatSummary.Should().Be(inventoryRow.PrimaryStatSummary);
            candidateRow.AdditionalStatSummary.Should().Be(inventoryRow.AdditionalStatSummary);
            editor.SelectedItemStatGroups.Should().BeSameAs(stats);
            editor.HasSelectedItemStats.Should().BeTrue();
            editor.ShowsSelectedItemStatsStatus.Should().BeFalse();

            editor.OpenItemDetailsCommand.CanExecute(inventoryRow.ResRef).Should().BeTrue();
            editor.OpenItemDetailsCommand.Execute(inventoryRow.ResRef);

            opened.Should().Equal("probe_armor");
        }

        [Test]
        public async Task InventoryCategoriesAndSaveFollowBaseItemsStorePanel()
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
                searchItems: (_, _, skip, take, _) =>
                    Task.FromResult<IReadOnlyList<MerchantItemDefinition>>(new[]
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
                    }.Skip(skip).Take(take).ToList()));

            await WaitForItemCandidatesAsync(editor);

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
            editor.SelectedItemCandidate = new MerchantItemCandidateViewModel(ring);

            editor.AddInventoryItemCommand.Execute(null);

            store.Inventory((int)MerchantInventoryCategory.PotionsScrolls).Should().BeEmpty();
            store.Inventory((int)MerchantInventoryCategory.RingsAmulets).Should().ContainSingle()
                .Which.GetStringOrNull("InventoryRes").Should().Be("probe_ring");
            editor.SelectedInventoryCategory!.Index.Should().Be(
                (int)MerchantInventoryCategory.RingsAmulets);
        }

        [Test]
        public async Task ItemCandidatePickerPublishesPagesAndRequestsPreviewsOnlyWhenRowsAreRealized()
        {
            var candidates = Enumerable.Range(0, 60)
                .Select(index => new MerchantItemDefinition(
                    $"item_{index:000}",
                    $"Item {index:000}",
                    100,
                    (int)MerchantInventoryCategory.Armor))
                .ToList();
            var searches = new List<(int Skip, int Take)>();
            var previewRequests = new List<string>();

            using var editor = new MerchantEditorViewModel(
                NewMerchant(),
                "probe_store",
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                searchItems: (_, _, skip, take, _) =>
                {
                    searches.Add((skip, take));
                    return Task.FromResult<IReadOnlyList<MerchantItemDefinition>>(
                        candidates.Skip(skip).Take(take).ToList());
                },
                requestItemPreview: (resRef, _) => previewRequests.Add(resRef));

            await WaitForItemCandidatesAsync(editor);

            editor.ItemCandidates.Should().HaveCount(48);
            editor.CanLoadMoreItemCandidates.Should().BeTrue();
            searches.Should().Equal((0, 49));
            previewRequests.Should().BeEmpty();

            var first = editor.ItemCandidates[0];
            editor.EnsureItemCandidatePreview(first);
            editor.EnsureItemCandidatePreview(first);
            previewRequests.Should().Equal("item_000");

            await editor.LoadMoreItemCandidatesCommand.ExecuteAsync(null);

            editor.ItemCandidates.Should().HaveCount(60);
            editor.CanLoadMoreItemCandidates.Should().BeFalse();
            searches.Should().Equal((0, 49), (48, 49));
        }

        [Test]
        public async Task CandidateCategoryChangesIgnoreSlowStaleResults()
        {
            var requests = new Dictionary<int,
                TaskCompletionSource<IReadOnlyList<MerchantItemDefinition>>>();
            using var editor = new MerchantEditorViewModel(
                NewMerchant(),
                "probe_store",
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                searchItems: (_, storePanel, _, _, _) =>
                {
                    var request = new TaskCompletionSource<IReadOnlyList<MerchantItemDefinition>>(
                        TaskCreationOptions.RunContinuationsAsynchronously);
                    requests[storePanel] = request;
                    return request.Task;
                });

            requests.Should().ContainKey((int)MerchantInventoryCategory.Armor);
            var potions = editor.InventoryCategories.Single(category =>
                category.Index == (int)MerchantInventoryCategory.PotionsScrolls);

            editor.SelectedInventoryCategory = potions;

            editor.SelectedInventoryCategory.Should().BeSameAs(potions);
            editor.IsLoadingItemCandidates.Should().BeTrue();
            requests.Should().ContainKey((int)MerchantInventoryCategory.PotionsScrolls);

            requests[(int)MerchantInventoryCategory.Armor].SetResult(new[]
            {
                new MerchantItemDefinition(
                    "stale_armor",
                    "Stale Armor",
                    1,
                    (int)MerchantInventoryCategory.Armor)
            });
            await Task.Yield();
            editor.ItemCandidates.Should().BeEmpty();

            requests[(int)MerchantInventoryCategory.PotionsScrolls].SetResult(new[]
            {
                new MerchantItemDefinition(
                    "probe_potion",
                    "Probe Potion",
                    1,
                    (int)MerchantInventoryCategory.PotionsScrolls)
            });
            await WaitForItemCandidatesAsync(editor);

            editor.ItemCandidates.Should().ContainSingle()
                .Which.ResRef.Should().Be("probe_potion");
        }

        [Test]
        public async Task CandidateIndexReusesClassificationAcrossEveryStorePanel()
        {
            var catalog = Enumerable.Range(0, 20)
                .Select(index => new MerchantItemDefinition(
                    $"item_{index:00}",
                    $"Item {index:00}",
                    1))
                .ToList();
            var summaryLoads = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var summaries = catalog.Select((item, index) => new MerchantItemDefinition(
                    item.ResRef,
                    item.Name,
                    1,
                    index % 2 == 0
                        ? (int)MerchantInventoryCategory.Armor
                        : (int)MerchantInventoryCategory.PotionsScrolls))
                .ToDictionary(item => item.ResRef, StringComparer.OrdinalIgnoreCase);
            var index = new MerchantItemSearchIndex(
                catalog,
                resRef =>
                {
                    summaryLoads[resRef] = summaryLoads.GetValueOrDefault(resRef) + 1;
                    return summaries[resRef];
                },
                resRef => summaries[resRef]);

            (await index.SearchAsync(
                    string.Empty,
                    (int)MerchantInventoryCategory.Armor,
                    0,
                    4,
                    CancellationToken.None))
                .Should().HaveCount(4);
            (await index.SearchAsync(
                    string.Empty,
                    (int)MerchantInventoryCategory.PotionsScrolls,
                    0,
                    4,
                    CancellationToken.None))
                .Should().HaveCount(4);
            (await index.SearchAsync(
                    string.Empty,
                    (int)MerchantInventoryCategory.Armor,
                    0,
                    4,
                    CancellationToken.None))
                .Should().HaveCount(4);

            summaryLoads.Should().HaveCount(8,
                "the shared progressive scan only needs eight alternating records for both pages");
            summaryLoads.Values.Should().OnlyContain(count => count == 1,
                "switching categories must reuse classifications instead of reparsing blueprints");
        }

        [Test]
        public async Task EmptyCategoryStillClassifiesCandidatesForEconomyEligibility()
        {
            var catalog = Enumerable.Range(0, 200)
                .Select(index => new MerchantItemDefinition(
                    $"armor_{index:000}",
                    $"Armor {index:000}",
                    1,
                    (int)MerchantInventoryCategory.Armor,
                    HasKnownStorePanel: true))
                .ToList();
            var summaryLoads = 0;
            var detailLoads = 0;
            var index = new MerchantItemSearchIndex(
                catalog,
                _ =>
                {
                    summaryLoads++;
                    return null;
                },
                _ =>
                {
                    detailLoads++;
                    return null;
                });

            var results = await index.SearchAsync(
                string.Empty,
                (int)MerchantInventoryCategory.PotionsScrolls,
                0,
                49,
                CancellationToken.None);

            results.Should().BeEmpty();
            summaryLoads.Should().Be(200,
                "catalog category metadata cannot bypass the UTI economy-restriction classifier");
            detailLoads.Should().Be(0,
                "an empty category has no visible rows that need complete stat details");
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
        public void BuyingRuleSearchUsesBuilderFacingNamesRatherThanInternalIds()
        {
            using var editor = new MerchantEditorViewModel(
                NewMerchant(),
                "probe_store",
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                baseItems: new[]
                {
                    new BehaviorChoice(42, "Probe Type"),
                    new BehaviorChoice(84, "Other Type")
                });

            editor.BuyingRuleSearchText = "42";
            editor.BuyingRules.Should().BeEmpty(
                "internal base item IDs are not part of the builder-facing picker");

            editor.BuyingRuleSearchText = "probe";
            editor.BuyingRules.Should().ContainSingle()
                .Which.Name.Should().Be("Probe Type");
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

            StoreInstanceSynchronizer.Inspect(
                    merchant,
                    expected,
                    "probe_store",
                    resRef => resRef == "probe_item" ? item : null)
                .Should().Be(new StoreInstanceSyncStatus(0, 0));

            expanded.Remove("Cost").Should().BeTrue();
            StoreInstanceSynchronizer.Inspect(
                    merchant,
                    expected,
                    "probe_store",
                    resRef => resRef == "probe_item" ? item : null)
                .Should().Be(new StoreInstanceSyncStatus(1, 1));

            expected = StoreInstanceSynchronizer.BuildExpected(
                merchant,
                expected,
                "probe_store",
                resRef => resRef == "probe_item" ? item : null);

            expected.SetInt("MarkUp", GffFieldType.Int, 175);
            StoreInstanceSynchronizer.IsCurrent(
                merchant,
                expected,
                "probe_store",
                resRef => resRef == "probe_item" ? item : null).Should().BeFalse();
            StoreInstanceSynchronizer.Inspect(
                    merchant,
                    expected,
                    "probe_store",
                    resRef => resRef == "probe_item" ? item : null)
                .Should().Be(new StoreInstanceSyncStatus(1, 0));
        }

        [Test]
        public async Task InstanceUpdateOnlyReadsAndWritesTheDisplayedTargetAreas()
        {
            var moduleRoot = Path.Combine(
                Path.GetTempPath(),
                "swlor-merchant-targeted-update-" + Guid.NewGuid().ToString("N"),
                "Module");
            Directory.CreateDirectory(Path.Combine(moduleRoot, "are"));
            Directory.CreateDirectory(Path.Combine(moduleRoot, "git"));
            Directory.CreateDirectory(Path.Combine(moduleRoot, "utm"));
            Directory.CreateDirectory(Path.Combine(moduleRoot, "utc"));

            var merchant = JsonGffDocument.Parse(
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Utm, "probe_store", "Probe Store"));
            WritePlacedMerchantArea(moduleRoot, "area_a", merchant);
            WritePlacedMerchantArea(moduleRoot, "area_b", merchant);
            merchant.Root.SetInt("MarkUp", GffFieldType.Int, 175);
            File.WriteAllBytes(
                Path.Combine(moduleRoot, "utm", "probe_store.utm.json"),
                merchant.ToBytes());

            var checkedForUnsavedEdits = new List<string>();
            var reloadedAreas = new List<string>();
            var log = new OutputLogService();
            var workspace = new WorkspaceContext(path => new ModuleWorkspace(path), log);
            workspace.Open(moduleRoot);
            var service = new MerchantInstanceService(
                workspace,
                log,
                areaResRef =>
                {
                    checkedForUnsavedEdits.Add(areaResRef);
                    return false;
                },
                reloadedAreas.Add);

            try
            {
                var placements = await service.FindAsync("probe_store");
                placements.Should().HaveCount(2);
                placements.Should().OnlyContain(placement =>
                    Math.Abs(placement.XPosition - 1f) < 0.001f &&
                    Math.Abs(placement.YPosition - 2f) < 0.001f &&
                    Math.Abs(placement.ZPosition - 3f) < 0.001f,
                    "merchant Go To needs the scanned coordinates when a saved index becomes stale");

                var updated = await service.UpdateOutOfDateAsync(
                    "probe_store",
                    new[] { "area_a" });

                updated.Should().Be(1);
                checkedForUnsavedEdits.Should().Equal("area_a");
                reloadedAreas.Should().Equal("area_a");
                StoreInstanceSynchronizer.IsCurrent(
                        merchant,
                        GitDocument.Load(Path.Combine(moduleRoot, "git", "area_a.git.json"))
                            .Stores.Single(),
                        "probe_store",
                        _ => null)
                    .Should().BeTrue();
                StoreInstanceSynchronizer.IsCurrent(
                        merchant,
                        GitDocument.Load(Path.Combine(moduleRoot, "git", "area_b.git.json"))
                            .Stores.Single(),
                        "probe_store",
                        _ => null)
                    .Should().BeFalse("an area outside the displayed update snapshot must be untouched");
            }
            finally
            {
                await workspace.Catalog!.BuildTask;
                Directory.Delete(Directory.GetParent(moduleRoot)!.FullName, recursive: true);
            }
        }

        [Test]
        public async Task InstanceUpdatePreservesDisplayedRowsAcrossPlacementInvalidation()
        {
            var moduleRoot = Path.Combine(
                Path.GetTempPath(),
                "swlor-merchant-update-invalidation-" + Guid.NewGuid().ToString("N"),
                "Module");
            Directory.CreateDirectory(Path.Combine(moduleRoot, "are"));
            Directory.CreateDirectory(Path.Combine(moduleRoot, "git"));
            Directory.CreateDirectory(Path.Combine(moduleRoot, "utm"));
            Directory.CreateDirectory(Path.Combine(moduleRoot, "utc"));

            var merchant = JsonGffDocument.Parse(
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Utm, "probe_store", "Probe Store"));
            WritePlacedMerchantArea(moduleRoot, "area_a", merchant);
            merchant.Root.SetInt("MarkUp", GffFieldType.Int, 175);
            File.WriteAllBytes(
                Path.Combine(moduleRoot, "utm", "probe_store.utm.json"),
                merchant.ToBytes());

            var log = new OutputLogService();
            var workspace = new WorkspaceContext(path => new ModuleWorkspace(path), log);
            workspace.Open(moduleRoot);
            var service = new MerchantInstanceService(workspace, log);
            using var editor = new MerchantEditorViewModel(
                merchant.Root,
                "probe_store",
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                instances: service);
            workspace.PlacementIndexInvalidated += editor.InvalidatePlacedInstances;

            try
            {
                await editor.RefreshPlacedInstancesAsync();
                editor.PlacedInstances.Should().ContainSingle()
                    .Which.IsCurrent.Should().BeFalse();

                await editor.UpdateOutOfDateInstancesCommand.ExecuteAsync(null);

                editor.PlacedInstances.Should().ContainSingle()
                    .Which.IsCurrent.Should().BeTrue(
                        "the command must restore its displayed snapshot after its own invalidation");
                editor.ArePlacedInstancesLoaded.Should().BeTrue();
                editor.PlacedInstancesNeedRefresh.Should().BeFalse();
            }
            finally
            {
                workspace.PlacementIndexInvalidated -= editor.InvalidatePlacedInstances;
                await workspace.Catalog!.BuildTask;
                Directory.Delete(Directory.GetParent(moduleRoot)!.FullName, recursive: true);
            }
        }

        [Test]
        public void InstanceSummaryReportsOutOfDateMerchantAndItemRecordCounts()
        {
            using var editor = new MerchantEditorViewModel(
                NewMerchant(),
                "probe_store",
                (_, mutation) =>
                {
                    mutation();
                    return true;
                });

            editor.PlacedInstances.Add(new MerchantInstancePlacement(
                "Area A", "area_a", "store_a", 0, 1, 2));
            editor.PlacedInstances.Add(new MerchantInstancePlacement(
                "Area B", "area_b", "store_b", 0, 1, 0));
            editor.PlacedInstances.Add(new MerchantInstancePlacement(
                "Area C", "area_c", "store_c", 0, 0, 0));
            editor.ArePlacedInstancesLoaded = true;

            editor.InstanceSummary.Should().Be(
                "2 merchant records and 2 item records out of date across 2 of 3 placed instances.");
            editor.PlacedInstances[0].Status.Should().Be(
                "1 merchant record, 2 item records out of date");
            editor.PlacedInstances[0].SyncState.Should().Be("Out of date");
            editor.PlacedInstances[2].SyncState.Should().Be("Up to date");
        }

        [Test]
        public void GoToInstance_UsesSavedSourceResRefAndRequiresNavigationCallback()
        {
            string? navigatedResRef = null;
            var placement = new MerchantInstancePlacement(
                "Area A", "area_a", "store_a", 0, 0, 0, 1f, 2f, 3f);
            using var editor = new MerchantEditorViewModel(
                NewMerchant(),
                "probe_store",
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                goToInstance: (resRef, _) => navigatedResRef = resRef);

            editor.DetailRows.Single(row => row.Definition.Name == "ResRef").Text = "unsaved_name";

            editor.GoToInstanceCommand.CanExecute(placement).Should().BeTrue();
            editor.GoToInstanceCommand.Execute(placement);
            navigatedResRef.Should().Be(
                "probe_store",
                "the displayed placement was loaded for the saved source, not the unsaved field value");

            using var withoutNavigation = new MerchantEditorViewModel(
                NewMerchant(), "probe_store", (_, mutation) =>
                {
                    mutation();
                    return true;
                });
            withoutNavigation.GoToInstanceCommand.CanExecute(placement).Should().BeFalse();
        }

        private static JsonGffStruct NewMerchant() =>
            JsonGffDocument.Parse(
                BlueprintTemplateFactory.CreateFileContent(
                    ResourceType.Utm, "probe_store", "Probe Store")).Root;

        private static async Task WaitForItemCandidatesAsync(MerchantEditorViewModel editor)
        {
            for (var attempt = 0; attempt < 500 && editor.IsLoadingItemCandidates; attempt++)
                await Task.Delay(10);

            editor.IsLoadingItemCandidates.Should().BeFalse(
                "candidate loading should finish within the test timeout");
        }

        private static void WritePlacedMerchantArea(
            string moduleRoot,
            string areaResRef,
            JsonGffDocument merchant)
        {
            File.WriteAllText(Path.Combine(moduleRoot, "are", areaResRef + ".are.json"), "{}");
            var root = new JsonGffStruct();
            using (EditScope.EnterConstruction())
            {
                var stores = JsonGffField.CreateList();
                stores.InsertElement(
                    0,
                    InstanceFieldMap.CreateInstance(
                        ResourceType.Utm,
                        merchant,
                        "probe_store",
                        1,
                        2,
                        3,
                        0,
                        1));
                root.Add("StoreList", stores);
            }

            File.WriteAllBytes(
                Path.Combine(moduleRoot, "git", areaResRef + ".git.json"),
                new JsonGffDocument("GIT ", root).ToBytes());
        }

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
