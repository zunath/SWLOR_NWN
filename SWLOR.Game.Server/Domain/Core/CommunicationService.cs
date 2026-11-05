using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using ChatChannel = SWLOR.Game.Server.Core.NWNX.Enum.ChatChannel;
using Player = SWLOR.Game.Server.Entity.Player;
using PlayerShip = SWLOR.Game.Server.Entity.PlayerShip;
using SkillType = SWLOR.Game.Server.Service.SkillService.SkillType;
using WorldProperty = SWLOR.Game.Server.Entity.WorldProperty;

namespace SWLOR.Game.Server.Domain.Core
{
    /// <summary>
    /// Chat handling: the logic that used to be static in <c>Service.Communication</c>, moved here unchanged. The
    /// facade still carries the event handlers and forwards every call to the container's instance.
    /// </summary>
    public class CommunicationService : ICommunicationService
    {
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

        public (byte, byte, byte) OOCChatColor { get; } = (64, 64, 64);
        public (byte, byte, byte) EmoteChatColor { get; } = (0, 255, 0);

        private readonly IDBService _db;
        private readonly IDMPresenceService _dmPresence;
        private readonly IDMPreferenceService _dmPreference;
        private readonly IPartyService _party;
        private readonly IHoloComService _holoCom;
        private readonly IPlanetService _planet;
        private readonly IAreaService _area;
        private readonly IPropertyService _property;

        public CommunicationService(
            IDBService db,
            IDMPresenceService dmPresence,
            IDMPreferenceService dmPreference,
            IPartyService party,
            IHoloComService holoCom,
            IPlanetService planet,
            IAreaService area,
            IPropertyService property)
        {
            _db = db;
            _dmPresence = dmPresence;
            _dmPreference = dmPreference;
            _party = party;
            _holoCom = holoCom;
            _planet = planet;
            _area = area;
            _property = property;
        }

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

