using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;
using ChatChannel = SWLOR.Game.Server.Core.NWNX.Enum.ChatChannel;
using Player = SWLOR.Game.Server.Entity.Player;
using SkillType = SWLOR.Game.Server.Service.SkillService.SkillType;

namespace SWLOR.Game.Server.Service
{
    public static class Communication
    {
        private const string DMPossessedCreature = "COMMUNICATION_DM_POSSESSED_CREATURE";
        private const int HolonetDelayMinutes = 5;

        public static (byte, byte, byte) OOCChatColor { get; } = (64, 64, 64);

        private class CommunicationComponent
        {
            public string Text { get; set; }
            public bool IsTranslatable { get; set; }
            public bool IsCustomColor { get; set; }
            public byte Red { get; set; }
            public byte Green { get; set; }
            public byte Blue { get; set; }
        }
        
        private enum WorkingOnEmoteStyle
        {
            None,
            Asterisk,
            Bracket,
            ColonForward,
            ColonBackward
        };

        /// <summary>
        /// Whenever a DM possesses a creature, track the NPC on their object so that messages can be
        /// sent to them during the possession.
        /// </summary>
        [NWNEventHandler("dm_poss_bef")]
        [NWNEventHandler("dm_possfull_bef")]
        public static void OnDMPossess()
        {
            var dm = OBJECT_SELF;
            var target = StringToObject(EventsPlugin.GetEventData("TARGET"));
            
            // Unpossession - Remove the variable
            if (!GetIsObjectValid(target))
            {
                dm = GetMaster(dm);
                DeleteLocalObject(dm, DMPossessedCreature);
            }
            // Possession - Store the variable
            else
            {
                SetLocalObject(dm, DMPossessedCreature, target);
            }
        }

        /// <summary>
        /// When a player enters the server, set a local bool on their PC representing
        /// the current state of their holonet visibility.
        /// </summary>
        [NWNEventHandler("mod_enter")]
        public static void LoadHolonetSetting()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId) ?? new Player(playerId);
            
