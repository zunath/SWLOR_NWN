using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Feature;

public class NotesWindowTests
{
    [Test]
    public void NoteUsage_TracksAgainstTheHundredNoteCap()
    {
        Notes.MaxNumberOfNotes.Should().Be(100);

        Notes.GetNoteUsageText(0).Should().Be("0 / 100 Notes");
        Notes.GetNoteUsageText(38).Should().Be("38 / 100 Notes");

        Notes.GetNoteUsagePercentage(0).Should().Be(0f);
        Notes.GetNoteUsagePercentage(25).Should().Be(0.25f);
        Notes.GetNoteUsagePercentage(100).Should().Be(1f);
        Notes.GetNoteUsagePercentage(140).Should().Be(1f);

        Notes.IsNoteListFull(99).Should().BeFalse();
        Notes.IsNoteListFull(100).Should().BeTrue();
        Notes.IsNoteListFull(101).Should().BeTrue();
    }

    [Test]
    public void CategoryUsage_TracksAgainstTheTwentyFiveCategoryCap()
    {
        Notes.MaxNumberOfCategories.Should().Be(25);

        Notes.GetCategoryUsageText(0).Should().Be("0 / 25 Categories");
        Notes.GetCategoryUsageText(7).Should().Be("7 / 25 Categories");

        Notes.GetCategoryUsagePercentage(0).Should().Be(0f);
        Notes.GetCategoryUsagePercentage(5).Should().Be(0.2f);
        Notes.GetCategoryUsagePercentage(25).Should().Be(1f);
        Notes.GetCategoryUsagePercentage(40).Should().Be(1f);

        Notes.IsCategoryListFull(24).Should().BeFalse();
        Notes.IsCategoryListFull(25).Should().BeTrue();
    }

    [Test]
    public void UsageColors_ShiftWhenTheLimitIsReached()
    {
        Notes.GetNoteUsageColor(0).Should().NotBeSameAs(Notes.GetNoteUsageColor(Notes.MaxNumberOfNotes));
        Notes.GetNoteUsageColor(0).Should().BeSameAs(Notes.GetNoteUsageColor(99));
        Notes.GetNoteUsageColor(Notes.MaxNumberOfNotes)
            .Should()
            .BeSameAs(Notes.GetCategoryUsageColor(Notes.MaxNumberOfCategories));
    }

    [Test]
    public void NotesLayout_IsResponsiveAndUsesTheSharedControls()
    {
        var definition = ReadDefinition();

        definition.Should().Contain("browser.AddPagination(");
        definition.Should().Contain("model => model.PageNumbers");
        definition.Should().Contain("model => model.SelectedPageIndex");

        definition.Should().Contain("row.AddProgressBar()");
        definition.Should().Contain("model => model.NoteUsageProgress");
        definition.Should().Contain("model => model.CategoryUsageProgress");

        definition.Should().Contain("model => model.SearchText");
        definition.Should().Contain("model => model.CategoryFilterOptions");
        definition.Should().Contain("model => model.NoteCategoryOptions");

        // The old layout pinned both panes and the note body to fixed heights, so the window had
        // dead space when resized. Only control rows may carry a height now.
        definition.Should().NotContain("SetHeight(300f)");
        definition.Should().NotContain("SetHeight(205f)");
    }

    [Test]
    public void NotesLayout_SwapsTabsViaPartialViewsRatherThanVisibilityToggles()
    {
        var definition = ReadDefinition();

        // Both tabs are partial views swapped into the window root. Rendering the notes tab as the
        // base window layout instead left its two panes unable to fill the window, because the
        // builder wraps the base layout in an extra row the swapped partials do not get.
        definition.Should().Contain($"DefinePartialView(NotesViewModel.{nameof(NotesViewModel.NotesTabPartial)}");
        definition.Should().Contain($"DefinePartialView(NotesViewModel.{nameof(NotesViewModel.CategoriesTabPartial)}");

        // The tab selector is repeated in both layouts so it survives a swap.
        Regex.Matches(definition, @"AddTabRow\(col\);").Count.Should().Be(2);

        definition.Should().Contain("model => model.IsNotesTabToggled");
        definition.Should().Contain("model => model.IsCategoriesTabToggled");

        // A hidden row still reserves its flex space, so tab panes must never be visibility-toggled.
        definition.Should().NotContain("IsNotesTabVisible");
        definition.Should().NotContain("IsCategoriesTabVisible");
    }

    [Test]
    public void Layout_LetsTheFrameworkSizeEverythingItCan()
    {
        var definition = ReadDefinition();
        var viewModel = ReadSource("SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel", "NotesViewModel.cs");

        // Both tabs swap into the window root, never into a nested placeholder group - a group sizes
        // itself to its content and would pin the panes to a fixed width.
        Regex.Matches(definition, @"AddPartialView\(").Count
            .Should()
            .Be(0, "tab content is swapped at the window root, not into a nested group");

        // Layout is declared once and left to NUI. Regenerating it per window width forces every
        // clickable element to carry a hand written id, which is a maintenance and performance trap.
        viewModel.Should().NotContain("SetGroupLayout(");
        definition.Should().NotContain(".SetId(");
        definition.Should().Contain("browser.AddPagination(", "the shared control is usable without regeneration");
    }

