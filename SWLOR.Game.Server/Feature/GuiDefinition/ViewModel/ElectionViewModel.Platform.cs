using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.EdictService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// The platform step of the election window (Player City Revamp, "Election changes" items 1 to 3): a candidate picks
    /// Edicts up to the city's slot count, edits the statement, and reads the platform's cost beside the city's upkeep
    /// and treasury, with a warning when their City Management rank would lower the city. Enter Race opens it, and
    /// Edit Platform reopens it until registration closes.
    /// </summary>
    public partial class ElectionViewModel
    {
        private const string FocusPrompt = "Select an Edict to add it to your platform and read what it does. Select it again to take it out.";

        // Row i of the Edict list shows _platformEdictTypes[i]; row i of the city picker shows _platformTargetCityIds[i].
        private readonly List<EdictType> _platformEdictTypes = new();
        private readonly List<string> _platformTargetCityIds = new();

        // The Edicts chosen so far, each with the city it names (empty for an Edict that names none).
        private readonly Dictionary<EdictType, string> _draftEdicts = new();
        private ElectionPlatformContext _platformContext;
        private string _platformCityId = string.Empty;
        private List<WorldProperty> _nameableCities;
        private EdictType _focusedEdict;
        private bool _isPlatformStepOpen;

        public string PlatformInstructions
        {
            get => Get<string>();
            set => Set(value);
        }

        /// <summary>How many Edicts are chosen, out of how many the city's level allows.</summary>
        public string PlatformSlotsText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> PlatformEdictNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> PlatformEdictTooltips
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        /// <summary>True for an Edict that is in the platform.</summary>
        public GuiBindingList<bool> PlatformEdictToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        /// <summary>False for an Edict the city's level cannot hold.</summary>
        public GuiBindingList<bool> PlatformEdictEnables
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        /// <summary>What the last Edict selected does, or a prompt to select one.</summary>
        public string FocusedEdictText
        {
            get => Get<string>();
            set => Set(value);
        }

        /// <summary>The city picker for an Alliance or Embargo that is in the platform.</summary>
        public bool IsPlatformTargetPickerVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string PlatformTargetPrompt
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> PlatformTargetNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> PlatformTargetToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        /// <summary>Why the last choice was refused, or empty.</summary>
        public string PlatformMessage
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsPlatformMessageVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        /// <summary>The platform's weekly cost beside the city's upkeep, treasury and weekly tax income.</summary>
        public string CostPreviewText
        {
            get => Get<string>();
            set => Set(value);
        }

        /// <summary>The warning that the candidate's City Management rank would lower the city's level and Edict slots.</summary>
        public string CityLevelWarningText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsCityLevelWarningVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        /// <summary>Enter Race for a citizen who is not yet a candidate, Save Platform for one who is.</summary>
        public string PlatformSubmitText
        {
            get => Get<string>();
            set => Set(value);
        }

        private void InitializePlatformStep()
        {
            PlatformInstructions = string.Empty;
            PlatformSlotsText = string.Empty;
            PlatformEdictNames = new GuiBindingList<string>();
            PlatformEdictTooltips = new GuiBindingList<string>();
            PlatformEdictToggles = new GuiBindingList<bool>();
            PlatformEdictEnables = new GuiBindingList<bool>();
            FocusedEdictText = FocusPrompt;
            IsPlatformTargetPickerVisible = false;
            PlatformTargetPrompt = string.Empty;
            PlatformTargetNames = new GuiBindingList<string>();
            PlatformTargetToggles = new GuiBindingList<bool>();
            PlatformMessage = string.Empty;
            IsPlatformMessageVisible = false;
            CostPreviewText = string.Empty;
            CityLevelWarningText = string.Empty;
            IsCityLevelWarningVisible = false;
            PlatformSubmitText = "Enter Race";
        }

        /// <summary>
        /// Opens the platform step: from the candidate's saved platform, or empty for a citizen who has not entered the race.
        /// </summary>
        public Action OpenPlatform() => () =>
        {
            var playerId = GetObjectUUID(Player);
            var election = GetElection();
            if (election == null || election.Id != _electionId)
            {
                SendMessageToPC(Player, "This election has ended.");
                LoadData(true);
                return;
            }

            var error = ElectionBallot.GetRegistrationAccessError(election, playerId);
            if (!string.IsNullOrEmpty(error))
            {
                SendMessageToPC(Player, error);
                LoadData(true);
                return;
            }

            var city = DB.Get<WorldProperty>(election.PropertyId);
            var isCandidate = election.CandidatePlayerIds.Contains(playerId);
            ElectionPlatform saved = null;
            if (isCandidate)
                election.Platforms?.TryGetValue(playerId, out saved);

            _platformCityId = city.Id;
            _nameableCities = null;
            _platformContext = ElectionPlatforms.GetContext(city, DB.Get<Entity.Player>(playerId));
            _draftEdicts.Clear();
            foreach (var edict in saved?.Edicts ?? new List<ElectionPlatformEdict>())
            {
                _draftEdicts[edict.Type] = edict.TargetCityId ?? string.Empty;
            }

            _focusedEdict = EdictType.Invalid;
            FocusedEdictText = FocusPrompt;
            PlatformMessage = string.Empty;
            IsPlatformMessageVisible = false;
            PlatformSubmitText = isCandidate ? "Save Platform" : "Enter Race";
            StatementInput = saved?.Statement ?? string.Empty;
            _isPlatformStepOpen = true;
            RefreshPlatformStep();

            ChangePartialView(MainContentElement, PlatformPartial);

            // The client keeps what a text box held before the swap, so the statement is pushed once the box exists.
            PushStatementInput();
        };

        /// <summary>Returns to the candidate list without saving.</summary>
        public Action ClosePlatform() => () => ClosePlatformStep(true);

        private void ClosePlatformStep(bool keepStatementInput)
        {
            _isPlatformStepOpen = false;
            LoadData(keepStatementInput);
            ChangePartialView(MainContentElement, MainContentPartial);
        }

        /// <summary>Sends the statement to the client again, after a layout swap has rebuilt the box that shows it.</summary>
        private void PushStatementInput()
        {
            var statement = StatementInput;
            StatementInput = statement;
        }

        protected override void OnModalClosedRestore()
        {
            // The modal swap rebuilds the platform step's statement box, so its text is pushed again.
            if (_isPlatformStepOpen)
                PushStatementInput();
        }

        /// <summary>Adds the selected Edict to the platform, or takes it out.</summary>
        public Action TogglePlatformEdict() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _platformEdictTypes.Count)
                return;

            var type = _platformEdictTypes[index];
            var level = _platformContext.CityLevel;
            _focusedEdict = type;
            SetPlatformMessage(string.Empty);

            if (!_draftEdicts.Remove(type))
            {
                if (!EdictRules.MeetsMinimumLevel(type, level))
                    SetPlatformMessage(EdictRules.DescribeRefusal(EdictRefusal.LevelTooLow, type));
                else if (_draftEdicts.Count >= EdictRules.GetSlots(level))
                    SetPlatformMessage(ElectionPlatformRules.DescribeRefusal(EdictRefusal.NoFreeSlot, type, level));
                else
                    _draftEdicts[type] = string.Empty;
            }

            RefreshPlatformStep();
        };

        /// <summary>Names the selected city for the Alliance or Embargo being edited.</summary>
        public Action SelectPlatformTarget() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _platformTargetCityIds.Count || !_draftEdicts.ContainsKey(_focusedEdict))
                return;

            var cityId = _platformTargetCityIds[index];
            var held = _draftEdicts
                .Where(x => x.Key != _focusedEdict)
                .ToDictionary(x => x.Key, x => new CityEdict { TargetCityId = x.Value });
            if (EdictRules.IsExclusivePair(_focusedEdict, cityId, held))
            {
                SetPlatformMessage("An Alliance and an Embargo cannot name the same city.");
            }
            else
            {
                _draftEdicts[_focusedEdict] = cityId;
                SetPlatformMessage(string.Empty);
            }

            RefreshPlatformStep();
        };

        /// <summary>
        /// Enters the race with the platform (after a confirmation) or saves the platform of a candidate already in it.
        /// </summary>
        public Action SubmitPlatform() => () =>
        {
            var playerId = GetObjectUUID(Player);
            var election = GetElection();
            if (election == null || election.Id != _electionId)
            {
                SendMessageToPC(Player, "This election has ended.");
                ClosePlatformStep(false);
                return;
            }

            var edicts = BuildDraftEdicts();
            var error = ElectionBallot.GetRegistrationAccessError(election, playerId);
            if (string.IsNullOrEmpty(error))
                error = ElectionBallot.GetPlatformError(election, edicts);
            if (!string.IsNullOrEmpty(error))
            {
                SetPlatformMessage(error);
                return;
            }

            if (election.CandidatePlayerIds.Contains(playerId))
            {
                error = ElectionBallot.UpdatePlatform(_electionId, playerId, StatementInput, edicts);
                SendMessageToPC(Player, string.IsNullOrEmpty(error) ? "Your platform has been saved." : error);
                if (string.IsNullOrEmpty(error))
                    ClosePlatformStep(false);
                else
                    SetPlatformMessage(error);

                return;
            }

            var dbCity = DB.Get<WorldProperty>(election.PropertyId);
            ShowModal($"Are you sure you want to enter the race? If you win the vote, you will immediately become the mayor of {dbCity.CustomName}. This includes being responsible for paying maintenance and upkeep fees. " +
                      $"Your platform promises {edicts.Count} Edict(s) costing {ElectionPlatformRules.GetWeeklyCost(edicts):N0} cr a week. If you win outright, it replaces every Edict the city holds. " +
                      "Your platform is saved with your entry, and you can edit it until registration closes. Will you enter the race?",
                () =>
                {
                    var registerError = ElectionBallot.RegisterCandidate(_electionId, playerId, StatementInput, edicts);
                    SendMessageToPC(Player, string.IsNullOrEmpty(registerError) ? "You have entered the race!" : registerError);
                    if (string.IsNullOrEmpty(registerError))
                        ClosePlatformStep(false);
                    else
                        SetPlatformMessage(registerError);
                });
        };

        private List<ElectionPlatformEdict> BuildDraftEdicts()
        {
            return ElectionPlatformRules.Normalize(_draftEdicts.Select(x => new ElectionPlatformEdict
            {
                Type = x.Key,
                TargetCityId = x.Value
            }));
        }

        private void SetPlatformMessage(string message)
        {
            PlatformMessage = message;
            IsPlatformMessageVisible = !string.IsNullOrEmpty(message);
        }

        /// <summary>
        /// Rebuilds the Edict list, the city picker, the cost preview and the warning from the Edicts chosen so far.
        /// </summary>
        private void RefreshPlatformStep()
        {
            var context = _platformContext;
            var level = context.CityLevel;
            var slots = EdictRules.GetSlots(level);

            var names = new GuiBindingList<string>();
            var tooltips = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();
            var enables = new GuiBindingList<bool>();
            _platformEdictTypes.Clear();

            foreach (var detail in EdictRules.GetOfferedEdicts(context.IsWildSpaceStation))
            {
                var isChosen = _draftEdicts.TryGetValue(detail.Type, out var targetId);
                var meetsLevel = EdictRules.MeetsMinimumLevel(detail.Type, level);

                var text = $"{detail.Name} - {detail.WeeklyCost:N0} cr a week";
                if (isChosen && detail.NamesTargetCity)
                {
                    text += string.IsNullOrWhiteSpace(targetId)
                        ? " - choose a city"
                        : $" - naming {DB.Get<WorldProperty>(targetId)?.CustomName ?? "a city that is gone"}";
                }

                if (!meetsLevel)
                    text += $" (needs level {detail.MinimumCityLevel})";

                _platformEdictTypes.Add(detail.Type);
                names.Add(text);
                tooltips.Add(detail.Description);
                toggles.Add(isChosen);
                enables.Add(meetsLevel || isChosen);
            }

            PlatformEdictNames = names;
            PlatformEdictTooltips = tooltips;
            PlatformEdictToggles = toggles;
            PlatformEdictEnables = enables;

            PlatformInstructions =
                $"Choose the Edicts you promise to enact if you win outright, up to the {slots} slot(s) a city of level {level} ({Property.GetCityLevelName(level)}) has. " +
                "Running with none is allowed. Your platform replaces every Edict the city holds, and repealing one of yours later breaks a promise on your public record. " +
                "You can edit it until registration closes.";
            PlatformSlotsText = $"Edicts chosen: {_draftEdicts.Count} of {slots}";

            var focused = EdictCatalog.Get(_focusedEdict);
            FocusedEdictText = focused == null
                ? FocusPrompt
                : $"{focused.Name}: {focused.Description}\nCosts {focused.WeeklyCost:N0} cr a week. Needs city level {focused.MinimumCityLevel}.";

            RefreshPlatformTargets(focused);

            var edicts = BuildDraftEdicts();
            CostPreviewText = ElectionPlatformRules.BuildCostPreview(context, edicts);
            CityLevelWarningText = ElectionPlatformRules.BuildCityLevelWarning(context, edicts.Count, Property.GetCityLevelName);
            IsCityLevelWarningVisible = CityLevelWarningText.Length > 0;
        }

        /// <summary>Lists the cities an Alliance or Embargo can name while one of them is the Edict being edited.</summary>
        private void RefreshPlatformTargets(EdictDetail focused)
        {
            var names = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();
            _platformTargetCityIds.Clear();

            var show = focused != null && focused.NamesTargetCity && _draftEdicts.ContainsKey(focused.Type);
            if (show)
            {
                var chosenId = _draftEdicts[focused.Type];
                _nameableCities ??= ElectionPlatforms.GetNameableCities(_platformCityId);
                foreach (var other in _nameableCities)
                {
                    _platformTargetCityIds.Add(other.Id);
                    names.Add(other.CustomName);
                    toggles.Add(other.Id == chosenId);
                }
            }

            IsPlatformTargetPickerVisible = show;
            PlatformTargetPrompt = !show
                ? string.Empty
                : names.Count == 0
                    ? $"No other city exists for the {focused.Name} Edict to name."
                    : $"City for the {focused.Name} Edict";
            PlatformTargetNames = names;
            PlatformTargetToggles = toggles;
        }
    }
}
