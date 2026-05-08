using System;
using System.Globalization;
using System.Threading.Tasks;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class BugReportViewModel: GuiViewModelBase<BugReportViewModel, GuiPayloadBase>
    {
        public const int MaxBugReportLength = 1000;
        private const int MaxDiscordThreadNameLength = 100;
        private static readonly ApplicationSettings _appSettings = ApplicationSettings.Get();

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            BugReportText = string.Empty;
            WatchOnClient(model => model.BugReportText);
        }        
        public string BugReportText
        {
            get => Get<string>();
            set
            {
                Set(value);
            }
        }

        public Action OnClickSubmit() => async () =>
        {
            if (string.IsNullOrWhiteSpace(BugReportText))
            {
                return;
            }

            var message = BugReportText;

            if (message.Length > 1000)
            {
                SendMessageToPC(Player, "Your message was too long. Please shorten it to no longer than 1000 characters and resubmit the bug. For reference, your message was: \"" + message + "\"");
                return;
            }
            var area = GetArea(Player);
            var position = GetPosition(Player);

            var discordWebhookUrl = Environment.GetEnvironmentVariable("SWLOR_BUG_DISCORD_WEBHOOK_URL");

            if (string.IsNullOrWhiteSpace(discordWebhookUrl))
            {
                SendMessageToPC(Player, ColorToken.Red("ERROR: Unable to send bug report because the server admin has not set SWLOR_BUG_DISCORD_WEBHOOK_URL."));
                return;
            }

            var authorName = $"{GetName(Player)} ({GetPCPlayerName(Player)}) [{GetPCPublicCDKey(Player)}]";
            var areaName = GetName(area);
            var areaTag = GetTag(area);
            var areaResref = GetResRef(area);
            var positionGroup = $"({position.X}, {position.Y}, {position.Z})";
            var dateReported = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            var playerId = GetObjectUUID(Player);
            var nextReportAllowed = DateTime.UtcNow.AddMinutes(1);
            if (!await SubmitBugReportToDiscord(
                discordWebhookUrl,
                message,
                authorName,
                areaName,
                areaTag,
                areaResref,
                positionGroup,
                dateReported,
                playerId))
            {
                SendMessageToPC(Player, ColorToken.Red("ERROR: Unable to queue bug report. Please notify a DM."));
                return;
            }

            SetLocalString(Player, "BUG_REPORT_LAST_SUBMISSION", nextReportAllowed.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            SendMessageToPC(Player, "Bug report submitted! Thank you for your report.");
            SendMessageToPC(Player, "Submitted Bug Report: " + BugReportText);
            Gui.TogglePlayerWindow(Player, GuiWindowType.BugReport);
        };


        private Task<bool> SubmitBugReportToDiscord(
            string discordWebhookUrl,
            string message,
            string authorName,
            string areaName,
            string areaTag,
            string areaResref,
            string positionGroup,
            string dateReported,
            string playerId)
        {
            var titlePrefix = _appSettings.ServerEnvironment switch
            {
                ServerEnvironmentType.Test => "[TEST] ",
                ServerEnvironmentType.Development => "[DEV] ",
                _ => string.Empty
            };
            var title = $"{titlePrefix}Bug Report";
            var environmentName = _appSettings.ServerEnvironment.ToString();
            var body = $"{message}\n\n---\n" +
                       $"**Server Environment**: {environmentName}\n" +
                       $"**Reporter**: {authorName}\n" +
                       $"**Area Name**: {areaName}\n" +
                       $"**Area Tag**: {areaTag}\n" +
                       $"**Area Resref**: {areaResref}\n" +
                       $"**Position**: {positionGroup}\n" +
                       $"**Date Reported (UTC)**: {dateReported}\n" +
                       $"**Player ID**: {playerId}";

            return BackgroundJob.EnqueueDiscordWebhook(
                discordWebhookUrl,
                authorName,
                body,
                15158332,
                title,
                createThread: true,
                threadName: CreateDiscordThreadName(title));
        }

        private static string CreateDiscordThreadName(string title)
        {
            if (title.Length <= MaxDiscordThreadNameLength)
            {
                return title;
            }

            return title.Substring(0, MaxDiscordThreadNameLength);
        }

        public Action OnClickCancel() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.BugReport);
        };
    }
}
