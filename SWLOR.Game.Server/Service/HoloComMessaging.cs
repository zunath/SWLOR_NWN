using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    public static class HoloComMessaging
    {
        public const int MaxMessageLength = 1000;

        private const string MessageLastSubmission = "HOLOCOM_MESSAGE_LAST_SUBMISSION";
        private const int MessageCooldownSeconds = 10;

        public static string SendMessage(uint sender, string recipientPlayerId, string rawText, bool allowSelfMessage = false)
        {
            var senderId = GetObjectUUID(sender);

            if (string.IsNullOrWhiteSpace(recipientPlayerId))
                return "Select a recipient first.";

            if (recipientPlayerId == senderId && !allowSelfMessage)
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
                SenderSnapshotJson = JsonDump(ObjectToJson(sender, false))
            };

            DB.Set(message);
            SetLocalString(sender, MessageLastSubmission, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));

            var recipientObject = HoloCom.FindOnlinePlayerByPlayerId(recipientPlayerId);
            if (GetIsObjectValid(recipientObject))
            {
                SendMessageToPC(recipientObject, "You have received a new HoloCom message.");
                Gui.PublishRefreshEvent(recipientObject, new HoloComMessageReceivedRefreshEvent());
            }

            return string.Empty;
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

        public static void MarkRead(string messageId)
        {
            var message = DB.Get<HoloComMessage>(messageId);
            if (message == null || message.IsRead)
                return;

            message.IsRead = true;
            DB.Set(message);
        }

        /// <summary>
        /// Deletes every read message belonging to this recipient. Unlike a mutate-in-place
        /// scan, deleting shrinks the underlying result set, so re-querying at offset 0 each
        /// pass is what correctly walks the whole remaining set instead of skipping rows.
        /// </summary>
        public static int DeleteAllRead(string recipientPlayerId)
        {
            const int PageSize = 50;
            var removed = 0;

            while (true)
            {
                var page = DB.Search(new DBQuery<HoloComMessage>()
                        .AddFieldSearch(nameof(HoloComMessage.RecipientPlayerId), recipientPlayerId, false)
                        .AddFieldSearch(nameof(HoloComMessage.IsRead), true)
                        .AddPaging(PageSize, 0))
                    .ToList();

                foreach (var message in page)
                    DB.Delete<HoloComMessage>(message.Id);

                removed += page.Count;

                if (page.Count < PageSize)
                    break;
            }

            return removed;
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
            if (message == null || string.IsNullOrWhiteSpace(message.SenderSnapshotJson))
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
                    AssignCommand(hologram, () => ActionPlayAnimation(animation, 1f, sentenceSeconds));
                    AssignCommand(hologram, () => ActionSpeakString(spokenSentence, TalkVolume.Talk));
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
