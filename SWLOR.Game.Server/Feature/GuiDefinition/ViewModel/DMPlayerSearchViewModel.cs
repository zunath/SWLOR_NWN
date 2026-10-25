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
        // Selected characters in pick order, kept across searches and pages to gather a group.
        private readonly List<DMPlayerSearchResult> _selected = new();
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

        public string SelectionSummary
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool HasSelection
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string BringSelectedText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool CanBringSelected
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

        public string SingleActionReason
        {
            get => Get<string>();
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
            PageNumbers = new GuiBindingList<GuiComboEntry>();
            SelectedPageIndex = 0;
            ResultSummary = string.Empty;
            // The view model is reused when the window reopens. An old selection must not carry into a new session.
            _selected.Clear();
            ShowSelection();
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

            // Refresh selections from the search, and swap in the offline record of anyone who logged off.
            for (var index = _selected.Count - 1; index >= 0; index--)
            {
                var selected = _selected[index];
                var result = _results.FirstOrDefault(listed => listed.PlayerId == selected.PlayerId);
                if (result != null)
                {
                    _selected[index] = result;
                    continue;
                }

                if (!selected.IsOnline || GetIsObjectValid(DMPlayerDirectory.GetOnlinePlayer(selected.PlayerId)))
                    continue;

                var offline = DMPlayerDirectory.Search(selected.PlayerId, DMPlayerSearchScope.Offline, out _)
                    .FirstOrDefault(listed => listed.PlayerId == selected.PlayerId);
                if (offline == null)
                    _selected.RemoveAt(index);
                else
                    _selected[index] = offline;
            }

            var names = new GuiBindingList<string>();
            var descriptors = new GuiBindingList<string>();
            var descriptorColors = new GuiBindingList<GuiColor>();
            var accounts = new GuiBindingList<string>();
            var statuses = new GuiBindingList<string>();
            var statusColors = new GuiBindingList<GuiColor>();
            var selections = new GuiBindingList<bool>();
            foreach (var result in _pageResults)
            {
                names.Add(result.Name);
                descriptors.Add(DMListText.Fit(result.Descriptor, DescriptorColumnLength));
                descriptorColors.Add(result.IsDisguised ? DisguisedDescriptorColor : DescriptorColor);
                accounts.Add(string.IsNullOrWhiteSpace(result.AccountName) ? "?" : DMListText.Fit(result.AccountName, AccountColumnLength));
                statuses.Add(result.IsOnline ? "Online" : "Offline");
                statusColors.Add(result.IsOnline ? OnlineColor : OfflineColor);
                selections.Add(IsSelected(result));
            }

            ResultNames = names;
            ResultDescriptors = descriptors;
            ResultDescriptorColors = descriptorColors;
            ResultAccounts = accounts;
            ResultStatuses = statuses;
            ResultStatusColors = statusColors;
            ResultSelections = selections;

            var onlineCount = _results.Count(result => result.IsOnline);
            ResultSummary = _results.Count == 0
                ? "No matches."
                : $"{_results.Count} character{(_results.Count == 1 ? string.Empty : "s")} ({onlineCount} online)." +
                  (_isTruncated ? " Narrow the search to see more." : string.Empty);

            ShowSelection();
        }

        private bool IsSelected(DMPlayerSearchResult result)
        {
            return _selected.Any(selected => selected.PlayerId == result.PlayerId);
        }

        // Selects or deselects one row. Other selections stay, so several characters can be picked at once.
        private void SelectResult(int index)
        {
            if (index < 0 || index >= _pageResults.Count)
                return;

            var result = _pageResults[index];
            var selectedIndex = _selected.FindIndex(selected => selected.PlayerId == result.PlayerId);
            if (selectedIndex >= 0)
                _selected.RemoveAt(selectedIndex);
            else
                _selected.Add(result);

            ResultSelections[index] = selectedIndex < 0;
            ShowSelection();
        }

        private void ShowSelection()
        {
            var count = _selected.Count;
            var onlineCount = _selected.Count(selected => selected.IsOnline);
            HasSelection = count > 0;
            SelectionSummary = count == 0
                ? "No characters selected."
                : $"{count} character{(count == 1 ? string.Empty : "s")} selected.";
            CanBringSelected = onlineCount > 0;
            BringSelectedText = onlineCount == 0 ? "Bring Selected Here" : $"Bring Selected Here ({onlineCount})";

            var result = GetSelectedResult();
            CanJumpTo = result != null;
            IsSelectedOnline = result is { IsOnline: true };
            SingleActionReason = count switch
            {
                0 => "Select a character first.",
                1 => "The character is offline.",
                _ => "Select only one character to use this.",
            };

            if (count == 0)
            {
                SelectedDetails = "Select a character to see their details and actions. Select several to bring them here together.";
                return;
            }

            if (result == null)
            {
                SelectedDetails = $"{count} characters selected:\n" + string.Join("\n", _selected.Select(selected =>
                    selected.IsOnline ? selected.Name : $"{selected.Name} (offline)"));
                return;
            }

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

        // The character that single-character actions use: the selection when exactly one is selected.
        private DMPlayerSearchResult GetSelectedResult()
        {
            return _selected.Count == 1 ? _selected[0] : null;
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
            SelectResult(NuiGetEventArrayIndex());
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

        public Action OnClickClearSelection() => () =>
        {
            _selected.Clear();
            ShowPage();
        };

        // Brings every selected online character. Offline selections stay selected and are skipped.
        public Action OnClickBringSelected() => () =>
        {
            var brought = 0;
            foreach (var result in _selected.Where(selected => selected.IsOnline).ToList())
            {
                var target = DMPlayerDirectory.GetOnlinePlayer(result.PlayerId);
                if (!GetIsObjectValid(target))
                {
                    DMPresence.SendMessage(Player, $"{result.Name} is no longer online.");
                    _selected.Remove(result);
                    continue;
                }

                var error = DMPresence.Summon(Player, target);
                if (string.IsNullOrEmpty(error))
                    brought++;
                else
                    DMPresence.SendMessage(Player, error);
            }

            DMPresence.SendMessage(Player, $"Brought {brought} character{(brought == 1 ? string.Empty : "s")} to you.");
            ShowPage();
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

                // The target is exact, so a chat-portrait click the DM left unsent never blocks it.
                var speaker = DMPresence.GetDM(Player);
                SetLocalBool(speaker, PlayerName.DirectTellVariable, true);
                try
                {
                    ChatPlugin.SendMessage(ChatChannel.DMTell, message, speaker, target);
                }
                finally
                {
                    DeleteLocalBool(speaker, PlayerName.DirectTellVariable);
                }
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
