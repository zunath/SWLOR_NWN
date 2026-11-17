using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Async;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Domain.Gameplay;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.World;
using ChatChannel = SWLOR.Game.Server.Core.NWNX.Enum.ChatChannel;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Composition;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    /// <summary>
    /// DM communication commands: broadcasts, area shouts, the comms filter and DM settings.
    /// </summary>
    public class DMCommunicationChatCommand: IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new ChatCommandBuilder();
        private static readonly ApplicationSettings _appSettings = ApplicationSettings.Get();


        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            Broadcast();
            AreaShout();
            CommsFilter();
            DMSettings();

            return _builder.Build();
        }

        private void Broadcast()
        {
            _builder.Create("broadcast", "bc")
                .Description("Sends your DM shout to Discord.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Validate((user, args) =>
                {
                    if (args.Length <= 0)
                        return "Please enter a message.";

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    var message = string.Join(" ", args);
                    var url = _appSettings.DMShoutWebhookUrl;

                    for (var onlinePlayer = GetFirstPC(); GetIsObjectValid(onlinePlayer); onlinePlayer = GetNextPC())
                        ChatPlugin.SendMessage(ChatChannel.DMShout, message, user, DMPresence.GetControlledCreature(onlinePlayer));

                    var authorName = $"{GetName(user)} ({GetPCPlayerName(user)}) [{GetPCPublicCDKey(user)}]";
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        _ = SendDMShoutWebhookAsync(user, url, authorName, message);
                    }
                });
        }

        private void AreaShout()
        {
            _builder.Create("areashout", "ashout")
                .Description("Shouts a message to everyone in your current area only.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Validate((user, args) =>
                {
                    if (args.Length <= 0)
                        return "Please enter a message.";

                    if (!GetIsObjectValid(GetArea(user)))
                        return "You are not in an area.";

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    var message = string.Join(" ", args);
                    foreach (var recipient in GetAreaShoutRecipients(user))
                        ChatPlugin.SendMessage(ChatChannel.DMShout, message, user, recipient);

                    Log.Write(LogGroup.DM, $"{DMPresence.DescribeDM(user)} shouted to {GetResRef(GetArea(user))}: {message}");
                });
        }

        /// <summary>
        /// Returns the speaker and everyone whose controlled creature is in the speaker's area. A possessing DM is
        /// addressed through the creature they possess, because chat sent to their avatar is dropped.
        /// </summary>
        /// <param name="speaker">The creature shouting.</param>
        private static List<uint> GetAreaShoutRecipients(uint speaker)
        {
            var area = GetArea(speaker);
            var recipients = new List<uint> { speaker };
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                var controlled = DMPresence.GetControlledCreature(player);
                if (GetArea(controlled) == area && !recipients.Contains(controlled))
                    recipients.Add(controlled);
            }

            return recipients;
        }

        private void CommsFilter()
        {
            const string Usage = "Usage: /commsfilter [all|mine]";

            _builder.Create("commsfilter")
                .Description("Chooses whose Comms you see as a DM: 'all' for every party, or 'mine' for only your own party (none while you are not in one). Omit the option to switch between them.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Validate((user, args) =>
                {
                    if (!GetIsDM(user) && !GetIsDMPossessed(user))
                        return "Only a DM avatar, or a creature a DM possesses, receives other parties' Comms.";

                    if (args.Length > 1 || args.Length == 1 && args[0].ToLower() is not ("all" or "mine"))
                        return Usage;

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    DMCommsFilter filter;
                    if (args.Length == 0)
                        filter = DMPreference.Get(user).CommsFilter == DMCommsFilter.AllParties
                            ? DMCommsFilter.OwnParty
                            : DMCommsFilter.AllParties;
                    else
                        filter = args[0].ToLower() == "mine"
                            ? DMCommsFilter.OwnParty
                            : DMCommsFilter.AllParties;

                    if (!DMPreference.Update(user, preferences => preferences.CommsFilter = filter))
                    {
                        SendMessageToPC(user, ColorToken.Red("Your Comms filter could not be saved."));
                        return;
                    }

                    SendMessageToPC(user, filter == DMCommsFilter.AllParties
                        ? "Comms filter: showing every party's Comms."
                        : "Comms filter: showing only your own party's Comms. You see none while you are not in a party.");
                });
        }

        private void DMSettings()
        {
            _builder.Create("dmsettings")
                .Description("Toggles the DM Settings window: emote style, Comms filter, helmet and cloak.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Validate((user, args) =>
                    GetIsDM(user) || GetIsDMPossessed(user)
                        ? string.Empty
                        : "Only a DM avatar, or a creature a DM possesses, has DM settings.")
                .Action((user, target, location, args) =>
                {
                    DMPresence.ToggleWindow(user, GuiWindowType.DMSettings);
                });
        }

        private static async Task SendDMShoutWebhookAsync(uint user, string url, string authorName, string message)
        {
            try
            {
                var userId = GetObjectUUID(user);
                var enqueued = await BackgroundJob.EnqueueDiscordWebhook(url, authorName, message, 15105570);
                await NwTask.SwitchToMainThread();

                if (!GetIsObjectValid(user) || !GetIsPC(user) || GetObjectUUID(user) != userId)
                    return;

                if (!enqueued)
                {
                    Log.Write(LogGroup.Error, "Failed to queue DM shout Discord webhook.");
                    SendMessageToPC(user, ColorToken.Red("ERROR: Unable to queue DM shout Discord webhook. Please notify an admin."));
                }
            }
            catch (Exception ex)
            {
                Log.WriteError(ex, "DM shout Discord webhook submission failed.");
            }
        }
    }
}
