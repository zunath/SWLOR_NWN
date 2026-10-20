using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PlayerListService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class DMPlayerSearchViewModel : GuiViewModelBase<DMPlayerSearchViewModel, GuiPayloadBase>
    {
        public const string MainContentElement = "dmplayersearch_content";
        public const string MainContentPartial = "DMPLAYERSEARCH_MAIN";

        private const int ResultsPerPage = 20;
        private const string NotRecorded = "(not recorded yet)";
        // Characters that fit the 150 px descriptor and 110 px account columns; the details box shows full values.
        private const int DescriptorColumnLength = 19;
        private const int AccountColumnLength = 14;

        private static readonly GuiColor OnlineColor = new(0, 200, 0);
        private static readonly GuiColor OfflineColor = new(150, 150, 150);
        private static readonly GuiColor DescriptorColor = new(255, 255, 255);
        private static readonly GuiColor DisguisedDescriptorColor = new(255, 127, 0);

        private List<DMPlayerSearchResult> _results = new();
        private readonly List<DMPlayerSearchResult> _pageResults = new();
        private int _selectedIndex = -1;
        private bool _isTruncated;
        private bool _suppressSearch;
        // Checked characters by player ID, kept across searches and pages. The names are for messages.
        private readonly Dictionary<string, string> _checkedPlayers = new();
        // Setting the checkbox list fires its watch event at once, before the page is fully written.
        private bool _isWritingChecks;

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

        public GuiBindingList<GuiColor> ResultDescriptorColors
        {
            get => Get<GuiBindingList<GuiColor>>();
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

        public GuiBindingList<bool> ResultChecks
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<bool> ResultCheckEnabled
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public string CheckedSummary
        {
            get => Get<string>();
            set => Set(value);
        }

        public string BringCheckedText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool HasChecked
        {
            get => Get<bool>();
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
            ResultDescriptorColors = new GuiBindingList<GuiColor>();
            ResultAccounts = new GuiBindingList<string>();
            ResultStatuses = new GuiBindingList<string>();
            ResultStatusColors = new GuiBindingList<GuiColor>();
            ResultSelections = new GuiBindingList<bool>();
            ResultChecks = new GuiBindingList<bool>();
            ResultCheckEnabled = new GuiBindingList<bool>();
            PageNumbers = new GuiBindingList<GuiComboEntry>();
            SelectedPageIndex = 0;
            ResultSummary = string.Empty;
            SelectedDetails = string.Empty;
            CanJumpTo = false;
            IsSelectedOnline = false;
            // The view model is reused when the window reopens. Old checks must not carry into a new session.
            _checkedPlayers.Clear();
            UpdateCheckedState();
            _suppressSearch = false;

            WatchOnClient(model => model.SelectedScope);
            WatchOnClient(model => model.SearchText);
            WatchOnClient(model => model.SelectedPageIndex);
            WatchOnClient(model => model.ResultChecks);

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
            var descriptorColors = new GuiBindingList<GuiColor>();
            var accounts = new GuiBindingList<string>();
            var statuses = new GuiBindingList<string>();
            var statusColors = new GuiBindingList<GuiColor>();
            var selections = new GuiBindingList<bool>();
            var checks = new GuiBindingList<bool>();
            var checkEnabled = new GuiBindingList<bool>();
            foreach (var result in _pageResults)
            {
                names.Add(result.Name);
                descriptors.Add(DMListText.Fit(result.Descriptor, DescriptorColumnLength));
                descriptorColors.Add(result.IsDisguised ? DisguisedDescriptorColor : DescriptorColor);
                accounts.Add(string.IsNullOrWhiteSpace(result.AccountName) ? "?" : DMListText.Fit(result.AccountName, AccountColumnLength));
                statuses.Add(result.IsOnline ? "Online" : "Offline");
                statusColors.Add(result.IsOnline ? OnlineColor : OfflineColor);
                selections.Add(false);
                checks.Add(result.IsOnline && _checkedPlayers.ContainsKey(result.PlayerId));
                checkEnabled.Add(result.IsOnline);
            }

            ResultNames = names;
            ResultDescriptors = descriptors;
            ResultDescriptorColors = descriptorColors;
            ResultAccounts = accounts;
            ResultStatuses = statuses;
            ResultStatusColors = statusColors;
            ResultSelections = selections;
            _isWritingChecks = true;
            ResultChecks = checks;
            _isWritingChecks = false;
            ResultCheckEnabled = checkEnabled;

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
            var onlinePlayer = result.IsOnline ? DMPlayerDirectory.GetOnlinePlayer(result.PlayerId) : OBJECT_INVALID;
            var details = new List<string>
            {
                $"Name: {result.Name}",
                $"Descriptor: {result.Descriptor}",
                $"Disguise: {result.Disguise}"
            };
            if (result.IsDisguised)
                details.Add($"Real Descriptor: {result.UndisguisedDescriptor}");

            details.AddRange(new[]
            {
                $"Account: {ValueOrNotRecorded(result.AccountName)}",
                $"CD Key: {ValueOrNotRecorded(result.CDKey)}",
                $"Player ID: {result.PlayerId}",
                $"Status: {GetStatusText(result, onlinePlayer)}",
                $"{(result.IsOnline ? "Area" : "Last Area")}: {ValueOrNotRecorded(result.LocationName)}",
                $"Last Login: {(result.LastLoginDate.HasValue ? result.LastLoginDate.Value.ToString("yyyy-MM-dd HH:mm") + " UTC" : NotRecorded)}"
            });
            if (GetIsObjectValid(onlinePlayer))
            {
                var (leader, playerCount) = PlayerList.GetParty(onlinePlayer);
                details.Add($"Party: {PlayerListRules.DescribeParty(leader == onlinePlayer, GetName(leader), playerCount)}");
                details.Add($"Flags: {PlayerListRules.DescribeFlags(PlayerList.GetFlags(onlinePlayer))}");
            }

            SelectedDetails = string.Join("\n", details);
            CanJumpTo = true;
            IsSelectedOnline = result.IsOnline;
        }

        private void UpdateCheckedState()
        {
            var count = _checkedPlayers.Count;
            HasChecked = count > 0;
            CheckedSummary = count == 0
                ? "No characters checked."
                : $"{count} character{(count == 1 ? string.Empty : "s")} checked.";
            BringCheckedText = count == 0 ? "Bring Checked Here" : $"Bring Checked Here ({count})";
        }

        // Reflects the page's checkboxes on the client into the checked set.
        private void ReadPageChecks()
        {
            if (_isWritingChecks || ResultChecks.Count != _pageResults.Count)
                return;

            for (var index = 0; index < _pageResults.Count; index++)
            {
                var result = _pageResults[index];
                if (result.IsOnline && ResultChecks[index])
                    _checkedPlayers[result.PlayerId] = result.Name;
                else
                    _checkedPlayers.Remove(result.PlayerId);
            }

            UpdateCheckedState();
        }

        private void ShowPageChecks()
        {
            var checks = new GuiBindingList<bool>();
            foreach (var result in _pageResults)
            {
                checks.Add(result.IsOnline && _checkedPlayers.ContainsKey(result.PlayerId));
            }

            _isWritingChecks = true;
            ResultChecks = checks;
            _isWritingChecks = false;
            UpdateCheckedState();
        }

        protected override void OnClientPropertyUpdated(string propertyName)
        {
            if (propertyName == nameof(ResultChecks))
                ReadPageChecks();
        }

        private static string GetStatusText(DMPlayerSearchResult result, uint onlinePlayer)
        {
            if (!result.IsOnline)
                return "Offline";

            return GetIsObjectValid(onlinePlayer) && PlayerName.IsAfk(onlinePlayer) ? "Online (AFK)" : "Online";
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
                DMPresence.SendMessage(Player, $"{result.Name} is no longer online.");
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
                DMPresence.JumpTo(Player, GetLocation(target), $"{result.Name} ({result.PlayerId})");
                return;
            }

            if (DMPlayerDirectory.TryGetLastLocation(result.PlayerId, out var location))
            {
                DMPresence.SendMessage(Player, $"{result.Name} is offline. Jumping to their last saved location.");
                DMPresence.JumpTo(Player, location, $"offline {result.Name} ({result.PlayerId})'s last location");
            }
            else
            {
                DMPresence.SendMessage(Player, $"{result.Name} has no saved location in an area that still exists.");
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
                DMPresence.SendMessage(Player, error);
        };

        public Action OnClickClearChecks() => () =>
        {
            _checkedPlayers.Clear();
            ShowPageChecks();
        };

        public Action OnClickBringChecked() => () =>
        {
            var brought = 0;
            foreach (var (playerId, name) in _checkedPlayers.ToList())
            {
                var target = DMPlayerDirectory.GetOnlinePlayer(playerId);
                if (!GetIsObjectValid(target))
                {
                    DMPresence.SendMessage(Player, $"{name} is no longer online.");
                    _checkedPlayers.Remove(playerId);
                    continue;
                }

                var error = DMPresence.Summon(Player, target);
                if (string.IsNullOrEmpty(error))
                    brought++;
                else
                    DMPresence.SendMessage(Player, error);
            }

            DMPresence.SendMessage(Player, $"Brought {brought} character{(brought == 1 ? string.Empty : "s")} to you.");
            ShowPageChecks();
        };

        public Action OnClickExamine() => () =>
        {
            var result = GetSelectedResult();
            if (result == null)
                return;

            var target = GetSelectedOnlinePlayer(result);
            if (!GetIsObjectValid(target))
                return;

            DMPresence.CloseWindow(Player, GuiWindowType.DMPlayerExamine);
            DMPresence.ToggleWindow(Player, GuiWindowType.DMPlayerExamine, new DMPlayerExaminePayload(target));
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

                ChatPlugin.SendMessage(ChatChannel.DMTell, message, DMPresence.GetDM(Player), target);
                DMPresence.SendMessage(Player, $"To {result.Name}: {message}");
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

                DMPresence.Boot(Player, target);
                DelayCommand(0.5f, () =>
                {
                    if (Gui.IsWindowOpen(Player, GuiWindowType.DMPlayerSearch))
                        RunSearch();
                });
            });
        };
    }
}
