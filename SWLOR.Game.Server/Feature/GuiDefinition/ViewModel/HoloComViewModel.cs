using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class HoloComViewModel: GuiViewModelBase<HoloComViewModel, GuiPayloadBase>,
        IGuiRefreshable<HoloComMessageReceivedRefreshEvent>,
        IGuiRefreshable<HoloComCallStateChangedRefreshEvent>
    {
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
            public string IdentityKey { get; }
            public string DisplayName { get; }
            public GuiColor StatusColor { get; }
            public bool IsOnline { get; }
            public bool CanMessage { get; }

            public FavoriteRow(string identityKey, string displayName, GuiColor statusColor, bool isOnline, bool canMessage)
            {
                IdentityKey = identityKey;
                DisplayName = displayName;
                StatusColor = statusColor;
                IsOnline = isOnline;
                CanMessage = canMessage;
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
                .Column((m, v) => m.FavoriteIsOnline = v, r => r.IsOnline)
                .Column((m, v) => m.FavoriteCanMessage = v, r => r.CanMessage);

        private int _inboxPageIndex;
        private IList<MessageRow> _messageRows = new List<MessageRow>();
        private List<uint> _onlinePlayerObjects = new();
        private IList<FavoriteRow> _favoriteRows = new List<FavoriteRow>();
        private string _composeRecipientId;
        private string _composeRecipientName;

        public int SelectedTabId
        {
            get => Get<int>();
            set
            {
                Set(value);
                TabToggle.SyncTo(value, v => TabToggleValue = v);
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
        public string InboxRefreshNotice { get => Get<string>(); set => Set(value); }
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
        public GuiBindingList<bool> FavoriteCanMessage { get => Get<GuiBindingList<bool>>(); set => Set(value); }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            ShowUnreadOnly = false;
            WatchOnClient(model => model.ShowUnreadOnly);
            _inboxPageIndex = 0;
            InboxRefreshNotice = string.Empty;

            SelectedTabId = MessagesTabId;
            WatchOnClient(model => model.TabToggleValue);
        }

        protected override void OnModalClosedRestore()
        {
            Tabs.Select(this, TabContentPartialElement, SelectedTabId);
        }

        private void RefreshMessages()
        {
            InboxRefreshNotice = string.Empty;
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
                    PlayerName.GetPlainDisplayNameByIdentity(Player, message.SenderIdentityKey, message.SenderDescriptor, message.SenderFallbackName),
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
            if (!TryGetMessageRow(out var row))
                return;

            var error = HoloComMessaging.PlayMessage(Player, row.Id);
            if (!string.IsNullOrWhiteSpace(error))
                SendMessageToPC(Player, ColorToken.Red(error));

            RefreshMessages();
        };

        public Action OnClickDeleteRow() => () =>
        {
            if (!TryGetMessageRow(out var row))
                return;

            ShowModal($"Delete this message from {row.SenderName}? This cannot be undone.",
                () =>
                {
                    var error = HoloComMessaging.DeleteMessage(GetObjectUUID(Player), row.Id);
                    if (!string.IsNullOrWhiteSpace(error))
                        SendMessageToPC(Player, ColorToken.Red(error));
                });
        };

        public Action OnClickToggleSaveRow() => () =>
        {
            if (!TryGetMessageRow(out var row))
                return;

            var error = HoloComMessaging.ToggleSaved(GetObjectUUID(Player), row.Id);
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
                });
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
            // Keep the server-side row snapshot aligned with the IDs currently
            // rendered by the client. Replacing a newest-first page here could shift
            // an already-queued array-index click onto a different message.
            InboxRefreshNotice = "New message available. Select Refresh to update.";
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
                    ? $"In a call with {GetPlainLiveDisplayName(target)}"
                    : "In a call";
            }
            else if (hasIncomingCall)
            {
                var callSender = HoloCom.GetCallSender(Player);
                CallStatusLabel = GetIsObjectValid(callSender)
                    ? $"Incoming call from {GetPlainLiveDisplayName(callSender)}"
                    : "Incoming call";
            }
            else if (hasOutgoingCall)
            {
                var callReceiver = HoloCom.GetCallReceiver(Player);
                CallStatusLabel = GetIsObjectValid(callReceiver)
                    ? $"Calling {GetPlainLiveDisplayName(callReceiver)}..."
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
            _onlinePlayerObjects = HoloCom.GetCallableOnlinePlayers(Player).ToList();

            var rows = _onlinePlayerObjects
                .Select(pc => new OnlinePlayerRow(
                    GetObjectUUID(pc),
                    GetPlainLiveDisplayName(pc),
                    HoloCom.IsInCall(pc) || HoloCom.IsCallSender(pc) || HoloCom.IsCallReceiver(pc)
                        ? GuiColor.Red
                        : GuiColor.White))
                .ToList();

            OnlinePlayersTable.Refresh(this, rows);
        }

        private void RefreshFavorites()
        {
            var favorites = HoloCom.GetFavorites(Player);
            var rows = new List<FavoriteRow>();

            foreach (var favorite in favorites)
            {
                var onlineObject = HoloCom.FindOnlinePlayerByIdentityKey(favorite.IdentityKey);
                var isOnline = GetIsObjectValid(onlineObject);
                var displayName = isOnline
                    ? GetPlainLiveDisplayName(onlineObject)
                    : PlayerName.GetPlainDisplayNameByIdentity(
                        Player,
                        favorite.IdentityKey,
                        favorite.Descriptor,
                        favorite.FallbackName);
                var canMessage = isOnline || !Disguise.IsDisguiseIdentityKey(favorite.IdentityKey);

                rows.Add(new FavoriteRow(
                    favorite.IdentityKey,
                    displayName,
                    isOnline ? GuiColor.Green : GuiColor.Grey,
                    isOnline,
                    canMessage));
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

            if (!TryGetOnlinePlayer(out var target))
                return;

            var displayName = GetPlainLiveDisplayName(target);

            ShowModal($"Call {displayName}?",
                () =>
                {
                    HoloCom.InitiateCall(Player, target);
                });
        };

        public Action OnClickMessageOnline() => () =>
        {
            if (!TryGetOnlinePlayer(out var target))
                return;

            OpenComposeModal(GetObjectUUID(target), GetPlainLiveDisplayName(target), string.Empty);
        };

        public Action OnClickFavoriteOnline() => () =>
        {
            if (!TryGetOnlinePlayer(out var target))
                return;

            var error = HoloCom.AddFavorite(Player, target);
            if (!string.IsNullOrWhiteSpace(error))
                SendMessageToPC(Player, ColorToken.Red(error));

            RefreshFavorites();
        };

        public Action OnClickCallFavorite() => () =>
        {
            if (IsBusyWithCall())
                return;

            if (!TryGetFavoriteRow(out var row))
                return;

            var target = HoloCom.FindOnlinePlayerByIdentityKey(row.IdentityKey);
            if (!GetIsObjectValid(target))
            {
                SendMessageToPC(Player, "That player is not currently online.");
                return;
            }

            ShowModal($"Call {row.DisplayName}?",
                () =>
                {
                    HoloCom.InitiateCall(Player, target);
                });
        };

        public Action OnClickMessageFavorite() => () =>
        {
            if (!TryGetFavoriteRow(out var row))
                return;

            var target = HoloCom.FindOnlinePlayerByIdentityKey(row.IdentityKey);
            var recipientPlayerId = GetIsObjectValid(target)
                ? GetObjectUUID(target)
                : Disguise.IsDisguiseIdentityKey(row.IdentityKey)
                    ? string.Empty
                    : row.IdentityKey;

            if (string.IsNullOrWhiteSpace(recipientPlayerId))
            {
                SendMessageToPC(Player, "That disguised contact is no longer available under this identity.");
                RefreshFavorites();
                return;
            }

            OpenComposeModal(recipientPlayerId, row.DisplayName, string.Empty);
        };

        public Action OnClickRemoveFavorite() => () =>
        {
            if (!TryGetFavoriteRow(out var row))
                return;

            ShowModal($"Remove {row.DisplayName} from your favorites?",
                () =>
                {
                    HoloCom.RemoveFavorite(Player, row.IdentityKey);
                });
        };

        private void OpenComposeModal(string recipientId, string recipientName, string initialText)
        {
            _composeRecipientId = recipientId;
            _composeRecipientName = recipientName;
            ShowInputModal($"Message to {recipientName}:", initialText, SendComposedMessage);
        }

        private string GetPlainLiveDisplayName(uint target)
        {
            return UtilPlugin.StripColors(PlayerName.GetDisplayName(Player, target));
        }

        private bool TryGetMessageRow(out MessageRow row)
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _messageRows.Count)
            {
                row = null;
                return false;
            }

            row = _messageRows[index];
            return true;
        }

        private bool TryGetOnlinePlayer(out uint target)
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _onlinePlayerObjects.Count || !GetIsObjectValid(_onlinePlayerObjects[index]))
            {
                target = OBJECT_INVALID;
                SendMessageToPC(Player, "That player is no longer online.");
                RefreshContacts();
                return false;
            }

            target = _onlinePlayerObjects[index];
            return true;
        }

        private bool TryGetFavoriteRow(out FavoriteRow row)
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _favoriteRows.Count)
            {
                row = null;
                return false;
            }

            row = _favoriteRows[index];
            return true;
        }

        private void SendComposedMessage()
        {
            var error = HoloComMessaging.SendMessage(Player, _composeRecipientId, ModalInputText);
            if (!string.IsNullOrWhiteSpace(error))
            {
                SendMessageToPC(Player, ColorToken.Red(error));
                // The framework restores the selected tab after this callback. Re-open
                // the composer on the next tick so that restoration does not overwrite
                // the modal, and preserve the rejected text for correction.
                var rejectedText = ModalInputText;
                DelayCommand(0.1f, () => OpenComposeModal(_composeRecipientId, _composeRecipientName, rejectedText));
                return;
            }

            SendMessageToPC(Player, "Message sent.");
        }
    }
}
