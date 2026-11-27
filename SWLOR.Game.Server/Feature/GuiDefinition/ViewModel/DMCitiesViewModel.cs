using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// The DM city window (Player City Revamp, "Story hooks" and "Planetary councils"): every city with its mayor, officers,
    /// recent logins, active Edicts, Gazette, and story request, with Take and Done buttons for the request and a planet
    /// filter that calls a council. Staff only: every action checks that a DM is acting.
    /// </summary>
    public class DMCitiesViewModel : GuiViewModelBase<DMCitiesViewModel, GuiPayloadBase>
    {
        public const string MainContentElement = "dmcities_content";
        public const string MainContentPartial = "DMCITIES_MAIN";

        private List<DMCitySummary> _allCities = new();
        private readonly List<DMCitySummary> _shownCities = new();
        private readonly List<PlanetType> _planetFilters = new();
        private string _selectedCityId = string.Empty;

        public string StatusText { get => Get<string>(); set => Set(value); }
        public GuiColor StatusColor { get => Get<GuiColor>(); set => Set(value); }

        public GuiBindingList<GuiComboEntry> PlanetOptions { get => Get<GuiBindingList<GuiComboEntry>>(); set => Set(value); }

        public int SelectedPlanetIndex
        {
            get => Get<int>();
            set
            {
                Set(value);
                ApplyFilter();
            }
        }

        public string SummonMessage { get => Get<string>(); set => Set(value); }
        public bool CanSummon { get => Get<bool>(); set => Set(value); }

        public GuiBindingList<string> CityLabels { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> CityToggles { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public string ListSummary { get => Get<string>(); set => Set(value); }

        public string DetailsText { get => Get<string>(); set => Set(value); }
        public string RequestText { get => Get<string>(); set => Set(value); }
        public bool CanTake { get => Get<bool>(); set => Set(value); }
        public bool CanFinish { get => Get<bool>(); set => Set(value); }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            Geometry = DMToolbarViewModel.PlaceBelowToolbar(Player, Geometry);

            StatusText = string.Empty;
            StatusColor = GuiColor.Green;
            PlanetOptions = new GuiBindingList<GuiComboEntry>();
            SummonMessage = string.Empty;
            CanSummon = false;
            CityLabels = new GuiBindingList<string>();
            CityToggles = new GuiBindingList<bool>();
            ListSummary = string.Empty;
            DetailsText = string.Empty;
            RequestText = string.Empty;
            CanTake = false;
            CanFinish = false;

            // The planet list is built from the cities, before the combo is watched.
            SelectedPlanetIndex = LoadCities();

            WatchOnClient(model => model.SelectedPlanetIndex);
            WatchOnClient(model => model.SummonMessage);

            ChangePartialView(MainContentElement, MainContentPartial);
        }

        /// <summary>
        /// Whether staff are acting: a DM, a creature a DM possesses, or an authorized DM or admin on their own character,
        /// as the /cities command allows. Every action asks, so the window does nothing for anyone else.
        /// </summary>
        private bool IsStaff()
        {
            if (DMPresence.IsControlledByDM(Player))
                return true;

            var level = Authorization.GetAuthorizationLevel(Player);
            return level.HasFlag(AuthorizationLevel.DM) || level.HasFlag(AuthorizationLevel.Admin);
        }

        private void ShowError(string message)
        {
            StatusText = message;
            StatusColor = GuiColor.Red;
        }

        private void ShowSuccess(string message)
        {
            StatusText = message;
            StatusColor = GuiColor.Green;
        }

        /// <summary>
        /// Reads every city again and rebuilds the planet filter. Returns the filter entry to select: the planet the DM was
        /// looking at when it still has cities, otherwise All planets.
        /// </summary>
        private int LoadCities()
        {
            _allCities = DMCityDirectory.GetCities();

            // One entry for every planet that has a city on its council, and "All planets" first.
            var selectedPlanet = SelectedPlanetIndex >= 0 && SelectedPlanetIndex < _planetFilters.Count
                ? _planetFilters[SelectedPlanetIndex]
                : PlanetType.Invalid;

            _planetFilters.Clear();
            _planetFilters.Add(PlanetType.Invalid);
            var options = new GuiBindingList<GuiComboEntry> { new GuiComboEntry("All planets", 0) };
            foreach (var group in _allCities
                         .Where(city => city.Planet != PlanetType.Invalid)
                         .GroupBy(city => city.Planet)
                         .OrderBy(group => PlanetaryCouncils.GetPlanetName(group.Key), StringComparer.OrdinalIgnoreCase))
            {
                options.Add(new GuiComboEntry($"{PlanetaryCouncils.GetPlanetName(group.Key)} ({group.Count()})", _planetFilters.Count));
                _planetFilters.Add(group.Key);
            }

            PlanetOptions = options;

            var keptIndex = _planetFilters.IndexOf(selectedPlanet);
            return keptIndex < 0 ? 0 : keptIndex;
        }

        private PlanetType GetSelectedPlanet()
        {
            return SelectedPlanetIndex >= 0 && SelectedPlanetIndex < _planetFilters.Count
                ? _planetFilters[SelectedPlanetIndex]
                : PlanetType.Invalid;
        }

        /// <summary>Lists the cities under the planet filter and keeps the selection when the city is still listed.</summary>
        private void ApplyFilter()
        {
            if (_planetFilters.Count == 0)
                return;

            var planet = GetSelectedPlanet();
            _shownCities.Clear();
            _shownCities.AddRange(_allCities.Where(city => DMCityRules.MatchesPlanet(planet, city.Planet)));

            if (_shownCities.All(city => city.CityId != _selectedCityId))
                _selectedCityId = string.Empty;

            var labels = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();
            foreach (var city in _shownCities)
            {
                labels.Add(DMCityRules.BuildListLabel(city));
                toggles.Add(city.CityId == _selectedCityId);
            }

            CityLabels = labels;
            CityToggles = toggles;
            ListSummary = $"{_shownCities.Count} of {_allCities.Count} cities. " +
                          $"{_shownCities.Count(city => StoryRequestRules.IsOpen(city.StoryRequest))} with an open story request.";
            CanSummon = planet != PlanetType.Invalid;
            ShowSelected();
        }

        private void ShowSelected()
        {
            var city = _shownCities.FirstOrDefault(c => c.CityId == _selectedCityId);
            DetailsText = city == null ? "Pick a city to read about it." : DMCityRules.BuildDetails(city);
            RequestText = city == null ? string.Empty : DMCityRules.BuildRequestText(city);
            CanTake = city != null && StoryRequestRules.CanTake(city.StoryRequest);
            CanFinish = city != null && StoryRequestRules.CanFinish(city.StoryRequest);
        }

        // Closing a modal rebuilds the window, so what was typed in the summons box is pushed again.
        protected override void OnModalClosedRestore() => SummonMessage = SummonMessage;

        public Action OnSelectCity() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _shownCities.Count)
                return;

            _selectedCityId = _shownCities[index].CityId;
            for (var row = 0; row < CityToggles.Count; row++)
            {
                CityToggles[row] = row == index;
            }

            ShowSelected();
        };

        public Action OnClickRefresh() => () =>
        {
            if (!IsStaff())
                return;

            SelectedPlanetIndex = LoadCities();
            ShowSuccess("Cities refreshed.");
        };

        public Action OnClickTake() => () => ChangeRequest(CityStoryRequests.Take, "Request taken.");

        public Action OnClickDone() => () => ChangeRequest(CityStoryRequests.Finish, "Request marked done.");

        private void ChangeRequest(Func<string, string> change, string success)
        {
            if (!IsStaff() || string.IsNullOrWhiteSpace(_selectedCityId))
                return;

            var error = change(_selectedCityId);
            if (string.IsNullOrEmpty(error))
                ShowSuccess(success);
            else
                ShowError(error);

            // The list reads the cities again, so the request's new status shows at once.
            SelectedPlanetIndex = LoadCities();
        }

        public Action OnClickSummon() => () =>
        {
            if (!IsStaff())
                return;

            var planet = GetSelectedPlanet();
            if (planet == PlanetType.Invalid)
            {
                ShowError("Pick a planet in the filter first.");
                return;
            }

            var planetName = PlanetaryCouncils.GetPlanetName(planet);
            var message = SummonMessage;
            ShowModal(
                $"Call a council on {planetName}? Every mayor and officer of its cities gets a message now if online and a notice at their next login, and each city's Gazette posts it.",
                () =>
                {
                    if (!IsStaff())
                        return;

                    ShowSuccess(PlanetaryCouncils.Summon(Player, planet, message));
                    SummonMessage = string.Empty;
                    SelectedPlanetIndex = LoadCities();
                },
                null,
                "Summon",
                "Cancel");
        };
    }
}
