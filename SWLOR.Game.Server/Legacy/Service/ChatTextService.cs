using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Event.Module;
using SWLOR.Game.Server.Legacy.Event.SWLOR;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Messaging;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using ChatChannel = SWLOR.Game.Server.Core.NWNX.Enum.ChatChannel;
using SkillType = SWLOR.Game.Server.Enumeration.SkillType;

namespace SWLOR.Game.Server.Legacy.Service
{
    public class ChatComponent
    {
        public string m_Text;
        public bool m_Translatable;
        public bool m_CustomColour;
        public byte m_ColourRed;
        public byte m_ColourGreen;
        public byte m_ColourBlue;
    };

    public static class ChatTextService
    {

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleEnter>(message => OnModuleEnter());
            MessageHub.Instance.Subscribe<OnModuleNWNXChat>(message => OnModuleNWNXChat());
        }

        private static void OnModuleNWNXChat()
        {
            var channel = (ChatChannel)Chat.GetChannel();

            // So we're going to play with a couple of channels here.

            // - PlayerTalk, PlayerWhisper, PlayerParty, and PlayerShout are all IC channels. These channels
            //   are subject to emote colouring and language translation. (see below for more info).
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

            if (!inCharacterChat && !messageToDm)
            {
                // We don't much care about traffic on the other channels.
                return;
            }

            NWObject sender = Chat.GetSender();
            var message = Chat.GetMessage().Trim();

            if (string.IsNullOrWhiteSpace(message))
            {
                // We can't handle empty messages, so skip it.
                return;
            }

            if (ChatCommandService.CanHandleChat(sender, message) ||
                BaseService.CanHandleChat(sender) ||
                CraftService.CanHandleChat(sender) ||
                MarketService.CanHandleChat(sender.Object) ||
                MessageBoardService.CanHandleChat(sender) ||
                ItemService.CanHandleChat(sender))
            {
                // This will be handled by other services, so just bail.
                return;
            }

            if (channel == ChatChannel.PlayerDM)
            {
                // Simply echo the message back to the player.
                Chat.SendMessage((int)ChatChannel.ServerMessage, "(Sent to DM) " + message, sender, sender);
                return;
            }

            // At this point, every channel left is one we want to manually handle.
            Chat.SkipMessage();

            // If this is a shout message, and the holonet is disabled, we disallow it.
            if (channel == ChatChannel.PlayerShout && sender.IsPC && 
                GetLocalBool(sender, "DISPLAY_HOLONET") == false)
            {
                NWPlayer player = sender.Object;
                player.SendMessage("You have disabled the holonet and cannot send this message.");
                return;
            }
            
            List<ChatComponent> chatComponents;

            // Quick early out - if we start with "//" or "((", this is an OOC message.
            var isOOC = false;
            if (message.Length >= 2 && (message.Substring(0, 2) == "//" || message.Substring(0, 2) == "(("))
            {
                var component = new ChatComponent
                {
                    m_Text = message,
                    m_CustomColour = true,
                    m_ColourRed = 64,
                    m_ColourGreen = 64,
                    m_ColourBlue = 64,
                    m_Translatable = false
                };

                chatComponents = new List<ChatComponent> {component};

                if (channel == ChatChannel.PlayerShout)
                {
                    SendMessageToPC(sender, "Out-of-character messages cannot be sent on the Holonet.");
                    return;
                }

                isOOC = true;
            }
            else
            {
                if (EmoteStyleService.GetEmoteStyle(sender) == EmoteStyle.Regular)
                {
                    chatComponents = SplitMessageIntoComponents_Regular(message);
                }
                else
                {
                    chatComponents = SplitMessageIntoComponents_Novel(message);
                }

                // For any components with colour, set the emote colour.
                foreach (var component in chatComponents)
                {
                    if (component.m_CustomColour)
                    {
                        component.m_ColourRed = 0;
                        component.m_ColourGreen = 255;
                        component.m_ColourBlue = 0;
                    }
                }
            }

            // Now, depending on the chat channel, we need to build a list of recipients.
            var needsAreaCheck = false;
            var distanceCheck = 0.0f;
            
            // The sender always wants to see their own message.
            var recipients = new List<NWObject> {sender};

            // This is a server-wide holonet message (that receivers can toggle on or off).
            if (channel == ChatChannel.PlayerShout)
            {
                recipients.AddRange(NWModule.Get().Players.Where(player => GetLocalBool(player, "DISPLAY_HOLONET") == true));
                recipients.AddRange(AppCache.ConnectedDMs);
            }
            // This is the normal party chat, plus everyone within 20 units of the sender.
            else if (channel == ChatChannel.PlayerParty)
            {
                // Can an NPC use the playerparty channel? I feel this is safe ...
                NWPlayer player = sender.Object;
                recipients.AddRange(player.PartyMembers.Cast<NWObject>().Where(x => x != sender));
                recipients.AddRange(AppCache.ConnectedDMs);

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
                var area = GetArea(sender);
                for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
                {
                    if (GetIsPC(obj) && GetDistanceBetween(sender, obj) <= distanceCheck)
                    {
                        recipients.Add(obj);
                    }
                }

                recipients.AddRange(AppCache.ConnectedDMs.Where(dm => dm.Area == sender.Area && GetDistanceBetween(sender, dm) <= distanceCheck));
            }

            // Now we have a list of who is going to actually receive a message, we need to modify
            // the message for each recipient then dispatch them.

            foreach (var obj in recipients.Distinct())
            {
                // Generate the final message as perceived by obj.

                var finalMessage = new StringBuilder();

                if (channel == ChatChannel.PlayerShout)
                {
                    finalMessage.Append("[Holonet] ");
                }
                else if (channel == ChatChannel.PlayerParty)
                {
                    finalMessage.Append("[Comms] ");

                    if (obj.IsDM)
                    {
                        // Convenience for DMs - append the party members.
                        finalMessage.Append("{ ");

                        var count = 0;
                        NWPlayer player = sender.Object;
                        var partyMembers = player.PartyMembers.ToList();

                        foreach (var otherPlayer in partyMembers)
                        {
                            var name = otherPlayer.Name;
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
                if (GetIsObjectValid(HoloComService.GetHoloGramOwner(sender)) == true)
                {
                    sender = HoloComService.GetHoloGramOwner(sender);
                }

                var language = Language.GetActiveLanguage(sender);
                
                // Wookiees cannot speak any other language (but they can understand them).
                // Swap their language if they attempt to speak in any other language.
                var race = (RacialType) GetRacialType(sender);                
                if (race == RacialType.Wookiee && language != SkillType.Shyriiwook)
                {
                    Language.SetActiveLanguage(sender, SkillType.Shyriiwook);
                    language = SkillType.Shyriiwook;
                }

                var colour = Language.GetColour(language);
                var r = (byte)(colour >> 24 & 0xFF);
                var g = (byte)(colour >> 16 & 0xFF);
                var b = (byte)(colour >> 8 & 0xFF);

                if (language != SkillType.Basic)
                {
                    var languageName = Language.GetName(language);
                    finalMessage.Append(ColorToken.Custom($"[{languageName}] ", r, g, b));
                }

                foreach (var component in chatComponents)
                {
                    var text = component.m_Text;

                    if (component.m_Translatable && language != SkillType.Basic)
                    {
                        text = Language.TranslateSnippetForListener(sender, obj.Object, language, component.m_Text);

                        if (colour != 0)
                        {
                            text = ColorToken.Custom(text, r, g, b);
                        }
                    }

                    if (component.m_CustomColour)
                    {
                        text = ColorToken.Custom(text, component.m_ColourRed, component.m_ColourGreen, component.m_ColourBlue);
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

                Chat.SendMessage((int)finalChannel, finalMessageColoured, sender, obj);
            }

            MessageHub.Instance.Publish(new OnChatProcessed(sender, channel, isOOC));
        }

        private static void OnModuleEnter()
        {
            NWPlayer player = GetEnteringObject();
            if (!player.IsPlayer) return;

            var dbPlayer = DataService.Player.GetByID(player.GlobalID);
            SetLocalBool(player, "DISPLAY_HOLONET", dbPlayer.DisplayHolonet ? true : false);
        }
        
        private enum WorkingOnEmoteStyle
        {
            None,
            Asterisk,
            Bracket,
            ColonForward,
            ColonBackward
        };

        private static List<ChatComponent> SplitMessageIntoComponents_Regular(string message)
        {
            var components = new List<ChatComponent>();

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
                            var component = new ChatComponent
                            {
                                m_CustomColour = false,
                                m_Translatable = true,
                                m_Text = message.Substring(indexStart, i - indexStart)
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
                            var component = new ChatComponent
                            {
                                m_CustomColour = false,
                                m_Translatable = true,
                                m_Text = message.Substring(indexStart, i - indexStart)
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
                                var component = new ChatComponent
                                {
                                    m_CustomColour = false,
                                    m_Translatable = true,
                                    m_Text = message.Substring(indexStart, i - indexStart)
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
                    var component = new ChatComponent
                    {
                        m_CustomColour = workingOn != WorkingOnEmoteStyle.Bracket || message[indexStart] == '[',
                        m_Translatable = false,
                        m_Text = message.Substring(indexStart, length)
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
                        var component = new ChatComponent
                        {
                            m_CustomColour = depth != 0,
                            m_Translatable = depth == 0,
                            m_Text = message.Substring(indexStart, i - indexStart + 1)
                        };
                        components.Add(component);
                    }
                }
            }

            // Strip any empty components.
            components.RemoveAll(comp => string.IsNullOrEmpty(comp.m_Text));

            return components;
        }

        private static List<ChatComponent> SplitMessageIntoComponents_Novel(string message)
        {
            var components = new List<ChatComponent>();

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
                        var component = new ChatComponent
                        {
                            m_CustomColour = true,
                            m_Translatable = false,
                            m_Text = message.Substring(indexStart, i - indexStart)
                        };
                        components.Add(component);

                        workingOnQuotes = true;
                        indexStart = i;
                    }
                    else
                    {
                        var component = new ChatComponent
                        {
                            m_CustomColour = false,
                            m_Translatable = true,
                            m_Text = message.Substring(indexStart, i - indexStart + 1)
                        };
                        components.Add(component);

                        workingOnQuotes = false;
                        indexStart = i + 1;
                    }
                }
                else if (ch == '[')
                {
                    var translate = workingOnQuotes;

                    var component = new ChatComponent
                    {
                        m_CustomColour = !translate,
                        m_Translatable = translate,
                        m_Text = message.Substring(indexStart, i - indexStart)
                    };
                    components.Add(component);

                    workingOnBrackets = true;
                    indexStart = i + 1;
                }
                else if (ch == ']')
                {
                    var component = new ChatComponent
                    {
                        m_CustomColour = true,
                        m_Translatable = false,
                        m_Text = message.Substring(indexStart, i - indexStart)
                    };
                    components.Add(component);

                    workingOnBrackets = false;
                    indexStart = i + 1;
                }
            }

            {
                var translate = workingOnQuotes && !workingOnBrackets;

                var component = new ChatComponent
                {
                    m_CustomColour = !translate,
                    m_Translatable = translate,
                    m_Text = message.Substring(indexStart, message.Length - indexStart)
                };
                components.Add(component);
            }

            // Strip any empty components.
            components.RemoveAll(comp => string.IsNullOrEmpty(comp.m_Text));

            return components;
        }

    }
}
