using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Associate;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;
using HoloCom = SWLOR.Game.Server.Service.HoloCom;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class CharacterChatCommand: IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new ChatCommandBuilder();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            Char();
            CDKey();
            Save();
            Skills();
            EndCall();
            Recipes();
            Perks();
            DeleteCommand();
            LanguageCommand();
            ToggleEmoteStyle();
            ChangeDescription();
            ConcentrationAbility();
            Customize();
            AlwaysWalk();
            AssociateCommands();
            Follow();

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

        private void Save()
        {
            _builder.Create("save")
                .Description("Manually saves your character. Your character also saves automatically every few minutes.")
                .Permissions(AuthorizationLevel.Player)
                .Action((user, target, location, args) =>
                {
                    ExportSingleCharacter(user);
                    SendMessageToPC(user, "Character saved successfully.");
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

        private void EndCall()
        {
            _builder.Create("endcall")
                .Description("Ends your current HoloCom call.")
                .Permissions(AuthorizationLevel.Player, AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Action((user, target, location, args) =>
                {
                    HoloCom.SetIsInCall(user, HoloCom.GetCallReceiver(user), false);
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

        private void LanguageCommand()
        {
            _builder.Create("language")
                .Description("Switches the active language. Use /language help for more information.")
                .Permissions(AuthorizationLevel.All)
                .Validate((user, args) =>
                {
                    if (args.Length < 1)
                    {
                        return ColorToken.Red("Please enter /language help for more information on how to use this command.");
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    var command = args[0].ToLower();
                    var race = GetRacialType(user);
                    var languages = Language.Languages;

                    if (command == "help")
                    {
                        var commands = new List<string>
                        {
                            "help: Displays this help text."
                        };

                        foreach (var language in languages)
                        {
                            var chatText = language.ChatNames.ElementAt(0);
                            var count = language.ChatNames.Count();

                            for (var x = 1; x < count; x++)
                            {
                                chatText += ", " + language.ChatNames.ElementAt(x);
                            }

                            commands.Add($"{chatText}: Sets the active language to {language.ProperName}.");
                        }

                        SendMessageToPC(user, commands.Aggregate((a, b) => a + '\n' + b));
                        return;
                    }

                    // Wookiees cannot speak any language besides Shyriiwook.
                    if (race == RacialType.Wookiee &&
                        command != SkillType.Shyriiwook.ToString().ToLower())
                    {
                        Language.SetActiveLanguage(user, SkillType.Shyriiwook);
                        SendMessageToPC(user, ColorToken.Red("Wookiees can only speak Shyriiwook."));
                        return;
                    }


                    foreach (var language in Language.Languages)
                    {
                        if (language.ChatNames.Contains(command))
                        {
                            Language.SetActiveLanguage(user, language.Skill);
                            SendMessageToPC(user, $"Set active language to {language.ProperName}.");
                            return;
                        }
                    }

                    SendMessageToPC(user, ColorToken.Red($"Unknown language {command}."));
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
        
        private void ToggleEmoteStyle()
        {
            _builder.Create("emotestyle")
                .Description("Toggles your emote style between regular and novel.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    var curStyle = Communication.GetEmoteStyle(user);
                    var newStyle = curStyle == EmoteStyle.Novel ? EmoteStyle.Regular : EmoteStyle.Novel;
                    Communication.SetEmoteStyle(user, newStyle);
                    SendMessageToPC(user, $"Toggled emote style to {newStyle}.");
                });
        }

        private void ChangeDescription()
        {
            _builder.Create("changedescription", "changedesc")
                .Description("Brings up an NUI window to change the description of a target. Players may only target items in their own inventory.")
                .Permissions(AuthorizationLevel.All)
                .RequiresTarget()
                .Action((user, target, location, args) =>
                {
                    var isDM = GetIsDM(user) || GetIsDMPossessed(user);

                    if (!isDM || GetIsObjectValid(target) == false)
                    {
                        if (!GetIsObjectValid(target) || GetItemPossessor(target) != user || GetObjectType(target) != ObjectType.Item)
                        {
                            SendMessageToPC(user, "You can only change descriptions of items in your inventory with this command.");
                            return;
                        }
                    }

                    var payload = new TargetDescriptionPayload(target);
                    Gui.TogglePlayerWindow(user, GuiWindowType.TargetDescription, payload);
                });
        }

        private void ConcentrationAbility()
        {
            _builder.Create("concentration", "conc")
                .Description("Tells you what concentration ability you have active. Follow with 'end' (no quotes) to turn your concentration ability off. Example: /concentration end")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    var doEnd = args.Length > 0 && args[0].ToLower() == "end";

                    if (doEnd)
                    {
                        Ability.EndConcentrationAbility(user);
                    }
                    else
                    {
                        var activeConcentration = Ability.GetActiveConcentration(user);
                        if (activeConcentration.Feat == FeatType.Invalid)
                        {
                            SendMessageToPC(user, "No concentration ability is currently active.");
                        }
                        else
                        {
                            var ability = Ability.GetAbilityDetail(activeConcentration.Feat);
                            SendMessageToPC(user, $"Currently active concentration ability: {ability.Name}");
                        }
                    }
                });
        }

        private void Customize()
        {
            _builder.Create("customize", "customise")
                .Description("Opens the appearance editor window.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    var player = user;
                    var uiTarget = OBJECT_INVALID;
                    if (GetIsDMPossessed(player))
                    {
                        uiTarget = player;
                        player = GetMaster(player);
                    }

                    var payload = new AppearanceEditorPayload(user);
                    Gui.TogglePlayerWindow(player, GuiWindowType.AppearanceEditor, payload, OBJECT_INVALID, uiTarget);
                });
        }

        private void AlwaysWalk()
        {
            _builder.Create("alwayswalk", "walk")
                .Description("Toggles forced walking when moving your character.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, _, _, _) =>
                {
                    var wasWalking = GetLocalInt(user, "WALK_TOGGLE") == 1;
                    PlayerPlugin.SetAlwaysWalk(user, !wasWalking);

                    if (wasWalking)
                    {
                        SetLocalInt(user, "WALK_TOGGLE", 0);
                        SendMessageToPC(user, $"Walk mode {ColorToken.Red("disabled")}.");
                    } else
                    {
                        SetLocalInt(user, "WALK_TOGGLE", 1);
                        SendMessageToPC(user, $"Walk mode {ColorToken.Green("enabled")}.");
                    }
                });
        }

        private void AssociateCommands()
        {
            _builder.Create("associate", "a")
                .Description("Makes your associate (droid, pet, etc.) speak a message.")
                .Permissions(AuthorizationLevel.All)
                .Validate((user, args) =>
                {
                    var associate = GetAssociate(AssociateType.Henchman, user);
                    if (!GetIsObjectValid(associate))
                    {
                        return "You do not have an active associate.";
                    }

                    if (GetIsDead(associate))
                    {
                        return "Your associate is dead.";
                    }

                    if (args.Length <= 0)
                    {
                        return "Please enter a message. Example: /associate Hello!";
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    var associate = GetAssociate(AssociateType.Henchman, user);
                    var message = string.Join(' ', args);

                    AssignCommand(associate, () => SpeakString(message));
                });
        }

        private void Follow()
        {
            _builder.Create("follow")
                .Description("Makes you follow a selected target.")
                .Permissions(AuthorizationLevel.All)
                .RequiresTarget()
                .Action((user, target, location, args) =>
                {
                    AssignCommand(user, () =>
                    {
                        ActionMoveToObject(target, true);
                    });
                });
        }
    }
}
