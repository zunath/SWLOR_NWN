using Discord;
using Discord.Webhook;
using SWLOR.Shared.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Abstractions.Models;
using Color = System.Drawing.Color;

namespace SWLOR.Shared.Core.Service
{
    public class DiscordNotificationService: IDiscordNotificationService
    {
        private readonly IAppSettings _appSettings;
        public DiscordNotificationService(
            IAppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        public void PublishMessage(
            string author, 
            string message, 
            Color color, 
            DiscordNotificationType type,
            string title = null,
            List<DiscordNotificationField> fields = null)
        {
            string url;

            switch (type)
            {
                case DiscordNotificationType.DMShout:
                    url = _appSettings.DMShoutWebHookUrl;
                    break;
                case DiscordNotificationType.Bug:
                    url = _appSettings.BugWebHookUrl;
                    break;
                case DiscordNotificationType.Holonet:
                    url = _appSettings.HolonetWebHookUrl;
                    break;
                default:
                    return;
            }

            Task.Run(async () =>
            {
                using (var client = new DiscordWebhookClient(url))
                {
                    var embed = new EmbedBuilder
                    {
                        Author = new EmbedAuthorBuilder
                        {
                            Name = author
                        },
                        Description = message,
                        Color = new Discord.Color(color.R, color.G, color.B)
                    };

                    if (fields != null)
                    {
                        foreach (var field in fields)
                        {
                            embed.Fields.Add(new EmbedFieldBuilder
                            {
                                IsInline = field.IsInline,
                                Name = field.Name,
                                Value = field.Value
                            });
                        }
                    }

                    await client.SendMessageAsync(
                        string.Empty, 
                        embeds: new[] { embed.Build() },
                        threadName: title);
                }
            });
        }
    }
}
