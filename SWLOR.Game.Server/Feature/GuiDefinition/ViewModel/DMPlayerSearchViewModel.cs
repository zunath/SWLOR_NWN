using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class DMPlayerSearchViewModel : GuiViewModelBase<DMPlayerSearchViewModel, GuiPayloadBase>
    {
        public const string MainContentElement = "dmplayersearch_content";
        public const string MainContentPartial = "DMPLAYERSEARCH_MAIN";

        private const int ResultsPerPage = 20;
        private const string NotRecorded = "(not recorded yet)";

        private static readonly GuiColor OnlineColor = new(0, 200, 0);
        private static readonly GuiColor OfflineColor = new(150, 150, 150);

        private List<DMPlayerSearchResult> _results = new();
        private readonly List<DMPlayerSearchResult> _pageResults = new();
        private int _selectedIndex = -1;
        private bool _isTruncated;
        private bool _suppressSearch;

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

        public GuiBindingList<string> ResultDescriptors
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> ResultAccounts
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> ResultStatuses
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> ResultStatusColors
        {
            get => Get<GuiBindingList<GuiColor>>();
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

        public bool IsSelectedOnline
        {
            get => Get<bool>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            Geometry = DMToolbarViewModel.PlaceBelowToolbar(Player, Geometry);
            _suppressSearch = true;
            SelectedScope = (int)DMPlayerSearchScope.Online;
            SearchText = string.Empty;
            ResultNames = new GuiBindingList<string>();
            ResultDescriptors = new GuiBindingList<string>();
            ResultAccounts = new GuiBindingList<string>();
            ResultStatuses = new GuiBindingList<string>();
            ResultStatusColors = new GuiBindingList<GuiColor>();
            ResultSelections = new GuiBindingList<bool>();
            PageNumbers = new GuiBindingList<GuiComboEntry>();
            SelectedPageIndex = 0;
            ResultSummary = string.Empty;
            SelectedDetails = string.Empty;
            CanJumpTo = false;
            IsSelectedOnline = false;
            _suppressSearch = false;

            WatchOnClient(model => model.SelectedScope);
            WatchOnClient(model => model.SearchText);
            WatchOnClient(model => model.SelectedPageIndex);

            ChangePartialView(MainContentElement, MainContentPartial);
            RunSearch();
        }

        private void RunSearch()
        {
            if (Player == 0 || WindowToken <= 0)
                return;

            var scope = (DMPlayerSearchScope)SelectedScope;
            _results = DMPlayerDirectory.Search(SearchText, scope, out _isTruncated);

            _suppressSearch = true;
            SelectedPageIndex = 0;
            _suppressSearch = false;
            ShowPage();

            if (scope == DMPlayerSearchScope.Offline && string.IsNullOrWhiteSpace(SearchText))
                ResultSummary = "Enter a name, descriptor, account, CD key, or player ID to search offline characters.";
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
            var descriptors = new GuiBindingList<string>();
            var accounts = new GuiBindingList<string>();
            var statuses = new GuiBindingList<string>();
            var statusColors = new GuiBindingList<GuiColor>();
            var selections = new GuiBindingList<bool>();
            foreach (var result in _pageResults)
            {
                names.Add(result.Name);
                descriptors.Add(result.Descriptor);
                accounts.Add(string.IsNullOrWhiteSpace(result.AccountName) ? "?" : result.AccountName);
                statuses.Add(result.IsOnline ? "Online" : "Offline");
                statusColors.Add(result.IsOnline ? OnlineColor : OfflineColor);
                selections.Add(false);
            }

            ResultNames = names;
            ResultDescriptors = descriptors;
            ResultAccounts = accounts;
            ResultStatuses = statuses;
            ResultStatusColors = statusColors;
            ResultSelections = selections;

            var onlineCount = _results.Count(result => result.IsOnline);
            ResultSummary = _results.Count == 0
                ? "No matches."
                : $"{_results.Count} character{(_results.Count == 1 ? string.Empty : "s")} ({onlineCount} online)." +
                  (_isTruncated ? " Narrow the search to see more." : string.Empty);

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
                SelectedDetails = "Select a character to see their details and actions.";
                CanJumpTo = false;
                IsSelectedOnline = false;
                return;
            }

            ResultSelections[_selectedIndex] = true;
            SelectedDetails = string.Join("\n",
                $"Name: {result.Name}",
                $"Descriptor: {result.Descriptor}",
                $"Account: {ValueOrNotRecorded(result.AccountName)}",
                $"CD Key: {ValueOrNotRecorded(result.CDKey)}",
                $"Player ID: {result.PlayerId}",
                $"Status: {(result.IsOnline ? "Online" : "Offline")}",
                $"{(result.IsOnline ? "Area" : "Last Area")}: {ValueOrNotRecorded(result.LocationName)}",
                $"Last Login: {(result.LastLoginDate.HasValue ? result.LastLoginDate.Value.ToString("yyyy-MM-dd HH:mm") + " UTC" : NotRecorded)}");
            CanJumpTo = true;
            IsSelectedOnline = result.IsOnline;
        }

        private static string ValueOrNotRecorded(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? NotRecorded : value;
        }

        private DMPlayerSearchResult GetSelectedResult()
        {
            return _selectedIndex >= 0 && _selectedIndex < _pageResults.Count
                ? _pageResults[_selectedIndex]
                : null;
        }

        // Resolves the selected character's current player object, telling the DM if they logged off.
        private uint GetSelectedOnlinePlayer(DMPlayerSearchResult result)
        {
            var player = DMPlayerDirectory.GetOnlinePlayer(result.PlayerId);
            if (!GetIsObjectValid(player))
            {
                SendMessageToPC(Player, $"{result.Name} is no longer online.");
                RunSearch();
            }

            return player;
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

        public Action OnClickJumpTo() => () =>
        {
            var result = GetSelectedResult();
            if (result == null)
                return;

            var target = DMPlayerDirectory.GetOnlinePlayer(result.PlayerId);
            if (GetIsObjectValid(target))
            {
                DMPresence.JumpTo(Player, GetLocation(target));
                return;
            }

            if (DMPlayerDirectory.TryGetLastLocation(result.PlayerId, out var location))
            {
                SendMessageToPC(Player, $"{result.Name} is offline. Jumping to their last saved location.");
                DMPresence.JumpTo(Player, location);
            }
            else
            {
                SendMessageToPC(Player, $"{result.Name} has no saved location in an area that still exists.");
            }
        };

        public Action OnClickJumpHere() => () =>
        {
            var result = GetSelectedResult();
            if (result == null)
                return;

            var target = GetSelectedOnlinePlayer(result);
            if (!GetIsObjectValid(target))
                return;

            var error = DMPresence.Summon(Player, target);
            if (!string.IsNullOrEmpty(error))
                SendMessageToPC(Player, error);
        };

        public Action OnClickExamine() => () =>
        {
            var result = GetSelectedResult();
            if (result == null)
                return;

            var target = GetSelectedOnlinePlayer(result);
            if (!GetIsObjectValid(target))
                return;

            Gui.ClosePlayerWindow(Player, GuiWindowType.DMPlayerExamine);
            Gui.TogglePlayerWindow(Player, GuiWindowType.DMPlayerExamine, new DMPlayerExaminePayload(target));
        };

        public Action OnClickSendMessage() => () =>
        {
            var result = GetSelectedResult();
            if (result == null || !result.IsOnline)
                return;

            ShowInputModal($"Private message to {result.Name}:", string.Empty, () =>
            {
                var message = ModalInputText?.Trim();
                if (string.IsNullOrWhiteSpace(message))
                    return;

                var target = GetSelectedOnlinePlayer(result);
                if (!GetIsObjectValid(target))
                    return;

                ChatPlugin.SendMessage(ChatChannel.DMTell, message, Player, target);
                SendMessageToPC(Player, $"To {result.Name}: {message}");
                Log.Write(LogGroup.DM, $"{DMPresence.DescribeDM(Player)} messaged {result.Name} ({result.PlayerId}): {message}");
            });
        };

        public Action OnClickBoot() => () =>
        {
            var result = GetSelectedResult();
            if (result == null || !result.IsOnline)
                return;

            ShowModal($"Disconnect {result.Name} ({result.AccountName}) from the server?", () =>
            {
                var target = GetSelectedOnlinePlayer(result);
                if (!GetIsObjectValid(target))
                    return;

                Log.Write(LogGroup.DM, $"{DMPresence.DescribeDM(Player)} booted {result.Name} ({result.AccountName}) [{result.CDKey}] ({result.PlayerId}).");
                BootPC(target, "You have been disconnected by a DM.");
                DelayCommand(0.5f, () =>
                {
                    if (Gui.IsWindowOpen(Player, GuiWindowType.DMPlayerSearch))
                        RunSearch();
                });
            });
        };
    }
}
