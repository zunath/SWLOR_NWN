using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Creatures;
using SWLOR.Toolset.Domain.Editors.Doors;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.Editors.Merchants;
using SWLOR.Toolset.Domain.Editors.Sounds;
using SWLOR.Toolset.Domain.Editors.Triggers;
using SWLOR.Toolset.Domain.Editors.Waypoints;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Editors.Behaviors;
using SWLOR.Toolset.Editors.Waypoints;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The one row every behavior editor draws. These used to be four classes with four copies of
    /// this behaviour, which is how they came to disagree about it.
    /// </summary>
    [TestFixture]
    public class BehaviorRowTests
    {
        [Test]
        public void ATextRowReadsTheStoredValueAndWritesThroughTheTransaction()
        {
            var store = Store("""{ "__data_type": "UTW ", "Tag": { "type": "cexostring", "value": "old" } }""");
            var descriptions = new List<string>();
            var row = Row(
                new BehaviorFieldDefinition
                {
                    Label = "Tag", Name = "Tag", Kind = BehaviorFieldKind.Text,
                    FieldType = GffFieldType.CExoString, MaxLength = 32
                },
                store,
                descriptions);

            row.Text.Should().Be("old");
            row.Counter.Should().Be("3/32");

            row.Text = "new";

            store.GetString(BehaviorFieldStorage.Field, "Tag").Should().Be("new");
            descriptions.Should().ContainSingle().Which.Should().Be("Change Tag");
            row.Counter.Should().Be("3/32");
        }

        [Test]
        public void ARefusedEditPutsTheRowBackRatherThanLeavingItLying()
        {
            var store = Store("""{ "__data_type": "UTW ", "Tag": { "type": "cexostring", "value": "kept" } }""");
            var row = new WaypointRowViewModel(
                new BehaviorFieldDefinition
                {
                    Label = "Tag", Name = "Tag", Kind = BehaviorFieldKind.Text,
                    FieldType = GffFieldType.CExoString
                },
                store,
                (_, _) => false);

            row.Text = "rejected";

            store.GetString(BehaviorFieldStorage.Field, "Tag").Should().Be("kept");
            row.Text.Should().Be("kept");
        }

        [Test]
        public void AReadOnlyRowNeverWrites()
        {
            var store = Store("""{ "__data_type": "UTS ", "TemplateResRef": { "type": "resref", "value": "amb_x" } }""");
            var edits = new List<string>();
            var row = Row(
                new BehaviorFieldDefinition
                {
                    Label = "ResRef", Name = "TemplateResRef", Kind = BehaviorFieldKind.Text,
                    FieldType = GffFieldType.ResRef, IsReadOnly = true
                },
                store,
                edits);

            row.Text = "something_else";

            edits.Should().BeEmpty();
            store.GetString(BehaviorFieldStorage.Field, "TemplateResRef").Should().Be("amb_x");
        }

        [Test]
        public void ARequiredChoiceRowCountsAsFilledInOnceSomethingIsChosen()
        {
            // The trigger editor used to read Text for this, so a required choice row reported
            // itself as empty no matter what was picked.
            var store = Store("""{ "__data_type": "UTT " }""");
            var row = Row(
                new BehaviorFieldDefinition
                {
                    Label = "Destination is a", Name = "LinkedToFlags",
                    Kind = BehaviorFieldKind.Choice, FieldType = GffFieldType.Byte,
                    IsRequired = true,
                    Choices = new[] { new BehaviorChoice(1, "Door"), new BehaviorChoice(2, "Waypoint") }
                },
                store,
                new List<string>());

            row.HasValue.Should().BeTrue();
            row.IsEmpty.Should().BeFalse();
        }

        [Test]
        public void AStringValuedChoiceMatchesTheStoredText()
        {
            var store = Store(
                """{ "__data_type": "UTW ", "Tag": { "type": "cexostring", "value": "FP_TWO" } }""");
            var row = Row(
                new BehaviorFieldDefinition
                {
                    Label = "Fishing point", Name = "Tag", Kind = BehaviorFieldKind.Choice,
                    FieldType = GffFieldType.CExoString,
                    Choices = new[]
                    {
                        new BehaviorChoice("FP_ONE", "One"),
                        new BehaviorChoice("FP_TWO", "Two")
                    }
                },
                store,
                new List<string>());

            row.Choice!.Display.Should().Be("Two");

            row.Choice = row.Choices.First();
            store.GetString(BehaviorFieldStorage.Field, "Tag").Should().Be("FP_ONE");
        }

        [Test]
        public void ALongChoiceSetBecomesASearchableSelectListWithoutBeingAsked()
        {
            var choices = Enumerable.Range(0, 120)
                .Select(index => new BehaviorChoice(index, $"Option {index:D3}"))
                .ToList();
            var row = Row(
                new BehaviorFieldDefinition
                {
                    Label = "Portrait", Name = "PortraitId", Kind = BehaviorFieldKind.Choice,
                    FieldType = GffFieldType.Word, Choices = choices
                },
                Store("""{ "__data_type": "UTD " }"""),
                new List<string>());

            row.IsSearchableChoice.Should().BeTrue();
            row.IsPlainChoice.Should().BeFalse();
            row.IsSearchExpanded.Should().BeFalse();
            row.FilteredChoices.Should().BeEmpty(
                "a closed picker must not publish controls while its editor is being laid out");

            row.OpenSearchCommand.Execute(null);
            row.IsSearchExpanded.Should().BeTrue();
            row.FilteredChoices.Should().HaveCount(BehaviorRowViewModel.SearchPageSize);

            row.ChoiceSearchText = "011";
            row.FilteredChoices.Should().ContainSingle()
                .Which.Display.Should().Be("Option 011");
            row.SearchSummary.Should().Be("1 of 120 options");
        }

        [Test]
        public void PaletteCategoriesUseTheSameVisibleFilteredSelectorAcrossBlueprintEditors()
        {
            var definitions = new BehaviorFieldDefinition[]
            {
                CreatureEditorLayout.Basic.Single(field => field.Label == "Category"),
                ItemEditorLayout.Basic.Single(field => field.Label == "Category"),
                DoorEditorLayout.Basic.Single(field => field.Label == "Category"),
                MerchantEditorLayout.Details.Single(field => field.Label == "Category"),
                TriggerEditorLayout.Basic.Single(field => field.Label == "Category"),
                SoundEditorLayout.Basic.Single(field => field.Label == "Category"),
                WaypointEditorLayout.Basic.Single(field => field.Label == "Category")
            };

            definitions.Should().OnlyContain(field => field.IsSearchable && field.IsInlineSearch);
        }

        [Test]
        public void AnInlineSearchPublishesItsVirtualizedFirstPageWithoutAChooseAction()
        {
            var calls = 0;
            var row = new BehaviorRowViewModel(
                new BehaviorFieldDefinition
                {
                    Label = "Category", Name = "PaletteID", Kind = BehaviorFieldKind.Choice,
                    FieldType = GffFieldType.Byte, IsSearchable = true, IsInlineSearch = true
                },
                Store("""{ "__data_type": "UTC " }"""),
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                choiceLoader: () =>
                {
                    calls++;
                    return Enumerable.Range(0, 90)
                        .Select(index => new BehaviorChoice(index, $"Category {index}"))
                        .ToList();
                });

            row.Reload();

            calls.Should().Be(1);
            row.IsInlineSearchChoice.Should().BeTrue();
            row.IsSearchExpanded.Should().BeTrue();
            row.FilteredChoices.Should().HaveCount(BehaviorRowViewModel.SearchPageSize);

            row.PickChoiceCommand.Execute(row.FilteredChoices[3]);

            row.IsSearchExpanded.Should().BeTrue("inline category selection remains visible after a choice");
        }

        [Test]
        public async Task AVisibleWorkPaneCanShowADeferredSearchWithoutAChooseAction()
        {
            var calls = 0;
            using var row = new BehaviorRowViewModel(
                new BehaviorFieldDefinition
                {
                    Label = "Dialog", Name = "Conversation", Kind = BehaviorFieldKind.Choice,
                    FieldType = GffFieldType.ResRef, IsSearchable = true
                },
                Store("""{ "__data_type": "UTC " }"""),
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                asyncChoiceLoader: () =>
                {
                    calls++;
                    return Task.FromResult<IReadOnlyList<BehaviorChoice>>(
                        Enumerable.Range(0, 90)
                            .Select(index => new BehaviorChoice($"dialog_{index}", $"Dialog {index}"))
                            .ToList());
                },
                forceInlineSearch: true);

            row.Reload();

            row.IsInlineSearchChoice.Should().BeTrue();
            row.IsSearchExpanded.Should().BeTrue();
            calls.Should().Be(0, "displaying the picker shell must not block on its catalog");
            row.FilteredChoices.Should().BeEmpty();

            await row.ActivateChoicesAsync();

            calls.Should().Be(1);
            row.FilteredChoices.Should().HaveCount(BehaviorRowViewModel.SearchPageSize);
        }

        [Test]
        public async Task AnExplicitInlineGalleryNeverFallsBackToAChooseButtonWhileItLoads()
        {
            using var row = new BehaviorRowViewModel(
                new BehaviorFieldDefinition
                {
                    Label = "Portrait", Name = "PortraitId", Kind = BehaviorFieldKind.Choice,
                    FieldType = GffFieldType.Word, IsSearchable = true, IsInlineGallery = true
                },
                Store("""{ "__data_type": "UTC ", "PortraitId": { "type": "word", "value": 4 } }"""),
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                asyncChoiceLoader: () => Task.FromResult<IReadOnlyList<BehaviorChoice>>(
                    Enumerable.Range(0, 140)
                        .Select(index => new BehaviorChoice(index, $"Portrait {index}", $"po_{index}"))
                        .ToList()));
            row.Reload();

            row.IsSearchableChoice.Should().BeFalse(
                "an inline gallery must not flash a Choose button while its deferred catalog loads");
            row.IsPlainChoice.Should().BeFalse();
            row.SearchSummary.Should().BeEmpty();

            await row.ActivateChoicesAsync();

            row.IsInlineGallery.Should().BeTrue();
            row.IsPopupGallery.Should().BeFalse();
            row.GalleryChoices.Should().HaveCount(BehaviorRowViewModel.GalleryPageSize);
        }

        [Test]
        public async Task AVisualCatalogGetsSharedFacetFiltersAndSortsWithoutEditorSpecificCode()
        {
            // Female (even-index) names run Zulu-block first, Alpha-block second, so the filtered
            // subset is NOT ascending by default - the sort assertion below must observe a real
            // reorder, not the incidental order the fixture was published in.
            var choices = Enumerable.Range(0, 140)
                .Select(index => new BehaviorChoice(
                    index,
                    index % 2 == 0
                        ? index < 70 ? $"Zulu {index:D3}" : $"Alpha {index:D3}"
                        : $"Mike {index:D3}",
                    $"portrait_{index}")
                {
                    GalleryFacets =
                    [
                        new BehaviorChoiceFacet(
                            "gender",
                            "Gender",
                            index % 2 == 0 ? "female" : "male",
                            index % 2 == 0 ? "Female" : "Male")
                    ]
                })
                .ToList();
            using var row = Row(
                new BehaviorFieldDefinition
                {
                    Label = "Portrait", Name = "PortraitId", Kind = BehaviorFieldKind.Choice,
                    FieldType = GffFieldType.Word, Choices = choices
                },
                Store("""{ "__data_type": "UTC " }"""),
                new List<string>());

            await row.OpenGalleryCommand.ExecuteAsync(null);

            var gender = row.GalleryFilters.Should().ContainSingle().Subject;
            gender.Label.Should().Be("Gender");
            gender.SelectedOption = gender.Options.Single(option => option.Display == "Female");

            // Changing a filter or sort fires an un-awaited background rebuild (Task.Run inside
            // RebuildGalleryAsync); wait for it to publish before asserting or the test races it.
            await WaitForGalleryRebuildAsync(() => row.GallerySummary == "48 of 70 choices");
            row.GalleryChoices.Should().OnlyContain(choice => choice.Value % 2 == 0);
            row.GallerySummary.Should().Be("48 of 70 choices");

            row.SelectedGallerySort = row.GallerySortOptions.Single(option =>
                option.Mode == GallerySortMode.NameAscending);
            await WaitForGalleryRebuildAsync(() =>
                row.GalleryChoices.Count > 0 && row.GalleryChoices[0].Display == "Alpha 070");
            row.GalleryChoices[0].Display.Should().Be("Alpha 070",
                "the ascending sort must reorder the Zulu-block-first default order");
            row.GalleryChoices.Select(choice => choice.Display).Should().BeInAscendingOrder();
        }

        private static async Task WaitForGalleryRebuildAsync(Func<bool> published)
        {
            for (var attempt = 0; attempt < 400 && !published(); attempt++)
                await Task.Delay(10);
        }

        [Test]
        public void ASearchableRowNeverPublishesMoreRowsThanItsCap()
        {
            var choices = Enumerable.Range(0, 5000)
                .Select(index => new BehaviorChoice(index, $"Tag {index}"))
                .ToList();
            var row = Row(
                new BehaviorFieldDefinition
                {
                    Label = "Tag", Name = "Tag", Kind = BehaviorFieldKind.Choice,
                    FieldType = GffFieldType.CExoString, Choices = choices, IsSearchable = true
                },
                Store("""{ "__data_type": "UTD " }"""),
                new List<string>());

            row.OpenSearchCommand.Execute(null);
            row.FilteredChoices.Should().HaveCount(BehaviorRowViewModel.SearchPageSize);
            while (row.CanLoadMoreSearchResults)
                row.LoadMoreSearchResultsCommand.Execute(null);
            row.FilteredChoices.Should().HaveCount(BehaviorRowViewModel.MaxSearchResults);
        }

        [Test]
        public void SearchableChoicePicker_LoadsMoreChoicesAsTheUserScrolls()
        {
            var picker = File.ReadAllText(Path.Combine(
                CorpusLocator.RepositoryRoot,
                "SWLOR.Toolset", "Editors", "Behaviors", "SearchableChoicePickerView.axaml"));
            var codeBehind = File.ReadAllText(Path.Combine(
                CorpusLocator.RepositoryRoot,
                "SWLOR.Toolset", "Editors", "Behaviors", "SearchableChoicePickerView.axaml.cs"));

            picker.Should().Contain("ScrollViewer.ScrollChanged=\"OnSearchResultsScrollChanged\"");
            picker.Should().NotContain("Content=\"Load more\"");
            codeBehind.Should().Contain("row.LoadMoreSearchResultsCommand.Execute(null)");
        }

        [Test]
        public void ATruncatedListStillShowsWhatIsStored()
        {
            // The cap excludes options by accident where a filter excludes them on purpose. A value
            // the editor will not show is one a builder cannot see they have.
            var choices = Enumerable.Range(0, 1000)
                .Select(index => new BehaviorChoice(index, $"Table {index:D4}"))
                .ToList();
            var store = Store(
                """{ "__data_type": "UTP ", "Table": { "type": "int", "value": 900 } }""");
            var row = Row(
                new BehaviorFieldDefinition
                {
                    Label = "Table", Name = "Table", Kind = BehaviorFieldKind.Choice,
                    FieldType = GffFieldType.Int, Choices = choices, IsSearchable = true
                },
                store,
                new List<string>());

            row.Choice!.Value.Should().Be(900);
            row.OpenSearchCommand.Execute(null);
            row.FilteredChoices.Should().HaveCount(BehaviorRowViewModel.SearchPageSize + 1);
            row.FilteredChoices[0].Should().BeSameAs(row.Choice, "the stored value goes back on top");

            // A filter is deliberate: it may exclude the selection without putting it back.
            row.ChoiceSearchText = "Table 0001";
            row.FilteredChoices.Should().ContainSingle()
                .Which.Display.Should().Be("Table 0001");
        }

        [Test]
        public void ADeferredChoiceSetLoadsOnlyWhenItsPickerIsOpened()
        {
            var calls = 0;
            var definition = new BehaviorFieldDefinition
            {
                Label = "Dialog", Name = "Conversation", Kind = BehaviorFieldKind.Choice,
                FieldType = GffFieldType.ResRef, IsSearchable = true
            };
            var row = new BehaviorRowViewModel(
                definition,
                Store("""{ "__data_type": "UTC ", "Conversation": { "type": "resref", "value": "dlg_42" } }"""),
                (_, mutation) =>
                {
                    mutation();
                    return true;
                },
                choiceLoader: () =>
                {
                    calls++;
                    return Enumerable.Range(0, 120)
                        .Select(index => new BehaviorChoice($"dlg_{index}", $"Dialog {index}"))
                        .ToList();
                });
            row.Reload();

            calls.Should().Be(0);
            row.AreChoicesLoaded.Should().BeFalse();
            row.SelectedChoiceDisplay.Should().Be("dlg_42");
            row.FilteredChoices.Should().BeEmpty();

            row.OpenSearchCommand.Execute(null);

            calls.Should().Be(1);
            row.AreChoicesLoaded.Should().BeTrue();
            row.SelectedChoiceDisplay.Should().Be("Dialog 42");
            row.FilteredChoices.Should().HaveCount(BehaviorRowViewModel.SearchPageSize);
        }

        [Test]
        public void AShortChoiceSetStaysADropDown()
        {
            var row = Row(
                new BehaviorFieldDefinition
                {
                    Label = "Initial state", Name = "AnimationState", Kind = BehaviorFieldKind.Choice,
                    FieldType = GffFieldType.Byte,
                    Choices = new[]
                    {
                        new BehaviorChoice(0, "Closed"),
                        new BehaviorChoice(1, "Open"),
                        new BehaviorChoice(2, "Destroyed")
                    }
                },
                Store("""{ "__data_type": "UTD " }"""),
                new List<string>());

            row.IsPlainChoice.Should().BeTrue();
            row.IsSearchableChoice.Should().BeFalse();
        }

        [Test]
        public void AStatementRowPrintsTheStoredValueAndAcceptsNoInput()
        {
            var store = Store("""{ "__data_type": "UTS ", "Priority": { "type": "byte", "value": 42 } }""");
            var row = Row(
                new BehaviorFieldDefinition
                {
                    Label = "Priority", Name = "Priority", Kind = BehaviorFieldKind.Statement,
                    FieldType = GffFieldType.Byte, Note = "Managed automatically."
                },
                store,
                new List<string>());

            row.IsStatement.Should().BeTrue();
            row.StatementText.Should().Be("42");
        }

        [Test]
        public void EveryEditorsRowIsTheSameRow()
        {
            typeof(WaypointRowViewModel).Should().BeAssignableTo<BehaviorRowViewModel>();
            typeof(Toolset.Editors.Triggers.TriggerRowViewModel)
                .Should().BeAssignableTo<BehaviorRowViewModel>();
            typeof(Toolset.Editors.Doors.DoorRowViewModel)
                .Should().BeAssignableTo<BehaviorRowViewModel>();
            typeof(Toolset.Editors.Sounds.SoundRowViewModel)
                .Should().BeAssignableTo<BehaviorRowViewModel>();
        }

        [Test]
        public void TheRailGroupsHeadingsDividersAndBehaviorsInOneFlatList()
        {
            var items = new System.Collections.ObjectModel.ObservableCollection<BehaviorListItemViewModel>();
            BehaviorListItemViewModel.Build(items, TriggerBehaviorCatalog.All);

            items.Should().NotBeEmpty();
            items.Where(item => item.IsSelectable).Should().HaveCount(TriggerBehaviorCatalog.All.Count);

            // Custom carries no group, so it must be separated from whatever heading precedes it
            // rather than filed under it.
            var custom = items.Single(item => item.Behavior?.Id == TriggerBehaviorCatalog.CustomId);
            items.IndexOf(custom).Should().BeGreaterThan(0);
            items[items.IndexOf(custom) - 1].IsRule.Should().BeTrue();

            BehaviorListItemViewModel.Select(items, TriggerBehaviorCatalog.CustomId);
            items.Where(item => item.IsSelected).Should().ContainSingle()
                .Which.Behavior!.Id.Should().Be(TriggerBehaviorCatalog.CustomId);
        }

        [Test]
        public void OneRailSerialisesEveryCatalogTheSameWay()
        {
            foreach (var catalog in new IEnumerable<IBehaviorDescriptor>[]
                     {
                         TriggerBehaviorCatalog.All,
                         DoorBehaviorCatalog.All,
                         SoundBehaviorCatalog.All
                     })
            {
                var items = new System.Collections.ObjectModel.ObservableCollection<BehaviorListItemViewModel>();
                BehaviorListItemViewModel.Build(items, catalog);

                items.Where(item => item.IsSelectable).Select(item => item.Behavior!.Id)
                    .Should().BeEquivalentTo(catalog.Select(behavior => behavior.Id));
                items.Where(item => item.IsHeader).Should().OnlyContain(
                    item => !string.IsNullOrEmpty(item.Text));
            }
        }

        [Test]
        public void EveryBehaviorCatalogEntryDescribesItself()
        {
            // The shared rail reads exactly these six facts. A catalog that does not implement the
            // interface cannot use it, which is what kept four copies alive.
            typeof(TriggerBehavior).Should().BeAssignableTo<IBehaviorDescriptor>();
            typeof(WaypointBehavior).Should().BeAssignableTo<IBehaviorDescriptor>();
            typeof(DoorBehavior).Should().BeAssignableTo<IBehaviorDescriptor>();
            typeof(SoundBehavior).Should().BeAssignableTo<IBehaviorDescriptor>();
        }

        private static WaypointRowViewModel Row(
            BehaviorFieldDefinition definition,
            BehaviorValueStore store,
            List<string> descriptions)
        {
            return new WaypointRowViewModel(
                definition,
                store,
                (description, mutation) =>
                {
                    descriptions.Add(description);
                    mutation();
                    return true;
                });
        }

        private static BehaviorValueStore Store(string json) =>
            new(JsonGffDocument.Parse(Encoding.UTF8.GetBytes(json)).Root);
    }
}