            SetLocalBool(player, "DISPLAY_HOLONET", dbPlayer.Settings.IsHolonetEnabled);
        }

        /// <summary>
        /// When a player focuses the chatbar, set a typing indicator on the player; when
        /// unfocused, remove the indicator.
        /// </summary>

        [NWNEventHandler("mod_gui_event")]
        public static void TypingIndicator()
        {
            var player = GetLastGuiEventPlayer();
            var type = GetLastGuiEventType();
            if (!GetIsPC(player)) return;

            if(type == GuiEventType.ChatBarFocus)
            {
                var chatIndicator = TagEffect(EffectVisualEffect(VisualEffect.Vfx_Dur_Chat_Bubble, false, 0.5f), "typingindicator");
                ApplyEffectToObject(DurationType.Temporary, chatIndicator, player, 120.0f);
            } else if (type == GuiEventType.ChatBarUnfocus)
            {
                RemoveEffectByTag(player, "typingindicator");
            }
        }

        // Register DMFI Voice Command Handler which lives in nwscript land.
        [NWNEventHandler("mod_chat")]
        public static void ProcessNativeChatMessage()
        {
            ExecuteScriptNWScript("dmfi_onplychat", OBJECT_SELF);
        }

        [NWNEventHandler("on_nwnx_chat")]
        public static void ProcessChatMessage()
        {
            var channel = ChatPlugin.GetChannel();

            // - PlayerTalk, PlayerWhisper, PlayerParty, and PlayerShout are all IC channels. These channels
            //   are subject to emote coloring and language translation. (see below for more info).
            // - PlayerParty is an IC channel with special behaviour. Those outside of the party but within
            //   range may listen in to the party chat. (see below for more information).
            // - PlayerShout sends a holocom message server-wide through the DMTell channel.
            // - PlayerDM echoes back the message received to the sender.

            var inCharacterChat =
                channel == ChatChannel.PlayerTalk ||
                channel == ChatChannel.PlayerWhisper ||
                channel == ChatChannel.PlayerParty ||
                channel == ChatChannel.PlayerShout;
            
            var messageToDm = channel == ChatChannel.PlayerDM;

            var sender = ChatPlugin.GetSender();
            var message = ChatPlugin.GetMessage().Trim();

            // if this is a DMFI chat command, exit as ProcessNativeChatMessage has already handled via mod_chat event.
            if (GetIsDM(sender) && message.Length >= 1 && message.Substring(0, 1) == ".")
            {                
                return;
            }

            // Ignore messages on other channels.
            if (!inCharacterChat && !messageToDm) return;

            if (string.IsNullOrWhiteSpace(message))
            {
                // We can't handle empty messages, so skip it.
                return;
            }

            // Echo the message back to the player.
            if (messageToDm)
            {
                ChatPlugin.SendMessage(ChatChannel.ServerMessage, "(Sent to DM) " + message, sender, sender);
                return;
            }

            ChatPlugin.SkipMessage();

            if (GetIsDead(sender) && !message.StartsWith("/"))
            {
                SendMessageToPC(sender, ColorToken.Red("You cannot speak while dead."));
                return;
            }

            var chatComponents = new List<CommunicationComponent>();

            // Quick early out - if we start with "//" or "((", this is an OOC message.
            if (message.Length >= 2 && (message.Substring(0, 2) == "//" || message.Substring(0, 2) == "(("))
            {
                var component = new CommunicationComponent
                {
                    Text = message,
                    IsCustomColor = true,
                    Red = OOCChatColor.Item1,
                    Green = OOCChatColor.Item2,
                    Blue = OOCChatColor.Item3,
                    IsTranslatable = false
                };
                chatComponents.Add(component);
                
                if (channel == ChatChannel.PlayerShout)
                {
                    SendMessageToPC(sender, "Out-of-character messages cannot be sent on the Holonet.");
                    return;
                }
            }
            // Another early out - if this is a chat command, exit.
            else if (message.Length >= 1 && message.Substring(0, 1) == "/")
            {
                return;
            }
            // Another early out - a completely empty message will just be skipped.
            else if (string.IsNullOrWhiteSpace(message.Trim()))
            {
                return;
            }
            else
            {
                if (GetEmoteStyle(sender) == EmoteStyle.Regular)
                {
                    chatComponents = SplitMessageIntoComponents_Regular(message);
                }
                else
                {
                    chatComponents = SplitMessageIntoComponents_Novel(message);
                }

                // For any components with color, set the emote color.
                foreach (var component in chatComponents)
                {
                    if (component.IsCustomColor)
                    {
                        component.Red = 0;
                        component.Green = 255;
                        component.Blue = 0;
                    }
                }
            }

            if (channel == ChatChannel.PlayerShout &&
                GetIsPC(sender) &&
                !GetIsDM(sender) &&
                !GetIsDMPossessed(sender))
            {
                var playerId = GetObjectUUID(sender);
                var dbPlayer = DB.Get<Player>(playerId);

                if (!dbPlayer.Settings.IsHolonetEnabled)
                {
                    SendMessageToPC(sender, "You have disabled the holonet and cannot send this message.");
                    return;
                }

                // 5 minute wait in between Holonet messages.
                var lastHolonet = GetLocalString(sender, "HOLONET_LAST_SEND");
                var now = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(lastHolonet))
                {
                    var dateTime = DateTime.ParseExact(lastHolonet, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    if (now <= dateTime.AddMinutes(HolonetDelayMinutes))
                    {
                        SendMessageToPC(sender, $"Holonet messages may only be sent once per {HolonetDelayMinutes} minutes.");
                        return;
                    }
                }

                SetLocalString(sender, "HOLONET_LAST_SEND", now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            }


            // Now, depending on the chat channel, we need to build a list of recipients.
            var needsAreaCheck = false;
            var distanceCheck = 0.0f;

            // The sender always wants to see their own message.
            var recipients = new List<uint> { sender };
            var allPlayersAndDMs = new List<uint>();
            var allPlayers = new List<uint>();
            var allDMs = new List<uint>();

            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                allPlayersAndDMs.Add(player);
                
                if (GetIsDM(player) || GetIsDMPossessed(player))
                {
                    allDMs.Add(player);
                }
                else
                {
                    allPlayers.Add(player);
                }
            }

            // This is a server-wide holonet message (that receivers can toggle on or off).
            if (channel == ChatChannel.PlayerShout)
            {
                recipients.AddRange(allPlayers.Where(player => GetLocalBool(player, "DISPLAY_HOLONET")));
                recipients.AddRange(allDMs);
            }
            // This is the normal party chat, plus everyone within 20 units of the sender.
            else if (channel == ChatChannel.PlayerParty)
            {
                // Can an NPC use the playerparty channel? I feel this is safe ...

                for (var member = GetFirstFactionMember(sender); GetIsObjectValid(member); member = GetNextFactionMember(sender))
                {
                    if (sender == member) continue;

                    recipients.Add(member);
                }

                recipients.AddRange(allDMs);

                needsAreaCheck = true;
                distanceCheck = 20.0f;
            }
            // Normal talk - 20 units.
            else if (channel == ChatChannel.PlayerTalk)
            {
                needsAreaCheck = true;
                distanceCheck = 20.0f;
            }
            // Whisper - 4 units.
            else if (channel == ChatChannel.PlayerWhisper)
            {
                needsAreaCheck = true;
                distanceCheck = 4.0f;
            }

            if (needsAreaCheck)
            {
                foreach (var player in allPlayersAndDMs)
                {
                    var target = player;
                    var possessedNPC = GetLocalObject(player, DMPossessedCreature);
                    if (GetIsObjectValid(possessedNPC))
                    {
                        target = possessedNPC;
                    }
                    
                    var distance = GetDistanceBetween(sender, target);

                    if (GetArea(target) == GetArea(sender) &&
                        distance <= distanceCheck &&
                        !recipients.Contains(target))
                    {
                        recipients.Add(target);
                    }
                }
            }

            // Now we have a list of who is going to actually receive a message, we need to modify
            // the message for each recipient then dispatch them.
            foreach (var receiver in recipients.Distinct())
            {
                var receiverId = GetObjectUUID(receiver);
                var dbReceiver = DB.Get<Player>(receiverId);

                // Generate the final message as perceived by obj.
                var finalMessage = new StringBuilder();

                if (channel == ChatChannel.PlayerShout)
                {
                    finalMessage.Append("[Holonet] ");
                }
                else if (channel == ChatChannel.PlayerParty)
                {
                    finalMessage.Append("[Comms] ");

                    if (GetIsDM(receiver))
                    {
                        // Convenience for DMs - append the party members.
                        finalMessage.Append("{ ");

                        var count = 0;

                        var partyMembers = new List<uint>();
                        for (var member = GetFirstFactionMember(sender); GetIsObjectValid(member); member = GetNextFactionMember(sender))
                        {
                            partyMembers.Add(member);
                        }

                        foreach (var otherPlayer in partyMembers)
                        {
                            var name = GetName(otherPlayer);
                            finalMessage.Append(name.Substring(0, Math.Min(name.Length, 10)));

                            ++count;

                            if (count >= 3)
                            {
                                finalMessage.Append(", ...");
                                break;
                            }
                            else if (count != partyMembers.Count)
                            {
                                finalMessage.Append(",");
                            }
                        }

                        finalMessage.Append(" } ");
                    }
                }

                var originalSender = sender;
                // temp set sender to hologram owner for holocoms
                if (GetIsObjectValid(HoloCom.GetHoloGramOwner(sender)))
                {
                    sender = HoloCom.GetHoloGramOwner(sender);
                }

                var language = Language.GetActiveLanguage(sender);

                // Wookiees cannot speak any other language (but they can understand them).
                // Swap their language if they attempt to speak in any other language.
                var race = GetRacialType(sender);
                if (race == RacialType.Wookiee && language != SkillType.Shyriiwook)
                {
                    Language.SetActiveLanguage(sender, SkillType.Shyriiwook);
                    language = SkillType.Shyriiwook;
                }

                var (r, g, b) = Language.GetColor(language);

                if (dbReceiver != null &&
                    dbReceiver.Settings.LanguageChatColors != null &&
                    dbReceiver.Settings.LanguageChatColors.ContainsKey(language))
                {
                    r = dbReceiver.Settings.LanguageChatColors[language].Red;
                    g = dbReceiver.Settings.LanguageChatColors[language].Green;
                    b = dbReceiver.Settings.LanguageChatColors[language].Blue;
                }

                if (language != SkillType.Basic)
                {
                    var languageName = Language.GetName(language);
                    finalMessage.Append(ColorToken.Custom($"[{languageName}] ", r, g, b));
                }

                foreach (var component in chatComponents)
                {
                    var text = component.Text;

                    if (component.IsTranslatable && language != SkillType.Basic)
                    {
                        text = Language.TranslateSnippetForListener(sender, receiver, language, component.Text);

                        if (r != 0 && g != 0 && b != 0)
                        {
                            text = ColorToken.Custom(text, r, g, b);
                        }
                    }

                    if (component.IsCustomColor)
                    {
                        text = ColorToken.Custom(text, component.Red, component.Green, component.Blue);
                    }

                    finalMessage.Append(text);
                }

                // Dispatch the final message - method depends on the original chat channel.
                // - Shout and party is sent as DMTalk. We do this to get around the restriction that
                //   the PC needs to be in the same area for the normal talk channel.
                //   We could use the native channels for these but the [shout] or [party chat] labels look silly.
                // - Talk and whisper are sent as-is.

                var finalChannel = channel;

                if (channel == ChatChannel.PlayerShout || channel == ChatChannel.PlayerParty)
                {
                    finalChannel = ChatChannel.DMTalk;
                }

                // There are a couple of colour overrides we want to use here.
                // - One for holonet (shout).
                // - One for comms (party chat).

                var finalMessageColoured = finalMessage.ToString();

                if (channel == ChatChannel.PlayerShout)
                {
                    finalMessageColoured = ColorToken.Custom(finalMessageColoured, 0, 180, 255);
                }
                else if (channel == ChatChannel.PlayerParty)
                {
                    finalMessageColoured = ColorToken.Orange(finalMessageColoured);
                }

                // set back to original sender, if it was changed by holocom connection
                sender = originalSender;

                ChatPlugin.SendMessage(finalChannel, finalMessageColoured, sender, receiver);
            }
        }


        private static List<CommunicationComponent> SplitMessageIntoComponents_Regular(string message)
        {
            var components = new List<CommunicationComponent>();

            var workingOn = WorkingOnEmoteStyle.None;
            var indexStart = 0;
            var length = -1;
            var depth = 0;

            for (var i = 0; i < message.Length; ++i)
            {
                var ch = message[i];

                if (ch == '[')
                {
                    if (workingOn == WorkingOnEmoteStyle.None || workingOn == WorkingOnEmoteStyle.Bracket)
                    {
                        depth += 1;
                        if (depth == 1)
                        {
                            var component = new CommunicationComponent
                            {
                                IsCustomColor = false,
                                IsTranslatable = true,
                                Text = message.Substring(indexStart, i - indexStart)
                            };
                            components.Add(component);

                            indexStart = i + 1;
                            workingOn = WorkingOnEmoteStyle.Bracket;
                        }
                    }
                }
                else if (ch == ']')
                {
                    if (workingOn == WorkingOnEmoteStyle.Bracket)
                    {
                        depth -= 1;
                        if (depth == 0)
                        {
                            length = i - indexStart;
                        }
                    }
                }
                else if (ch == '*')
                {
                    if (workingOn == WorkingOnEmoteStyle.None || workingOn == WorkingOnEmoteStyle.Asterisk)
                    {
                        if (depth == 0)
                        {
                            var component = new CommunicationComponent
                            {
                                IsCustomColor = false,
                                IsTranslatable = true,
                                Text = message.Substring(indexStart, i - indexStart)
                            };
                            components.Add(component);

                            depth = 1;
                            indexStart = i;
                            workingOn = WorkingOnEmoteStyle.Asterisk;
                        }
                        else
                        {
                            depth = 0;
                            length = i - indexStart + 1;
                        }
                    }
                }
                else if (ch == ':')
                {
                    if (workingOn == WorkingOnEmoteStyle.None || workingOn == WorkingOnEmoteStyle.ColonForward)
                    {
                        depth += 1;
                        if (depth == 1)
                        {
                            // Only match this colon if the next symbol is also a colon.
                            // This needs to be done because a single colon can be used in normal chat.
                            if (i + 1 < message.Length && message[i + 1] == ':')
                            {
                                var component = new CommunicationComponent
                                {
                                    IsCustomColor = false,
                                    IsTranslatable = true,
                                    Text = message.Substring(indexStart, i - indexStart)
                                };
                                components.Add(component);

                                indexStart = i;
                                workingOn = WorkingOnEmoteStyle.ColonForward;
                            }
                            else
                            {
                                depth -= 1;
                            }
                        }
                        else if (depth == 2)
                        {
                            workingOn = WorkingOnEmoteStyle.ColonBackward;
                        }
                    }
                    else if (workingOn == WorkingOnEmoteStyle.ColonBackward)
                    {
                        depth -= 1;
                        if (depth == 0)
                        {
                            length = i - indexStart + 1;
                        }
                    }
                }

                if (length != -1)
                {
                    var component = new CommunicationComponent
                    {
                        IsCustomColor = workingOn != WorkingOnEmoteStyle.Bracket || message[indexStart] == '[',
                        IsTranslatable = false,
                        Text = message.Substring(indexStart, length)
                    };
                    components.Add(component);

                    indexStart = i + 1;
                    length = -1;
                    workingOn = WorkingOnEmoteStyle.None;
                }
                else
                {
                    // If this is the last character in the string, we should just display what we've got.
                    if (i == message.Length - 1)
                    {
                        var component = new CommunicationComponent
                        {
                            IsCustomColor = depth != 0,
                            IsTranslatable = depth == 0,
                            Text = message.Substring(indexStart, i - indexStart + 1)
                        };
                        components.Add(component);
                    }
                }
            }

            // Strip any empty components.
            components.RemoveAll(comp => string.IsNullOrEmpty(comp.Text));

            return components;
        }

        private static List<CommunicationComponent> SplitMessageIntoComponents_Novel(string message)
        {
            var components = new List<CommunicationComponent>();

            var indexStart = 0;
            var workingOnQuotes = false;
            var workingOnBrackets = false;

            for (var i = 0; i < message.Length; ++i)
            {
                var ch = message[i];

                if (ch == '"')
                {
                    if (!workingOnQuotes)
                    {
                        var component = new CommunicationComponent
                        {
                            IsCustomColor = true,
                            IsTranslatable = false,
                            Text = message.Substring(indexStart, i - indexStart)
                        };
                        components.Add(component);

                        workingOnQuotes = true;
                        indexStart = i;
                    }
                    else
                    {
                        var component = new CommunicationComponent
                        {
                            IsCustomColor = false,
                            IsTranslatable = true,
                            Text = message.Substring(indexStart, i - indexStart + 1)
                        };
                        components.Add(component);

                        workingOnQuotes = false;
                        indexStart = i + 1;
                    }
                }
                else if (ch == '[')
                {
                    var translate = workingOnQuotes;

                    var component = new CommunicationComponent
                    {
                        IsCustomColor = !translate,
                        IsTranslatable = translate,
                        Text = message.Substring(indexStart, i - indexStart)
                    };
                    components.Add(component);

                    workingOnBrackets = true;
                    indexStart = i + 1;
                }
                else if (ch == ']')
                {
                    var component = new CommunicationComponent
                    {
                        IsCustomColor = true,
                        IsTranslatable = false,
                        Text = message.Substring(indexStart, i - indexStart)
                    };
                    components.Add(component);

                    workingOnBrackets = false;
                    indexStart = i + 1;
                }
            }

            {
                var translate = workingOnQuotes && !workingOnBrackets;

                var component = new CommunicationComponent
                {
                    IsCustomColor = !translate,
                    IsTranslatable = translate,
                    Text = message.Substring(indexStart, message.Length - indexStart)
                };
                components.Add(component);
            }

            // Strip any empty components.
            components.RemoveAll(comp => string.IsNullOrEmpty(comp.Text));

            return components;
        }

        public static EmoteStyle GetEmoteStyle(uint player)
        {
            if (GetIsPC(player) && !GetIsDM(player) && !GetIsDMPossessed(player))
            {
                var playerId = GetObjectUUID(player);
                var dbPlayer = DB.Get<Player>(playerId);

                return dbPlayer.EmoteStyle;
            }

            return EmoteStyle.Regular;
        }

        public static void SetEmoteStyle(uint player, EmoteStyle style)
        {
            if (GetIsPC(player) && !GetIsDM(player))
            {
                var playerId = GetObjectUUID(player);
                var dbPlayer = DB.Get<Player>(playerId);
                dbPlayer.EmoteStyle = style;
                DB.Set(dbPlayer);
            }
        }
    }
}