    [Test]
    public void ComboBoxes_UseTheWidthPatternProvenByPerksAndKeyItems()
    {
        // A combo does not stretch to fill its row, so it needs an explicit width. That width is
        // only safe in a row with no pinned height - the combination blanked every list in the tab.
        var definition = ReadDefinition();

        var comboRows = Regex.Matches(
            definition,
            @"AddRow\(row =>\s*\{(?:(?!AddRow)[\s\S])*?AddComboBox\(\)(?:(?!AddRow)[\s\S])*?\}\);");

        comboRows.Count.Should().Be(2, "the category filter and the note category picker");
        foreach (Match comboRow in comboRows)
        {
            comboRow.Value.Should().Contain(".SetWidth(ComboWidth)");
            comboRow.Value.Should().NotContain(
                "row.SetHeight(",
                "a width-pinned combo in a fixed-height row blanks the window's lists");
        }
    }

    [Test]
    public void ComboSelections_AreReassertedWheneverTheOptionsOrLayoutChange()
    {
        // Assigning a combo's option list clears the client's selection. Without re-asserting the
        // bound index the filter renders blank - most visibly with no categories created.
        var viewModel = ReadSource("SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel", "NotesViewModel.cs");

        viewModel.Should().Contain("private void RefreshComboSelections()");
        viewModel.Should().Contain("private int ClampCategoryOptionIndex(int optionIndex)");

        // Once after rebuilding the option lists, once after the tab view is (re)applied.
        Regex.Matches(viewModel, @"RefreshComboSelections\(\);").Count
            .Should()
            .BeGreaterThanOrEqualTo(2);
    }

    [Test]
    public void ReservedCategoryNames_MatchTheSyntheticComboOptions()
    {
        // A real category sharing a synthetic option's label renders as two identical entries with
        // different meanings, so creation must reject those names in any casing or padding.
        Notes.IsReservedCategoryName(Notes.UncategorizedLabel).Should().BeTrue();
        Notes.IsReservedCategoryName(Notes.AllCategoriesLabel).Should().BeTrue();
        Notes.IsReservedCategoryName("uncategorized").Should().BeTrue();
        Notes.IsReservedCategoryName("  <ALL CATEGORIES>  ").Should().BeTrue();

        Notes.IsReservedCategoryName("Crafting").Should().BeFalse();
        Notes.IsReservedCategoryName("Uncategorized Stuff").Should().BeFalse();
        Notes.IsReservedCategoryName(string.Empty).Should().BeFalse();
        Notes.IsReservedCategoryName(null).Should().BeFalse();
    }

    [Test]
    public void ReloadPaths_FlushPendingEditsInsteadOfDiscardingThem()
    {
        // Every search/filter/paging/category reload funnels through LoadNotesList, which rebuilds
        // the editor. It must write pending edits out first, and the category handlers must flush
        // before they reorder the positional combo indices the editor is bound to.
        var viewModel = ReadSource("SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel", "NotesViewModel.cs");

        viewModel.Should().Contain("private void LoadNotesList()");
        viewModel.Should().MatchRegex(@"private void LoadNotesList\(\)\s*\{[^}]*?SaveDirtyNote\(\);");

        viewModel.Should().Contain("public Action OnCloseWindow() => SaveDirtyNote;");

        // Deleting a note must not resurrect it via the flush on the reload that follows.
        viewModel.Should().MatchRegex(@"IsSaveEnabled = false;\s*SelectedNoteIndex = -1;\s*DB\.Delete<PlayerNote>");
    }

    [Test]
    public void NoteQueries_PageToTheirOwnCapRatherThanTheDefaultRowLimit()
    {
        // An unpaged DBQuery falls back to a 50 record limit, which is below the note cap. Every
        // query that loads a whole collection must state its own paging or it truncates silently.
        var viewModel = ReadSource("SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel", "NotesViewModel.cs");
        var service = ReadSource("SWLOR.Game.Server", "Service", "Notes.cs");

        viewModel.Should().Contain("AddPaging(Notes.MaxNumberOfNotes, 0)");
        service.Should().Contain("AddPaging(MaxNumberOfCategories, 0)");
        service.Should().Contain("AddPaging(MaxNumberOfNotes, 0)");

        Notes.MaxNumberOfNotes.Should().BeGreaterThan(
            50,
            "the paging above only matters because the cap exceeds the default row limit");
    }

    private static string ReadDefinition()
    {
        return ReadSource("SWLOR.Game.Server", "Feature", "GuiDefinition", "NotesDefinition.cs");
    }

    private static string ReadSource(params string[] relativePath)
    {
        var segments = new[] { FindRepositoryRoot().FullName }.Concat(relativePath).ToArray();

        return File.ReadAllText(Path.Combine(segments));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("repository root should be discoverable from the test directory");
        return directory!;
    }
}
