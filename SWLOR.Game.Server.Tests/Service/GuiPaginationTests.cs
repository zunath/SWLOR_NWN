using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Tests.Service;

public class GuiPaginationTests
{
    [TestCase(0, 25, 0, 1, 0)]
    [TestCase(25, 25, 0, 1, 0)]
    [TestCase(26, 25, 0, 2, 0)]
    [TestCase(26, 25, 99, 2, 1)]
    [TestCase(26, 25, -1, 2, 0)]
    public void PaginationState_BuildsPagesAndClampsSelection(
        long totalRecordCount,
        int recordsPerPage,
        int selectedPageIndex,
        int expectedPageCount,
        int expectedSelectedPageIndex)
    {
        var pagination = GuiPaginationState.Create(
            totalRecordCount,
            recordsPerPage,
            selectedPageIndex);

        pagination.PageNumbers.Should().HaveCount(expectedPageCount);
        pagination.PageNumbers.Select(x => x.Label).Should().Equal(
            Enumerable.Range(1, expectedPageCount).Select(page => $"Page {page}"));
        pagination.SelectedPageIndex.Should().Be(expectedSelectedPageIndex);
    }

    [Test]
    public void PaginationState_RejectsInvalidCounts()
    {
        var negativeRecordCount = () => GuiPaginationState.Create(-1, 25, 0);
        var invalidPageSize = () => GuiPaginationState.Create(1, 0, 0);

        negativeRecordCount.Should().Throw<ArgumentOutOfRangeException>();
        invalidPageSize.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void PaginationComponent_AddsAStandardRowToTheColumn()
    {
        var column = new GuiColumn<TestPaginationViewModel>();

        column.AddPagination(
            model => model.PageNumbers,
            model => model.SelectedPageIndex,
            model => model.OnPreviousPage(),
            model => model.OnNextPage());

        var row = column.Elements.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<GuiRow<TestPaginationViewModel>>()
            .Which;

        row.Elements.Should().HaveCount(5);
        row.Elements[0].Should().BeOfType<GuiSpacer<TestPaginationViewModel>>();
        row.Elements[1].Should().BeOfType<GuiButton<TestPaginationViewModel>>();
        row.Elements[2].Should().BeOfType<GuiComboBox<TestPaginationViewModel>>();
        row.Elements[3].Should().BeOfType<GuiButton<TestPaginationViewModel>>();
        row.Elements[4].Should().BeOfType<GuiSpacer<TestPaginationViewModel>>();

        row.Elements[1].Events["click"].Method.Name.Should().Be(nameof(TestPaginationViewModel.OnPreviousPage));
        row.Elements[3].Events["click"].Method.Name.Should().Be(nameof(TestPaginationViewModel.OnNextPage));
    }

    [TestCase("AchievementsDefinition.cs")]
    [TestCase("CreatureManagerDefinition.cs")]
    [TestCase("DMToolsDefinition.cs")]
    [TestCase("KeyItemsDefinition.cs")]
    [TestCase("ManageStructuresDefinition.cs")]
    [TestCase("MarketBuyDefinition.cs")]
    [TestCase("PerksDefinition.cs")]
    [TestCase("RecipesDefinition.cs")]
    public void PaginatedWindowDefinitions_UseSharedComponent(string fileName)
    {
        var root = FindRepositoryRoot();
        var definition = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            fileName));

        definition.Should().Contain(".AddPagination(");
    }

    private sealed class TestPaginationViewModel :
        GuiViewModelBase<TestPaginationViewModel, GuiPayloadBase>
    {
        public GuiBindingList<GuiComboEntry> PageNumbers
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public int SelectedPageIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
        }

        public Action OnPreviousPage() => () => { };

        public Action OnNextPage() => () => { };
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
