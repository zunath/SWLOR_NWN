using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Component;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class HoloComViewModel: GuiViewModelBase<HoloComViewModel, GuiPayloadBase>,
        IGuiRefreshable<HoloComMessageReceivedRefreshEvent>,
        IGuiRefreshable<HoloComCallStateChangedRefreshEvent>
    {
        // TESTING ONLY - lets you message/favorite yourself for one-client proof-of-concept testing.
        // Flip to false (or delete this and its call sites) before this ships.
        private const bool IsTestingModeEnabled = true;

        private const int MessagesTabId = 0;
        private const int ContactsTabId = 1;
        public const string TabContentPartialElement = "holocom_tab_content";
        public const string MessagesTabPartial = "HOLOCOM_MESSAGES_TAB";
        public const string ContactsTabPartial = "HOLOCOM_CONTACTS_TAB";
        public const string ComposePartial = "HOLOCOM_COMPOSE";

        private const int InboxPageSize = 15;
        private const int PreviewLength = 80;

        private static readonly GuiTabGroup<HoloComViewModel, GuiPayloadBase> Tabs =
            new GuiTabGroup<HoloComViewModel, GuiPayloadBase>()
                .AddTab(MessagesTabId, MessagesTabPartial, m => m.RefreshMessages())
                .AddTab(ContactsTabId, ContactsTabPartial, m => m.RefreshContacts());

        private static readonly GuiToggleGroupSync TabToggle = new(MessagesTabId, ContactsTabId);

        private sealed class MessageRow
        {
            public string Id { get; }
            public string SenderName { get; }
            public GuiColor RowColor { get; }
            public string Timestamp { get; }
            public string Preview { get; }
            public bool IsRead { get; }

            public MessageRow(string id, string senderName, GuiColor rowColor, string timestamp, string preview, bool isRead)
            {
                Id = id;
                SenderName = senderName;
                RowColor = rowColor;
                Timestamp = timestamp;
                Preview = preview;
                IsRead = isRead;
            }
        }

        private sealed class OnlinePlayerRow
        {
            public string PlayerId { get; }
            public string DisplayName { get; }
            public GuiColor NameColor { get; }

            public OnlinePlayerRow(string playerId, string displayName, GuiColor nameColor)
            {
                PlayerId = playerId;
                DisplayName = displayName;
                NameColor = nameColor;
            }
        }

        private sealed class FavoriteRow
        {
            public string PlayerId { get; }
            public string DisplayName { get; }
            public GuiColor StatusColor { get; }
            public bool IsOnline { get; }

            public FavoriteRow(string playerId, string displayName, GuiColor statusColor, bool isOnline)
            {
                PlayerId = playerId;
                DisplayName = displayName;
                StatusColor = statusColor;
                IsOnline = isOnline;
            }
        }

        private static readonly GuiTableSource<HoloComViewModel, MessageRow> MessagesTable =
            new GuiTableSource<HoloComViewModel, MessageRow>()
                .Column((m, v) => m.MessageSenderNames = v, r => r.SenderName)
                .Column((m, v) => m.MessageRowColors = v, r => r.RowColor)
                .Column((m, v) => m.MessageTimestamps = v, r => r.Timestamp)
                .Column((m, v) => m.MessagePreviews = v, r => r.Preview)
                .Column((m, v) => m.MessageIsUnread = v, r => !r.IsRead);

        private static readonly GuiTableSource<HoloComViewModel, OnlinePlayerRow> OnlinePlayersTable =
            new GuiTableSource<HoloComViewModel, OnlinePlayerRow>()
                .Column((m, v) => m.OnlinePlayerNames = v, r => r.DisplayName)
                .Column((m, v) => m.OnlinePlayerColors = v, r => r.NameColor);

        private static readonly GuiTableSource<HoloComViewModel, FavoriteRow> FavoritesTable =
            new GuiTableSource<HoloComViewModel, FavoriteRow>()
                .Column((m, v) => m.FavoriteNames = v, r => r.DisplayName)
                .Column((m, v) => m.FavoriteStatusColors = v, r => r.StatusColor)
                .Column((m, v) => m.FavoriteIsOnline = v, r => r.IsOnline);

        private int _inboxPageIndex;
        private IList<MessageRow> _messageRows = new List<MessageRow>();
        private List<uint> _onlinePlayerObjects = new();
        private IList<FavoriteRow> _favoriteRows = new List<FavoriteRow>();
        private string _composeRecipientId;
        private string _composeRecipientName;

        // ShowModal swaps the whole window to the modal view and returns to the main
        // view on close, which resets the nested content slot. Track what the content
        // slot currently shows so modal confirm/cancel can restore it - the same
        // pattern CharacterSheetViewModel uses for its tabs.
        private string _currentPartial = MessagesTabPartial;
        private Action _currentPartialRefresh;

        private void RestoreContentPartial()
        {
            SwapNestedPartialView(TabContentPartialElement, _currentPartial, _currentPartialRefresh);
        }

        public int SelectedTabId
        {
            get => Get<int>();
            set
            {
                Set(value);
                TabToggle.SyncTo(value, v => TabToggleValue = v);
                _currentPartial = Tabs.GetPartialName(value);
                _currentPartialRefresh = value == ContactsTabId
                    ? RefreshContacts
                    : (Action)RefreshMessages;
                Tabs.Select(this, TabContentPartialElement, value);
            }
        }

        public int TabToggleValue
        {
            get => Get<int>();
            set
            {
                Set(value);
                TabToggle.HandleClientChange(value, tabId => SelectedTabId = tabId);
            }
        }

        public GuiBindingList<string> MessageSenderNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<GuiColor> MessageRowColors { get => Get<GuiBindingList<GuiColor>>(); set => Set(value); }
        public GuiBindingList<string> MessageTimestamps { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> MessagePreviews { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> MessageIsUnread { get => Get<GuiBindingList<bool>>(); set => Set(value); }

        public bool ShowUnreadOnly
        {
            get => Get<bool>();
            set
            {
                Set(value);
                _inboxPageIndex = 0;
                RefreshMessages();
            }
        }

        public string InboxPageLabel { get => Get<string>(); set => Set(value); }
        public bool IsPrevPageEnabled { get => Get<bool>(); set => Set(value); }
        public bool IsNextPageEnabled { get => Get<bool>(); set => Set(value); }

        public bool IsInActiveCall { get => Get<bool>(); set => Set(value); }
        public string ActiveCallLabel { get => Get<string>(); set => Set(value); }
        public bool HasIncomingCall { get => Get<bool>(); set => Set(value); }
        public string IncomingCallLabel { get => Get<string>(); set => Set(value); }
        public bool HasOutgoingCall { get => Get<bool>(); set => Set(value); }
        public string OutgoingCallLabel { get => Get<string>(); set => Set(value); }
        public bool IsContactsListVisible { get => Get<bool>(); set => Set(value); }

        public GuiBindingList<string> OnlinePlayerNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<GuiColor> OnlinePlayerColors { get => Get<GuiBindingList<GuiColor>>(); set => Set(value); }
        public GuiBindingList<string> FavoriteNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<GuiColor> FavoriteStatusColors { get => Get<GuiBindingList<GuiColor>>(); set => Set(value); }
        public GuiBindingList<bool> FavoriteIsOnline { get => Get<GuiBindingList<bool>>(); set => Set(value); }

        public string ComposeRecipientLabel { get => Get<string>(); set => Set(value); }
        public string ComposeText { get => Get<string>(); set => Set(value); }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            ShowUnreadOnly = false;
            _inboxPageIndex = 0;
            ComposeText = string.Empty;
            ComposeRecipientLabel = "Select a contact to message.";

            WatchOnClient(model => model.ComposeText);

            SelectedTabId = MessagesTabId;
            WatchOnClient(model => model.TabToggleValue);
        }

        private void RefreshMessages()
        {
            var recipientId = GetObjectUUID(Player);

            var totalCount = HoloComMessaging.GetInboxCount(recipientId, ShowUnreadOnly);
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)InboxPageSize));
            _inboxPageIndex = Math.Clamp(_inboxPageIndex, 0, totalPages - 1);

            var messages = HoloComMessaging.GetInboxPage(recipientId, ShowUnreadOnly, _inboxPageIndex, InboxPageSize);
            var rows = messages.Select(message => new MessageRow(
                    message.Id,
                    // Strictly identity-key based: a message sent while disguised keeps
                    // the disguise identity forever, regardless of the sender's current
                    // state. Only staff (and the sender themselves) see the canonical name.
                    PlayerName.GetDisplayNameByIdentity(Player, message.SenderIdentityKey, message.SenderDescriptor, message.SenderFallbackName),
                    message.IsRead ? GuiColor.Grey : GuiColor.White,
                    new DateTime(message.SentDateTicks, DateTimeKind.Utc).ToLocalTime().ToString("MMM d, h:mm tt"),
                    BuildPreview(message.Text),
                    message.IsRead))
                .ToList();

            _messageRows = MessagesTable.Refresh(this, rows);

            InboxPageLabel = $"Page {_inboxPageIndex + 1} of {totalPages}";
            IsPrevPageEnabled = _inboxPageIndex > 0;
            IsNextPageEnabled = _inboxPageIndex < totalPages - 1;
        }

        private static string BuildPreview(string text)
        {
            var singleLine = text.Replace("\n", " ").Replace("\r", "");
            return singleLine.Length > PreviewLength ? singleLine.Substring(0, PreviewLength) + "..." : singleLine;
        }

        public Action OnClickRefreshMessages() => () => RefreshMessages();

        public Action OnClickPlayMessage() => () =>
        {
            var index = NuiGetEventArrayIndex();
            var row = _messageRows[index];

            var error = HoloComMessaging.PlayMessage(Player, row.Id);
            if (!string.IsNullOrWhiteSpace(error))
                SendMessageToPC(Player, ColorToken.Red(error));

            RefreshMessages();
        };

        public Action OnClickMarkReadRow() => () =>
        {
            var index = NuiGetEventArrayIndex();
            HoloComMessaging.MarkRead(_messageRows[index].Id);
            RefreshMessages();
        };

        public Action OnClickDeleteRead() => () =>
        {
            ShowModal("Delete all read messages? This cannot be undone.",
                () =>
                {
                    HoloComMessaging.DeleteAllRead(GetObjectUUID(Player));
                    RefreshMessages();
                    RestoreContentPartial();
                },
                RestoreContentPartial);
        };

        public Action OnClickPrevPage() => () =>
        {
            if (_inboxPageIndex > 0)
                _inboxPageIndex--;

            RefreshMessages();
        };

        public Action OnClickNextPage() => () =>
        {
            _inboxPageIndex++;
            RefreshMessages();
        };

        public void Refresh(HoloComMessageReceivedRefreshEvent payload)
        {
            RefreshMessages();
        }

        public void Refresh(HoloComCallStateChangedRefreshEvent payload)
        {
            // Updates the call-state banner binds (and contact lists when no call is
            // active). Only binds change - no partial swap - so this is safe regardless
            // of which tab or screen the player currently has open.
            RefreshContacts();
        }

        private void RefreshContacts()
        {
            if (HoloCom.IsInCall(Player))
            {
                var target = HoloCom.GetTargetForActiveCall(Player);
                IsInActiveCall = true;
                ActiveCallLabel = $"In a call with {PlayerName.GetDisplayName(Player, target)}";
            }
            else
            {
                IsInActiveCall = false;
            }

            if (HoloCom.IsCallReceiver(Player) && !HoloCom.IsInCall(Player))
            {
                var callSender = HoloCom.GetCallSender(Player);
                HasIncomingCall = true;
                IncomingCallLabel = $"Incoming call from {PlayerName.GetDisplayName(Player, callSender)}";
            }
            else
            {
                HasIncomingCall = false;
            }

            if (HoloCom.IsCallSender(Player) && !HoloCom.IsInCall(Player))
            {
                var callReceiver = HoloCom.GetCallReceiver(Player);
                HasOutgoingCall = true;
                OutgoingCallLabel = GetIsObjectValid(callReceiver)
                    ? $"Calling {PlayerName.GetDisplayName(Player, callReceiver)}..."
                    : "Calling...";
            }
            else
            {
                HasOutgoingCall = false;
            }

            IsContactsListVisible = !IsInActiveCall && !HasIncomingCall && !HasOutgoingCall;

            if (IsContactsListVisible)
            {
                RefreshOnlinePlayers();
                RefreshFavorites();
            }
        }

        private void RefreshOnlinePlayers()
        {
            _onlinePlayerObjects = HoloCom.GetCallableOnlinePlayers(Player, IsTestingModeEnabled).ToList();

            var rows = _onlinePlayerObjects
                .Select(pc => new OnlinePlayerRow(
                    GetObjectUUID(pc),
                    PlayerName.GetDisplayName(Player, pc),
                    HoloCom.IsInCall(pc) ? GuiColor.Red : GuiColor.White))
                .ToList();

            OnlinePlayersTable.Refresh(this, rows);
        }

        private void RefreshFavorites()
        {
            var favoriteIds = HoloCom.GetFavoritePlayerIds(Player);
            var rows = new List<FavoriteRow>();

            foreach (var favoriteId in favoriteIds)
            {
                var onlineObject = HoloCom.FindOnlinePlayerByPlayerId(favoriteId);
                var isOnline = GetIsObjectValid(onlineObject);
                var fallbackName = DB.Get<Player>(favoriteId)?.Name ?? string.Empty;

                var displayName = isOnline
                    ? PlayerName.GetDisplayName(Player, onlineObject)
                    : PlayerName.GetDisplayNameByPlayerId(Player, favoriteId, fallbackName);

                rows.Add(new FavoriteRow(favoriteId, displayName, isOnline ? GuiColor.Green : GuiColor.Grey, isOnline));
            }

            _favoriteRows = FavoritesTable.Refresh(this, rows);
        }

        public Action OnClickRefreshContacts() => () => RefreshContacts();

        public Action OnClickEndCall() => () =>
        {
            var target = HoloCom.GetTargetForActiveCall(Player);
            HoloCom.SetIsInCall(Player, target, false);
            RefreshContacts();
        };

        public Action OnClickAnswerCall() => () =>
        {
            var callSender = HoloCom.GetCallSender(Player);
            HoloCom.SetIsInCall(Player, callSender, true);
            RefreshContacts();
        };

        public Action OnClickDeclineCall() => () =>
        {
            var callSender = HoloCom.GetCallSender(Player);
            SendMessageToPC(callSender, "Your HoloCom call was declined.");
            HoloCom.CleanupCallAttempt(callSender, Player);
            RefreshContacts();
        };

        public Action OnClickCancelOutgoingCall() => () =>
        {
            var callReceiver = HoloCom.GetCallReceiver(Player);
            if (GetIsObjectValid(callReceiver))
                SendMessageToPC(callReceiver, "Your HoloCom stops buzzing.");

            HoloCom.CleanupCallAttempt(Player, callReceiver);
            SendMessageToPC(Player, "You cancel your HoloCom call.");
            RefreshContacts();
        };

        public Action OnClickCallOnline() => () =>
        {
            var index = NuiGetEventArrayIndex();
            var target = _onlinePlayerObjects[index];
            var displayName = PlayerName.GetDisplayName(Player, target);

            ShowModal($"Call {displayName}?",
                () =>
                {
                    HoloCom.InitiateCall(Player, target);
                    RefreshContacts();
                    RestoreContentPartial();
                },
                RestoreContentPartial);
        };

        public Action OnClickMessageOnline() => () =>
        {
            var index = NuiGetEventArrayIndex();
            var target = _onlinePlayerObjects[index];
            SetComposeTarget(GetObjectUUID(target), PlayerName.GetDisplayName(Player, target));
        };

        public Action OnClickFavoriteOnline() => () =>
        {
            var index = NuiGetEventArrayIndex();
            var target = _onlinePlayerObjects[index];
            var error = HoloCom.AddFavorite(Player, GetObjectUUID(target), IsTestingModeEnabled);
            if (!string.IsNullOrWhiteSpace(error))
                SendMessageToPC(Player, ColorToken.Red(error));

            RefreshFavorites();
        };

        public Action OnClickCallFavorite() => () =>
        {
            var index = NuiGetEventArrayIndex();
            var row = _favoriteRows[index];
            var target = HoloCom.FindOnlinePlayerByPlayerId(row.PlayerId);
            if (!GetIsObjectValid(target))
            {
                SendMessageToPC(Player, "That player is not currently online.");
                return;
            }

            ShowModal($"Call {row.DisplayName}?",
                () =>
                {
                    HoloCom.InitiateCall(Player, target);
                    RefreshContacts();
                    RestoreContentPartial();
                },
                RestoreContentPartial);
        };

        public Action OnClickMessageFavorite() => () =>
        {
            var index = NuiGetEventArrayIndex();
            var row = _favoriteRows[index];
            SetComposeTarget(row.PlayerId, row.DisplayName);
        };

        public Action OnClickRemoveFavorite() => () =>
        {
            var index = NuiGetEventArrayIndex();
            var row = _favoriteRows[index];

            ShowModal($"Remove {row.DisplayName} from your favorites?",
                () =>
                {
                    HoloCom.RemoveFavorite(Player, row.PlayerId);
                    RefreshFavorites();
                    RestoreContentPartial();
                },
                RestoreContentPartial);
        };

        private void SetComposeTarget(string playerId, string displayName)
        {
            _composeRecipientId = playerId;
            _composeRecipientName = displayName;
            ComposeRecipientLabel = $"To: {displayName}";
            ComposeText = string.Empty;
            _currentPartial = ComposePartial;
            _currentPartialRefresh = null;
            SwapNestedPartialView(TabContentPartialElement, ComposePartial, null);
        }

        public Action OnClickSend() => () =>
        {
            ShowModal($"Send this message to {_composeRecipientName}?",
                () =>
                {
                    var error = HoloComMessaging.SendMessage(Player, _composeRecipientId, ComposeText, IsTestingModeEnabled);
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        SendMessageToPC(Player, ColorToken.Red(error));
                        // Stay on the Compose screen with the typed text intact.
                        RestoreContentPartial();
                        return;
                    }

                    SendMessageToPC(Player, "Message sent.");
                    ReturnToContacts();
                },
                RestoreContentPartial);
        };

        public Action OnClickClearCompose() => () => { ComposeText = string.Empty; };

        public Action OnClickComposeBack() => () => ReturnToContacts();

        private void ReturnToContacts()
        {
            _currentPartial = ContactsTabPartial;
            _currentPartialRefresh = RefreshContacts;
            SwapNestedPartialView(TabContentPartialElement, ContactsTabPartial, () => RefreshContacts());
        }
    }
}
