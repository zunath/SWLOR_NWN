using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    public static class HoloComMessaging
    {
        public const int MaxMessageLength = 4000;

        private const string MessageLastSubmission = "HOLOCOM_MESSAGE_LAST_SUBMISSION";
        private const int MessageCooldownSeconds = 10;

        // Messages expire this many days after being sent unless the recipient has
        // saved them. Read messages are removed before unread ones so a player who
        // never checks their inbox loses new mail last.
        private const int MessageRetentionDays = 30;

        // A recipient may keep at most this many saved messages exempt from cleanup.
        public const int MaxSavedMessages = 20;

        public static string SendMessage(uint sender, string recipientPlayerId, string rawText)
        {
            var senderId = GetObjectUUID(sender);

            if (string.IsNullOrWhiteSpace(recipientPlayerId))
                return "Select a recipient first.";

            if (recipientPlayerId == senderId)
                return "You cannot send a message to yourself.";

            var validationError = ValidateMessageText(rawText);
            if (!string.IsNullOrWhiteSpace(validationError))
                return validationError;

            if (IsOnMessageCooldown(sender))
                return "You are sending messages too quickly. Try again in a few seconds.";

            // Everything an offline sender can no longer provide is captured now:
            // appearance snapshot, the identity as observers currently perceive it
            // (disguise-aware), its descriptor, and the active language.
            var message = new HoloComMessage
            {
                SenderPlayerId = senderId,
                SenderFallbackName = GetName(sender),
                SenderIdentityKey = Disguise.GetIdentityKey(sender),
                SenderDescriptor = Disguise.GetDisplayDescriptor(sender),
                SenderLanguage = (int)Language.GetActiveLanguage(sender),
                RecipientPlayerId = recipientPlayerId,
                Text = SanitizeMessageText(rawText),
                IsRead = false,
                ExpirationDateTicks = DateTime.UtcNow.AddDays(MessageRetentionDays).Ticks
            };

            DB.Set(message);
            CaptureSenderSnapshot(sender, message.Id);
            SetLocalString(sender, MessageLastSubmission, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));

            var recipientObject = HoloCom.FindOnlinePlayerByPlayerId(recipientPlayerId);
            if (GetIsObjectValid(recipientObject))
            {
                SendMessageToPC(recipientObject, "You have received a new HoloCom message.");
                Gui.PublishRefreshEvent(recipientObject, new HoloComMessageReceivedRefreshEvent());
            }

            return string.Empty;
        }

        /// <summary>
        /// Serializes a purged copy of the sender into the message's snapshot. Backpack
        /// items and gold only bloat the recording, so they are stripped from a temporary
        /// copy first; equipped gear is deliberately kept so the playback hologram keeps
        /// the sender's look. DestroyObject is deferred by the engine until the current
        /// script finishes, so serialization runs one tick later, after the purge has
        /// actually applied. The copy is destroyed only after the snapshot is persisted.
        /// </summary>
        private static void CaptureSenderSnapshot(uint sender, string messageId)
        {
            var copy = CopyObject(sender, GetLocation(sender));
            SetPlotFlag(copy, true);
            // Effects aren't serialized (bSaveObjectState: false), so hiding the copy
            // during its brief life doesn't leak into the snapshot.
            ApplyEffectToObject(DurationType.Temporary, EffectInvisibility(InvisibilityType.Normal), copy, 6f);

            TakeGoldFromCreature(GetGold(copy), copy, true);

            for (var item = GetFirstItemInInventory(copy); GetIsObjectValid(item); item = GetNextItemInInventory(copy))
            {
                SetDroppableFlag(item, false);
                DestroyObject(item);
            }

            DelayCommand(0.1f, () =>
            {
                if (!GetIsObjectValid(copy))
                    return;

                var message = DB.Get<HoloComMessage>(messageId);
                if (message != null)
                {
                    message.SenderSnapshotJson = JsonDump(ObjectToJson(copy, false));
                    DB.Set(message);
                }

                DestroyObject(copy);
            });
        }

        public static long GetInboxCount(string recipientPlayerId, bool unreadOnly)
        {
            var query = new DBQuery<HoloComMessage>()
                .AddFieldSearch(nameof(HoloComMessage.RecipientPlayerId), recipientPlayerId, false);

            if (unreadOnly)
                query.AddFieldSearch(nameof(HoloComMessage.IsRead), false);

            return DB.SearchCount(query);
        }

        public static List<HoloComMessage> GetInboxPage(string recipientPlayerId, bool unreadOnly, int pageIndex, int pageSize)
        {
            var query = new DBQuery<HoloComMessage>()
                .AddFieldSearch(nameof(HoloComMessage.RecipientPlayerId), recipientPlayerId, false);

            if (unreadOnly)
                query.AddFieldSearch(nameof(HoloComMessage.IsRead), false);

            query.OrderBy(nameof(HoloComMessage.SentDateTicks), false);
            query.AddPaging(pageSize, pageSize * pageIndex);

            return DB.Search(query).ToList();
        }

        /// <summary>
        /// Deletes a single message belonging to this recipient. Saved messages are
        /// protected - they must be unsaved before deletion. Read status is tracked
        /// automatically (playback marks a message read), so deletion is the only
        /// per-message action besides play and save.
        /// Returns an empty string on success or a user-facing error message.
        /// </summary>
        public static string DeleteMessage(string recipientPlayerId, string messageId)
        {
            var message = DB.Get<HoloComMessage>(messageId);
            if (message == null || message.RecipientPlayerId != recipientPlayerId)
                return "That message is no longer available.";

            if (message.IsSaved)
                return "Unsave the message before deleting it.";

            DB.Delete<HoloComMessage>(message.Id);
            return string.Empty;
        }

        /// <summary>
        /// Toggles whether the recipient has saved this message, exempting it from
        /// retention cleanup while saved. Saving is capped at <see cref="MaxSavedMessages"/>
        /// so a player cannot exempt their entire inbox from cleanup.
        /// Returns an empty string on success or a user-facing error message.
        /// </summary>
        public static string ToggleSaved(string recipientPlayerId, string messageId)
        {
            var message = DB.Get<HoloComMessage>(messageId);
            if (message == null || message.RecipientPlayerId != recipientPlayerId)
                return "That message is no longer available.";

            if (!message.IsSaved)
            {
                var savedCount = DB.SearchCount(new DBQuery<HoloComMessage>()
                    .AddFieldSearch(nameof(HoloComMessage.RecipientPlayerId), recipientPlayerId, false)
                    .AddFieldSearch(nameof(HoloComMessage.IsSaved), true));

                if (savedCount >= MaxSavedMessages)
                    return $"You may only save up to {MaxSavedMessages} messages.";
            }

            message.IsSaved = !message.IsSaved;
            DB.Set(message);

            return string.Empty;
        }

        /// <summary>
        /// Deletes every read message belonging to this recipient, except messages the
        /// recipient has saved. Deletions shrink the underlying result set while skipped
        /// (saved) messages stay in it, so each re-query offsets past the survivors
        /// counted so far instead of restarting at 0 or advancing a full page.
        /// </summary>
        public static int DeleteAllRead(string recipientPlayerId)
        {
            const int PageSize = 50;
            var removed = 0;
            var kept = 0;

            while (true)
            {
                var page = DB.Search(new DBQuery<HoloComMessage>()
                        .AddFieldSearch(nameof(HoloComMessage.RecipientPlayerId), recipientPlayerId, false)
                        .AddFieldSearch(nameof(HoloComMessage.IsRead), true)
                        .AddPaging(PageSize, kept))
                    .ToList();

                foreach (var message in page)
                {
                    if (message.IsSaved)
                    {
                        kept++;
                        continue;
                    }

                    DB.Delete<HoloComMessage>(message.Id);
                    removed++;
                }

                if (page.Count < PageSize)
                    break;
            }

            return removed;
        }

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void OnModuleLoad()
        {
            CleanUpExpiredMessages();
        }

        /// <summary>
        /// Deletes messages older than <see cref="MessageRetentionDays"/> days, skipping any
        /// message the recipient has saved. Read messages are queried and deleted before
        /// unread ones, so if a player has been away long enough that both are expiring,
        /// their still-unread mail is the last thing lost.
        /// The query itself can only filter by IsRead - the DB layer supports exact-match
        /// field searches, not numeric ranges, so IsSaved and the expiration date are both
        /// checked in memory once the candidates are loaded.
        /// </summary>
        private static void CleanUpExpiredMessages()
        {
            var now = DateTime.UtcNow.Ticks;
            var expiredRead = 0;
            var expiredUnread = 0;

            foreach (var isRead in new[] { true, false })
            {
                var query = new DBQuery<HoloComMessage>()
                    .AddFieldSearch(nameof(HoloComMessage.IsRead), isRead);
                var count = (int)DB.SearchCount(query);
                var messages = DB.Search(query.OrderBy(nameof(HoloComMessage.SentDateTicks), true).AddPaging(count, 0));

                foreach (var message in messages)
                {
                    if (message.IsSaved)
                        continue;

                    if (GetEffectiveExpirationTicks(message) > now)
                        continue;

                    DB.Delete<HoloComMessage>(message.Id);

                    if (isRead)
                        expiredRead++;
                    else
                        expiredUnread++;
                }
            }

            if (expiredRead > 0 || expiredUnread > 0)
            {
                Log.Write(LogGroup.Server, $"HoloCom retention cleanup removed {expiredRead} expired read message(s) and {expiredUnread} expired unread message(s).");
            }
        }

        /// <summary>
        /// Rows written before ExpirationDateTicks existed have it as 0, which must not be
        /// treated as "already expired". For those, the expiration is derived from the send
        /// date plus the retention window instead.
        /// </summary>
        private static long GetEffectiveExpirationTicks(HoloComMessage message)
        {
            return message.ExpirationDateTicks > 0
                ? message.ExpirationDateTicks
                : new DateTime(message.SentDateTicks, DateTimeKind.Utc).AddDays(MessageRetentionDays).Ticks;
        }

        /// <summary>
        /// Recreates a hologram of the message's sender at the recipient's location and has it
        /// speak the stored message text sentence by sentence, mirroring the live call system's
        /// hologram behavior. Everything needed was captured at send time (appearance snapshot,
        /// disguise identity, active language), so playback works with the sender offline.
        /// Returns an empty string on success or a user-facing error message.
        /// </summary>
        public static string PlayMessage(uint recipient, string messageId)
        {
            if (GetIsObjectValid(HoloCom.GetActivePlaybackHologram(recipient)))
                return "A recording is already playing. Wait for it to finish.";

            var message = DB.Get<HoloComMessage>(messageId);
            if (message == null ||
                message.RecipientPlayerId != GetObjectUUID(recipient) ||
                string.IsNullOrWhiteSpace(message.SenderSnapshotJson))
                return "This message's recording is no longer available.";

            var location = BiowareVector.MoveLocation(GetLocation(recipient), GetFacing(recipient), 2.0f, 180);
            var hologram = JsonToObject(JsonParse(message.SenderSnapshotJson), location, bLoadObjectState: false);

            if (!GetIsObjectValid(hologram))
                return "This message's recording is no longer available.";

            HoloCom.ConfigureHologram(hologram);
            HoloCom.SetActivePlaybackHologram(recipient, hologram);

            // Recordings speak the language the sender had active when the message was
            // composed. Non-player speakers are treated as fluent by the chat pipeline,
            // so listeners translate strictly by their own comprehension skill.
            Language.SetActiveLanguage(hologram, (SkillType)message.SenderLanguage);

            AssignCommand(recipient, () => PlaySound("hologram_on"));

            // Deserialized player copies are not reliably commandable, and actions
            // assigned to a non-commandable creature are silently dropped - the live
            // call relay forces commandable before every line for the same reason.
            // The initial delay lets the fresh copy finish initializing before actions
            // are queued; same-frame assignments get flushed by creature spawn-in.
            var offset = 0.5f;
            foreach (var sentence in SplitIntoSentences(message.Text))
            {
                var spokenSentence = sentence;
                var animation = Animation.LoopingTalkNormal;
                if (spokenSentence.Contains("!"))
                    animation = Animation.LoopingTalkForceful;
                if (spokenSentence.Contains("?"))
                    animation = Animation.LoopingTalkPleading;

                var sentenceSeconds = EstimatePlaybackSeconds(spokenSentence);

                DelayCommand(offset, () =>
                {
                    if (!GetIsObjectValid(hologram))
                        return;

                    SetCommandable(true, hologram);
                    AssignCommand(hologram, () => ClearAllActions());
                    AssignCommand(hologram, () => ActionSpeakString(spokenSentence, TalkVolume.Talk));
                    AssignCommand(hologram, () => ActionPlayAnimation(animation, 1f, sentenceSeconds));
                });

                offset += sentenceSeconds;
            }

            DelayCommand(offset + 0.5f, () =>
            {
                if (GetIsObjectValid(hologram))
                    DestroyObject(hologram);

                HoloCom.ClearActivePlaybackHologram(recipient);
                AssignCommand(recipient, () => PlaySound("hologram_off"));
            });

            if (!message.IsRead)
            {
                message.IsRead = true;
                DB.Set(message);
            }

            return string.Empty;
        }

        /// <summary>
        /// Splits message text into sentences on runs of '.', '!' and '?', keeping the
        /// terminator with its sentence. Text without terminators is a single sentence.
        /// </summary>
        public static List<string> SplitIntoSentences(string text)
        {
            var sentences = new List<string>();

            if (string.IsNullOrWhiteSpace(text))
                return sentences;

            var builder = new StringBuilder();

            for (var i = 0; i < text.Length; i++)
            {
                var character = text[i];
                builder.Append(character);

                var isTerminator = character == '.' || character == '!' || character == '?';
                var nextIsTerminator = i + 1 < text.Length &&
                                       (text[i + 1] == '.' || text[i + 1] == '!' || text[i + 1] == '?');

                if (isTerminator && !nextIsTerminator)
                {
                    var sentence = builder.ToString().Trim();
                    if (sentence.Length > 0)
                        sentences.Add(sentence);

                    builder.Clear();
                }
            }

            var remainder = builder.ToString().Trim();
            if (remainder.Length > 0)
                sentences.Add(remainder);

            return sentences;
        }

        private static float EstimatePlaybackSeconds(string text)
        {
            var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            return Math.Clamp(wordCount / 2.5f + 1.5f, 2f, 30f);
        }

        private static bool IsOnMessageCooldown(uint sender)
        {
            var lastSubmission = GetLocalString(sender, MessageLastSubmission);
            if (string.IsNullOrWhiteSpace(lastSubmission))
                return false;

            var lastSend = DateTime.ParseExact(lastSubmission, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            return DateTime.UtcNow <= lastSend.AddSeconds(MessageCooldownSeconds);
        }

        public static string ValidateMessageText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "Please enter a message.";

            if (ContainsColorToken(text))
                return "Messages may not contain color codes.";

            var sanitized = SanitizeMessageText(text);
            if (sanitized.Length > MaxMessageLength)
                return $"Messages may be no longer than {MaxMessageLength} characters.";

            return string.Empty;
        }

        private static bool ContainsColorToken(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return !string.Equals(text, UtilPlugin.StripColors(text), StringComparison.Ordinal);
        }

        private static string SanitizeMessageText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var stripped = UtilPlugin.StripColors(text).Trim();
            var builder = new StringBuilder();

            foreach (var character in stripped)
            {
                if (character != '\n' && character != '\r' && char.IsControl(character))
                    continue;

                builder.Append(character);
            }

            return builder.ToString();
        }
    }
}
