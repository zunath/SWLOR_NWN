using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using ChatChannel = SWLOR.Game.Server.Core.NWNX.Enum.ChatChannel;
using Player = SWLOR.Game.Server.Entity.Player;
using PlayerShip = SWLOR.Game.Server.Entity.PlayerShip;
using SkillType = SWLOR.Game.Server.Service.SkillService.SkillType;
using WorldProperty = SWLOR.Game.Server.Entity.WorldProperty;

namespace SWLOR.Game.Server.Service
{
    public static class Communication
    {
        private const string DMPossessedCreature = "COMMUNICATION_DM_POSSESSED_CREATURE";
        public const string EventCommsAreaVariable = "COMMS_EVENT_AREA";
        private const string DisabledChannelMessage = "This chat channel is disabled.";
        private const string CommsOutOfRangeMessage = "Your Comms message could not reach one or more out-of-range receivers.";
        // Base-game dialog.tlk 66755 is the PlayerParty chat-input label, while 10303 is the
        // prefix rendered on received PlayerParty messages. Comms must still use the native
        // Party packet so NWNX_Rename can apply observer-specific player names, but neither
        // player-facing chat label should expose the underlying Party transport.
        private const int PartyChatChannelNameStrRef = 66755;
        private const int PartyChatMessagePrefixStrRef = 10303;
        private const string CommsChannelName = "Comms";
        private const string CommsMessagePrefix = "[Comms] ";

        public static (byte, byte, byte) OOCChatColor { get; } = (64, 64, 64);
        public static (byte, byte, byte) EmoteChatColor { get; } = (0, 255, 0);

        private class CommunicationComponent
        {
            public string Text { get; set; }
            public bool IsTranslatable { get; set; }
            public bool IsCustomColor { get; set; }
            public bool IsOOC { get; set; }
            public bool IsEmote { get; set; }
        }

