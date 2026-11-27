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
    /// DM tool windows and server control.
    /// </summary>
    public class DMToolsChatCommand: IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new ChatCommandBuilder();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            RestartServer();
            Notes();
            CreatureManager();
            DMTools();
            BattleGrid();
            MusicWindow();

            return _builder.Build();
        }

        private void RestartServer()
        {
            _builder.Create("restartserver")
                .Description("Restarts the server. Requires CD Key to be entered. Example: /restartserver XXXXYYYY")
                .Permissions(AuthorizationLevel.Admin, AuthorizationLevel.DM)
                .Validate((user, args) =>
                {
                    if (args.Length <= 0)
                    {
                        return "Requires CD Key to be entered. Example: /restartserver XXXXYYYY";
                    }
                    else if (String.IsNullOrWhiteSpace(args[0]))
                    {
                        return "Please enter your public CD Key to confirm the server reset. Use /cdkey to retrieve this. E.G: /restartserver XXXXYYYY";
                    }
                    else if (GetPCPublicCDKey(user) != args[0])
                    {
                        return $"Invalid public CD Key. {args[0]} does not match your CDKey:{GetPCPublicCDKey(user)}. Try again. E.G: /restartserver XXXXYYYY";
                    }
                    else
                    {
                        return string.Empty;
                    }
                })
                .Action((user, target, location, args) =>
                {
                    var initiatorName = GetName(user);
                    var accountName = GetPCPlayerName(user);
                    var cdKey = GetPCPublicCDKey(user);
                    var restartUtc = DateTime.UtcNow;

                    Log.Write(
                        LogGroup.Server,
                        $"Manual restart audit: InitiatorName={initiatorName}, AccountName={accountName}, CdKey={cdKey}, RestartUtc={restartUtc:O}, Reason=Manual /restartserver command");

                    var notificationMessage = $"Manual restart initiated by {initiatorName} ({accountName}) [{cdKey}] at {restartUtc:yyyy-MM-dd HH:mm:ss} UTC.";
                    ServerTasks.SendServerLifecycleNotificationForShutdown(notificationMessage);

                    ServerRestart.BootAllPlayers("The server is restarting.");

                    AdministrationPlugin.ShutdownServer();
                });
        }

        private void Notes()
        {
            _builder.Create("notes", "note")
                .Description("Toggles the area notes window.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .Action((user, target, location, args) =>
                {
                    DMPresence.ToggleWindow(user, GuiWindowType.AreaNotes);
                });
        }

        private void CreatureManager()
        {
            _builder.Create("cm")
                .Description("Toggles the Creature Manager window.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .Action((user, target, location, args) =>
                {
                    DMPresence.ToggleWindow(user, GuiWindowType.CreatureManager);
                });
        }

        private void DMTools()
        {
            _builder.Create("plctools", "placeabletools")
                .Description("Toggles the DM Placeable Tools window.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Action((user, target, location, args) =>
                {
                    DMPresence.ToggleWindow(user, GuiWindowType.DMTools);
                });
        }

        private void BattleGrid()
        {
            _builder.Create("battlegrid")
                .Description("Toggles the Battle Grid window: show, size and move a grid over the current area.")
                // DMs, the creatures they possess, and admins on their own characters may all run a battle.
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Action((user, target, location, args) =>
                {
                    DMPresence.ToggleWindow(user, GuiWindowType.DMBattleGrid);
                });
        }

        private void MusicWindow()
        {
            _builder.Create("music")
                .Description("Toggles the area music window, which can play any track.")
                .Permissions(AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .Action((user, target, location, args) =>
                {
                    DMPresence.ToggleWindow(user, GuiWindowType.MusicPicker, new MusicPickerPayload(MusicPickerMode.DungeonMaster));
                });
        }
    }
}