        public void ApplyCommsChannelName()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player))
                return;

            // The chat-channel selector treats angle-bracket color markup as TLK substitution tokens.
            PlayerPlugin.SetTlkOverride(player, PartyChatChannelNameStrRef, CommsChannelName);
            PlayerPlugin.SetTlkOverride(player, PartyChatMessagePrefixStrRef, CommsMessagePrefix);
        }

        public void ApplyShoutChannelName()
        {
            var player = GetEnteringObject();
            var isDM = GetIsDM(player) || GetIsDMPossessed(player);
            if (!GetIsPC(player) && !isDM)
                return;

            // Keep this per-client; a global override would also label DM Shout as disabled.
            PlayerPlugin.SetTlkOverride(player, 66751, isDM ? "Shout" : "Disabled");
        }

        /// <summary>
        /// Whenever a DM possesses a creature, clear its busy status to prevent ability usage issues.
        /// This runs before the engine accepts the possession, and the engine refuses creatures that have a
        /// master (familiars, henchmen, and other associates), so nothing here may assume the possession happened.
        /// </summary>
        public void OnDMPossess()
        {
            var target = StringToObject(EventsPlugin.GetEventData("TARGET"));
            if (GetIsObjectValid(target))
            {
                Activity.ClearBusy(target);
            }
        }

        /// <summary>
        /// When a player focuses the chatbar, set a typing indicator on the player; when
        /// unfocused, remove the indicator.
        /// </summary>

        public void TypingIndicator()
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
        public void ProcessNativeChatMessage()
        {
            ExecuteScript("dmfi_onplychat", OBJECT_SELF);
        }

        public void ProcessChatMessage()
        {
            var channel = ChatPlugin.GetChannel();

            // - PlayerTalk, PlayerWhisper, and PlayerParty are IC channels. These channels
            //   are subject to emote coloring and language translation. (see below for more info).
            // - PlayerParty is the Comms channel. It is sent to online players who match the
            //   sender's current location scope: planet, space area, event area, instance fallback, or starship.
            // - PlayerShout is disabled for players. DMs use the native channel behaviour.
            // - PlayerDM echoes back the message received to the sender.
            // - DMTalk and DMWhisper are a DM avatar's Talk and Whisper. The engine only uses the DM channels while
            //   the DM is not possessing a creature, so they get the same emote handling as possessed creatures.

            var handledChat =
                IsTalkChannel(channel) ||
                IsWhisperChannel(channel) ||
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
                // A HoloCom hologram relays its owner's words, so the owner's emote style applies.
                chatComponents = GetEmoteStyle(GetEffectiveChatSpeaker(sender)) == EmoteStyle.Regular
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
            // DMs whose Comms filter hides this message, by the creature they control.
            var mutedDMs = new HashSet<uint>();

            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                allPlayersAndDMs.Add(player);
            }

            // This is the Comms channel. Party members matching the sender's current
            // Comms range rules receive it. Nearby non-party listeners can still overhear it.
            // Party members outside that range trigger a warning.
            if (channel == ChatChannel.PlayerParty)
            {
                var (dmReaders, dmsFilteringOut) = GetDMCommsAudience(sender, allPlayersAndDMs);
                mutedDMs = dmsFilteringOut;

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

                recipients.AddRange(dmReaders);

                needsAreaCheck = true;
                distanceCheck = 20.0f;
            }
            // Normal talk - 20 units.
            else if (IsTalkChannel(channel))
            {
                needsAreaCheck = true;
                distanceCheck = 20.0f;
            }
            // Whisper - 4 units.
            else if (IsWhisperChannel(channel))
            {
                needsAreaCheck = true;
                distanceCheck = 4.0f;
            }

            if (needsAreaCheck)
            {
                AddNearbyListeners(sender, allPlayersAndDMs, distanceCheck, mutedDMs, recipients);
            }

            if (outOfRangeCommsPartyMembers > 0)
            {
                var dbSender = _db.Get<Player>(GetObjectUUID(sender));
                if (dbSender?.Settings?.DisplayCommsOutOfRangeWarnings ?? true)
                {
                    SendMessageToPC(sender, ColorToken.Red(CommsOutOfRangeMessage));
                }
            }

            // The speaker and the language being spoken are the same for every recipient, so resolve
            // them once before dispatching rather than recomputing (and re-writing language state)
            // per receiver.
            var speaker = GetEffectiveChatSpeaker(sender);
            var isHoloComRelay = sender != speaker;
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
                var dbReceiver = _db.Get<Player>(receiverId);

                // Generate the final message as perceived by obj.
                var finalMessage = new StringBuilder();

                if (channel == ChatChannel.PlayerParty)
                {
                    if (GetIsDM(receiver) || GetIsDMPossessed(receiver))
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

                // HoloCom holograms have a deliberately generic object name because the nearby
                // observers may know their owner by different names. Render the owner's chat name
                // in the message body for each observer while keeping the shared hologram name generic.
                if (isHoloComRelay)
                {
                    finalMessage.Append(GetColoredChatDisplayName(receiver, speaker));
                    finalMessage.Append(": ");
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
                        text = TranslateSnippetForListener(speaker, receiver, language, component.Text);
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

        /// <summary>
        /// Adds every player and DM who can overhear local speech: anyone in the sender's area and within range.
        /// Each one is addressed by the creature they control, so a possessing DM hears through the possessed creature
        /// and a DM back on their avatar hears through the avatar.
        /// </summary>
        /// <param name="sender">The creature that spoke.</param>
        /// <param name="onlinePlayers">Every object returned by GetFirstPC/GetNextPC.</param>
        /// <param name="distanceCheck">The channel's hearing range.</param>
        /// <param name="mutedDMs">DMs, by the creature they control, whose Comms filter hides this message.</param>
        /// <param name="recipients">The recipient list to add to.</param>
        private void AddNearbyListeners(
            uint sender,
            List<uint> onlinePlayers,
            float distanceCheck,
            HashSet<uint> mutedDMs,
            List<uint> recipients)
        {
            foreach (var player in onlinePlayers)
            {
                var target = _dmPresence.GetControlledCreature(player);
                var distance = GetDistanceBetween(sender, target);

                // Preserve the Master behavior for overhearing: anyone in the same area and
                // within the channel's local range can hear the message, regardless of party
                // membership or long-range Comms scope. Comms scope applies only to the party
                // member delivery pass above.
                if (GetArea(target) == GetArea(sender) &&
                    distance <= distanceCheck &&
                    !mutedDMs.Contains(target) &&
                    !recipients.Contains(target))
                {
                    recipients.Add(target);
                }
            }
        }

        private void SendProcessedChatMessage(
            ChatChannel channel,
            uint receiver,
            uint transportSpeaker,
            uint identitySpeaker,
            string message)
        {
            // Keep relayed speech in the chat log on its original channel. Talk/Whisper need the
            // area-local hologram as their native sender, whose shared name is safely generic.
            // The message body identifies its owner with the recipient's chat display name;
            // the native packet supplies any Whisper/Comms prefix.
            if (transportSpeaker != identitySpeaker)
            {
                ChatPlugin.SendMessage(channel, message, transportSpeaker, receiver);
                return;
            }

            // NWNX_Rename only patches the per-observer PC name override around three native chat
            // functions - Party, Shout, and Tell (see the plugin's HOOK_CHAT registrations). Talk and
            // Whisper are not among them; they render correctly today only because the speaker's
            // object update is already visible/patched for a nearby observer. DM_Talk is not hooked at
            // all, so routing cross-area Comms through it (as before) always rendered the speaker's
            // true name regardless of the override. Route Comms through the native Party channel
            // instead - Rename patches it, and an explicit per-receiver target dispatches directly
            // rather than broadcasting to nearby party members, so it still crosses area/planet
            // boundaries the same way DMTalk did.
            SendChatMessageWithChatNameOverride(
                receiver,
                identitySpeaker,
                () => ChatPlugin.SendMessage(channel, message, identitySpeaker, receiver));
        }

        private bool IsTalkChannel(ChatChannel channel) =>
            channel == ChatChannel.PlayerTalk || channel == ChatChannel.DMTalk;

        private bool IsWhisperChannel(ChatChannel channel) =>
            channel == ChatChannel.PlayerWhisper || channel == ChatChannel.DMWhisper;

        /// <summary>
        /// Splits the online DMs into those who read a Comms message and those whose Comms filter hides it.
        /// Each DM is identified by the creature they control, since chat sent to a possessing DM's avatar is dropped.
        /// </summary>
        /// <param name="sender">The creature that sent the Comms message.</param>
        /// <param name="onlinePlayers">Every object returned by GetFirstPC/GetNextPC.</param>
        private (List<uint> Readers, HashSet<uint> Muted) GetDMCommsAudience(uint sender, List<uint> onlinePlayers)
        {
            var readers = new List<uint>();
            var muted = new HashSet<uint>();
            // Party membership belongs to each player's own character, which is the avatar for a possessing DM.
            var senderCharacter = _dmPresence.GetDM(sender);

            foreach (var player in onlinePlayers)
            {
                if (!_dmPresence.IsControlledByDM(player))
                    continue;

                var controlled = _dmPresence.GetControlledCreature(player);
                if (_dmPreference.Get(controlled).CommsFilter == DMCommsFilter.AllParties ||
                    _party.IsInParty(senderCharacter, player))
                {
                    readers.Add(controlled);
                }
                else
                {
                    muted.Add(controlled);
                }
            }

            return (readers, muted);
        }

        private bool IsChatCommandMessage(string message)
        {
            return message.Length >= 2 &&
                   message[0] == '/' &&
                   message[1] != '/';
        }

        private bool IsCommsReceiverInRange(uint sender, uint receiver)
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

        private bool IsSpaceCommsArea(uint area)
        {
            return GetLocalBool(area, "SPACE") || GetName(area).StartsWith("Space -");
        }

        private bool IsEventCommsArea(uint area)
        {
            return GetLocalBool(area, EventCommsAreaVariable);
        }

        private PlanetType ResolveCommsPlanet(uint area)
        {
            var planet = _planet.GetPlanetType(area);
            if (planet != PlanetType.Invalid)
                return planet;

            var propertyId = _property.GetPropertyId(area);
            return ResolveCommsPlanetForPropertyId(propertyId, new HashSet<string>());
        }

        private PlanetType ResolveCommsPlanetForPropertyId(
            string propertyId,
            HashSet<string> visitedPropertyIds)
        {
            if (string.IsNullOrWhiteSpace(propertyId))
                return PlanetType.Invalid;

            var property = _db.Get<WorldProperty>(propertyId);
            return ResolveCommsPlanetForProperty(property, visitedPropertyIds);
        }

        private PlanetType ResolveCommsPlanetForProperty(
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

        private PlanetType ResolveCommsPlanetForLocation(
            PropertyLocation location,
            HashSet<string> visitedPropertyIds)
        {
            if (location == null)
                return PlanetType.Invalid;

            if (!string.IsNullOrWhiteSpace(location.InstancePropertyId))
                return ResolveCommsPlanetForPropertyId(location.InstancePropertyId, visitedPropertyIds);

            return ResolveCommsPlanetForAreaResref(location.AreaResref);
        }

        private PlanetType ResolveCommsPlanetForAreaResref(string areaResref)
        {
            if (string.IsNullOrWhiteSpace(areaResref))
                return PlanetType.Invalid;

            var area = _area.GetAreaByResref(areaResref);
            if (GetIsObjectValid(area))
            {
                var planet = _planet.GetPlanetType(area);
                if (planet != PlanetType.Invalid)
                    return planet;
            }

            return _planet.GetPlanetTypeByAreaResref(areaResref);
        }

        private bool IsSameStarshipComms(uint sender, uint receiver)
        {
            var senderShipPropertyId = ResolveStarshipPropertyIdForComms(sender);
            if (string.IsNullOrWhiteSpace(senderShipPropertyId))
                return false;

            return ResolveStarshipPropertyIdForComms(receiver) == senderShipPropertyId;
        }

        private string ResolveStarshipPropertyIdForComms(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return string.Empty;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            if (dbPlayer != null && !string.IsNullOrWhiteSpace(dbPlayer.ActiveShipId) && IsPlayerInSpaceMode(player))
            {
                var dbShip = _db.Get<PlayerShip>(dbPlayer.ActiveShipId);
                if (dbShip != null)
                    return dbShip.PropertyId;
            }

            var area = GetArea(player);
            var propertyId = _property.GetPropertyId(area);
            if (string.IsNullOrWhiteSpace(propertyId))
                return string.Empty;

            var property = _db.Get<WorldProperty>(propertyId);
            return property?.PropertyType == PropertyType.Starship
                ? property.Id
                : string.Empty;
        }

        private uint GetEffectiveChatSpeaker(uint sender)
        {
            var hologramOwner = _holoCom.GetHoloGramOwner(sender);
            return GetIsObjectValid(hologramOwner)
                ? hologramOwner
                : sender;
        }


        private List<CommunicationComponent> SplitMessageIntoComponents_Regular(string message)
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

        private List<CommunicationComponent> SplitMessageIntoComponents_Novel(string message)
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

        // The four methods below call services that are not converted yet and whose static code reads the database or
        // the native engine for a real player. Each body is the original call, so a test subclass can answer them.

        /// <summary>The listener's version of a snippet: <c>Language.TranslateSnippetForListener</c>, whose rolls and skill reads are unchanged.</summary>
        protected virtual string TranslateSnippetForListener(uint speaker, uint listener, SkillType language, string snippet)
        {
            return Language.TranslateSnippetForListener(speaker, listener, language, snippet);
        }

        /// <summary>The colored chat name of <paramref name="target"/> as <paramref name="observer"/> sees it: <c>PlayerName.GetColoredChatDisplayName</c>.</summary>
        protected virtual string GetColoredChatDisplayName(uint observer, uint target)
        {
            return PlayerName.GetColoredChatDisplayName(observer, target);
        }

        /// <summary>Runs <paramref name="sendMessage"/> with the per-observer chat name override in place: <c>PlayerName.SendChatMessageWithChatNameOverride</c>.</summary>
        protected virtual void SendChatMessageWithChatNameOverride(uint observer, uint target, Action sendMessage)
        {
            PlayerName.SendChatMessageWithChatNameOverride(observer, target, sendMessage);
        }

        /// <summary>True when the player is flying their ship: <c>Space.IsPlayerInSpaceMode</c>, which reads the static database.</summary>
        protected virtual bool IsPlayerInSpaceMode(uint player)
        {
            return Service.Space.IsPlayerInSpaceMode(player);
        }

        public EmoteStyle GetEmoteStyle(uint player)
        {
            // DMs keep one emote style for their avatar and every creature they possess.
            if (GetIsDM(player) || GetIsDMPossessed(player))
                return _dmPreference.Get(player).EmoteStyle;

            if (GetIsPC(player))
            {
                var playerId = GetObjectUUID(player);
                var dbPlayer = _db.Get<Player>(playerId);

                return dbPlayer?.EmoteStyle ?? EmoteStyle.Regular;
            }

            return EmoteStyle.Regular;
        }

        /// <summary>
        /// Saves a player's or DM's emote style.
        /// </summary>
        /// <param name="player">A player, a DM avatar, or the creature a DM possesses.</param>
        /// <param name="style">The new style.</param>
        /// <returns>false when there was nowhere to save it.</returns>
        public bool SetEmoteStyle(uint player, EmoteStyle style)
        {
            if (GetIsDM(player) || GetIsDMPossessed(player))
                return _dmPreference.Update(player, preferences => preferences.EmoteStyle = style);

            if (!GetIsPC(player))
                return false;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            if (dbPlayer == null)
                return false;

            dbPlayer.EmoteStyle = style;
            _db.Set(dbPlayer);
            return true;
        }
    }
}
