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
    /// Social commands: calls, languages, emote style, away status, known names, introductions and the description.
    /// </summary>
    public class CharacterSocialChatCommand: IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new ChatCommandBuilder();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            EndCall();
            AnswerCall();
            DeclineCall();
            LanguageCommand();
            ToggleEmoteStyle();
            Afk();
            SetKnownName();
            ForgetKnownName();
            Introductions();
            ChangeDescription();

            return _builder.Build();
        }

        private void EndCall()
        {
            _builder.Create("endcall")
                .Description("Ends your current HoloCom call or cancels an outgoing call attempt.")
                .Permissions(AuthorizationLevel.Player, AuthorizationLevel.DM, AuthorizationLevel.Admin)
                .Action((user, target, location, args) =>
                {
                    if (!HoloCom.EndCall(user))
                    {
                        SendMessageToPC(user, "You don't have any active calls or outgoing call attempts to end.");
                    }
                });
        }

        private void AnswerCall()
        {
            _builder.Create("answer")
                .Description("Answers your incoming HoloCom call.")
                .Permissions(AuthorizationLevel.Player)
                .Action((user, target, location, args) =>
                {
                    if (!HoloCom.IsCallReceiver(user) || HoloCom.IsInCall(user))
                    {
                        SendMessageToPC(user, "You don't have an incoming HoloCom call.");
                        return;
                    }

                    HoloCom.AnswerCall(user);
                });
        }

        private void DeclineCall()
        {
            _builder.Create("decline")
                .Description("Declines your incoming HoloCom call.")
                .Permissions(AuthorizationLevel.Player)
                .Action((user, target, location, args) =>
                {
                    if (!HoloCom.IsCallReceiver(user) || HoloCom.IsInCall(user))
                    {
                        SendMessageToPC(user, "You don't have an incoming HoloCom call.");
                        return;
                    }

                    HoloCom.DeclineCall(user);
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

        private void ToggleEmoteStyle()
        {
            _builder.Create("emotestyle")
                .Description("Toggles your emote style between regular and novel.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    var curStyle = Communication.GetEmoteStyle(user);
                    var newStyle = curStyle == EmoteStyle.Novel ? EmoteStyle.Regular : EmoteStyle.Novel;
                    if (!Communication.SetEmoteStyle(user, newStyle))
                    {
                        SendMessageToPC(user, ColorToken.Red("Your emote style could not be saved."));
                        return;
                    }

                    SendMessageToPC(user, $"Toggled emote style to {newStyle}.");
                });
        }

        private void Afk()
        {
            _builder.Create("afk")
                .Description("Toggles a purple [AFK] indicator on your displayed name. Moving or using /afk again removes it.")
                .Permissions(AuthorizationLevel.All)
                .Validate((user, args) => !GetIsPC(user) || GetIsDM(user)
                    ? "This command can only be used on player characters."
                    : string.Empty)
                .Action((user, target, location, args) =>
                {
                    var isAfk = PlayerName.ToggleAfk(user);
                    SendMessageToPC(user, isAfk
                        ? ColorToken.Purple("You are now AFK. Move or use /afk again when you return.")
                        : "You are no longer AFK.");
                });
        }

        private void SetKnownName()
        {
            _builder.Create("name")
                .Description("Target another player to save a private label only you can see. Target yourself to set your gray public description.")
                .Permissions(AuthorizationLevel.All)
                .Validate((user, args) =>
                {
                    var rawName = string.Join(" ", args);
                    return PlayerName.ValidateKnownNameInput(rawName);
                })
                .RequiresTarget(ObjectType.Creature)
                .Action((user, target, location, args) =>
                {
                    if (!GetIsObjectValid(target) || !GetIsPC(target) || GetIsDM(target) || GetIsDMPossessed(target))
                    {
                        SendMessageToPC(user, ColorToken.Red("You may only name player characters."));
                        return;
                    }

                    var rawName = string.Join(" ", args);
                    var validationError = PlayerName.ValidateKnownNameInput(rawName);
                    if (!string.IsNullOrWhiteSpace(validationError))
                    {
                        SendMessageToPC(user, ColorToken.Red(validationError));
                        return;
                    }

                    var name = PlayerName.SanitizeKnownName(rawName);
                    if (target == user)
                    {
                        PlayerDescriptor.SetUnknownDisplayName(user, name);
                        SendMessageToPC(user, ColorToken.Green($"Public description set to '{name}'. Players who have not labeled your current identity will see this in gray."));
                        return;
                    }

                    validationError = PlayerName.ValidateKnownNameAssignment(user, target, name);
                    if (!string.IsNullOrWhiteSpace(validationError))
                    {
                        SendMessageToPC(user, ColorToken.Red(validationError));
                        return;
                    }

                    var previousName = PlayerName.TryGetKnownName(user, target, out var knownName)
                        ? knownName
                        : string.Empty;
                    var identityKey = Disguise.GetIdentityKey(target);
                    PlayerName.SetKnownName(user, target, name);

                    Log.WriteStructured(
                        LogGroup.PrivateNames,
                        "Player identity name change: Action={Action} ObserverPlayerId={ObserverPlayerId} ObserverName={ObserverName} TargetPlayerId={TargetPlayerId} TargetName={TargetName} IdentityKey={IdentityKey} PreviousName={PreviousName} Name={Name}",
                        "name-set",
                        GetObjectUUID(user),
                        PlayerName.GetAuditName(user),
                        GetObjectUUID(target),
                        PlayerName.GetAuditName(target),
                        identityKey,
                        previousName,
                        name);
                    SendMessageToPC(user, ColorToken.Green($"Private label saved as '{name}'. Only you can see this label."));
                });
        }

        private void Introductions()
        {
            _builder.Create("introduce")
                .Description("Offers a name or alias to visible players within 20m. They choose whether to remember it.")
                .Permissions(AuthorizationLevel.All)
                .Validate((user, args) => PlayerIntroduction.ValidateIntroduction(user, string.Join(" ", args)))
                .Action((user, target, location, args) => PlayerIntroduction.Introduce(user, string.Join(" ", args)));

            _builder.Create("introductions", "intro")
                .Description("Review nearby introductions. Remembering a name requires your approval.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) => Gui.TogglePlayerWindow(user, GuiWindowType.Introductions));
        }

        private void ForgetKnownName()
        {
            _builder.Create("forgetname")
                .Description("Removes the private label only you can see for a targeted player character's current identity.")
                .Permissions(AuthorizationLevel.All)
                .RequiresTarget(ObjectType.Creature)
                .Action((user, target, location, args) =>
                {
                    if (!GetIsObjectValid(target) || !GetIsPC(target) || GetIsDM(target) || GetIsDMPossessed(target))
                    {
                        SendMessageToPC(user, ColorToken.Red("You may only forget names for player characters."));
                        return;
                    }

                    if (target == user)
                    {
                        SendMessageToPC(user, ColorToken.Red("You cannot forget your own name."));
                        return;
                    }

                    var previousName = PlayerName.TryGetKnownName(user, target, out var knownName)
                        ? knownName
                        : string.Empty;
                    var identityKey = Disguise.GetIdentityKey(target);
                    PlayerName.ForgetKnownName(user, target);
                    Log.WriteStructured(
                        LogGroup.PrivateNames,
                        "Player identity name change: Action={Action} ObserverPlayerId={ObserverPlayerId} ObserverName={ObserverName} TargetPlayerId={TargetPlayerId} TargetName={TargetName} IdentityKey={IdentityKey} PreviousName={PreviousName} Name={Name}",
                        "name-forget",
                        GetObjectUUID(user),
                        PlayerName.GetAuditName(user),
                        GetObjectUUID(target),
                        PlayerName.GetAuditName(target),
                        identityKey,
                        previousName,
                        string.Empty);
                    SendMessageToPC(user, ColorToken.Green("Private label removed. This changes only what you see."));
                });
        }

        private void ChangeDescription()
        {
            _builder.Create("description", "desc")
                .Description("Brings up a window to change the description of a target.")
                .Permissions(AuthorizationLevel.All)
                .RequiresTarget()
                .Action((user, target, _, _) =>
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
    }
}
