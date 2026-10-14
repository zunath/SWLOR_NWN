using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class DMChooserViewModel : GuiViewModelBase<DMChooserViewModel, GuiPayloadBase>,
        IGuiRefreshable<DMAreaChangedRefreshEvent>
    {
        public const string MainContentElement = "dmchooser_content";
        public const string MainContentPartial = "DMCHOOSER_MAIN";
        public const string AreaContentPartial = "DMCHOOSER_AREAS";

        private const int ResultsPerPage = 20;

        private List<DMObjectSearchResult> _results = new();
        private readonly List<DMObjectSearchResult> _pageResults = new();
        private int _selectedIndex = -1;
        private bool _isTruncated;
        private bool _suppressSearch;
        private string _contentPartial;
        private uint _dm;
        private DMObjectScan _scan;
        private int _searchGeneration;

        public int SelectedCategory
        {
            get => Get<int>();
            set
            {
                Set(value);
                IsScopeEnabled = value != (int)DMObjectCategory.Areas;
                InfoHeader = (DMObjectCategory)value switch
                {
                    DMObjectCategory.Creatures => "Level · HP",
                    DMObjectCategory.Items => "Type",
                    DMObjectCategory.Placeables or DMObjectCategory.Doors => "State",
                    _ => string.Empty
                };
                if (!_suppressSearch)
                {
                    ShowCategoryContent();
                    RunSearch();
                }
            }
        }

        public int SelectedScope
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_suppressSearch)
                    RunSearch();
            }
        }

        public bool IsScopeEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string SearchText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> ResultNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> ResultIcons
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> ResultInfo
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public string InfoHeader
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

        public GuiBindingList<string> ResultAreas
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

        public string SelectedDetails
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool CanJumpTo
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool CanBringHere
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool CanExamine
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool CanDestroy
        {
            get => Get<bool>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            _dm = DMPresence.GetDM(Player);
            Geometry = DMToolbarViewModel.PlaceBelowToolbar(Player, Geometry);
            _suppressSearch = true;
            SelectedCategory = (int)DMObjectCategory.Areas;
            SelectedScope = (int)DMObjectSearchScope.CurrentArea;
            SearchText = string.Empty;
            ResultIcons = new GuiBindingList<string>();
            ResultNames = new GuiBindingList<string>();
            ResultInfo = new GuiBindingList<string>();
            ResultAreas = new GuiBindingList<string>();
            ResultSelections = new GuiBindingList<bool>();
            PageNumbers = new GuiBindingList<GuiComboEntry>();
            SelectedPageIndex = 0;
            ResultSummary = string.Empty;
            SelectedDetails = string.Empty;
            DetailIcon = DMObjectDetails.NoSelectionIcon;
            DetailTitle = string.Empty;
            CanJumpTo = false;
            CanBringHere = false;
            CanExamine = false;
            CanDestroy = false;
            _suppressSearch = false;

            WatchOnClient(model => model.SelectedCategory);
            WatchOnClient(model => model.SelectedScope);
            WatchOnClient(model => model.SearchText);
            WatchOnClient(model => model.SelectedPageIndex);

            _contentPartial = null;
            ShowCategoryContent();
            RunSearch();
        }

        // Areas have their own list layout. The layout is only resent when switching to or from areas.
        private void ShowCategoryContent()
        {
            var partial = SelectedCategory == (int)DMObjectCategory.Areas ? AreaContentPartial : MainContentPartial;
            if (partial == _contentPartial)
                return;

            _contentPartial = partial;
            ChangePartialView(MainContentElement, partial);
        }

        private void RunSearch()
        {
            if (Player == 0 || WindowToken <= 0)
                return;

            // The window can stay on a creature the DM no longer possesses, so Current Area is the area of
            // whatever the DM controls now.
            _scan = DMObjectSearch.StartSearch(
                _dm,
                (DMObjectCategory)SelectedCategory,
                (DMObjectSearchScope)SelectedScope,
                SearchText);
            ContinueSearch(++_searchGeneration, true);
        }

        // A search of every area reads a slice of objects per server tick, so a large search never stalls the server.
        private void ContinueSearch(int generation, bool isFirstSlice)
        {
            if (generation != _searchGeneration ||
                !GetIsObjectValid(Player) ||
                NuiFindWindow(Player, Gui.BuildWindowId(WindowType)) != WindowToken)
                return;

            if (!_scan.Continue(DMObjectSearch.ObjectsPerSlice))
            {
                if (isFirstSlice)
                {
                    _results = new List<DMObjectSearchResult>();
                    _isTruncated = false;
                    ShowResults();
                    ResultSummary = "Searching every area...";
                }

                Scheduler.Schedule(() => ContinueSearch(generation, false), TimeSpan.Zero);
                return;
            }

            _results = _scan.Results.ToList();
            _isTruncated = _scan.IsTruncated;
            ShowResults();
        }

        private void ShowResults()
        {
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
            var areas = new GuiBindingList<string>();
            var selections = new GuiBindingList<bool>();
            foreach (var result in _pageResults)
            {
                var (icon, summary) = GetRow(result);
                icons.Add(icon);
                names.Add(string.IsNullOrWhiteSpace(result.Name) ? result.Resref : result.Name);
                info.Add(summary);
                areas.Add(result.AreaName);
                selections.Add(false);
            }

            ResultIcons = icons;
            ResultNames = names;
            ResultInfo = info;
            ResultAreas = areas;
            ResultSelections = selections;

            ResultSummary = _results.Count == 0
                ? "No matches."
                : _isTruncated
                    ? $"Showing the first {_results.Count} matches. Narrow the search to see the rest."
                    : $"{_results.Count} match{(_results.Count == 1 ? string.Empty : "es")}.";

            SelectResult(-1);
        }

        private void SelectResult(int index)
        {
            if (_selectedIndex >= 0 && _selectedIndex < ResultSelections.Count)
                ResultSelections[_selectedIndex] = false;

            _selectedIndex = index;
            var result = GetSelectedResult();
            if (result == null)
            {
                DetailIcon = DMObjectDetails.NoSelectionIcon;
                DetailTitle = string.Empty;
                SelectedDetails = "Select a result to see its details and actions.";
                CanJumpTo = false;
                CanBringHere = false;
                CanExamine = false;
                CanDestroy = false;
                return;
            }

            ResultSelections[_selectedIndex] = true;

            var details = result.Category == DMObjectCategory.Areas
                ? DMObjectDetails.DescribeArea(result.Object)
                : DMObjectDetails.DescribeObject(result.Object);
            DetailIcon = details.IconResref;
            DetailTitle = details.Title;
            SelectedDetails = details.Body;

            var category = result.Category;
            CanJumpTo = true;
            CanBringHere = category == DMObjectCategory.Creatures && !GetIsDMPossessed(result.Object);
            CanExamine = category is DMObjectCategory.Creatures or DMObjectCategory.Placeables
                or DMObjectCategory.Items or DMObjectCategory.Doors;
            // A creature a DM is possessing is someone else's body for now. Bring Here also rechecks when clicked.
            CanDestroy = category is DMObjectCategory.Creatures or DMObjectCategory.Placeables
                or DMObjectCategory.Items && !GetIsDMPossessed(result.Object);
        }

        private static (string Icon, string Summary) GetRow(DMObjectSearchResult result)
        {
            if (!GetIsObjectValid(result.Object))
                return (DMObjectDetails.NoSelectionIcon, "Gone");

            if (result.Category != DMObjectCategory.Areas)
                return DMObjectDetails.GetObjectRow(result.Object);

            var players = DMObjectDetails.CountPlayersInArea(result.Object);
            return (DMObjectDetails.AreaIcon, players == 0 ? string.Empty : players.ToString());
        }

        private DMObjectSearchResult GetSelectedResult()
        {
            return _selectedIndex >= 0 && _selectedIndex < _pageResults.Count
                ? _pageResults[_selectedIndex]
                : null;
        }

        // Returns the selected object if it still exists, and tells the DM when it does not.
        private DMObjectSearchResult GetLiveSelection()
        {
            var result = GetSelectedResult();
            if (result == null)
                return null;

            if (GetIsObjectValid(result.Object))
                return result;

            DMPresence.SendMessage(Player, "That object no longer exists.");
            RunSearch();
            return null;
        }

        public Action OnSelectResult() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _pageResults.Count)
                return;

            SelectResult(index);
        };

        public Action OnClickSearch() => RunSearch;

        public void Refresh(DMAreaChangedRefreshEvent payload)
        {
            // Results scoped to the current area belong to the area the DM just left.
            if (SelectedScope == (int)DMObjectSearchScope.CurrentArea &&
                SelectedCategory != (int)DMObjectCategory.Areas)
            {
                RunSearch();
            }
        }

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

        public Action OnClickJumpTo() => () =>
        {
            var result = GetLiveSelection();
            if (result == null)
                return;

            var location = result.Category == DMObjectCategory.Areas
                ? DMObjectSearch.GetAreaJumpLocation(result.Object)
                : GetLocation(result.Object);
            DMPresence.JumpTo(Player, location, $"{result.Category} '{result.Name}' ({result.Resref}, tag '{result.Tag}')");
        };

        public Action OnClickBringHere() => () =>
        {
            var result = GetLiveSelection();
            if (result == null || result.Category != DMObjectCategory.Creatures)
                return;

            var error = DMPresence.Summon(Player, result.Object);
            if (!string.IsNullOrEmpty(error))
                DMPresence.SendMessage(Player, error);
        };

        public Action OnClickExamine() => () =>
        {
            var result = GetLiveSelection();
            if (result == null)
                return;

            var target = result.Object;
            AssignCommand(DMPresence.GetControlledCreature(Player), () => ActionExamine(target));
        };

        public Action OnClickDestroy() => () =>
        {
            var result = GetSelectedResult();
            if (result == null || !CanDestroy)
                return;

            ShowModal($"Destroy {result.Name} ({result.Resref})? This cannot be undone.", () =>
            {
                if (!GetIsObjectValid(result.Object))
                {
                    DMPresence.SendMessage(Player, "That object no longer exists.");
                }
                else if (GetIsDMPossessed(result.Object))
                {
                    DMPresence.SendMessage(Player, "A DM is possessing that creature.");
                }
                else
                {
                    Log.Write(LogGroup.DM,
                        $"{DMPresence.DescribeDM(Player)} destroyed {result.Category} '{result.Name}' ({result.Resref}, tag '{result.Tag}') in {result.AreaName}.");
                    DestroyObject(result.Object);
                }

                // DestroyObject finishes at the end of the script, so search again on the next tick.
                DelayCommand(0.1f, RunSearch);
            });
        };
    }
}
