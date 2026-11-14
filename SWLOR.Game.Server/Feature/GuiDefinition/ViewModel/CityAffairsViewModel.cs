using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public partial class CityAffairsViewModel : GuiViewModelBase<CityAffairsViewModel, CityAffairsPayload>
    {
        public const string TabContentElement = "cityaffairs_content";
        public const string SituationsTabPartial = "CITYAFFAIRS_SITUATIONS";
        public const string OfficersTabPartial = "CITYAFFAIRS_OFFICERS";
        public const string StoryTabPartial = "CITYAFFAIRS_STORY";
        public const string BountiesTabPartial = "CITYAFFAIRS_BOUNTIES";

        private const int SituationsTabId = 0;
        private const int OfficersTabId = 1;
        private const int StoryTabId = 2;
        private const int BountiesTabId = 3;

        private static readonly GuiTabGroup<CityAffairsViewModel, CityAffairsPayload> Tabs =
            new GuiTabGroup<CityAffairsViewModel, CityAffairsPayload>()
                .AddTab(SituationsTabId, SituationsTabPartial, model => model.RefreshSituations())
                .AddTab(OfficersTabId, OfficersTabPartial, model => model.RefreshOfficers())
                .AddTab(StoryTabId, StoryTabPartial, model => model.RefreshStory())
                .AddTab(BountiesTabId, BountiesTabPartial, model => model.RefreshBounties());

        private static readonly GuiToggleGroupSync TabToggles = new(SituationsTabId, OfficersTabId, StoryTabId, BountiesTabId);

        private string _cityId = string.Empty;
        private bool _isMayor;
        private readonly List<string> _situationIds = new();
        private int _selectedSituationIndex = -1;
        private readonly List<string> _officerIds = new();
        private int _selectedOfficerIndex = -1;
        private readonly List<string> _citizenIds = new();

        public int SelectedTabId { get => Get<int>(); set => Set(value); }

        public int TabToggleValue
        {
            get => Get<int>();
            set
            {
                Set(value);
                TabToggles.HandleClientChange(value, SelectTab);
            }
        }

        public string Instructions { get => Get<string>(); set => Set(value); }
        public GuiColor InstructionsColor { get => Get<GuiColor>(); set => Set(value); }

        // Situations tab
        public string StatusText { get => Get<string>(); set => Set(value); }
        public bool IsSituationListEmpty { get => Get<bool>(); set => Set(value); }
        public GuiBindingList<string> SituationNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> SituationToggles { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public bool IsSituationSelected { get => Get<bool>(); set => Set(value); }
        public string SelectedTitle { get => Get<string>(); set => Set(value); }
        public string SelectedText { get => Get<string>(); set => Set(value); }
        public string SelectedTimeLeft { get => Get<string>(); set => Set(value); }
        public GuiBindingList<string> ChoiceTexts { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> ChoiceInfos { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> ChoiceEnabled { get => Get<GuiBindingList<bool>>(); set => Set(value); }

        // Officers tab
        public string OfficerSummary { get => Get<string>(); set => Set(value); }
        public GuiBindingList<string> OfficerNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> OfficerTitles { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> OfficerDates { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> OfficerToggles { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public bool IsMayor { get => Get<bool>(); set => Set(value); }
        public GuiBindingList<GuiComboEntry> CitizenOptions { get => Get<GuiBindingList<GuiComboEntry>>(); set => Set(value); }
        public int SelectedCitizenIndex { get => Get<int>(); set => Set(value); }
        public string TitleInput { get => Get<string>(); set => Set(value); }
        public bool CanAppoint { get => Get<bool>(); set => Set(value); }
        public bool HasSelectedOfficer { get => Get<bool>(); set => Set(value); }

        // Story request tab
        public string StoryStatusText { get => Get<string>(); set => Set(value); }
        public string CurrentStoryText { get => Get<string>(); set => Set(value); }
        public bool HasCurrentStory { get => Get<bool>(); set => Set(value); }
        public string StoryInput { get => Get<string>(); set => Set(value); }
        public bool CanPostStory { get => Get<bool>(); set => Set(value); }

        protected override void Initialize(CityAffairsPayload initialPayload)
        {
            _cityId = initialPayload?.CityId ?? string.Empty;
            var city = string.IsNullOrWhiteSpace(_cityId) ? null : DB.Get<WorldProperty>(_cityId);
            // Only the sitting mayor appoints. An Acting Mayor and the officers decide situations and post requests only.
            _isMayor = city != null && CityPowers.IsMayor(city, GetObjectUUID(Player));

            TabToggleValue = SituationsTabId;
            SelectedTabId = SituationsTabId;
            Instructions = string.Empty;
            InstructionsColor = GuiColor.Green;

            StatusText = string.Empty;
            IsSituationListEmpty = true;
            SituationNames = new GuiBindingList<string>();
            SituationToggles = new GuiBindingList<bool>();
            IsSituationSelected = false;
            SelectedTitle = string.Empty;
            SelectedText = string.Empty;
            SelectedTimeLeft = string.Empty;
            ChoiceTexts = new GuiBindingList<string>();
            ChoiceInfos = new GuiBindingList<string>();
            ChoiceEnabled = new GuiBindingList<bool>();

            OfficerSummary = string.Empty;
            OfficerNames = new GuiBindingList<string>();
            OfficerTitles = new GuiBindingList<string>();
            OfficerDates = new GuiBindingList<string>();
            OfficerToggles = new GuiBindingList<bool>();
            IsMayor = _isMayor;
            CitizenOptions = new GuiBindingList<GuiComboEntry>();
            SelectedCitizenIndex = 0;
            TitleInput = string.Empty;
            CanAppoint = false;
            HasSelectedOfficer = false;

            StoryStatusText = string.Empty;
            CurrentStoryText = string.Empty;
            HasCurrentStory = false;
            StoryInput = string.Empty;
            CanPostStory = false;

            InitializeBounties();

            WatchOnClient(model => model.TabToggleValue);
            WatchOnClient(model => model.SelectedCitizenIndex);
            WatchOnClient(model => model.TitleInput);
            WatchOnClient(model => model.StoryInput);
            WatchOnClient(model => model.SelectedTargetIndex);
            WatchOnClient(model => model.RewardInput);

            SelectTab(SituationsTabId);
        }

        private void SelectTab(int tabId)
        {
            SelectedTabId = tabId;
            TabToggles.SyncTo(tabId, value => TabToggleValue = value);
            Tabs.Select(this, TabContentElement, tabId, RestoreStoryInput);
        }

        protected override void OnModalClosedRestore() => Tabs.Select(this, TabContentElement, SelectedTabId, RestoreStoryInput);

        /// <summary>
        /// Pushes the story request and bounty reward fields again once their tab exists, since the client does not keep
        /// what was typed into a field that was off screen.
        /// </summary>
        private void RestoreStoryInput()
        {
            if (SelectedTabId == StoryTabId)
                StoryInput = StoryInput;
            else if (SelectedTabId == BountiesTabId)
                RewardInput = RewardInput;
        }

        private void ShowError(string message)
        {
            Instructions = message;
            InstructionsColor = GuiColor.Red;
        }

        private void ShowSuccess(string message)
        {
            Instructions = message;
            InstructionsColor = GuiColor.Green;
        }

        #region Situations

        private void RefreshSituations()
        {
            var city = DB.Get<WorldProperty>(_cityId);
            if (city == null)
            {
                ShowError("This city no longer exists.");
                return;
            }

            var now = DateTime.UtcNow;
            var open = CityAffairs.LoadOpenSituations(_cityId);
            var selectedId = _selectedSituationIndex >= 0 && _selectedSituationIndex < _situationIds.Count
                ? _situationIds[_selectedSituationIndex]
                : string.Empty;

            _situationIds.Clear();
            var names = new GuiBindingList<string>();
            foreach (var situation in open)
            {
                var detail = SituationCatalog.Get(situation.Type);
                _situationIds.Add(situation.Id);
                names.Add($"{detail?.Title ?? situation.Type.ToString()} - {SituationRules.FormatTimeLeft(situation.ExpiresUtc, now)}");
            }

            _selectedSituationIndex = open.Count == 0
                ? -1
                : Math.Max(0, _situationIds.IndexOf(selectedId));

            var toggles = new GuiBindingList<bool>();
            for (var index = 0; index < open.Count; index++)
            {
                toggles.Add(index == _selectedSituationIndex);
            }

            var restrictedText = city.RestrictedTreasury > 0
                ? $", plus {SituationRules.FormatCredits((int)Math.Min(int.MaxValue, city.RestrictedTreasury))} that cannot be withdrawn"
                : string.Empty;
            StatusText = $"Treasury: {SituationRules.FormatCredits((int)Math.Min(int.MaxValue, city.Treasury))}{restrictedText}. " +
                         $"Ignored in a row: {city.NeglectCount} of {RecallRules.NeglectThreshold}.";
            SituationNames = names;
            SituationToggles = toggles;
            IsSituationListEmpty = open.Count == 0;
            RefreshSelectedSituation(city, open, now);
        }

        private void RefreshSelectedSituation(WorldProperty city, List<CitySituation> open, DateTime now)
        {
            var choiceTexts = new GuiBindingList<string>();
            var choiceInfos = new GuiBindingList<string>();
            var choiceEnabled = new GuiBindingList<bool>();

            var situation = _selectedSituationIndex >= 0 && _selectedSituationIndex < open.Count
                ? open[_selectedSituationIndex]
                : null;
            var detail = situation == null ? null : SituationCatalog.Get(situation.Type);

            IsSituationSelected = detail != null;
            if (detail == null)
            {
                SelectedTitle = string.Empty;
                SelectedText = string.Empty;
                SelectedTimeLeft = string.Empty;
            }
            else
            {
                SelectedTitle = detail.Title;
                SelectedText = string.IsNullOrWhiteSpace(situation.SubjectNote) ? detail.Text : $"{detail.Text} {situation.SubjectNote}";
                SelectedTimeLeft = SituationRules.CanDecide(situation, now)
                    ? SituationRules.FormatTimeLeft(situation.ExpiresUtc, now)
                    : "Out of time. It takes its ignored outcome at the next restart.";

                var context = CityAffairs.BuildContext(city, CityAffairs.GetSkillRanks(GetObjectUUID(Player)));
                var canDecide = SituationRules.CanDecide(situation, now);
                var subjectCredits = CityAffairs.GetSubjectCredits(situation);
                foreach (var choice in detail.Choices)
                {
                    var unmet = SituationRules.DescribeUnmet(choice.Requirements, context);
                    var affordable = SituationRules.CanAfford(choice.GetCost(subjectCredits), SituationTreasury.GetSpendable(city));
                    choiceTexts.Add(choice.Text);
                    choiceInfos.Add(DescribeChoice(choice, unmet, affordable, subjectCredits));
                    choiceEnabled.Add(canDecide && unmet.Count == 0 && affordable);
                }
            }

            ChoiceTexts = choiceTexts;
            ChoiceInfos = choiceInfos;
            ChoiceEnabled = choiceEnabled;
        }

        private static string DescribeChoice(SituationChoice choice, List<string> unmet, bool affordable, int subjectCredits)
        {
            var parts = new List<string>();
            var effects = SituationRules.DescribeEffects(choice.Effects, choice.IsSecret, subjectCredits);
            if (effects.Length > 0)
                parts.Add(effects);
            else
                parts.Add("No cost.");

            if (choice.Requirements.Count > 0)
            {
                var needs = string.Join(", ", choice.Requirements.Select(SituationRules.Describe));
                parts.Add(unmet.Count > 0 ? $"Needs {needs}." : $"Needs {needs}. You qualify.");
            }

            if (!affordable)
                parts.Add("The treasury cannot afford this.");

            return string.Join(" ", parts);
        }

        public Action SelectSituation() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _situationIds.Count)
                return;

            _selectedSituationIndex = index;
            RefreshSituations();
        };

        public Action OnClickChoice() => () =>
        {
            var choiceIndex = NuiGetEventArrayIndex();
            if (_selectedSituationIndex < 0 || _selectedSituationIndex >= _situationIds.Count)
                return;

            var situationId = _situationIds[_selectedSituationIndex];
            var situation = DB.Get<CitySituation>(situationId);
            var detail = situation == null ? null : SituationCatalog.Get(situation.Type);
            if (detail == null || choiceIndex < 0 || choiceIndex >= detail.Choices.Count)
                return;

            var choice = detail.Choices[choiceIndex];
            var effects = SituationRules.DescribeEffects(choice.Effects, choice.IsSecret, CityAffairs.GetSubjectCredits(situation));
            var publication = choice.IsSecret
                ? "The decision is final."
                : "The decision is final and goes in the City Gazette under your name and title.";
            ShowModal(
                $"{detail.Title}: {choice.Text}? {effects} {publication}",
                () =>
                {
                    var result = CityAffairs.Decide(Player, _cityId, situationId, choiceIndex);
                    if (result.Succeeded)
                        ShowSuccess(result.Message);
                    else
                        ShowError(result.Message);

                    RefreshSituations();
                },
                null,
                "Decide",
                "Cancel");
        };

        #endregion

        #region Officers

        private void RefreshOfficers()
        {
            var city = DB.Get<WorldProperty>(_cityId);
            if (city == null)
            {
                ShowError("This city no longer exists.");
                return;
            }

            var level = CityOfficers.GetCityLevel(city);
            var limit = OfficerRules.GetOfficerLimit(level);
            var selectedId = _selectedOfficerIndex >= 0 && _selectedOfficerIndex < _officerIds.Count
                ? _officerIds[_selectedOfficerIndex]
                : string.Empty;

            _officerIds.Clear();
            var names = new GuiBindingList<string>();
            var titles = new GuiBindingList<string>();
            var dates = new GuiBindingList<string>();
            foreach (var (officerId, officer) in city.Officers.OrderBy(pair => pair.Value.AppointedUtc))
            {
                var player = DB.Get<Player>(officerId);
                _officerIds.Add(officerId);
                names.Add(PlayerName.GetKnownNameOrFallbackByPlayerId(Player, officerId, player?.Name));
                titles.Add(officer.Title);
                dates.Add(officer.AppointedUtc.ToString("yyyy-MM-dd"));
            }

            _selectedOfficerIndex = Math.Max(-1, _officerIds.IndexOf(selectedId));
            var toggles = new GuiBindingList<bool>();
            for (var index = 0; index < _officerIds.Count; index++)
            {
                toggles.Add(index == _selectedOfficerIndex);
            }

            OfficerSummary = _isMayor
                ? $"Officers: {_officerIds.Count} of {limit} for a level {level} city."
                : $"Officers: {_officerIds.Count} of {limit}. Only the mayor can appoint or remove officers.";
            OfficerNames = names;
            OfficerTitles = titles;
            OfficerDates = dates;
            OfficerToggles = toggles;
            HasSelectedOfficer = _selectedOfficerIndex >= 0;

            LoadAppointableCitizens(city, limit);
        }

        private void LoadAppointableCitizens(WorldProperty city, int limit)
        {
            _citizenIds.Clear();
            var options = new GuiBindingList<GuiComboEntry>();

            if (_isMayor)
            {
                var query = new DBQuery<Player>()
                    .AddFieldSearch(nameof(Entity.Player.CitizenPropertyId), city.Id, false)
                    .AddFieldSearch(nameof(Entity.Player.IsDeleted), false);
                var count = (int)DB.SearchCount(query);
                var citizens = count <= 0
                    ? new List<Player>()
                    : DB.Search(query.AddPaging(count, 0)).ToList();

                foreach (var citizen in citizens.Where(citizen => citizen.Id != city.OwnerPlayerId && !city.Officers.ContainsKey(citizen.Id)))
                {
                    options.Add(new GuiComboEntry(
                        PlayerName.GetKnownNameOrFallbackByPlayerId(Player, citizen.Id, citizen.Name),
                        _citizenIds.Count));
                    _citizenIds.Add(citizen.Id);
                }
            }

            var hasCitizens = _citizenIds.Count > 0;
            if (!hasCitizens)
                options.Add(new GuiComboEntry("No citizens to appoint", 0));

            CitizenOptions = options;
            SelectedCitizenIndex = 0;
            CanAppoint = _isMayor && hasCitizens && city.Officers.Count < limit;
        }

        public Action SelectOfficer() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _officerIds.Count)
                return;

            _selectedOfficerIndex = index;
            RefreshOfficers();
            if (_selectedOfficerIndex >= 0 && _selectedOfficerIndex < OfficerTitles.Count)
                TitleInput = OfficerTitles[_selectedOfficerIndex];
        };

        public Action OnClickAppoint() => () =>
        {
            if (SelectedCitizenIndex < 0 || SelectedCitizenIndex >= _citizenIds.Count)
            {
                ShowError("Pick a citizen to appoint.");
                return;
            }

            var error = CityOfficers.Appoint(_cityId, GetObjectUUID(Player), _citizenIds[SelectedCitizenIndex], TitleInput);
            if (!string.IsNullOrEmpty(error))
            {
                ShowError(error);
                RefreshOfficers();
                return;
            }

            ShowSuccess("Officer appointed. They can now decide City Affairs situations at City Hall.");
            TitleInput = string.Empty;
            RefreshOfficers();
        };

        public Action OnClickSetTitle() => () =>
        {
            if (_selectedOfficerIndex < 0 || _selectedOfficerIndex >= _officerIds.Count)
            {
                ShowError("Pick an officer in the list first.");
                return;
            }

            var error = CityOfficers.SetTitle(_cityId, GetObjectUUID(Player), _officerIds[_selectedOfficerIndex], TitleInput);
            if (!string.IsNullOrEmpty(error))
            {
                ShowError(error);
                return;
            }

            ShowSuccess("Title changed.");
            RefreshOfficers();
        };

        public Action OnClickRemoveOfficer() => () =>
        {
            if (_selectedOfficerIndex < 0 || _selectedOfficerIndex >= _officerIds.Count)
            {
                ShowError("Pick an officer in the list first.");
                return;
            }

            var officerId = _officerIds[_selectedOfficerIndex];
            ShowModal(
                $"Remove {OfficerNames[_selectedOfficerIndex]} as {OfficerTitles[_selectedOfficerIndex]}? They can no longer decide City Affairs situations.",
                () =>
                {
                    var error = CityOfficers.Remove(_cityId, GetObjectUUID(Player), officerId);
                    if (!string.IsNullOrEmpty(error))
                        ShowError(error);
                    else
                        ShowSuccess("Officer removed.");

                    _selectedOfficerIndex = -1;
                    RefreshOfficers();
                },
                null,
                "Remove",
                "Cancel");
        };

        #endregion

        #region Story request

        private void RefreshStory()
        {
            var city = DB.Get<WorldProperty>(_cityId);
            if (city == null)
            {
                ShowError("This city no longer exists.");
                return;
            }

            var request = city.StoryRequest;
            HasCurrentStory = request != null;
            CurrentStoryText = request?.Text ?? string.Empty;
            StoryStatusText = request == null
                ? "Your city has no story request. Post one to invite the DMs to run a story, such as a plot or a hint about ruins near the city."
                : $"Posted {request.PostedUtc:yyyy-MM-dd} UTC by the {request.PosterTitle}. DM status: {StoryRequestRules.DescribeStatus(request.Status)}. " +
                  "A new request replaces this one.";
            CanPostStory = CityPowerRules.RoleHoldsPower(CityPowers.GetRole(city, GetObjectUUID(Player)), CityPower.PostRequests);
        }

        public Action OnClickPostStory() => () =>
        {
            var error = StoryRequestRules.CheckPost(CityPowers.GetRole(DB.Get<WorldProperty>(_cityId), GetObjectUUID(Player)), StoryInput);
            if (!string.IsNullOrEmpty(error))
            {
                ShowError(error);
                return;
            }

            var text = StoryInput;
            ShowModal(
                $"Post this story request? It goes to the DMs and replaces your city's current request. It is up to {StoryRequestRules.MaxLength} characters.",
                () =>
                {
                    var result = CityStoryRequests.Post(_cityId, GetObjectUUID(Player), text);
                    if (string.IsNullOrEmpty(result))
                    {
                        StoryInput = string.Empty;
                        ShowSuccess("Story request posted. The DMs have it.");
                    }
                    else
                    {
                        ShowError(result);
                    }

                    RefreshStory();
                },
                null,
                "Post",
                "Cancel");
        };

        #endregion
    }
}
