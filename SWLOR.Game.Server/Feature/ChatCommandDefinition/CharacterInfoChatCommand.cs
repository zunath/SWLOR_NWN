using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.AppearanceDefinition.Species;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using Player = SWLOR.Game.Server.Entity.Player;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Companions;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    /// <summary>
    /// Character information, the main windows and character deletion.
    /// </summary>
    public class CharacterInfoChatCommand: IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new ChatCommandBuilder();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            Char();
            CDKey();
            Skills();
            Recipes();
            Perks();
            Techniques();
            Hotbars();
            DeleteCommand();
            ResetWindows();
            Overflow();

            return _builder.Build();
        }

        private void Char()
        {
            _builder.Create("character", "char")
                .Description("Displays your character's info.")
                .Permissions(AuthorizationLevel.All)
                .Validate((user, args) =>
                {
                    if (GetIsDM(user))
                    {
                        return "This command can only be used on PCs.";
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    var playerId = GetObjectUUID(user);
                    var dbPlayer = DB.Get<Player>(playerId);
                    var cdKey = GetPCPublicCDKey(user);
                    var daysOld = (DateTime.UtcNow - dbPlayer.DateCreated).Days;
                    var daysOldMessage = daysOld == 1 ? "day old" : "days old";
                    var statRebuild = "Now";

                    var (isOnDelay, timeToWait) = Recast.IsOnRecastDelay(user, RecastGroup.StatRebuild);
                    if (isOnDelay)
                    {
                        statRebuild = timeToWait;
                    }

                    var message = $"{ColorToken.Green("Character Info:")}\n" +
                                  $"{ColorToken.Green("Public CD Key:")} {ColorToken.White(cdKey)}\n" +
                                  $"{ColorToken.Green("Player Id:")} {ColorToken.White(playerId)}\n" +
                                  $"{ColorToken.Green("Date Created:")} {ColorToken.White(dbPlayer.DateCreated.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))}\n" +
                                  $"{ColorToken.Green("Age:")} {ColorToken.White(daysOld.ToString())} {ColorToken.White(daysOldMessage)}\n" +
                                  $"{ColorToken.Green("AP Rebuild Available:")} {ColorToken.White(statRebuild)}";

                    SendMessageToPC(user, message);
                });
        }

        private void CDKey()
        {
            _builder.Create("cdkey")
                .Description("Displays your public CD key.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    var cdKey = GetPCPublicCDKey(user);
                    SendMessageToPC(user, "Your public CD Key is: " + cdKey);
                });
        }

        private void Skills()
        {
            _builder.Create("skills")
                .Description("Toggles the skills menu.")
                .Permissions(AuthorizationLevel.Player)
                .Action((user, target, location, args) =>
                {
                    Gui.TogglePlayerWindow(user, GuiWindowType.Skills);
                });
        }

        private void Recipes()
        {
            _builder.Create("recipe", "recipes")
                .Description("Toggles the recipes menu.")
                .Permissions(AuthorizationLevel.Player)
                .Action((user, target, location, args) =>
                {
                    Gui.TogglePlayerWindow(user, GuiWindowType.Recipes);
                });
        }

        private void Perks()
        {
            _builder.Create("perk", "perks")
                .Description("Toggles the perks menu.")
                .Permissions(AuthorizationLevel.Player)
                .Action((user, target, location, args) =>
                {
                    Gui.TogglePlayerWindow(user, GuiWindowType.Perks);
                });

        }

        private void Techniques()
        {
            _builder.Create("techniques", "tech")
                .Description("Toggles the techniques menu, where mimicked techniques can be equipped and unequipped.")
                .Permissions(AuthorizationLevel.All)
                .Validate((user, args) =>
                {
                    if (Perk.GetPerkLevel(user, PerkType.CombatAnalyzer) < 1)
                    {
                        return ColorToken.Red("You need the Combat Analyzer perk to use this feature.");
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    Gui.TogglePlayerWindow(user, GuiWindowType.Techniques, new TechniquesPayload());
                });
        }

        private void Hotbars()
        {
            _builder.Create("hotbars")
                .Description("Toggles the Hotbars window, where extra hotbars are turned on, sized and moved.")
                .Permissions(AuthorizationLevel.All)
                .Validate((user, args) => GetIsPC(user) && !GetIsDM(user) && !GetIsDMPossessed(user)
                    ? string.Empty
                    : ColorToken.Red("Only player characters have extra hotbars."))
                .Action((user, target, location, args) =>
                {
                    Gui.TogglePlayerWindow(user, GuiWindowType.ExtraHotbarSetup);
                });
        }

        private void DeleteCommand()
        {
            _builder.Create("delete")
                .Description("Permanently deletes your character.")
                .Permissions(AuthorizationLevel.All)
                .Validate((user, args) =>
                {
                    if (!GetIsPC(user) || GetIsDM(user))
                        return "You can only delete a player character.";

                    var cdKey = GetPCPublicCDKey(user);
                    var enteredCDKey = args.Length > 0 ? args[0] : string.Empty;

                    if (cdKey != enteredCDKey)
                    {
                        return "Invalid CD key entered. Please enter the command as follows: \"/delete <CD Key>\". You can retrieve your CD key with the /CDKey chat command.";
                    }

                    if (GetIsDM(user) || GetIsDMPossessed(user))
                    {
                        return "DM characters cannot use this chat command.";
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    var lastSubmission = GetLocalString(user, "DELETE_CHARACTER_LAST_SUBMISSION");
                    var isFirstSubmission = true;

                    // Check for the last submission, if any.
                    if (!string.IsNullOrWhiteSpace(lastSubmission))
                    {
                        // Found one, parse it.
                        var dateTime = DateTime.ParseExact(lastSubmission, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                        if (DateTime.UtcNow <= dateTime.AddSeconds(30))
                        {
                            // Player submitted a second request within 30 seconds of the last one.
                            // This is a confirmation they want to delete.
                            isFirstSubmission = false;
                        }
                    }

                    // Player hasn't submitted or time has elapsed
                    if (isFirstSubmission)
                    {
                        SetLocalString(user, "DELETE_CHARACTER_LAST_SUBMISSION", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                        FloatingTextStringOnCreature("Please confirm your deletion by entering another \"/delete <CD Key>\" command within 30 seconds.", user, false);
                    }
                    else
                    {
                        var playerId = GetObjectUUID(user);
                        var entity = DB.Get<Player>(playerId);
                        entity.IsDeleted = true;
                        DB.Set(entity);

                        var playerName = GetPCPlayerName(user);
                        var characterName = GetName(user);
                        AdministrationPlugin.DeletePlayerCharacter(user, true, "Your character has been deleted.");
                        AdministrationPlugin.DeleteTURD(playerName, characterName);
                    }
                });
        }

        private void Overflow()
        {
            _builder.Create("overflow")
                .Description("Moves items that did not fit in your inventory during a character update back into your inventory.")
                .Permissions(AuthorizationLevel.All)
                .Validate((user, args) =>
                {
                    if (!GetIsPC(user) || GetIsDM(user) || GetIsDMPossessed(user))
                        return "Only player characters can claim overflow items.";

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    OverflowStorage.Claim(user);
                });
        }

        private void ResetWindows()
        {
            _builder.Create("resetwindows")
                .Description("Resets all window positions and sizes to their default values.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    // A DM possessing a creature types as the creature, but their windows are cached under the DM avatar
                    // and can be shown on the creature, the avatar, or a creature they possessed earlier.
                    var owner = DMPresence.GetDM(user);

                    // Close all open windows for the player
                    foreach (GuiWindowType type in Enum.GetValues(typeof(GuiWindowType)))
                    {
                        // The DM toolbar stands in for the hidden native taskbar and has a fixed position, so it stays as it is.
                        if (type == GuiWindowType.Invalid || type == GuiWindowType.DMToolbar) continue;
                        foreach (var uiTarget in DMPresence.GetShownOn(user, type))
                        {
                            if (Gui.IsWindowOpen(uiTarget, type))
                            {
                                Gui.TogglePlayerWindow(owner, type, uiTarget: uiTarget);
                            }
                        }
                    }

                    var playerId = GetObjectUUID(owner);
                    var dbPlayer = DB.Get<Player>(playerId);

                    if (dbPlayer != null)
                    {
                        // Clear all stored window geometries
                        dbPlayer.WindowGeometries.Clear();

                        // Save the player data
                        DB.Set(dbPlayer);
                    }

                    // Update all player window instances with default geometries
                    // This ensures that when windows are reopened, they use default positions
                    foreach (GuiWindowType type in Enum.GetValues(typeof(GuiWindowType)))
                    {
                        // The DM toolbar stands in for the hidden native taskbar and has a fixed position, so it stays as it is.
                        if (type == GuiWindowType.Invalid || type == GuiWindowType.DMToolbar) continue;

                        try
                        {
                            var playerWindow = Gui.GetPlayerWindow(owner, type);
                            var template = Gui.GetWindowTemplate(type);
                            playerWindow.ViewModel.Geometry = template.InitialGeometry;
                        }
                        catch
                        {
                            // Window might not exist for this player, skip it
                            continue;
                        }
                    }

                    // Extra hotbars are part of the HUD, so the ones the player turned on come back at their default spots.
                    ExtraHotbarsWindow.ApplyVisibility(owner);

                    SendMessageToPC(user, ColorToken.Green("All window positions and sizes have been reset to their default values."));
                });
        }
    }
}
