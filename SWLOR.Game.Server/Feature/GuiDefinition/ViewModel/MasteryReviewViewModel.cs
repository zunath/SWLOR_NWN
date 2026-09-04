using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.MasteryService;
using SWLOR.Game.Server.Service.SkillService;
// Both SWLOR.Game.Server.Entity.Mastery (the catalog entity) and
// SWLOR.Game.Server.Service.Mastery (the static orchestration service) are in scope in
// this file. This alias pins the bare "Mastery" identifier to the service so calls like
// Mastery.GetOrCreateProfile(...) resolve unambiguously - matching Service/Mastery.cs's
// and MasteriesViewModel.cs's own convention for the same collision.
using Mastery = SWLOR.Game.Server.Service.Mastery;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// DM/Admin staff window: the mastery review queue (/masteryreview) plus a catalog
    /// management screen folded in as a second tab. Every business-rule decision
    /// (eligibility, durations, retrain credits) is delegated to Service.Mastery /
    /// Service.MasteryService.MasteryRules - the same engine the player's request form
    /// used - so staff and players can never see disagreeing rule checks. Every mutation
    /// (approve/deny/grant/etc) goes through Service.Mastery so audit entries are appended
    /// consistently; this class only loads data for display and relays staff intent.
    /// </summary>
    public class MasteryReviewViewModel: GuiViewModelBase<MasteryReviewViewModel, GuiPayloadBase>
    {
        // Stable placeholder element the two tab panels are swapped into via
        // ChangePartialView - see MasteriesViewModel/CharacterSheetViewModel for the same
        // runtime-swapped-partial pattern (avoids the NUI flex-layout pitfall where hidden
        // flexible rows still reserve their share of vertical space).
        public const string ContentPartialElement = "mastery_review_content";
        public const string ReviewQueuePartial = "mastery_review_queue";
        public const string CatalogManagePartial = "mastery_review_catalog";

        private const int ReviewQueueTabId = 0;
        private const int CatalogManageTabId = 1;
        private const int PageSize = 8;
        private const int CatalogPageSize = 10;

        private List<MasteryRequest> _rows = new();
        private int _selectedIndex = -1;
        private int _page;

        private List<Entity.Mastery> _catalogFiltered = new();
        private List<Entity.Mastery> _catalogPageRows = new();
        private int _catalogPage;
        private int _catalogSelectedIndex = -1;

        /// <summary>
        /// True while the catalog edit form holds an unsaved "New Mastery" draft that has
        /// never been persisted - see OnClickNewMasteryEntry/OnClickSaveCatalogEntry. Reset
        /// to false the moment the draft is saved, an existing row is selected instead, or
        /// the catalog list reloads for any other reason (page/filter change, tab switch).
        /// </summary>
        private bool _isEditingNewDraft;

        private static readonly GuiTabGroup<MasteryReviewViewModel, GuiPayloadBase> Tabs =
            new GuiTabGroup<MasteryReviewViewModel, GuiPayloadBase>()
                .AddTab(ReviewQueueTabId, ReviewQueuePartial, model => model.LoadReviewQueueTab())
                .AddTab(CatalogManageTabId, CatalogManagePartial, model => model.LoadCatalogManageTab());

        private static readonly GuiToggleGroupSync TabToggles =
            new(ReviewQueueTabId, CatalogManageTabId);

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
        // Review queue - list/filter
        // ---------------------------------------------------------------

        public string SearchText
        {
            get => Get<string>();
            set => Set(value);
        }

        /// <summary>0 = Pending, 1 = In Review, 2 = Recently Decided (Approved/Denied).</summary>
        public int SelectedStatusFilterId
        {
            get => Get<int>();
            set => Set(value);
        }

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

        public string PageText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsPrevEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsNextEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        // ---------------------------------------------------------------
        // Review queue - detail
        // ---------------------------------------------------------------

        public bool IsRequestSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string DetailHeaderText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string JustificationText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string RulesCheckText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string PlayerProfileText
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

        public bool UseQuickSlot
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsQuickSlotCheckboxEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsInstantGrant
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string DecisionDurationText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string FeedbackText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string OverrideReasonText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsOverrideReasonVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string StatusMessageText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsApproveEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsOpenProfileEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string OpenProfileDisabledTooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        // ---------------------------------------------------------------
        // Catalog management
        // ---------------------------------------------------------------

        public string CatalogSearchText
        {
            get => Get<string>();
            set => Set(value);
        }

        public int CatalogSelectedCategoryId
        {
            get => Get<int>();
            set => Set(value);
        }

        public GuiBindingList<string> CatalogManageLabels
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> CatalogManageToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public string CatalogManagePageText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsCatalogManagePrevEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsCatalogManageNextEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsCatalogEntrySelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string CatalogEditName
        {
            get => Get<string>();
            set => Set(value);
        }

        public int CatalogEditCategoryId
        {
            get => Get<int>();
            set => Set(value);
        }

        public string CatalogEditDescription
        {
            get => Get<string>();
            set => Set(value);
        }

        public int CatalogEditRarityId
        {
            get => Get<int>();
            set => Set(value);
        }

        /// <summary>-1 = no associated skill.</summary>
        public int CatalogEditSkillId
        {
            get => Get<int>();
            set => Set(value);
        }

        public bool CatalogEditIsActive
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string CatalogStatusText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<GuiComboEntry> SkillOptions
        {
            get => Get<GuiBindingList<GuiComboEntry>>();
            set => Set(value);
        }

        // ---------------------------------------------------------------
        // Initialize
        // ---------------------------------------------------------------

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            SearchText = string.Empty;
            SelectedStatusFilterId = 0;
            ReplyText = string.Empty;
            FeedbackText = string.Empty;
            OverrideReasonText = string.Empty;
            StatusMessageText = string.Empty;

            CatalogSearchText = string.Empty;
            CatalogSelectedCategoryId = -1;
            CatalogEditName = string.Empty;
            CatalogEditDescription = string.Empty;
            CatalogStatusText = string.Empty;
            TabToggleValue = ReviewQueueTabId;

            LoadSkillOptions();

            WatchOnClient(model => model.TabToggleValue);
            WatchOnClient(model => model.SearchText);
            WatchOnClient(model => model.SelectedStatusFilterId);
            WatchOnClient(model => model.ReplyText);
            WatchOnClient(model => model.UseQuickSlot);
            WatchOnClient(model => model.IsInstantGrant);
            WatchOnClient(model => model.FeedbackText);
            WatchOnClient(model => model.OverrideReasonText);
            WatchOnClient(model => model.CatalogSearchText);
            WatchOnClient(model => model.CatalogSelectedCategoryId);
            WatchOnClient(model => model.CatalogEditName);
            WatchOnClient(model => model.CatalogEditCategoryId);
            WatchOnClient(model => model.CatalogEditDescription);
            WatchOnClient(model => model.CatalogEditRarityId);
            WatchOnClient(model => model.CatalogEditSkillId);
            WatchOnClient(model => model.CatalogEditIsActive);

            SelectTab(ReviewQueueTabId);
        }

        protected override void OnClientPropertyUpdated(string propertyName)
        {
            if (SelectedTabId == ReviewQueueTabId)
            {
                if (propertyName == nameof(SearchText) || propertyName == nameof(SelectedStatusFilterId))
                {
                    _page = 0;
                    LoadQueue();
                }
                else if (propertyName == nameof(UseQuickSlot) || propertyName == nameof(IsInstantGrant))
                {
                    RefreshDecisionDuration();
                }
            }
            else if (SelectedTabId == CatalogManageTabId)
            {
                if (propertyName == nameof(CatalogSearchText) || propertyName == nameof(CatalogSelectedCategoryId))
                {
                    _catalogPage = 0;
                    LoadCatalogManage();
                }
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

        private void LoadReviewQueueTab()
        {
            _page = 0;
            LoadQueue();
        }

        private void LoadCatalogManageTab()
        {
            _catalogPage = 0;
            LoadCatalogManage();
        }

        // ---------------------------------------------------------------
        // Review queue - list
        // ---------------------------------------------------------------

        private static string FormatAge(TimeSpan age)
        {
            if (age.TotalDays >= 1) return $"{(int)age.TotalDays}d";
            if (age.TotalHours >= 1) return $"{(int)age.TotalHours}h";
            return $"{Math.Max(1, (int)age.TotalMinutes)}m";
        }

        private static string ToRomanTier(int tier) => tier switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            5 => "V",
            _ => tier.ToString()
        };

        private static GuiColor GetRowColor(MasteryRequestStatus status) => status switch
        {
            MasteryRequestStatus.Approved => GuiColor.Green,
            MasteryRequestStatus.Denied => GuiColor.Red,
            MasteryRequestStatus.InReview => GuiColor.Cyan,
            _ => GuiColor.White
        };

        private static uint FindOnlinePlayerObject(string playerId)
        {
            for (var candidate = GetFirstPC(); GetIsObjectValid(candidate); candidate = GetNextPC())
            {
                if (GetObjectUUID(candidate) == playerId)
                    return candidate;
            }

            return OBJECT_INVALID;
        }

        private void LoadQueue()
        {
            var statuses = SelectedStatusFilterId switch
            {
                1 => new[] { MasteryRequestStatus.InReview },
                2 => new[] { MasteryRequestStatus.Approved, MasteryRequestStatus.Denied },
                _ => new[] { MasteryRequestStatus.Pending }
            };

            var all = Mastery.GetRequestsByStatus(statuses);

            var search = (SearchText ?? string.Empty).Trim().ToLower();
            if (search.Length > 0)
                all = all.Where(r => r.CharacterName.ToLower().Contains(search)).ToList();

            all = SelectedStatusFilterId == 2
                ? all.OrderByDescending(r => r.DateReviewed ?? r.DateCreated).ToList()
                : all.OrderBy(r => r.DateCreated).ToList();

            var totalPages = Math.Max(1, (int)Math.Ceiling(all.Count / (double)PageSize));
            if (_page >= totalPages) _page = totalPages - 1;
            if (_page < 0) _page = 0;

            _rows = all.Skip(_page * PageSize).Take(PageSize).ToList();

            var labels = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();
            var colors = new GuiBindingList<GuiColor>();

            foreach (var request in _rows)
            {
                var name = request.Type == MasteryRequestType.Custom
                    ? $"Custom: {request.CustomName}"
                    : Mastery.GetMastery(request.MasteryId)?.Name ?? "Unknown Mastery";

                var referenceDate = SelectedStatusFilterId == 2 ? request.DateReviewed ?? request.DateCreated : request.DateCreated;
                var age = DateTime.UtcNow - referenceDate;

                labels.Add($"{request.CharacterName}\n{name} - T{request.TargetTier} ({FormatAge(age)})");
                toggles.Add(false);
                colors.Add(GetRowColor(request.Status));
            }

            RequestLabels = labels;
            RequestToggles = toggles;
            RequestColors = colors;
            PageText = $"Page {_page + 1} of {totalPages}";
            IsPrevEnabled = _page > 0;
            IsNextEnabled = _page < totalPages - 1;

            _selectedIndex = -1;
            LoadRequestDetail();
        }

        public Action OnClickPrevPage() => () =>
        {
            if (_page <= 0) return;
            _page--;
            LoadQueue();
        };

        public Action OnClickNextPage() => () =>
        {
            _page++;
            LoadQueue();
        };

        public Action OnClickSelectRequest() => () =>
        {
            if (_selectedIndex > -1 && _selectedIndex < RequestToggles.Count)
                RequestToggles[_selectedIndex] = false;

            var index = NuiGetEventArrayIndex();
            _selectedIndex = index;

            if (index >= 0 && index < RequestToggles.Count)
                RequestToggles[index] = true;

            StatusMessageText = string.Empty;
            ReplyText = string.Empty;
            FeedbackText = string.Empty;
            OverrideReasonText = string.Empty;
            UseQuickSlot = false;
            IsInstantGrant = false;

            // Opening a request for the first time transitions it out of Pending, so the
            // filter combo distinguishes "never looked at" from "someone is on it".
            if (_selectedIndex >= 0 && _selectedIndex < _rows.Count)
                Mastery.MarkInReview(_rows[_selectedIndex].Id);

            LoadRequestDetail();
        };

        // ---------------------------------------------------------------
        // Review queue - detail
        // ---------------------------------------------------------------

        private void LoadRequestDetail()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _rows.Count)
            {
                IsRequestSelected = false;
                DetailHeaderText = "Select a request to review it.";
                JustificationText = string.Empty;
                RulesCheckText = string.Empty;
                PlayerProfileText = string.Empty;
                CommentsText = string.Empty;
                DecisionDurationText = string.Empty;
                IsOverrideReasonVisible = false;
                IsApproveEnabled = false;
                IsOpenProfileEnabled = false;
                OpenProfileDisabledTooltip = string.Empty;
                IsQuickSlotCheckboxEnabled = false;
                return;
            }

            var request = DB.Get<MasteryRequest>(_rows[_selectedIndex].Id) ?? _rows[_selectedIndex];
            IsRequestSelected = true;

            var mastery = string.IsNullOrEmpty(request.MasteryId) ? null : Mastery.GetMastery(request.MasteryId);
            var displayName = mastery?.Name ?? (request.Type == MasteryRequestType.Custom ? $"{request.CustomName} (Unlisted)" : "Unknown Mastery");

            DetailHeaderText = $"{request.CharacterName} - {displayName} - Tier {request.TargetTier} [{request.Status}]";
            JustificationText = string.IsNullOrWhiteSpace(request.Justification) ? "(No justification provided)" : request.Justification;

            var profile = Mastery.GetOrCreateProfile(request.PlayerId);
            var dbPlayer = DB.Get<Player>(request.PlayerId);
            var characterCreatedDate = dbPlayer?.DateCreated ?? DateTime.UtcNow;

            // A Custom (unlisted) request has no catalog row yet - a transient,
            // never-persisted stand-in drives the shared eligibility-check logic, matching
            // MasteriesViewModel.RefreshEligibility's treatment of the same case.
            var checkMastery = mastery ?? new Entity.Mastery
            {
                Name = request.Type == MasteryRequestType.Custom ? request.CustomName : displayName,
                Rarity = MasteryRarityType.Standard
            };

            int? skillRank = null;
            if (checkMastery.AssociatedSkill != null && dbPlayer != null && dbPlayer.Skills.TryGetValue(checkMastery.AssociatedSkill.Value, out var skill))
                skillRank = skill.Rank;

            var checks = Mastery.BuildEligibilityChecks(request.PlayerId, checkMastery, request.TargetTier, characterCreatedDate, DateTime.UtcNow, skillRank);

            var rulesSb = new StringBuilder();
            foreach (var (passed, label) in checks)
                rulesSb.Append(passed ? "[OK] " : "[!!] ").Append(label).Append('\n');
            RulesCheckText = rulesSb.ToString().TrimEnd('\n');

            var isOffLimitBlocked = checkMastery.Rarity == MasteryRarityType.OffLimit;
            var hasFailure = checks.Any(c => !c.Passed);

            IsOverrideReasonVisible = hasFailure && !isOffLimitBlocked;
            IsApproveEnabled = request.Status != MasteryRequestStatus.Approved
                               && request.Status != MasteryRequestStatus.Denied
                               && !isOffLimitBlocked;

            var ownedCatalog = Mastery.GetOwnedMasteryCatalog(profile);
            var rareCount = ownedCatalog.Values.Count(m => m.Rarity == MasteryRarityType.Rare);

            var tiersSummary = profile.Masteries.Count == 0
                ? "No masteries earned yet."
                : string.Join(" | ", profile.Masteries.Select(kvp =>
                {
                    var ownedName = Mastery.GetMastery(kvp.Key)?.Name ?? "Unknown Mastery";
                    var isTraining = profile.TrainingQueue.Any(t => t.MasteryId == kvp.Key);
                    return $"{ownedName} {ToRomanTier(kvp.Value.Tier)}{(isTraining ? " [training]" : string.Empty)}";
                }));

            PlayerProfileText =
                $"{tiersSummary}\n" +
                $"Quick Slots: {profile.QuickSlotsAvailable}   Rare: {rareCount}/1   " +
                $"Levels Trained: {profile.LifetimeLevelsTrained}   Queue: {profile.TrainingQueue.Count}/{MasteryRules.MaxQueueSize}";

            var commentsSb = new StringBuilder();
            foreach (var comment in request.Comments)
            {
                var author = comment.IsStaff ? $"{comment.AuthorName} (Staff)" : comment.AuthorName;
                commentsSb.Append($"{author} - {comment.Date:yyyy-MM-dd}: {comment.Text}\n");
            }
            CommentsText = commentsSb.Length > 0 ? commentsSb.ToString().TrimEnd('\n') : "No comments yet.";

            IsQuickSlotCheckboxEnabled = profile.QuickSlotsAvailable > 0;
            if (!IsQuickSlotCheckboxEnabled)
                UseQuickSlot = false;

            RefreshDecisionDuration();

            IsOpenProfileEnabled = GetIsObjectValid(FindOnlinePlayerObject(request.PlayerId));
            OpenProfileDisabledTooltip = IsOpenProfileEnabled
                ? string.Empty
                : "This character must be online to open their full profile.";
        }

        private void RefreshDecisionDuration()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _rows.Count)
            {
                DecisionDurationText = string.Empty;
                return;
            }

            var request = _rows[_selectedIndex];
            var profile = Mastery.GetOrCreateProfile(request.PlayerId);
            var useRetrainCredit = MasteryRules.ShouldUseRetrainCredit(profile, request.TargetTier, UseQuickSlot, IsInstantGrant);
            var duration = MasteryRules.GetTrainingDuration(profile, request.TargetTier, UseQuickSlot, useRetrainCredit, IsInstantGrant);
            var creditSuffix = useRetrainCredit ? " (retrain credit)" : string.Empty;

            DecisionDurationText = duration <= 0
                ? "Duration if approved: instant (0 days)"
                : $"Duration if approved: {duration} day(s){creditSuffix} - completes {DateTime.UtcNow.AddDays(duration):yyyy-MM-dd}";
        }

        public Action OnClickApprove() => () =>
        {
            if (_selectedIndex < 0 || _selectedIndex >= _rows.Count) return;

            var liveRequest = DB.Get<MasteryRequest>(_rows[_selectedIndex].Id);
            if (liveRequest == null || liveRequest.Status == MasteryRequestStatus.Approved || liveRequest.Status == MasteryRequestStatus.Denied)
            {
                StatusMessageText = "This request is no longer pending.";
                LoadQueue();
                return;
            }

            var mastery = string.IsNullOrEmpty(liveRequest.MasteryId) ? null : Mastery.GetMastery(liveRequest.MasteryId);
            var checkMastery = mastery ?? new Entity.Mastery
            {
                Name = liveRequest.CustomName,
                Rarity = MasteryRarityType.Standard
            };

            if (checkMastery.Rarity == MasteryRarityType.OffLimit)
            {
                StatusMessageText = "This mastery is off-limits and cannot be approved through the request flow.";
                return;
            }

            var dbPlayer = DB.Get<Player>(liveRequest.PlayerId);
            var characterCreatedDate = dbPlayer?.DateCreated ?? DateTime.UtcNow;

            int? skillRank = null;
            if (checkMastery.AssociatedSkill != null && dbPlayer != null && dbPlayer.Skills.TryGetValue(checkMastery.AssociatedSkill.Value, out var skill))
                skillRank = skill.Rank;

            var violations = Mastery.ValidateRequest(liveRequest.PlayerId, checkMastery, liveRequest.TargetTier, characterCreatedDate, DateTime.UtcNow, skillRank);

            if (violations.Count > 0 && string.IsNullOrWhiteSpace(OverrideReasonText))
            {
                StatusMessageText = "An override reason is required to approve a request with failed rule checks.";
                return;
            }

            var profile = Mastery.GetOrCreateProfile(liveRequest.PlayerId);
            var useRetrainCreditPreview = MasteryRules.ShouldUseRetrainCredit(profile, liveRequest.TargetTier, UseQuickSlot, IsInstantGrant);
            var duration = MasteryRules.GetTrainingDuration(profile, liveRequest.TargetTier, UseQuickSlot, useRetrainCreditPreview, IsInstantGrant);
            var finishText = duration <= 0 ? "completes immediately" : $"completes {DateTime.UtcNow.AddDays(duration):yyyy-MM-dd}";
            var creditSuffix = useRetrainCreditPreview ? " (retrain credit)" : string.Empty;

            var prompt = $"Approve {checkMastery.Name} - Tier {liveRequest.TargetTier} for {liveRequest.CharacterName}?\n" +
                         $"Training: {duration} day(s){creditSuffix} - {finishText}." +
                         (violations.Count > 0
                             ? $"\nRules override: {string.Join("; ", violations.Select(v => v.Message))} - reason will be logged."
                             : string.Empty);

            var overrideReason = OverrideReasonText;
            var feedback = FeedbackText;
            var useQuickSlot = UseQuickSlot;
            var isInstant = IsInstantGrant;

            ShowModal(prompt, () =>
            {
                // Materializing a Custom (unlisted) request's catalog entry, re-validating
                // current state, and rejecting a stale/no-longer-Pending request are all
                // handled inside Mastery.ApproveRequest itself now - it re-fetches the
                // request fresh rather than trusting this captured liveRequest/violations
                // snapshot, which could be stale by the time the modal is confirmed (e.g.
                // another reviewer decided it, or the player cancelled, in the meantime).
                var approved = Mastery.ApproveRequest(
                    liveRequest.Id,
                    GetName(Player),
                    GetPCPublicCDKey(Player),
                    feedback,
                    overrideReason,
                    useQuickSlot,
                    isInstant,
                    DateTime.UtcNow);

                StatusMessageText = approved ? "Request approved." : "This request could no longer be approved - it may have changed since it was opened.";
                FeedbackText = string.Empty;
                OverrideReasonText = string.Empty;
                LoadQueue();
            });
        };

        public Action OnClickDeny() => () =>
        {
            if (_selectedIndex < 0 || _selectedIndex >= _rows.Count) return;

            if (string.IsNullOrWhiteSpace(FeedbackText))
            {
                StatusMessageText = "Feedback is required to deny a request.";
                return;
            }

            var request = _rows[_selectedIndex];
            var feedback = FeedbackText;

            ShowModal($"Deny this request for {request.CharacterName}?", () =>
            {
                var ok = Mastery.DenyRequest(request.Id, GetName(Player), GetPCPublicCDKey(Player), feedback, DateTime.UtcNow);

                StatusMessageText = ok ? "Request denied." : "This request is no longer pending.";
                FeedbackText = string.Empty;
                LoadQueue();
            });
        };

        public Action OnClickCommentOnly() => () =>
        {
            if (_selectedIndex < 0 || _selectedIndex >= _rows.Count) return;

            if (string.IsNullOrWhiteSpace(ReplyText))
            {
                StatusMessageText = "Enter a comment before posting.";
                return;
            }

            var request = _rows[_selectedIndex];
            Mastery.MarkInReview(request.Id);
            Mastery.AddComment(request.Id, GetName(Player), true, ReplyText, DateTime.UtcNow);

            ReplyText = string.Empty;
            StatusMessageText = "Comment posted.";
            LoadRequestDetail();
        };

        /// <summary>
        /// Opens the DM Player Examine window targeting this request's character, jumping
        /// straight to its Masteries tab. DMPlayerExaminePayload requires a live NWN object
        /// (Details/Skills/Perks read directly off it), so this is only possible while the
        /// character is online - see the handoff report for the "offline" tradeoff.
        /// </summary>
        public Action OnClickOpenFullProfile() => () =>
        {
            if (_selectedIndex < 0 || _selectedIndex >= _rows.Count) return;

            var request = _rows[_selectedIndex];
            var target = FindOnlinePlayerObject(request.PlayerId);

            if (!GetIsObjectValid(target))
            {
                StatusMessageText = "This character must be online to open their full profile.";
                return;
            }

            var payload = new DMPlayerExaminePayload(target, DMPlayerExamineViewModel.MasteriesView);
            Gui.TogglePlayerWindow(Player, GuiWindowType.DMPlayerExamine, payload);
        };

        // ---------------------------------------------------------------
        // Catalog management
        // ---------------------------------------------------------------

        private void LoadSkillOptions()
        {
            var options = new GuiBindingList<GuiComboEntry>
            {
                new GuiComboEntry("None", -1)
            };

            foreach (var (type, detail) in Skill.GetAllActiveSkills())
            {
                options.Add(new GuiComboEntry(detail.Name, (int)type));
            }

            SkillOptions = options;
        }

        private void LoadCatalogManage()
        {
            // Any reload (search/filter/page change, tab switch) discards an in-progress
            // "New Mastery" draft rather than leaving it half-applied to whatever row ends
            // up re-selected.
            _isEditingNewDraft = false;

            var search = (CatalogSearchText ?? string.Empty).Trim().ToLower();

            _catalogFiltered = Mastery.GetAllMasteries()
                .Where(m => CatalogSelectedCategoryId == -1 || (int)m.Category == CatalogSelectedCategoryId)
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

            var labels = new GuiBindingList<string>();
            var toggles = new GuiBindingList<bool>();

            foreach (var mastery in _catalogPageRows)
            {
                var retiredTag = mastery.IsActive ? string.Empty : " [retired]";
                labels.Add($"{mastery.Name} [{mastery.Rarity}]{retiredTag}");
                toggles.Add(false);
            }

            CatalogManageLabels = labels;
            CatalogManageToggles = toggles;
            CatalogManagePageText = $"Page {_catalogPage + 1} of {totalPages}";
            IsCatalogManagePrevEnabled = _catalogPage > 0;
            IsCatalogManageNextEnabled = _catalogPage < totalPages - 1;

            _catalogSelectedIndex = -1;
            LoadCatalogEditFields();
        }

        private void LoadCatalogEditFields()
        {
            if (_catalogSelectedIndex < 0 || _catalogSelectedIndex >= _catalogPageRows.Count)
            {
                IsCatalogEntrySelected = false;
                CatalogEditName = string.Empty;
                CatalogEditDescription = string.Empty;
                CatalogEditCategoryId = 0;
                CatalogEditRarityId = 0;
                CatalogEditSkillId = -1;
                CatalogEditIsActive = true;
                return;
            }

            var mastery = _catalogPageRows[_catalogSelectedIndex];
            IsCatalogEntrySelected = true;
            CatalogEditName = mastery.Name;
            CatalogEditDescription = mastery.Description;
            CatalogEditCategoryId = (int)mastery.Category;
            CatalogEditRarityId = (int)mastery.Rarity;
            CatalogEditSkillId = mastery.AssociatedSkill.HasValue ? (int)mastery.AssociatedSkill.Value : -1;
            CatalogEditIsActive = mastery.IsActive;
            CatalogStatusText = string.Empty;
        }

        public Action OnClickCatalogPrevPage() => () =>
        {
            if (_catalogPage <= 0) return;
            _catalogPage--;
            LoadCatalogManage();
        };

        public Action OnClickCatalogNextPage() => () =>
        {
            _catalogPage++;
            LoadCatalogManage();
        };

        public Action OnClickSelectCatalogRow() => () =>
        {
            // Selecting an existing row discards any in-progress "New Mastery" draft.
            _isEditingNewDraft = false;

            if (_catalogSelectedIndex > -1 && _catalogSelectedIndex < CatalogManageToggles.Count)
                CatalogManageToggles[_catalogSelectedIndex] = false;

            var index = NuiGetEventArrayIndex();
            _catalogSelectedIndex = index;

            if (index >= 0 && index < CatalogManageToggles.Count)
                CatalogManageToggles[index] = true;

            LoadCatalogEditFields();
        };

        public Action OnClickSaveCatalogEntry() => () =>
        {
            if (string.IsNullOrWhiteSpace(CatalogEditName))
            {
                CatalogStatusText = "Name is required.";
                return;
            }

            var skill = CatalogEditSkillId >= 0 ? (SkillType?)CatalogEditSkillId : null;

            if (_isEditingNewDraft)
            {
                var trimmedName = CatalogEditName.Trim();
                var isDuplicateName = Mastery.GetAllMasteries()
                    .Any(m => string.Equals(m.Name, trimmedName, StringComparison.OrdinalIgnoreCase));

                if (isDuplicateName)
                {
                    CatalogStatusText = "A mastery with this name already exists.";
                    return;
                }

                var created = Mastery.CreateMastery(
                    trimmedName,
                    (MasteryCategoryType)CatalogEditCategoryId,
                    CatalogEditDescription,
                    (MasteryRarityType)CatalogEditRarityId,
                    skill,
                    GetName(Player),
                    GetPCPublicCDKey(Player));

                _isEditingNewDraft = false;
                _catalogPage = 0;
                LoadCatalogManage();

                var newIndex = _catalogPageRows.FindIndex(m => m.Id == created.Id);
                if (newIndex < 0) return;

                _catalogSelectedIndex = newIndex;
                CatalogManageToggles[newIndex] = true;
                LoadCatalogEditFields();
                return;
            }

            if (_catalogSelectedIndex < 0 || _catalogSelectedIndex >= _catalogPageRows.Count) return;

            var mastery = _catalogPageRows[_catalogSelectedIndex];

            Mastery.UpdateMastery(
                mastery.Id,
                CatalogEditName,
                (MasteryCategoryType)CatalogEditCategoryId,
                CatalogEditDescription,
                (MasteryRarityType)CatalogEditRarityId,
                skill,
                CatalogEditIsActive,
                GetName(Player),
                GetPCPublicCDKey(Player));

            CatalogStatusText = "Saved.";
            LoadCatalogManage();
        };

        public Action OnClickNewMasteryEntry() => () =>
        {
            // The "New Mastery" form is a local, never-persisted draft until Save
            // validates and confirms it (see OnClickSaveCatalogEntry) - this used to call
            // CreateMastery immediately, leaving an orphan active "New Mastery" catalog row
            // behind on every click (compounding on repeated clicks) even before a name was
            // ever entered.
            if (_catalogSelectedIndex > -1 && _catalogSelectedIndex < CatalogManageToggles.Count)
                CatalogManageToggles[_catalogSelectedIndex] = false;

            _catalogSelectedIndex = -1;
            _isEditingNewDraft = true;

            IsCatalogEntrySelected = true;
            CatalogEditName = "New Mastery";
            CatalogEditDescription = string.Empty;
            CatalogEditCategoryId = (int)MasteryCategoryType.General;
            CatalogEditRarityId = (int)MasteryRarityType.Standard;
            CatalogEditSkillId = -1;
            CatalogEditIsActive = true;
            CatalogStatusText = "Unsaved - enter a name and click Save to create this mastery.";
        };
    }
}
