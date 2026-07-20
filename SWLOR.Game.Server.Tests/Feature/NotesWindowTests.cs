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

        definition.Should().Contain($"DefinePartialView(NotesViewModel.{nameof(NotesViewModel.NotesTabPartial)}");
        definition.Should().Contain($"DefinePartialView(NotesViewModel.{nameof(NotesViewModel.CategoriesTabPartial)}");
        definition.Should().Contain($"row.AddPartialView(NotesViewModel.{nameof(NotesViewModel.TabContentPartialElement)})");

        definition.Should().Contain("model => model.IsNotesTabToggled");
        definition.Should().Contain("model => model.IsCategoriesTabToggled");

        // A hidden row still reserves its flex space, so tab panes must never be visibility-toggled.
        definition.Should().NotContain("IsNotesTabVisible");
        definition.Should().NotContain("IsCategoriesTabVisible");
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
