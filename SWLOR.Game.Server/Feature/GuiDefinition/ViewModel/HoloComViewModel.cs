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

        private const int InboxPageSize = 15;

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
            public bool IsRead { get; }
            public bool IsSaved { get; }

            public MessageRow(string id, string senderName, GuiColor rowColor, string timestamp, bool isRead, bool isSaved)
            {
                Id = id;
                SenderName = senderName;
                RowColor = rowColor;
                Timestamp = timestamp;
                IsRead = isRead;
                IsSaved = isSaved;
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
                .Column((m, v) => m.MessageSaveLabels = v, r => r.IsSaved ? "Unsave" : "Save")
                .Column((m, v) => m.MessageCanDelete = v, r => !r.IsSaved);

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
        public GuiBindingList<string> MessageSaveLabels { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> MessageCanDelete { get => Get<GuiBindingList<bool>>(); set => Set(value); }

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

        public string CallStatusLabel { get => Get<string>(); set => Set(value); }
        public bool IsAnswerEnabled { get => Get<bool>(); set => Set(value); }
        public bool IsDeclineEndEnabled { get => Get<bool>(); set => Set(value); }

        public GuiBindingList<string> OnlinePlayerNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<GuiColor> OnlinePlayerColors { get => Get<GuiBindingList<GuiColor>>(); set => Set(value); }
        public GuiBindingList<string> FavoriteNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<GuiColor> FavoriteStatusColors { get => Get<GuiBindingList<GuiColor>>(); set => Set(value); }
        public GuiBindingList<bool> FavoriteIsOnline { get => Get<GuiBindingList<bool>>(); set => Set(value); }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            ShowUnreadOnly = false;
            WatchOnClient(model => model.ShowUnreadOnly);
            _inboxPageIndex = 0;

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
                    message.IsRead,
                    message.IsSaved))
                .ToList();

            _messageRows = MessagesTable.Refresh(this, rows);

            InboxPageLabel = $"Page {_inboxPageIndex + 1} of {totalPages}";
            IsPrevPageEnabled = _inboxPageIndex > 0;
            IsNextPageEnabled = _inboxPageIndex < totalPages - 1;
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

        public Action OnClickDeleteRow() => () =>
        {
            var index = NuiGetEventArrayIndex();
            var row = _messageRows[index];

            ShowModal($"Delete this message from {row.SenderName}? This cannot be undone.",
                () =>
                {
                    var error = HoloComMessaging.DeleteMessage(GetObjectUUID(Player), row.Id);
                    if (!string.IsNullOrWhiteSpace(error))
                        SendMessageToPC(Player, ColorToken.Red(error));

                    RefreshMessages();
                    RestoreContentPartial();
                },
                RestoreContentPartial);
        };

        public Action OnClickToggleSaveRow() => () =>
        {
            var index = NuiGetEventArrayIndex();
            var error = HoloComMessaging.ToggleSaved(GetObjectUUID(Player), _messageRows[index].Id);
            if (!string.IsNullOrWhiteSpace(error))
                SendMessageToPC(Player, ColorToken.Red(error));

            RefreshMessages();
        };

        public Action OnClickDeleteRead() => () =>
        {
            ShowModal("Delete all read messages? Saved messages are kept. This cannot be undone.",
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
            var isInCall = HoloCom.IsInCall(Player);
            var hasIncomingCall = !isInCall && HoloCom.IsCallReceiver(Player);
            var hasOutgoingCall = !isInCall && !hasIncomingCall && HoloCom.IsCallSender(Player);

            if (isInCall)
            {
                var target = HoloCom.GetTargetForActiveCall(Player);
                CallStatusLabel = GetIsObjectValid(target)
                    ? $"In a call with {PlayerName.GetDisplayName(Player, target)}"
                    : "In a call";
            }
            else if (hasIncomingCall)
            {
                var callSender = HoloCom.GetCallSender(Player);
                CallStatusLabel = GetIsObjectValid(callSender)
                    ? $"Incoming call from {PlayerName.GetDisplayName(Player, callSender)}"
                    : "Incoming call";
            }
            else if (hasOutgoingCall)
            {
                var callReceiver = HoloCom.GetCallReceiver(Player);
                CallStatusLabel = GetIsObjectValid(callReceiver)
                    ? $"Calling {PlayerName.GetDisplayName(Player, callReceiver)}..."
                    : "Calling...";
            }
            else
            {
                CallStatusLabel = "No active call.";
            }

            IsAnswerEnabled = hasIncomingCall;
            IsDeclineEndEnabled = isInCall || hasIncomingCall || hasOutgoingCall;

            RefreshOnlinePlayers();
            RefreshFavorites();
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

        public Action OnClickAnswerCall() => () =>
        {
            var callSender = HoloCom.GetCallSender(Player);

            // Guard against stale UI state: only connect when a live incoming call
            // attempt actually exists, otherwise answering would wedge the player
            // in a phantom call with an invalid partner.
            if (!HoloCom.IsCallReceiver(Player) || HoloCom.IsInCall(Player) || !GetIsObjectValid(callSender))
            {
                SendMessageToPC(Player, "You don't have an incoming call to answer.");
                RefreshContacts();
                return;
            }

            HoloCom.SetIsInCall(Player, callSender, true);
            RefreshContacts();
        };

        public Action OnClickDeclineEndCall() => () =>
        {
            HoloCom.EndOrDeclineCall(Player);
            RefreshContacts();
        };

        /// <summary>
        /// The contact lists stay interactive during calls, so call attempts need a
        /// pre-modal guard. HoloCom.InitiateCall enforces the same rule server-side;
        /// this just rejects before the confirm modal instead of after it.
        /// </summary>
        private bool IsBusyWithCall()
        {
            if (HoloCom.IsInCall(Player) || HoloCom.IsCallSender(Player) || HoloCom.IsCallReceiver(Player))
            {
                SendMessageToPC(Player, "You are already in a call.");
                return true;
            }

            return false;
        }

        public Action OnClickCallOnline() => () =>
        {
            if (IsBusyWithCall())
                return;

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
            OpenComposeModal(GetObjectUUID(target), PlayerName.GetDisplayName(Player, target), string.Empty);
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
            if (IsBusyWithCall())
                return;

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
            OpenComposeModal(row.PlayerId, row.DisplayName, string.Empty);
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

        private void OpenComposeModal(string recipientId, string recipientName, string initialText)
        {
            _composeRecipientId = recipientId;
            _composeRecipientName = recipientName;
            ShowInputModal($"Message to {recipientName}:", initialText, SendComposedMessage, RestoreContentPartial);
        }

        private void SendComposedMessage()
        {
            var error = HoloComMessaging.SendMessage(Player, _composeRecipientId, ModalInputText, IsTestingModeEnabled);
            if (!string.IsNullOrWhiteSpace(error))
            {
                SendMessageToPC(Player, ColorToken.Red(error));
                // Re-open the composer with the typed text intact so nothing is lost.
                OpenComposeModal(_composeRecipientId, _composeRecipientName, ModalInputText);
                return;
            }

            SendMessageToPC(Player, "Message sent.");
            RestoreContentPartial();
        }
    }
}