        private enum WorkingOnEmoteStyle
        {
            None,
            Asterisk,
            Bracket,
            ColonForward,
            ColonBackward
        };

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void ApplyCommsChannelName()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player))
                return;

            // The chat-channel selector treats angle-bracket color markup as TLK substitution tokens.
            PlayerPlugin.SetTlkOverride(player, PartyChatChannelNameStrRef, CommsChannelName);
            PlayerPlugin.SetTlkOverride(player, PartyChatMessagePrefixStrRef, CommsMessagePrefix);
        }

        /// <summary>
        /// Whenever a DM possesses a creature, track the NPC on their object so that messages can be
        /// sent to them during the possession.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDMPossessBefore)]
        [NWNEventHandler(ScriptName.OnDMPossessFullPowerBefore)]
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
            // Possession - Store the variable and clear busy status
            else
            {
                SetLocalObject(dm, DMPossessedCreature, target);

                // Clear busy status of the possessed creature to prevent ability usage issues
                Activity.ClearBusy(target);
            }
        }

        /// <summary>
        /// When a player focuses the chatbar, set a typing indicator on the player; when
        /// unfocused, remove the indicator.
        /// </summary>

        [NWNEventHandler(ScriptName.OnModuleGuiEvent)]
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
        [NWNEventHandler(ScriptName.OnModuleChat)]
        public static void ProcessNativeChatMessage()
        {
            ExecuteScript("dmfi_onplychat", OBJECT_SELF);
        }

        [NWNEventHandler(ScriptName.OnNWNXChat)]
        public static void ProcessChatMessage()
        {
            var channel = ChatPlugin.GetChannel();

            // - PlayerTalk, PlayerWhisper, and PlayerParty are IC channels. These channels
            //   are subject to emote coloring and language translation. (see below for more info).
            // - PlayerParty is the Comms channel. It is sent to online players who match the
            //   sender's current location scope: planet, space area, event area, instance fallback, or starship.
            // - PlayerShout is disabled for players. DMs use the native channel behaviour.
            // - PlayerDM echoes back the message received to the sender.

            var handledChat =
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
            if (!handledChat && !messageToDm) return;

            if (string.IsNullOrWhiteSpace(message))
            {
                // We can't handle empty messages, so skip it.
                return;
            }

            if (IsChatCommandMessage(message))
            {
                return;
            }

            // Echo the message back to the player.
            if (messageToDm)
            {
                ChatPlugin.SendMessage(ChatChannel.ServerMessage, "(Sent to DM) " + message, sender, sender);
                return;
            }

            if (channel == ChatChannel.PlayerShout && (GetIsDM(sender) || GetIsDMPossessed(sender)))
            {
                return;
            }

            ChatPlugin.SkipMessage();

            if (channel == ChatChannel.PlayerShout)
            {
                SendMessageToPC(sender, ColorToken.Red(DisabledChannelMessage));
                return;
            }

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
                    IsOOC = true,
                    IsTranslatable = false
                };
                chatComponents.Add(component);
            }
            // Another early out - a completely empty message will just be skipped.
            else if (string.IsNullOrWhiteSpace(message.Trim()))
            {
                return;
            }
            else
            {
                chatComponents = GetEmoteStyle(sender) == EmoteStyle.Regular
                    ? SplitMessageIntoComponents_Regular(message)
                    : SplitMessageIntoComponents_Novel(message);

                // For any components with color, set the emote color.
                foreach (var component in chatComponents)
                {
                    if (component.IsCustomColor)
                    {
                        component.IsEmote = true;
                    }
                }
            }

            // Now, depending on the chat channel, we need to build a list of recipients.
            var needsAreaCheck = false;
            var distanceCheck = 0.0f;
            var outOfRangeCommsPartyMembers = 0;

            // The sender always wants to see their own message.
            var recipients = new List<uint> { sender };
            var allPlayersAndDMs = new List<uint>();
            var allDMs = new List<uint>();

            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                allPlayersAndDMs.Add(player);

                if (GetIsDM(player) || GetIsDMPossessed(player))
                {
                    allDMs.Add(player);
                }
            }

            // This is the Comms channel. Party members matching the sender's current
            // Comms range rules receive it. Nearby non-party listeners can still overhear it.
            // Party members outside that range trigger a warning.
            if (channel == ChatChannel.PlayerParty)
            {
                for (var member = GetFirstFactionMember(sender); GetIsObjectValid(member); member = GetNextFactionMember(sender))
                {
                    if (sender == member) continue;

                    if (IsCommsReceiverInRange(sender, member))
                    {
                        recipients.Add(member);
                    }
                    else if (GetIsPC(member) &&
                             !GetIsDM(member) &&
                             !GetIsDMPossessed(member))
                    {
                        outOfRangeCommsPartyMembers++;
                    }
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

                    // Preserve the Master behavior for overhearing: anyone in the same area and
                    // within the channel's local range can hear the message, regardless of party
                    // membership or long-range Comms scope. Comms scope applies only to the party
                    // member delivery pass above.
                    if (GetArea(target) == GetArea(sender) &&
                        distance <= distanceCheck &&
                        !recipients.Contains(target))
                    {
                        recipients.Add(target);
                    }
                }
            }

            if (outOfRangeCommsPartyMembers > 0)
            {
                var dbSender = DB.Get<Player>(GetObjectUUID(sender));
                if (dbSender?.Settings?.DisplayCommsOutOfRangeWarnings ?? true)
                {
                    SendMessageToPC(sender, ColorToken.Red(CommsOutOfRangeMessage));
                }
            }

            // The speaker and the language being spoken are the same for every recipient, so resolve
            // them once before dispatching rather than recomputing (and re-writing language state)
            // per receiver.
            var speaker = GetEffectiveChatSpeaker(sender);
            var language = Language.GetActiveLanguage(speaker);

            // Wookiees cannot speak any other language (but they can understand them).
            // Swap their language if they attempt to speak in any other language.
            if (GetRacialType(speaker) == RacialType.Wookiee && language != SkillType.Shyriiwook)
            {
                Language.SetActiveLanguage(speaker, SkillType.Shyriiwook);
                language = SkillType.Shyriiwook;
            }

            // Now we have a list of who is going to actually receive a message, we need to modify
            // the message for each recipient then dispatch them.
            foreach (var receiver in recipients.Distinct())
            {
                var receiverId = GetObjectUUID(receiver);
                var dbReceiver = DB.Get<Player>(receiverId);

                // Generate the final message as perceived by obj.
                var finalMessage = new StringBuilder();

                if (channel == ChatChannel.PlayerParty)
                {
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
                var (r, g, b) = Language.GetColor(language);

                if (dbReceiver?.Settings?.LanguageChatColors != null &&
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
                        text = Language.TranslateSnippetForListener(speaker, receiver, language, component.Text);
                    }

                    if (component.IsOOC)
                    {
                        if (dbReceiver?.Settings?.OOCChatColor != null)
                        {
                            r = dbReceiver.Settings.OOCChatColor.Red;
                            g = dbReceiver.Settings.OOCChatColor.Green;
                            b = dbReceiver.Settings.OOCChatColor.Blue;
                        }
                        else
                        {
                            r = OOCChatColor.Item1;
                            g = OOCChatColor.Item2;
                            b = OOCChatColor.Item3;
                        }
                    }

                    if (component.IsEmote)
                    {
                        byte emoteRed, emoteGreen, emoteBlue;

                        if (dbReceiver?.Settings?.EmoteChatColor != null)
                        {
                            emoteRed = dbReceiver.Settings.EmoteChatColor.Red;
                            emoteGreen = dbReceiver.Settings.EmoteChatColor.Green;
                            emoteBlue = dbReceiver.Settings.EmoteChatColor.Blue;
                        }
                        else
                        {
                            emoteRed = EmoteChatColor.Item1;
                            emoteGreen = EmoteChatColor.Item2;
                            emoteBlue = EmoteChatColor.Item3;
                        }
                        text = ColorToken.Custom(text, emoteRed, emoteGreen, emoteBlue);
                    }
                    else
                    {
                        text = ColorToken.Custom(text, r, g, b);
                    }

                    finalMessage.Append(text);
                }

                var finalMessageColored = finalMessage.ToString();

                if (channel == ChatChannel.PlayerParty)
                {
                    finalMessageColored = ColorToken.Orange(finalMessageColored);
                }

                SendProcessedChatMessage(channel, receiver, sender, speaker, finalMessageColored);
            }
        }

        private static void SendProcessedChatMessage(
            ChatChannel channel,
            uint receiver,
            uint transportSpeaker,
            uint identitySpeaker,
            string message)
        {
            // A HoloCom message is spoken by a copied creature in the receiver's area. The hologram's
            // owner supplies the player-facing identity and language, but the area-local hologram must
            // remain the native transport speaker or Talk/Whisper packets from the distant owner are
            // discarded by the engine. Holograms retain their generic, display-safe object name.
            // NWNX_Rename only patches the per-observer PC name override around three native chat
            // functions - Party, Shout, and Tell (see the plugin's HOOK_CHAT registrations). Talk and
            // Whisper are not among them; they render correctly today only because the speaker's
            // object update is already visible/patched for a nearby observer. DM_Talk is not hooked at
            // all, so routing cross-area Comms through it (as before) always rendered the speaker's
            // true name regardless of the override. Route Comms through the native Party channel
            // instead - Rename patches it, and an explicit per-receiver target dispatches directly
            // rather than broadcasting to nearby party members, so it still crosses area/planet
            // boundaries the same way DMTalk did.
            PlayerName.SendChatMessageWithChatNameOverride(
                receiver,
                identitySpeaker,
                () => ChatPlugin.SendMessage(channel, message, transportSpeaker, receiver));
        }

        private static bool IsChatCommandMessage(string message)
        {
            return message.Length >= 2 &&
                   message[0] == '/' &&
                   message[1] != '/';
        }

        private static bool IsCommsReceiverInRange(uint sender, uint receiver)
        {
            if (!GetIsObjectValid(receiver))
                return false;

            if (GetIsDM(receiver) || GetIsDMPossessed(receiver))
                return true;

            if (IsSameStarshipComms(sender, receiver))
                return true;

            var senderArea = GetArea(sender);
            var receiverArea = GetArea(receiver);
            if (!GetIsObjectValid(senderArea) || !GetIsObjectValid(receiverArea))
                return false;

            if (IsSpaceCommsArea(senderArea))
                return senderArea == receiverArea;

            if (IsEventCommsArea(senderArea))
                return IsEventCommsArea(receiverArea);

            var senderPlanet = ResolveCommsPlanet(senderArea);
            if (senderPlanet != PlanetType.Invalid)
            {
                return ResolveCommsPlanet(receiverArea) == senderPlanet;
            }

            return senderArea == receiverArea;
        }

        private static bool IsSpaceCommsArea(uint area)
        {
            return GetLocalBool(area, "SPACE") || GetName(area).StartsWith("Space -");
        }

        private static bool IsEventCommsArea(uint area)
        {
            return GetLocalBool(area, EventCommsAreaVariable);
        }

        private static PlanetType ResolveCommsPlanet(uint area)
        {
            var planet = Planet.GetPlanetType(area);
            if (planet != PlanetType.Invalid)
                return planet;

            var propertyId = Property.GetPropertyId(area);
            return ResolveCommsPlanetForPropertyId(propertyId, new HashSet<string>());
        }

        private static PlanetType ResolveCommsPlanetForPropertyId(
            string propertyId,
            HashSet<string> visitedPropertyIds)
        {
            if (string.IsNullOrWhiteSpace(propertyId))
                return PlanetType.Invalid;

            var property = DB.Get<WorldProperty>(propertyId);
            return ResolveCommsPlanetForProperty(property, visitedPropertyIds);
        }

        private static PlanetType ResolveCommsPlanetForProperty(
            WorldProperty property,
            HashSet<string> visitedPropertyIds)
        {
            if (property == null || !visitedPropertyIds.Add(property.Id))
                return PlanetType.Invalid;

            if (property.PropertyType == PropertyType.City)
                return ResolveCommsPlanetForAreaResref(property.ParentPropertyId);

            if (property.PropertyType == PropertyType.Starship)
            {
                if (property.Positions.TryGetValue(PropertyLocationType.CurrentPosition, out var currentPosition))
                {
                    var currentPlanet = ResolveCommsPlanetForLocation(currentPosition, visitedPropertyIds);
                    return currentPlanet;
                }

                if (property.Positions.TryGetValue(PropertyLocationType.DockPosition, out var dockPosition))
                {
                    return ResolveCommsPlanetForLocation(dockPosition, visitedPropertyIds);
                }

                return PlanetType.Invalid;
            }

            if (string.IsNullOrWhiteSpace(property.ParentPropertyId))
                return PlanetType.Invalid;

            var parentPlanet = ResolveCommsPlanetForPropertyId(property.ParentPropertyId, visitedPropertyIds);
            return parentPlanet != PlanetType.Invalid
                ? parentPlanet
                : ResolveCommsPlanetForAreaResref(property.ParentPropertyId);
        }

        private static PlanetType ResolveCommsPlanetForLocation(
            PropertyLocation location,
            HashSet<string> visitedPropertyIds)
        {
            if (location == null)
                return PlanetType.Invalid;

            if (!string.IsNullOrWhiteSpace(location.InstancePropertyId))
                return ResolveCommsPlanetForPropertyId(location.InstancePropertyId, visitedPropertyIds);

            return ResolveCommsPlanetForAreaResref(location.AreaResref);
        }

        private static PlanetType ResolveCommsPlanetForAreaResref(string areaResref)
        {
            if (string.IsNullOrWhiteSpace(areaResref))
                return PlanetType.Invalid;

            var area = Area.GetAreaByResref(areaResref);
            if (GetIsObjectValid(area))
            {
                var planet = Planet.GetPlanetType(area);
                if (planet != PlanetType.Invalid)
                    return planet;
            }

            return Planet.GetPlanetTypeByAreaResref(areaResref);
        }

        private static bool IsSameStarshipComms(uint sender, uint receiver)
        {
            var senderShipPropertyId = ResolveStarshipPropertyIdForComms(sender);
            if (string.IsNullOrWhiteSpace(senderShipPropertyId))
                return false;

            return ResolveStarshipPropertyIdForComms(receiver) == senderShipPropertyId;
        }

        private static string ResolveStarshipPropertyIdForComms(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return string.Empty;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer != null && !string.IsNullOrWhiteSpace(dbPlayer.ActiveShipId) && Space.IsPlayerInSpaceMode(player))
            {
                var dbShip = DB.Get<PlayerShip>(dbPlayer.ActiveShipId);
                if (dbShip != null)
                    return dbShip.PropertyId;
            }

            var area = GetArea(player);
            var propertyId = Property.GetPropertyId(area);
            if (string.IsNullOrWhiteSpace(propertyId))
                return string.Empty;

            var property = DB.Get<WorldProperty>(propertyId);
            return property?.PropertyType == PropertyType.Starship
                ? property.Id
                : string.Empty;
        }

        private static uint GetEffectiveChatSpeaker(uint sender)
        {
            var hologramOwner = HoloCom.GetHoloGramOwner(sender);
            return GetIsObjectValid(hologramOwner)
                ? hologramOwner
                : sender;
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
                    // This block only runs when an emote delimiter has closed (bracket, asterisk, or
                    // double-colon), so the captured segment is always an emote and must carry the
                    // emote (custom) color. The previous bracket-specific condition left bracketed
                    // emotes uncolored and untranslated, rendering them as plain language-colored text.
                    var component = new CommunicationComponent
                    {
                        IsCustomColor = true,
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

                return dbPlayer?.EmoteStyle ?? EmoteStyle.Regular;
            }

            return EmoteStyle.Regular;
        }

        public static void SetEmoteStyle(uint player, EmoteStyle style)
        {
            if (GetIsPC(player) && !GetIsDM(player))
            {
                var playerId = GetObjectUUID(player);
                var dbPlayer = DB.Get<Player>(playerId);
                if (dbPlayer == null)
                    return;

                dbPlayer.EmoteStyle = style;
                DB.Set(dbPlayer);
            }
        }
    }
}
