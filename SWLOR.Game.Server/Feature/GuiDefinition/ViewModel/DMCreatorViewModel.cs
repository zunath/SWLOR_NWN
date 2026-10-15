using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class DMCreatorViewModel : GuiViewModelBase<DMCreatorViewModel, GuiPayloadBase>
    {
        public const string MainContentElement = "dmcreator_content";
        public const string MainContentPartial = "DMCREATOR_MAIN";

        private const int ResultsPerPage = 20;
        private const string AllCategories = "All categories";

        private DMPalette _palette = new(new List<string>(), new List<DMBlueprint>());
        private List<DMBlueprint> _results = new();
        private readonly List<DMBlueprint> _pageResults = new();
        private int _selectedIndex = -1;
        private bool _suppressSearch;

        public int SelectedType
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_suppressSearch)
                    LoadPalette();
            }
        }

        public int SelectedSource
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_suppressSearch)
                    LoadPalette();
            }
        }

        public GuiBindingList<GuiComboEntry> CategoryOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public int SelectedCategoryIndex
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_suppressSearch)
                    RunSearch();
            }
        }

        public string SearchText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string InfoHeader
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> ResultIcons
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> ResultNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> ResultInfo
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> ResultSelections
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> PageNumbers
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        public int SelectedPageIndex
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_suppressSearch)
                    ShowPage();
            }
        }

        public string ResultSummary
        {
            get => Get<string>();
            set => Set(value);
        }

        public string DetailIcon
        {
            get => Get<string>();
            set => Set(value);
        }

        public string DetailTitle
        {
            get => Get<string>();
            set => Set(value);
        }

        public string DetailBody
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool HasSelection
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool CanCreateInInventory
        {
            get => Get<bool>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            Geometry = DMToolbarViewModel.PlaceBelowToolbar(Player, Geometry);
            _suppressSearch = true;
            SelectedType = (int)DMBlueprintCategory.Creatures;
            SelectedSource = (int)DMBlueprintSource.Custom;
            CategoryOptions = new GuiBindingList<GuiComboEntry> { new(AllCategories, 0) };
            SelectedCategoryIndex = 0;
            SearchText = string.Empty;
            InfoHeader = string.Empty;
            ResultIcons = new GuiBindingList<string>();
            ResultNames = new GuiBindingList<string>();
            ResultInfo = new GuiBindingList<string>();
            ResultSelections = new GuiBindingList<bool>();
            PageNumbers = new GuiBindingList<GuiComboEntry>();
            SelectedPageIndex = 0;
            ResultSummary = string.Empty;
            DetailIcon = DMObjectDetails.NoSelectionIcon;
            DetailTitle = string.Empty;
            DetailBody = string.Empty;
            HasSelection = false;
            CanCreateInInventory = false;
            _suppressSearch = false;

            WatchOnClient(model => model.SelectedType);
            WatchOnClient(model => model.SelectedSource);
            WatchOnClient(model => model.SelectedCategoryIndex);
            WatchOnClient(model => model.SearchText);
            WatchOnClient(model => model.SelectedPageIndex);

            ChangePartialView(MainContentElement, MainContentPartial);
            LoadPalette();
        }

        // Reads the chosen palette and rebuilds the category list.
        private void LoadPalette()
        {
            if (Player == 0 || WindowToken <= 0)
                return;

            var category = (DMBlueprintCategory)SelectedType;
            _palette = DMBlueprintCatalog.GetPalette(category, (DMBlueprintSource)SelectedSource);
            InfoHeader = category switch
            {
                DMBlueprintCategory.Creatures => "Level · Faction",
                DMBlueprintCategory.Items => "Type",
                DMBlueprintCategory.Placeables => "Use",
                _ => "Category"
            };

            var options = new GuiBindingList<GuiComboEntry> { new(AllCategories, 0) };
            for (var index = 0; index < _palette.CategoryPaths.Count; index++)
            {
                options.Add(new GuiComboEntry(_palette.CategoryPaths[index], index + 1));
            }

            _suppressSearch = true;
            CategoryOptions = options;
            SelectedCategoryIndex = 0;
            _suppressSearch = false;
            RunSearch();
        }

        private string GetSelectedCategoryPath()
        {
            var index = SelectedCategoryIndex - 1;
            return index >= 0 && index < _palette.CategoryPaths.Count
                ? _palette.CategoryPaths[index]
                : string.Empty;
        }

        private void RunSearch()
        {
            if (Player == 0 || WindowToken <= 0)
                return;

            var categoryPath = GetSelectedCategoryPath();
            var terms = DMSearchText.GetTerms(SearchText);
            _results = _palette.Blueprints
                .Where(blueprint => blueprint.IsInCategory(categoryPath) &&
                                    DMSearchText.Matches(terms, blueprint.Name, blueprint.Resref, blueprint.CategoryPath))
                .ToList();

            _suppressSearch = true;
            SelectedPageIndex = 0;
            _suppressSearch = false;
            ShowPage();
        }

        private void ShowPage()
        {
            _suppressSearch = true;
            var pagination = GuiPaginationState.Create(_results.Count, ResultsPerPage, SelectedPageIndex);
            PageNumbers = pagination.PageNumbers;
            SelectedPageIndex = pagination.SelectedPageIndex;
            _suppressSearch = false;

            _pageResults.Clear();
            _pageResults.AddRange(_results
                .Skip(SelectedPageIndex * ResultsPerPage)
                .Take(ResultsPerPage));

            var icons = new GuiBindingList<string>();
            var names = new GuiBindingList<string>();
            var info = new GuiBindingList<string>();
            var selections = new GuiBindingList<bool>();
            foreach (var blueprint in _pageResults)
            {
                var (icon, summary) = DMObjectDetails.GetBlueprintRow(blueprint);
                icons.Add(icon);
                names.Add(blueprint.Name);
                info.Add(summary);
                selections.Add(false);
            }

            ResultIcons = icons;
            ResultNames = names;
            ResultInfo = info;
            ResultSelections = selections;
            // A tooltip keeps a NUI dropdown from opening, so the summary explains Custom and Standard instead.
            var palette = (DMBlueprintSource)SelectedSource == DMBlueprintSource.Standard
                ? "the base game's palette"
                : "the module's palette";
            ResultSummary = _results.Count == 0
                ? $"No matches in {palette}."
                : $"{_results.Count} blueprint{(_results.Count == 1 ? string.Empty : "s")} in {palette}.";

            SelectResult(-1);
        }

        private void SelectResult(int index)
        {
            if (_selectedIndex >= 0 && _selectedIndex < ResultSelections.Count)
                ResultSelections[_selectedIndex] = false;

            _selectedIndex = index;
            var blueprint = GetSelectedBlueprint();
            if (blueprint == null)
            {
                DetailIcon = DMObjectDetails.NoSelectionIcon;
                DetailTitle = string.Empty;
                DetailBody = "Select a blueprint to see its details and create it.";
                HasSelection = false;
                CanCreateInInventory = false;
                return;
            }

            ResultSelections[_selectedIndex] = true;
            var details = DMObjectDetails.DescribeBlueprint(blueprint);
            DetailIcon = details.IconResref;
            DetailTitle = details.Title;
            DetailBody = details.Body;
            HasSelection = true;
            CanCreateInInventory = blueprint.Category == DMBlueprintCategory.Items;
        }

        private DMBlueprint GetSelectedBlueprint()
        {
            return _selectedIndex >= 0 && _selectedIndex < _pageResults.Count
                ? _pageResults[_selectedIndex]
                : null;
        }

        private void ReportCreated(DMBlueprint blueprint, uint created)
        {
            SendMessageToPC(DMPresence.GetControlledCreature(Player), GetIsObjectValid(created)
                ? $"Created {blueprint.Name} ({blueprint.Resref})."
                : $"Could not create {blueprint.Name} ({blueprint.Resref}).");
        }

        // New objects face the DM, or the creature the DM possesses.
        private Location BuildSpawnLocation(uint area, Vector3 position)
        {
            var controlled = DMPresence.GetControlledCreature(Player);
            var toDM = GetPosition(controlled) - position;
            var facing = toDM == Vector3() ? GetFacing(controlled) : VectorToAngle(toDM);
            return Location(area, position, facing);
        }

        public Action OnSelectResult() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _pageResults.Count)
                return;

            SelectResult(index);
        };

        public Action OnClickSearch() => RunSearch;

        public Action OnClickClearSearch() => () =>
        {
            SearchText = string.Empty;
            RunSearch();
        };

        public Action OnClickPreviousPage() => () =>
        {
            SelectedPageIndex = Math.Max(0, SelectedPageIndex - 1);
        };

        public Action OnClickNextPage() => () =>
        {
            SelectedPageIndex = Math.Min(PageNumbers.Count - 1, SelectedPageIndex + 1);
        };

        public Action OnClickCreateAtTarget() => () =>
        {
            var blueprint = GetSelectedBlueprint();
            if (blueprint == null)
                return;

            // A DM possessing a creature targets and creates through that creature; the DM stays the creator of record.
            var controlled = DMPresence.GetControlledCreature(Player);
            var dm = DMPresence.GetDM(Player);
            Targeting.EnterTargetingMode(controlled, ObjectType.All, $"Click where to create {blueprint.Name}.", target =>
            {
                var area = GetArea(controlled);
                // Creatures, stores, and container placeables or items take the item into their inventory.
                if (blueprint.Category == DMBlueprintCategory.Items && DMBlueprintCatalog.CanReceiveItems(target))
                {
                    ReportCreated(blueprint, DMBlueprintCatalog.CreateInInventory(dm, blueprint, target));
                    return;
                }

                var position = GetTargetingModeSelectedPosition();
                if (position == Vector3() && GetIsObjectValid(target) && target != area)
                    position = GetPosition(target);

                if (position == Vector3())
                    return;

                var created = DMBlueprintCatalog.CreateAtLocation(dm, blueprint, BuildSpawnLocation(area, position));
                ReportCreated(blueprint, created);
            }, true);
        };

        public Action OnClickCreateHere() => () =>
        {
            var blueprint = GetSelectedBlueprint();
            if (blueprint == null)
                return;

            var created = DMBlueprintCatalog.CreateAtLocation(
                DMPresence.GetDM(Player), blueprint, GetLocation(DMPresence.GetControlledCreature(Player)));
            ReportCreated(blueprint, created);
        };

        public Action OnClickCreateInInventory() => () =>
        {
            var blueprint = GetSelectedBlueprint();
            if (blueprint == null || blueprint.Category != DMBlueprintCategory.Items)
                return;

            ReportCreated(blueprint, DMBlueprintCatalog.CreateInInventory(
                DMPresence.GetDM(Player), blueprint, DMPresence.GetControlledCreature(Player)));
        };
    }
}
