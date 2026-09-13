using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MasteryService;
using SWLOR.Game.Server.Service.SkillService;
// Both SWLOR.Game.Server.Entity.Mastery (the catalog entity) and
// SWLOR.Game.Server.Service.Mastery (the static orchestration service) are in scope in
// this file. This alias pins the bare "Mastery" identifier to the service so calls like
// Mastery.GetOrCreateProfile(...) resolve unambiguously; the entity type is always
// referenced as the qualified "Entity.Mastery" instead (matching Service/Mastery.cs's
// own convention for the same collision).
using Mastery = SWLOR.Game.Server.Service.Mastery;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// Player-facing Masteries window: My Masteries / Catalog / My Requests tabs, plus a
    /// request-submission form nested inside the Catalog tab. Opened only from the
    /// Character Sheet actions rail - see CharacterSheetViewModel.OnClickMasteries.
    /// Every business-rule decision (eligibility, durations, caps) is delegated to
    /// Service.Mastery / Service.MasteryService.MasteryRules; this class only loads data
    /// for display and relays player intent (submit, reply, cancel) to that service.
    /// </summary>
    public class MasteriesViewModel: GuiViewModelBase<MasteriesViewModel, GuiPayloadBase>
    {
        // Stable placeholder element the four tab panels are swapped into via
        // ChangePartialView. Using a runtime-swapped partial (rather than four
        // BindIsVisible-toggled rows sharing the window) avoids the NUI flex-layout
        // pitfall where hidden flexible rows still reserve their share of vertical
        // space - see MasteriesDefinition.cs and CharacterSheetDefinition's tab area
        // for the same pattern.
        public const string ContentPartialElement = "masteries_tab_content";
        public const string MyMasteriesPartial = "masteries_mine";
        public const string CatalogPartial = "masteries_catalog";
        public const string RequestFormPartial = "masteries_request_form";
        public const string MyRequestsPartial = "masteries_requests";

        private const int MyMasteriesTabId = 0;
        private const int CatalogTabId = 1;
        private const int MyRequestsTabId = 2;
        private const int RequestFormTabId = 3;

        public const int MaxJustificationLength = 1000;
        public const int MaxCustomNameLength = 64;
        public const int MaxCustomDescriptionLength = 500;
        public const int MaxReplyLength = 500;

        private const int CatalogPageSize = 6;
        private const int MaxDiscordJustificationLength = 300;
        private const int MaxDiscordThreadNameLength = 100;

        private static readonly ApplicationSettings _appSettings = ApplicationSettings.Get();

        private List<Entity.Mastery> _catalogFiltered = new();
        private List<Entity.Mastery> _catalogPageRows = new();
        private int _catalogPage;

        private bool _isCustomRequest;
        private string _selectedRequestMasteryId;
        private int _requestTargetTier;

        // In-flight guard for OnClickSubmitRequest - blocks a second click landing while
        // the handler is still awaiting the Discord enqueue after the request has already
        // been persisted (which would otherwise create a duplicate Pending request).
        private bool _isSubmittingRequest;

        private List<MasteryRequest> _myRequests = new();
        private int _selectedRequestIndex = -1;

        private static readonly GuiTabGroup<MasteriesViewModel, GuiPayloadBase> Tabs =
            new GuiTabGroup<MasteriesViewModel, GuiPayloadBase>()
                .AddTab(MyMasteriesTabId, MyMasteriesPartial, model => model.LoadMyMasteries())
                .AddTab(CatalogTabId, CatalogPartial, model => model.LoadCatalogTab())
                .AddTab(MyRequestsTabId, MyRequestsPartial, model => model.LoadMyRequests())
                .AddTab(RequestFormTabId, RequestFormPartial);

        private static readonly GuiToggleGroupSync TabToggles =
            new(MyMasteriesTabId, CatalogTabId, MyRequestsTabId);

        // ---------------------------------------------------------------
        // Tabs
        // ---------------------------------------------------------------

        public int SelectedTabId
        {
            get => Get<int>();
            set => Set(value);
        }

        public int TabToggleValue
        {
            get => Get<int>();
            set
            {
                Set(value);
                TabToggles.HandleClientChange(value, SelectTab);
            }
        }

        // ---------------------------------------------------------------
        // My Masteries tab
        // ---------------------------------------------------------------

        public string TotalsText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string TrainingText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsTrainingVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public float TrainingProgress
        {
            get => Get<float>();
            set => Set(value);
        }

        public GuiBindingList<string> OwnedMasteryLabels
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        // ---------------------------------------------------------------
        // Catalog tab
        // ---------------------------------------------------------------

        public string SearchText
        {
            get => Get<string>();
            set => Set(value);
        }

        public int SelectedCategoryId
        {
            get => Get<int>();
            set => Set(value);
        }

        public GuiBindingList<string> CatalogLabels
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> CatalogRequestEnabled
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<string> CatalogRequestTooltips
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public string CatalogPageText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsCatalogPrevEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsCatalogNextEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        // ---------------------------------------------------------------
        // Request form
        // ---------------------------------------------------------------

        public string RequestMasteryLabel
        {
            get => Get<string>();
            set => Set(value);
        }

        public string RequestTierLabel
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsCustomFieldsVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string CustomName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string CustomDescription
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Justification
        {
            get => Get<string>();
            set => Set(value);
        }

        public string EligibilityText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsSubmitEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string FormStatusText
        {
            get => Get<string>();
            set => Set(value);
        }

        // ---------------------------------------------------------------
        // My Requests tab
        // ---------------------------------------------------------------

        public GuiBindingList<string> RequestLabels
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> RequestToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> RequestColors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        public string RequestDetailText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string CommentsText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ReplyText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsReplyEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsCancelEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        // ---------------------------------------------------------------
        // Initialize
        // ---------------------------------------------------------------

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            var playerId = GetObjectUUID(Player);

            // Lazy-evaluate the training queue every time the window is opened, per
            // MASTERY_SPEC.md's Processing section - there are no schedulers, so this
            // (plus the login hook in MasteryNotifications) is what completes tiers.
            Mastery.EvaluateTrainingQueue(playerId, DateTime.UtcNow);

            // Deliver any completion notices queued since this character was last
            // notified - including ones added by a DM evaluating this profile from the
            // examine window while the character was already online in this session but
            // hadn't yet reopened Masteries. EvaluateTrainingQueue is the only place a
            // notice is ever appended. Notices are only acknowledged (cleared) once
            // they've actually been sent below, so a UI exception in between can never
            // silently lose them.
            var pendingNotices = Mastery.PeekPendingCompletionNotices(playerId);
            foreach (var notice in pendingNotices)
            {
                SendMessageToPC(Player, ColorToken.Green(notice));
            }

            if (pendingNotices.Count > 0)
            {
                Mastery.AcknowledgeCompletionNotices(playerId);
            }

            SearchText = string.Empty;
            SelectedCategoryId = -1;
            Justification = string.Empty;
            ReplyText = string.Empty;
            CustomName = string.Empty;
            CustomDescription = string.Empty;
            FormStatusText = string.Empty;
            TabToggleValue = MyMasteriesTabId;

            WatchOnClient(model => model.TabToggleValue);
            WatchOnClient(model => model.SearchText);
            WatchOnClient(model => model.SelectedCategoryId);
            WatchOnClient(model => model.Justification);
            WatchOnClient(model => model.ReplyText);
            WatchOnClient(model => model.CustomName);
            WatchOnClient(model => model.CustomDescription);

            SelectTab(MyMasteriesTabId);
        }

        protected override void OnClientPropertyUpdated(string propertyName)
        {
            if (SelectedTabId != CatalogTabId)
                return;

            if (propertyName == nameof(SearchText) || propertyName == nameof(SelectedCategoryId))
            {
                _catalogPage = 0;
                LoadCatalog();
            }
        }

        protected override void OnModalClosedRestore() =>
            Tabs.Select(this, ContentPartialElement, SelectedTabId);

        // ---------------------------------------------------------------
        // Tab switching
        // ---------------------------------------------------------------

        private void SelectTab(int tabId)
        {
            SelectedTabId = tabId;
            TabToggles.SyncTo(tabId, value => TabToggleValue = value);
            Tabs.Select(this, ContentPartialElement, tabId);
        }

        private void LoadCatalogTab()
        {
            _catalogPage = 0;
            LoadCatalog();
        }

        // ---------------------------------------------------------------
        // My Masteries
        // ---------------------------------------------------------------

        private void LoadMyMasteries()
        {
            var playerId = GetObjectUUID(Player);
            var profile = Mastery.GetOrCreateProfile(playerId);
            var ownedCatalog = Mastery.GetOwnedMasteryCatalog(profile);

            var earnedLevels = MasteryRules.GetEarnedLevelTotal(profile);
            var rareCount = ownedCatalog.Values.Count(m => m.Rarity == MasteryRarityType.Rare);

            TotalsText =
                $"Mastery Levels: {earnedLevels} / {MasteryRules.MaxTotalLevels}\n" +
                $"Rare Masteries: {rareCount} / 1\n" +
                $"Quick Slots: {profile.QuickSlotsAvailable} available";

            if (profile.TrainingQueue.Count > 0)
            {
                var active = profile.TrainingQueue[0];
                var activeMastery = Mastery.GetMastery(active.MasteryId);
                var activeName = string.IsNullOrWhiteSpace(activeMastery?.Name) ? "Unknown Mastery" : activeMastery.Name;
                var totalDays = Math.Max(1, active.DurationDays - active.ReductionDays);
                var elapsedDays = Math.Max(0.0, (DateTime.UtcNow - active.StartDate).TotalDays);
                var finish = active.StartDate.AddDays(totalDays);

                var text = $"{activeName} - Tier {active.TargetTier}\n" +
                           $"{(int)Math.Min(elapsedDays, totalDays)} of {totalDays} days - completes {finish:yyyy-MM-dd}";

                if (profile.TrainingQueue.Count > 1)
                {
                    var next = profile.TrainingQueue[1];
                    var nextMastery = Mastery.GetMastery(next.MasteryId);
                    var nextName = string.IsNullOrWhiteSpace(nextMastery?.Name) ? "Unknown Mastery" : nextMastery.Name;
                    text += $"\nQueued next: {nextName} - Tier {next.TargetTier} ({Math.Max(0, next.DurationDays - next.ReductionDays)} days)";
                }

                TrainingText = text;
                TrainingProgress = (float)Math.Min(1.0, elapsedDays / totalDays);
                IsTrainingVisible = true;
            }
            else
            {
                TrainingText = "No training currently in progress.";
                TrainingProgress = 0f;
                IsTrainingVisible = false;
            }

            var labels = new GuiBindingList<string>();
            foreach (var pair in profile.Masteries.OrderBy(kvp => Mastery.GetMastery(kvp.Key)?.Name ?? string.Empty))
            {
                var mastery = Mastery.GetMastery(pair.Key);
                var name = mastery?.Name ?? "Unknown Mastery";
                var category = mastery != null
                    ? mastery.Category.GetAttribute<MasteryCategoryType, MasteryCategoryAttribute>().Label
                    : string.Empty;
                var rarity = mastery?.Rarity.ToString() ?? string.Empty;
                var isTraining = profile.TrainingQueue.Any(t => t.MasteryId == pair.Key);
                var trainingTag = isTraining ? " [training]" : string.Empty;

                labels.Add($"{name} ({category}) - Tier {ToRoman(pair.Value.Tier)} - {rarity}{trainingTag}");
            }

            OwnedMasteryLabels = labels;
        }

        private static string ToRoman(int tier) => tier switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            5 => "V",
            _ => tier.ToString()
        };

        // ---------------------------------------------------------------
        // Catalog
        // ---------------------------------------------------------------

        private void LoadCatalog()
        {
            var search = (SearchText ?? string.Empty).Trim().ToLower();

            _catalogFiltered = Mastery.GetAllMasteries()
                .Where(m => m.IsActive)
                .Where(m => SelectedCategoryId == -1 || (int)m.Category == SelectedCategoryId)
                .Where(m => search.Length == 0 || m.Name.ToLower().Contains(search))
                .OrderBy(m => m.Name)
                .ToList();

            var totalPages = Math.Max(1, (int)Math.Ceiling(_catalogFiltered.Count / (double)CatalogPageSize));
            if (_catalogPage >= totalPages) _catalogPage = totalPages - 1;
            if (_catalogPage < 0) _catalogPage = 0;

            _catalogPageRows = _catalogFiltered
                .Skip(_catalogPage * CatalogPageSize)
                .Take(CatalogPageSize)
                .ToList();

            var playerId = GetObjectUUID(Player);
            var profile = Mastery.GetOrCreateProfile(playerId);

            var labels = new GuiBindingList<string>();
            var enabledList = new GuiBindingList<bool>();
            var tooltips = new GuiBindingList<string>();

            foreach (var mastery in _catalogPageRows)
            {
                var currentTier = profile.Masteries.TryGetValue(mastery.Id, out var level) ? level.Tier : 0;
                var category = mastery.Category.GetAttribute<MasteryCategoryType, MasteryCategoryAttribute>().Label;
                var description = mastery.Description ?? string.Empty;
                if (description.Length > 90)
                    description = description.Substring(0, 90) + "...";

                labels.Add($"{mastery.Name} [{mastery.Rarity}] - {category} - Tier {currentTier} - {description}");

                if (mastery.Rarity == MasteryRarityType.OffLimit)
                {
                    enabledList.Add(false);
                    tooltips.Add("This mastery is off-limits and cannot be requested.");
                }
                else if (currentTier >= 5)
                {
                    enabledList.Add(false);
                    tooltips.Add("This character has already reached the maximum tier in this mastery.");
                }
                else
                {
                    enabledList.Add(true);
                    tooltips.Add(string.Empty);
                }
            }

            CatalogLabels = labels;
            CatalogRequestEnabled = enabledList;
            CatalogRequestTooltips = tooltips;
            CatalogPageText = $"Page {_catalogPage + 1} of {totalPages}";
            IsCatalogPrevEnabled = _catalogPage > 0;
            IsCatalogNextEnabled = _catalogPage < totalPages - 1;
        }

        public Action OnClickCatalogPrevPage() => () =>
        {
            if (_catalogPage <= 0) return;
            _catalogPage--;
            LoadCatalog();
        };

        public Action OnClickCatalogNextPage() => () =>
        {
            _catalogPage++;
            LoadCatalog();
        };

        public Action OnClickRequestCatalogRow() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _catalogPageRows.Count) return;

            ShowRequestForm(_catalogPageRows[index].Id, false);
        };

        public Action OnClickRequestUnlisted() => () => ShowRequestForm(null, true);

        public Action OnClickBackToCatalog() => () => SelectTab(CatalogTabId);

        // ---------------------------------------------------------------
        // Request form
        // ---------------------------------------------------------------

        private void ShowRequestForm(string masteryId, bool isCustom)
        {
            _selectedRequestMasteryId = masteryId;
            _isCustomRequest = isCustom;

            IsCustomFieldsVisible = isCustom;
            FormStatusText = string.Empty;
            Justification = string.Empty;

            var playerId = GetObjectUUID(Player);
            var profile = Mastery.GetOrCreateProfile(playerId);

            if (isCustom)
            {
                CustomName = string.Empty;
                CustomDescription = string.Empty;
                _requestTargetTier = 1;
                RequestMasteryLabel = "New Unlisted Mastery";
                RequestTierLabel = "Tier 1";
            }
            else
            {
                var mastery = Mastery.GetMastery(masteryId);
                var currentTier = mastery != null && profile.Masteries.TryGetValue(masteryId, out var level) ? level.Tier : 0;
                _requestTargetTier = currentTier + 1;
                RequestMasteryLabel = mastery?.Name ?? "Unknown Mastery";
                RequestTierLabel = $"Tier {_requestTargetTier}";
            }

            RefreshEligibility();
            SelectTab(RequestFormTabId);
        }

        /// <summary>
        /// Builds the request form's live eligibility panel. For a Custom (unlisted)
        /// request there is no catalog row yet, so a transient, never-persisted
        /// Entity.Mastery stand-in is used purely to drive the shared check-building
        /// logic in Service.Mastery/MasteryRules - it is never written to the database.
        /// </summary>
        private void RefreshEligibility()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var characterCreatedDate = dbPlayer?.DateCreated ?? DateTime.UtcNow;

            Entity.Mastery mastery;
            int? skillRank = null;

            if (_isCustomRequest)
            {
                mastery = new Entity.Mastery
                {
                    Name = string.IsNullOrWhiteSpace(CustomName) ? "New Unlisted Mastery" : CustomName,
                    Rarity = MasteryRarityType.Standard
                };
            }
            else
            {
                mastery = Mastery.GetMastery(_selectedRequestMasteryId);
                if (mastery == null)
                {
                    EligibilityText = "This mastery is no longer available.";
                    IsSubmitEnabled = false;
                    return;
                }

                if (mastery.AssociatedSkill != null)
                    skillRank = Skill.GetCreatureSkillRank(Player, mastery.AssociatedSkill.Value);
            }

            var checks = Mastery.BuildEligibilityChecks(playerId, mastery, _requestTargetTier, characterCreatedDate, DateTime.UtcNow, skillRank);

            var sb = new StringBuilder();
            var hasBlocking = false;

            foreach (var (passed, label) in checks)
            {
                sb.Append(passed ? "[OK] " : "[!!] ").Append(label).Append('\n');

                if (!passed && mastery.Rarity == MasteryRarityType.OffLimit)
                    hasBlocking = true;
            }

            EligibilityText = sb.ToString().TrimEnd('\n');
            IsSubmitEnabled = !hasBlocking;
        }

        public Action OnClickSubmitRequest() => async () =>
        {
            // Guard against a second click landing while this async handler is still
            // awaiting the Discord enqueue below - Mastery.SubmitRequest persists a
            // request immediately, so without this a double-click before the first call
            // returns could create a second Pending request (Mastery.SubmitRequest itself
            // also rejects an exact duplicate as defense-in-depth for this same race).
            if (_isSubmittingRequest)
                return;

            var playerId = GetObjectUUID(Player);
            var characterName = GetName(Player);

            if (string.IsNullOrWhiteSpace(Justification))
            {
                FormStatusText = "Please provide an RP justification before submitting.";
                return;
            }

            Entity.Mastery mastery = null;

            if (_isCustomRequest)
            {
                if (string.IsNullOrWhiteSpace(CustomName))
                {
                    FormStatusText = "Please enter a name for the unlisted mastery.";
                    return;
                }
            }
            else
            {
                mastery = Mastery.GetMastery(_selectedRequestMasteryId);
                if (mastery == null)
                {
                    FormStatusText = "This mastery is no longer available.";
                    SelectTab(CatalogTabId);
                    return;
                }
            }

            _isSubmittingRequest = true;
            IsSubmitEnabled = false;

            try
            {
                var profile = Mastery.GetOrCreateProfile(playerId);
                var currentTier = !_isCustomRequest && profile.Masteries.TryGetValue(mastery.Id, out var level) ? level.Tier : 0;
                var requestType = _isCustomRequest
                    ? MasteryRequestType.Custom
                    : currentTier == 0 ? MasteryRequestType.NewMastery : MasteryRequestType.RankUp;

                var request = Mastery.SubmitRequest(
                    playerId,
                    characterName,
                    requestType,
                    _isCustomRequest ? null : mastery.Id,
                    _isCustomRequest ? CustomName : null,
                    _isCustomRequest ? CustomDescription : null,
                    _requestTargetTier,
                    Justification);

                bool discordEnqueued;
                try
                {
                    discordEnqueued = await SendMasteryDiscordNotification(request, mastery, playerId, characterName);
                }
                catch (Exception ex)
                {
                    Log.Write(LogGroup.Mastery, $"Failed to enqueue Discord notification for mastery request '{request.Id}'. {ex}");
                    discordEnqueued = false;
                }

                // The request itself is already persisted at this point, so a failed Discord
                // notification must never be treated as a failed submission - staff can still
                // review the request in-game via /masteryreview. Just surface a heads-up so
                // the player knows staff may not have been pinged immediately.
                if (!discordEnqueued)
                {
                    Log.Write(LogGroup.Mastery, $"Discord notification failed to enqueue for mastery request '{request.Id}' (player '{playerId}').");
                    SendMessageToPC(Player, ColorToken.Green("Mastery request submitted! Staff will review it soon."));
                    SendMessageToPC(Player, ColorToken.Orange("(Staff Discord notification could not be sent - staff will still see this in /masteryreview.)"));
                }
                else
                {
                    SendMessageToPC(Player, ColorToken.Green("Mastery request submitted! Staff will review it soon."));
                }

                SelectTab(MyRequestsTabId);
            }
            finally
            {
                _isSubmittingRequest = false;
            }
        };

        private Task<bool> SendMasteryDiscordNotification(MasteryRequest request, Entity.Mastery mastery, string playerId, string characterName)
        {
            var webhookUrl = _appSettings.MasteryStaffWebhookUrl;
            if (string.IsNullOrWhiteSpace(webhookUrl))
                return Task.FromResult(true);

            var masteryName = _isCustomRequest ? $"{CustomName} (Unlisted)" : mastery?.Name ?? "Unknown Mastery";
            var rarity = _isCustomRequest ? "Unknown" : mastery?.Rarity.ToString() ?? "Unknown";

            var justification = Justification ?? string.Empty;
            if (justification.Length > MaxDiscordJustificationLength)
                justification = justification.Substring(0, MaxDiscordJustificationLength) + "...";

            var dbPlayer = DB.Get<Player>(playerId);
            var characterCreatedDate = dbPlayer?.DateCreated ?? DateTime.UtcNow;

            int? skillRank = !_isCustomRequest && mastery?.AssociatedSkill != null
                ? Skill.GetCreatureSkillRank(Player, mastery.AssociatedSkill.Value)
                : null;

            var checkMastery = _isCustomRequest
                ? new Entity.Mastery { Name = CustomName, Rarity = MasteryRarityType.Standard }
                : mastery;

            var checksSummary = string.Empty;
            if (checkMastery != null)
            {
                var checks = Mastery.BuildEligibilityChecks(playerId, checkMastery, request.TargetTier, characterCreatedDate, DateTime.UtcNow, skillRank);
                checksSummary = string.Join(" | ", checks.Select(c => (c.Passed ? "OK " : "FAIL ") + c.Label));
            }

            var title = $"New Mastery Request - {characterName}";
            var body =
                $"**{masteryName} - Tier {request.TargetTier}** ({rarity})\n" +
                $"\"{justification}\"\n\n" +
                $"**Rules Check**\n{checksSummary}\n\n" +
                "**Review**\nIn-game: /masteryreview";

            var threadName = TruncateForDiscordThread($"{characterName}-{masteryName}-t{request.TargetTier}");

            return BackgroundJob.EnqueueDiscordWebhook(
                webhookUrl,
                characterName,
                body,
                15158332,
                title,
                createThread: true,
                threadName: threadName);
        }

        private static string TruncateForDiscordThread(string name)
        {
            return name.Length <= MaxDiscordThreadNameLength ? name : name.Substring(0, MaxDiscordThreadNameLength);
        }

        // ---------------------------------------------------------------
        // My Requests
        // ---------------------------------------------------------------

        private void LoadMyRequests()
        {
            var playerId = GetObjectUUID(Player);
            _myRequests = Mastery.GetPlayerRequests(playerId);

            var labels = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();
            var colors = new GuiBindingList<GuiColor>();

            foreach (var request in _myRequests)
            {
                var name = request.Type == MasteryRequestType.Custom
                    ? request.CustomName
                    : Mastery.GetMastery(request.MasteryId)?.Name ?? "Unknown Mastery";

                labels.Add($"{name} - T{request.TargetTier} ({request.DateCreated:yyyy-MM-dd}) [{request.Status}]");
                toggles.Add(false);
                colors.Add(GetStatusColor(request.Status));
            }

            RequestLabels = labels;
            RequestToggles = toggles;
            RequestColors = colors;
            _selectedRequestIndex = -1;

            LoadRequestDetail();
        }

        private static GuiColor GetStatusColor(MasteryRequestStatus status) => status switch
        {
            MasteryRequestStatus.Approved => GuiColor.Green,
            MasteryRequestStatus.Denied => GuiColor.Red,
            MasteryRequestStatus.Cancelled => GuiColor.Grey,
            _ => GuiColor.White
        };

        private void LoadRequestDetail()
        {
            if (_selectedRequestIndex < 0 || _selectedRequestIndex >= _myRequests.Count)
            {
                RequestDetailText = "Select one of your requests to view its details.";
                CommentsText = string.Empty;
                IsCancelEnabled = false;
                IsReplyEnabled = false;
                return;
            }

            var request = _myRequests[_selectedRequestIndex];
            var name = request.Type == MasteryRequestType.Custom
                ? request.CustomName
                : Mastery.GetMastery(request.MasteryId)?.Name ?? "Unknown Mastery";

            var sb = new StringBuilder();
            sb.Append($"{name} - Tier {request.TargetTier} - {request.Status}\n");
            sb.Append($"Submitted {request.DateCreated:yyyy-MM-dd}\n");

            if (request.DateReviewed != null)
                sb.Append($"Reviewed {request.DateReviewed:yyyy-MM-dd}\n");

            if (!string.IsNullOrWhiteSpace(request.ReviewFeedback))
                sb.Append($"Feedback: {request.ReviewFeedback}\n");

            RequestDetailText = sb.ToString().TrimEnd('\n');

            var commentsSb = new StringBuilder();
            foreach (var comment in request.Comments)
            {
                var author = comment.IsStaff ? $"{comment.AuthorName} (Staff)" : comment.AuthorName;
                commentsSb.Append($"{author} - {comment.Date:yyyy-MM-dd}: {comment.Text}\n");
            }

            CommentsText = commentsSb.Length > 0 ? commentsSb.ToString().TrimEnd('\n') : "No comments yet.";

            IsCancelEnabled = request.Status == MasteryRequestStatus.Pending || request.Status == MasteryRequestStatus.InReview;
            IsReplyEnabled = true;
            ReplyText = string.Empty;
        }

        public Action OnClickSelectRequest() => () =>
        {
            if (_selectedRequestIndex > -1 && _selectedRequestIndex < RequestToggles.Count)
                RequestToggles[_selectedRequestIndex] = false;

            var index = NuiGetEventArrayIndex();
            _selectedRequestIndex = index;

            if (index >= 0 && index < RequestToggles.Count)
                RequestToggles[index] = true;

            LoadRequestDetail();
        };

        public Action OnClickSendReply() => () =>
        {
            if (_selectedRequestIndex < 0 || _selectedRequestIndex >= _myRequests.Count) return;
            if (string.IsNullOrWhiteSpace(ReplyText)) return;

            var request = _myRequests[_selectedRequestIndex];
            var selectedRequestId = request.Id;

            Mastery.AddComment(request.Id, GetName(Player), false, ReplyText, DateTime.UtcNow);

            LoadMyRequests();

            _selectedRequestIndex = _myRequests.FindIndex(r => r.Id == selectedRequestId);
            if (_selectedRequestIndex >= 0 && _selectedRequestIndex < RequestToggles.Count)
                RequestToggles[_selectedRequestIndex] = true;

            LoadRequestDetail();
        };

        public Action OnClickCancelRequest() => () =>
        {
            if (_selectedRequestIndex < 0 || _selectedRequestIndex >= _myRequests.Count) return;

            var request = _myRequests[_selectedRequestIndex];
            var name = request.Type == MasteryRequestType.Custom
                ? request.CustomName
                : Mastery.GetMastery(request.MasteryId)?.Name ?? "this mastery";

            ShowModal($"Cancel your request for {name}?", () =>
            {
                var playerId = GetObjectUUID(Player);
                Mastery.CancelRequest(request.Id, playerId);
                LoadMyRequests();
            });
        };
    }
}
