using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Webhook;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.ChatCommandService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class SystemChatCommand: IChatCommandListDefinition
    {
        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            var builder = new ChatCommandBuilder();

            BugCommand(builder);
            HelpCommand(builder);
            ListEmotesCommand(builder);

            return builder.Build();
        }

        private static void BugCommand(ChatCommandBuilder builder)
        {

            builder.Create("bug")
                .Description("Toggles the bug report window to submit bugs to the developers. Please include as much detail as possible.")
                .Permissions(AuthorizationLevel.All)
                .Validate((user, args) =>
                {
                    var lastSubmission = GetLocalString(user, "BUG_REPORT_LAST_SUBMISSION");
                    if (!string.IsNullOrWhiteSpace(lastSubmission))
                    {
                        var dateTime = DateTime.ParseExact(lastSubmission, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                        if (DateTime.UtcNow <= dateTime)
                        {
                            return "You may only submit one bug report per minute. Please wait and try again.";
                        }
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    Gui.TogglePlayerWindow(user, GuiWindowType.BugReport);
                    /*
                    var message = string.Empty;

                    foreach (var arg in args)
                    {
                        message += " " + arg;
                    }

                    if (message.Length > 1000)
                    {
                        SendMessageToPC(user, "Your message was too long. Please shorten it to no longer than 1000 characters and resubmit the bug. For reference, your message was: \"" + message + "\"");
                        return;
                    }
                    var area = GetArea(user);
                    var position = GetPosition(user);

                    var url = Environment.GetEnvironmentVariable("SWLOR_BUG_WEBHOOK_URL");
                    
                    if (string.IsNullOrWhiteSpace(url))
                    {
                        SendMessageToPC(user, ColorToken.Red("ERROR: Unable to send bug report because server admin has not specified the 'SWLOR_BUG_WEBHOOK_HOST' and 'SWLOR_BUG_WEBHOOK_PATH' environment variables."));
                        return;
                    }

                    var authorName = $"{GetName(user)} ({GetPCPlayerName(user)}) [{GetPCPublicCDKey(user)}]";
                    var areaName = GetName(area);
                    var areaTag = GetTag(area);
                    var areaResref = GetResRef(area);
                    var positionGroup = $"({position.X}, {position.Y}, {position.X})";
                    var dateReported = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss");
                    var playerId = GetObjectUUID(user);
                    var nextReportAllowed = DateTime.UtcNow.AddMinutes(1);

                    Task.Run(async () =>
                    {
                        using (var client = new DiscordWebhookClient(url))
                        {
                            var embed = new EmbedBuilder
                            {
                                Title = "Bug Report",
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


                            await client.SendMessageAsync(string.Empty, embeds: new[] { embed.Build() });
                        }
                    });
                    */

                    //SetLocalString(user, "BUG_REPORT_LAST_SUBMISSION", nextReportAllowed.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                    //SendMessageToPC(user, "Bug report submitted! Thank you for your report.");
                });
        }

        private static void HelpCommand(ChatCommandBuilder builder)
        {
            builder.Create("help")
                .Description("Displays all chat commands available to you.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    var authorization = Authorization.GetAuthorizationLevel(user);

                    if (authorization == AuthorizationLevel.DM)
                    {
                        SendMessageToPC(user, Service.ChatCommand.HelpTextDM);
                    }
                    else if (authorization == AuthorizationLevel.Admin)
                    {
                        SendMessageToPC(user, Service.ChatCommand.HelpTextAdmin);
                    }
                    else
                    {
                        SendMessageToPC(user, Service.ChatCommand.HelpTextPlayer);
                    }
                });
        }
        private static void ListEmotesCommand(ChatCommandBuilder builder)
        {
            builder.Create("listemotes")
                .Description("Displays all emotes available to you.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    SendMessageToPC(user, Service.ChatCommand.HelpTextEmote);
                });
        }
    }
}
