using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.KeyItemService;

namespace SWLOR.Game.Server.Tests.Feature;

public class KeyItemsWindowTests
{
    [Test]
    public void KeyItemsLayout_UsesDetailsPaneInsteadOfDescriptionTooltips()
    {
        var root = FindRepositoryRoot();
        var definition = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "KeyItemsDefinition.cs"));
        definition.Should().Contain(".BindOnClicked(model => model.OnSelectKeyItem())");
        definition.Should().Contain(".BindIsToggled(model => model.Selections)");
        definition.Should().Contain(".BindResref(model => model.SelectedIcon)");
        definition.Should().Contain(".BindText(model => model.SelectedName)");
        definition.Should().Contain(".BindText(model => model.SelectedType)");
        definition.Should().Contain(".BindText(model => model.SelectedDescription)");
        definition.Should().NotContain(".BindTooltip(model => model.Descriptions)");
        definition.Should().Contain(".SetWidth(260f)");
        definition.Should().Contain("browser.AddPagination(");
        definition.Should().Contain("model => model.PageNumbers");
        definition.Should().Contain("model => model.SelectedPageIndex");
        definition.Should().NotContain(".SetPageSelectorWidth(100f)");
        definition.Should().NotContain(".SetRowHeight(35f)");
    }

    [Test]
    public void SelectingKeyItem_UpdatesSelectionAndDetails()
    {
        KeyItem.LoadData();
        var entries = GetUniqueIconEntries().Take(2).ToArray();
        var viewModel = new KeyItemsViewModel();

        viewModel.LoadKeyItems(entries.Select(x => x.Type));

        viewModel.Selections.Should().Equal(true, false);
        AssertSelectedEntry(viewModel, entries[0]);

        viewModel.SelectKeyItem(1);

        viewModel.Selections.Should().Equal(false, true);
        AssertSelectedEntry(viewModel, entries[1]);
    }

    [Test]
    public void EmptyUnfilteredList_UsesGeneralNoKeyItemsMessage()
    {
        var viewModel = new KeyItemsViewModel();

        viewModel.LoadKeyItems(Array.Empty<KeyItemType>());

        viewModel.Selections.Should().BeEmpty();
        viewModel.SelectedIcon.Should().Be(KeyItemIcon.Default);
        viewModel.SelectedName.Should().Be("No Key Items");
        viewModel.SelectedType.Should().BeEmpty();
        viewModel.SelectedDescription.Should().Be("You do not have any Key Items.");
    }

    [Test]
    public void EmptyFilteredList_UsesCategorySpecificMessage()
    {
        var viewModel = new KeyItemsViewModel
        {
            SelectedCategoryId = (int) KeyItemCategoryType.Documents,
        };

        viewModel.LoadKeyItems(Array.Empty<KeyItemType>());

        viewModel.SelectedIcon.Should().Be(KeyItemIcon.Default);
        viewModel.SelectedDescription.Should().Be("No Key Items match the selected category.");
    }

    [Test]
    public void KeyItems_ArePaginatedAtTwentyFiveEntries()
    {
        KeyItem.LoadData();
        var entries = GetUniqueIconEntries().Take(30).ToArray();
        var viewModel = new KeyItemsViewModel();

        viewModel.LoadKeyItems(entries.Select(x => x.Type));

        viewModel.Names.Should().HaveCount(25);
        viewModel.PageNumbers.Select(x => x.Label).Should().Equal("Page 1", "Page 2");
        AssertSelectedEntry(viewModel, entries[0]);

        viewModel.SelectedPageIndex = 1;
        viewModel.LoadKeyItems(entries.Select(x => x.Type));

        viewModel.Names.Should().HaveCount(5);
        viewModel.Selections.Should().Equal(true, false, false, false, false);
        AssertSelectedEntry(viewModel, entries[25]);
    }

    [Test]
    public void ChangingCategory_ResetsPagination()
    {
        var viewModel = new KeyItemsViewModel
        {
            SelectedPageIndex = 3,
        };

        viewModel.SelectedCategoryId = (int) KeyItemCategoryType.FieldNotes;

        viewModel.SelectedPageIndex.Should().Be(0);
    }

    private static IEnumerable<KeyItemEntry> GetUniqueIconEntries()
    {
        return Enum.GetValues<KeyItemType>()
            .Select(type => new KeyItemEntry(
                type,
                typeof(KeyItemType)
                    .GetField(type.ToString())!
                    .GetCustomAttribute<KeyItemAttribute>()!))
            .Where(x => x.Detail.IsActive &&
                        x.Detail.Category is KeyItemCategoryType.QuestItems or
                            KeyItemCategoryType.Documents or
                            KeyItemCategoryType.Keys);
    }

    private static void AssertSelectedEntry(KeyItemsViewModel viewModel, KeyItemEntry entry)
    {
        viewModel.SelectedIcon.Should().Be(KeyItemIcon.GetIconResref(entry.Type));
        viewModel.SelectedName.Should().Be(entry.Detail.Name);
        viewModel.SelectedType.Should().Be(KeyItem.GetKeyItemCategory(entry.Detail.Category).Name);
        viewModel.SelectedDescription.Should().Be(entry.Detail.Description);
    }

    private sealed record KeyItemEntry(KeyItemType Type, KeyItemAttribute Detail);

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
