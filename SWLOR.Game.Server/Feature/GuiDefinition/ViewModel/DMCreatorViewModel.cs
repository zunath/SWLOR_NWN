using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class DMCreatorViewModel : GuiViewModelBase<DMCreatorViewModel, GuiPayloadBase>
    {
        public const string MainContentElement = "dmcreator_content";
        public const string MainContentPartial = "DMCREATOR_MAIN";

        private const int ResultsPerPage = 20;

        private List<DMBlueprint> _results = new();
        private readonly List<DMBlueprint> _pageResults = new();
        private int _selectedIndex = -1;
        private bool _suppressSearch;

        public int SelectedCategory
        {
            get => Get<int>();
            set
            {
                Set(value);
                if (!_suppressSearch)
                    RunSearch();
            }
        }

        public int SelectedSource
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

        public GuiBindingList<string> ResultNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> ResultResrefs
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> ResultTags
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
            SelectedCategory = (int)DMBlueprintCategory.Creatures;
            SelectedSource = (int)DMBlueprintSource.Custom;
            SearchText = string.Empty;
            ResultNames = new GuiBindingList<string>();
            ResultResrefs = new GuiBindingList<string>();
            ResultTags = new GuiBindingList<string>();
            ResultSelections = new GuiBindingList<bool>();
            PageNumbers = new GuiBindingList<GuiComboEntry>();
            SelectedPageIndex = 0;
            ResultSummary = string.Empty;
            SelectedDetails = string.Empty;
            HasSelection = false;
            CanCreateInInventory = false;
            _suppressSearch = false;

            WatchOnClient(model => model.SelectedCategory);
            WatchOnClient(model => model.SelectedSource);
            WatchOnClient(model => model.SearchText);
            WatchOnClient(model => model.SelectedPageIndex);

            ChangePartialView(MainContentElement, MainContentPartial);
            RunSearch();
        }

        private void RunSearch()
        {
            if (Player == 0 || WindowToken <= 0)
                return;

            var category = (DMBlueprintCategory)SelectedCategory;
            if (!DMBlueprintCatalog.IsLoaded(category))
            {
                _results = new List<DMBlueprint>();
                ShowPage();
                ResultSummary = $"Loading {category.ToString().ToLower()}. This happens once per server restart.";
                DMBlueprintCatalog.Load(category, () =>
                {
                    if (Gui.IsWindowOpen(Player, GuiWindowType.DMCreator) && SelectedCategory == (int)category)
                        RunSearch();
                });
                return;
            }

            var terms = DMSearchText.GetTerms(SearchText);
            var source = (DMBlueprintSource)SelectedSource;
            _results = DMBlueprintCatalog.GetBlueprints(category)
                .Where(blueprint => blueprint.Source == source &&
                                    DMSearchText.Matches(terms, blueprint.Name, blueprint.Resref, blueprint.Tag))
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

            var names = new GuiBindingList<string>();
            var resrefs = new GuiBindingList<string>();
            var tags = new GuiBindingList<string>();
            var selections = new GuiBindingList<bool>();
            foreach (var blueprint in _pageResults)
            {
                names.Add(blueprint.Name);
                resrefs.Add(blueprint.Resref);
                tags.Add(blueprint.Tag);
                selections.Add(false);
            }

            ResultNames = names;
            ResultResrefs = resrefs;
            ResultTags = tags;
            ResultSelections = selections;
            ResultSummary = _results.Count == 0
                ? "No matches."
                : $"{_results.Count} blueprint{(_results.Count == 1 ? string.Empty : "s")}.";

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
                SelectedDetails = "Select a blueprint to create it.";
                HasSelection = false;
                CanCreateInInventory = false;
                return;
            }

            ResultSelections[_selectedIndex] = true;
            SelectedDetails = string.Join("\n",
                $"Name: {blueprint.Name}",
                $"Resref: {blueprint.Resref}",
                $"Tag: {blueprint.Tag}",
                $"Palette: {blueprint.Source}");
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
            SendMessageToPC(Player, GetIsObjectValid(created)
                ? $"Created {blueprint.Name} ({blueprint.Resref})."
                : $"Could not create {blueprint.Name} ({blueprint.Resref}).");
        }

        // New objects face the DM.
        private Location BuildSpawnLocation(uint area, Vector3 position)
        {
            var toDM = GetPosition(Player) - position;
            var facing = toDM == Vector3() ? GetFacing(Player) : VectorToAngle(toDM);
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

            Targeting.EnterTargetingMode(Player, ObjectType.All, $"Click where to create {blueprint.Name}.", target =>
            {
                var area = GetArea(Player);
                var targetType = GetIsObjectValid(target) ? GetObjectType(target) : ObjectType.Invalid;
                if (blueprint.Category == DMBlueprintCategory.Items &&
                    (targetType == ObjectType.Creature || targetType == ObjectType.Placeable) &&
                    GetHasInventory(target))
                {
                    ReportCreated(blueprint, DMBlueprintCatalog.CreateInInventory(Player, blueprint, target));
                    return;
                }

                var position = GetTargetingModeSelectedPosition();
                if (position == Vector3() && GetIsObjectValid(target) && target != area)
                    position = GetPosition(target);

                if (position == Vector3())
                    return;

                var created = DMBlueprintCatalog.CreateAtLocation(Player, blueprint, BuildSpawnLocation(area, position));
                ReportCreated(blueprint, created);
            }, true);
        };

        public Action OnClickCreateHere() => () =>
        {
            var blueprint = GetSelectedBlueprint();
            if (blueprint == null)
                return;

            var created = DMBlueprintCatalog.CreateAtLocation(Player, blueprint, GetLocation(Player));
            ReportCreated(blueprint, created);
        };

        public Action OnClickCreateInInventory() => () =>
        {
            var blueprint = GetSelectedBlueprint();
            if (blueprint == null || blueprint.Category != DMBlueprintCategory.Items)
                return;

            ReportCreated(blueprint, DMBlueprintCatalog.CreateInInventory(Player, blueprint, Player));
        };
    }
}
