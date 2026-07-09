using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service
{
    public static class HoloComMessaging
    {
        public const int MaxMessageLength = 1000;

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

            var message = new HoloComMessage
            {
                SenderPlayerId = senderId,
                SenderFallbackName = GetName(sender),
                RecipientPlayerId = recipientPlayerId,
                Text = SanitizeMessageText(rawText),
                IsRead = false,
                SenderSnapshotJson = JsonDump(ObjectToJson(sender, false))
            };

            DB.Set(message);

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
        /// speak the stored message text, mirroring the old two-way call system's hologram
        /// behavior. The sender's appearance was snapshotted to JSON at send time (while they
        /// were online), so this works even if the sender is offline or no longer exists.
        /// </summary>
        public static bool PlayMessage(uint recipient, string messageId)
        {
            var message = DB.Get<HoloComMessage>(messageId);
            if (message == null || string.IsNullOrWhiteSpace(message.SenderSnapshotJson))
                return false;

            var location = BiowareVector.MoveLocation(GetLocation(recipient), GetFacing(recipient), 2.0f, 180);
            var hologram = JsonToObject(JsonParse(message.SenderSnapshotJson), location, bLoadObjectState: false);

            if (!GetIsObjectValid(hologram))
                return false;

            SetName(hologram, "HoloCom Recording");
            SetPlotFlag(hologram, true);
            ApplyEffectToObject(DurationType.Instant, EffectHeal(GetMaxHitPoints(hologram)), hologram);
            ApplyEffectToObject(DurationType.Permanent, EffectVisualEffect(VisualEffect.Vfx_Dur_Ghostly_Visage_No_Sound, false), hologram);

            AssignCommand(recipient, () => PlaySound("hologram_on"));
            AssignCommand(hologram, () => ClearAllActions());
            AssignCommand(hologram, () => ActionSpeakString(message.Text, TalkVolume.Talk));

            var speakSeconds = EstimatePlaybackSeconds(message.Text);
            DelayCommand(speakSeconds, () =>
            {
                if (GetIsObjectValid(hologram))
                    DestroyObject(hologram);

                AssignCommand(recipient, () => PlaySound("hologram_off"));
            });

            if (!message.IsRead)
            {
                message.IsRead = true;
                DB.Set(message);
            }

            return true;
        }

        private static float EstimatePlaybackSeconds(string text)
        {
            var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            return Math.Clamp(wordCount / 2.5f + 1.5f, 3f, 30f);
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
