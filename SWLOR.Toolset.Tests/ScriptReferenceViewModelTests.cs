using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Shell.Panels;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The reference browser's two tabs. Functions and constants are separate because their counts
    /// are lopsided — 1,187 against 6,201 — and one combined tree buried the functions under a branch
    /// that dwarfed them.
    /// </summary>
    public class ScriptReferenceViewModelTests
    {
        private static ScriptReferenceViewModel Build(out RecordingLinks links)
        {
            links = new RecordingLinks();
            var vm = new ScriptReferenceViewModel(
                new ScriptLanguageService(
                    new WorkspaceContext(_ => throw new NotSupportedException(), new OutputLogService()),
                    new OutputLogService()),
                links);

            vm.EnsureBuilt();
            return vm;
        }

        [Test]
        public void HasAFunctionsTabAndAConstantsTab()
        {
            var vm = Build(out _);

            vm.Tabs.Should().HaveCount(2);
            vm.Tabs[0].Label.Should().Be("Functions");
            vm.Tabs[1].Label.Should().Be("Constants");
        }

        [Test]
        public void FunctionsIsSelectedFirst()
        {
            var vm = Build(out _);

            vm.SelectedSection.Should().Be(ScriptReferenceSection.Functions);
            vm.Tabs[0].IsSelected.Should().BeTrue();
            vm.Tabs[1].IsSelected.Should().BeFalse();
        }

        [Test]
        public void SelectingATabMovesTheSelectionAndTheWatermark()
        {
            var vm = Build(out _);

            vm.SelectTabCommand.Execute(vm.Tabs[1]);

            vm.SelectedSection.Should().Be(ScriptReferenceSection.Constants);
            vm.Tabs[1].IsSelected.Should().BeTrue();
            vm.Tabs[0].IsSelected.Should().BeFalse();
            vm.FilterWatermark.Should().Contain("constants");
        }

        [Test]
        public void SwitchingTabsClearsTheFilter()
        {
            var vm = Build(out _);
            vm.Filter = "GetNearest";

            vm.SelectTabCommand.Execute(vm.Tabs[1]);

            // A term that matched functions almost never matches constants; carrying it across makes a
            // freshly-picked tab look empty.
            vm.Filter.Should().BeEmpty();
        }

        [Test]
        public void ConstantsTabGroupsAbilityConstantsUnderAbilityFamily()
        {
            var vm = Build(out _);
            vm.SelectTabCommand.Execute(vm.Tabs[1]);
            vm.Filter = "ABILITY_CHARISMA";

            vm.Rows.Should().Contain(r => r.IsCategory && r.Label == "ABILITY_*");
            vm.Rows.Should().NotContain(r => r.IsCategory && r.Label == "ABILITY_CHARISMA_*");
            vm.Rows.Should().Contain(r => r.IsSymbol && r.Label == "ABILITY_CHARISMA");
        }

        [Test]
        public void LexiconIsUnavailableWithNothingSelected()
        {
            var vm = Build(out _);

            vm.SelectedRow = null;
            vm.CanOpenLexicon.Should().BeFalse();
        }

        [Test]
        public void LexiconIsUnavailableOnAGroupHeaderThatIsNotAnIdentifier()
        {
            var vm = Build(out _);

            // Constant family headers read "CREATURE_TYPE_*", which is not a page title.
            vm.SelectedRow = new ReferenceNodeViewModel("CREATURE_TYPE_*", 12);
            vm.CanOpenLexicon.Should().BeFalse();
        }

        [Test]
        public void InsertIsUnavailableWithoutAnActiveScript()
        {
            var vm = Build(out _);

            vm.SetInsertTarget(null);
            vm.CanInsert.Should().BeFalse("there is nowhere to insert into");
        }

        [Test]
        public void SearchExpansionDoesNotChangeManualExpansionState()
        {
            var category = new ReferenceNodeViewModel("Effects", 42);
            category.IsExpanded.Should().BeFalse();

            category.IsAutoExpanded = true;
            category.IsEffectivelyExpanded.Should().BeTrue();
            category.IsExpanded.Should().BeFalse();

            category.IsAutoExpanded = false;
            category.IsEffectivelyExpanded.Should().BeFalse();
            category.IsExpanded.Should().BeFalse(
                "clearing a search must restore the category's prior collapsed state");
        }

        [Test]
        public void ClearingSearchPreservesAManuallyExpandedCategory()
        {
            var category = new ReferenceNodeViewModel("Effects", 42)
            {
                IsExpanded = true,
                IsAutoExpanded = true
            };

            category.IsAutoExpanded = false;

            category.IsEffectivelyExpanded.Should().BeTrue();
            category.IsExpanded.Should().BeTrue();
        }

        /// <summary>Records opened URLs instead of launching a browser.</summary>
        private sealed class RecordingLinks : IExternalLinkService
        {
            public List<string> Opened { get; } = new();

            public void Open(string url) => Opened.Add(url);
        }
    }
}
