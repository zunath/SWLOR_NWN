using System;
using System.Collections.Generic;
using Discord;
using Discord.Webhook;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
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

            return builder.Build();
        }

        private static void BugCommand(ChatCommandBuilder builder)
        {
            builder.Create("bug")
                .Description("Report a bug to the developers. Please include as much detail as possible.")
                .Permissions(AuthorizationLevel.All)
                .Validate((user, args) =>
                {
                    if (args.Length <= 0 || args[0].Length <= 0)
                    {
                        return "Please enter in a description for the bug.";
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
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

                    // todo: this must be made async
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

                        
                        client.SendMessageAsync(string.Empty, embeds: new[] { embed.Build() });
                    }

                    SendMessageToPC(user, "Bug report submitted! Thank you for your report.");
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
    }
}
