using System.Globalization;
using Discord;
using Discord.Webhook;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;
using Color = System.Drawing.Color;

namespace SWLOR.Component.Communication.UI.ViewModel
{
    public class BugReportViewModel: GuiViewModelBase<BugReportViewModel, IGuiPayload>
    {
        private readonly IAppSettings _appSettings;
        private readonly IDiscordNotificationService _discord;

        public BugReportViewModel(
            IGuiService guiService, 
            IAppSettings appSettings,
            IDiscordNotificationService discord) 
            : base(guiService)
        {
            _appSettings = appSettings;
            _discord = discord;
        }

        public const int MaxBugReportLength = 1000;

        protected override void Initialize(IGuiPayload initialPayload)
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

        public Action OnClickSubmit() => () =>
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

            var url = Environment.GetEnvironmentVariable("SWLOR_BUG_WEBHOOK_URL");

            if (string.IsNullOrWhiteSpace(url))
            {
                SendMessageToPC(Player, ColorToken.Red("ERROR: Unable to send bug report because server admin has not specified the 'SWLOR_BUG_WEBHOOK_HOST' and 'SWLOR_BUG_WEBHOOK_PATH' environment variables."));
                return;
            }

            var authorName = $"{GetName(Player)} ({GetPCPlayerName(Player)}) [{GetPCPublicCDKey(Player)}]";
            var areaName = GetName(area);
            var areaTag = GetTag(area);
            var areaResref = GetResRef(area);
            var positionGroup = $"({position.X}, {position.Y}, {position.X})";
            var dateReported = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");
            var playerId = GetObjectUUID(Player);
            var nextReportAllowed = DateTime.UtcNow.AddMinutes(1);
            var title = _appSettings.ServerEnvironment == ServerEnvironmentType.Test
                ? "Bug Report [TEST SERVER]"
                : "Bug Report";

            

            Task.Run(async () =>
            {
                using (var client = new DiscordWebhookClient(url))
                {
                    var embed = new EmbedBuilder
                    {
                        Title = title,
                        Description = message,
                        Author = new EmbedAuthorBuilder
                        {
                            Name = authorName
                        },
                        Color = Color.Red,
                        Fields = new List<EmbedFieldBuilder>
                        {
                            new()
                            {
                                IsInline = true,
                                Name = "Area Name",
                                Value = areaName
                            },
                            new()
                            {
                                IsInline = true,
                                Name = "Area Tag",
                                Value = areaTag
                            },
                            new()
                            {
                                IsInline = true,
                                Name = "Area Resref",
                                Value = areaResref
                            },
                            new()
                            {
                                IsInline = true,
                                Name = "Position",
                                Value = positionGroup
                            },
                            new()
                            {
                                IsInline = true,
                                Name = "Date Reported",
                                Value = dateReported,
                            },
                            new()
                            {
                                IsInline = true,
                                Name = "Player ID",
                                Value = playerId
                            },
                        }
                    };


                    await client.SendMessageAsync(
                        string.Empty, 
                        embeds: new[] { embed.Build() },
                        threadName: title);
                }
            });

            SetLocalString(Player, "BUG_REPORT_LAST_SUBMISSION", nextReportAllowed.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            SendMessageToPC(Player, "Bug report submitted! Thank you for your report.");
            SendMessageToPC(Player, "Submitted Bug Report: " + BugReportText);
            _guiService.TogglePlayerWindow(Player, GuiWindowType.BugReport);
        };

        public Action OnClickCancel() => () =>
        {
            _guiService.TogglePlayerWindow(Player, GuiWindowType.BugReport);
        };
    }
}
