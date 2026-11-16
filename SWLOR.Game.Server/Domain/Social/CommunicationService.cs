using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Enumeration;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using ChatChannel = SWLOR.Game.Server.Core.NWNX.Enum.ChatChannel;
using Player = SWLOR.Game.Server.Entity.Player;
using SkillType = SWLOR.Game.Server.Domain.Foundation.SkillType;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// Chat handling: the logic that used to be static in the <c>Communication</c> facade, moved here unchanged. The
    /// event handlers run on the container's instance; static callers reach it through <c>GameServices.Communication</c>.
    /// </summary>
    public class CommunicationService : ICommunicationService
    {
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
        private readonly IHoloComService _holoCom;
        private readonly IActivityService _activity;
        private readonly ILanguageService _language;
        private readonly IPlayerNameService _playerName;
        private readonly ICommunicationAudienceService _commsAudience;
        private readonly ICommunicationMessageSplitService _messageSplit;

        public CommunicationService(
            IDBService db,
            IDMPresenceService dmPresence,
            IDMPreferenceService dmPreference,
            IHoloComService holoCom,
            IActivityService activity,
            ILanguageService language,
            IPlayerNameService playerName,
            ICommunicationAudienceService commsAudience,
            ICommunicationMessageSplitService messageSplit)
        {
            _db = db;
            _dmPresence = dmPresence;
            _dmPreference = dmPreference;
            _holoCom = holoCom;
            _activity = activity;
            _language = language;
            _playerName = playerName;
            _commsAudience = commsAudience;
            _messageSplit = messageSplit;
        }

        [PlayerSession(PlayerReload.LoginOnly,
            "The TLK override stays in the engine.")]
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public void ApplyCommsChannelName()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player))
                return;

            // The chat-channel selector treats angle-bracket color markup as TLK substitution tokens.
            PlayerPlugin.SetTlkOverride(player, PartyChatChannelNameStrRef, CommsChannelName);
            PlayerPlugin.SetTlkOverride(player, PartyChatMessagePrefixStrRef, CommsMessagePrefix);
        }

        [PlayerSession(PlayerReload.LoginOnly,
            "The TLK override stays in the engine.")]
        [NWNEventHandler(ScriptName.OnModuleEnter)]
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
        [NWNEventHandler(ScriptName.OnDMPossessBefore)]
        [NWNEventHandler(ScriptName.OnDMPossessFullPowerBefore)]
        public void OnDMPossess()
        {
            var target = StringToObject(EventsPlugin.GetEventData("TARGET"));
            if (GetIsObjectValid(target))
            {
                _activity.ClearBusy(target);
            }
        }

        /// <summary>
        /// When a player focuses the chatbar, set a typing indicator on the player; when
        /// unfocused, remove the indicator.
        /// </summary>

        [NWNEventHandler(ScriptName.OnModuleGuiEvent)]
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
        [NWNEventHandler(ScriptName.OnModuleChat)]
        public void ProcessNativeChatMessage()
        {
            ExecuteScript("dmfi_onplychat", OBJECT_SELF);
        }

        [NWNEventHandler(ScriptName.OnNWNXChat)]
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

            if (!TryAcceptMessage(channel, handledChat, messageToDm, sender, message))
                return;

            var chatComponents = BuildChatComponents(sender, message);
            if (chatComponents == null)
                return;

            // Now, depending on the chat channel, we need to build a list of recipients.
            var recipients = BuildRecipients(channel, sender, out var outOfRangeCommsPartyMembers);

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
            var language = _language.GetActiveLanguage(speaker);

            // Wookiees cannot speak any other language (but they can understand them).
            // Swap their language if they attempt to speak in any other language.
            if (GetRacialType(speaker) == RacialType.Wookiee && language != SkillType.Shyriiwook)
            {
                _language.SetActiveLanguage(speaker, SkillType.Shyriiwook);
                language = SkillType.Shyriiwook;
            }

            // Now we have a list of who is going to actually receive a message, we need to modify
            // the message for each recipient then dispatch them.
            foreach (var receiver in recipients.Distinct())
            {
                var finalMessageColored = BuildFinalMessage(
                    channel, sender, receiver, speaker, isHoloComRelay, language, chatComponents);

                SendProcessedChatMessage(channel, receiver, sender, speaker, finalMessageColored);
            }
        }

        /// <summary>
        /// The checks that decide whether this chat message is handled here: DMFI commands, other channels, empty
        /// messages, chat commands, the message to a DM, the DM's Shout, the disabled Shout and a dead speaker.
        /// Skips the native message once it is ours.
        /// </summary>
        /// <returns>true when the message goes on to be colored and dispatched.</returns>
        private bool TryAcceptMessage(ChatChannel channel, bool handledChat, bool messageToDm, uint sender, string message)
        {
            // if this is a DMFI chat command, exit as ProcessNativeChatMessage has already handled via mod_chat event.
            if (GetIsDM(sender) && message.Length >= 1 && message.Substring(0, 1) == ".")
            {
                return false;
            }

            // Ignore messages on other channels.
            if (!handledChat && !messageToDm) return false;

            if (string.IsNullOrWhiteSpace(message))
            {
                // We can't handle empty messages, so skip it.
                return false;
            }

            if (IsChatCommandMessage(message))
            {
                return false;
            }

            // Echo the message back to the player.
            if (messageToDm)
            {
                ChatPlugin.SendMessage(ChatChannel.ServerMessage, "(Sent to DM) " + message, sender, sender);
                return false;
            }

            if (channel == ChatChannel.PlayerShout && (GetIsDM(sender) || GetIsDMPossessed(sender)))
            {
                return false;
            }

            ChatPlugin.SkipMessage();

            if (channel == ChatChannel.PlayerShout)
            {
                SendMessageToPC(sender, ColorToken.Red(DisabledChannelMessage));
                return false;
            }

            if (GetIsDead(sender) && !message.StartsWith("/"))
            {
                SendMessageToPC(sender, ColorToken.Red("You cannot speak while dead."));
                return false;
            }

            return true;
        }

        /// <summary>Splits the message into its spoken and emote runs, or null when there is nothing to send.</summary>
        private List<ChatComponent> BuildChatComponents(uint sender, string message)
        {
            var chatComponents = new List<ChatComponent>();

            // Quick early out - if we start with "//" or "((", this is an OOC message.
            if (message.Length >= 2 && (message.Substring(0, 2) == "//" || message.Substring(0, 2) == "(("))
            {
                var component = new ChatComponent
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
                return null;
            }
            else
            {
                // A HoloCom hologram relays its owner's words, so the owner's emote style applies.
                chatComponents = GetEmoteStyle(GetEffectiveChatSpeaker(sender)) == EmoteStyle.Regular
                    ? _messageSplit.SplitMessageIntoComponents_Regular(message)
                    : _messageSplit.SplitMessageIntoComponents_Novel(message);

                // For any components with color, set the emote color.
                foreach (var component in chatComponents)
                {
                    if (component.IsCustomColor)
                    {
                        component.IsEmote = true;
                    }
                }
            }

            return chatComponents;
        }

        /// <summary>
        /// The sender and everyone who receives the message on this channel: Comms party members in range and the DMs who
        /// read it, then everyone near enough to overhear.
        /// </summary>
        /// <param name="outOfRangeCommsPartyMembers">How many party members the Comms message could not reach.</param>
        private List<uint> BuildRecipients(ChatChannel channel, uint sender, out int outOfRangeCommsPartyMembers)
        {
            var needsAreaCheck = false;
            var distanceCheck = 0.0f;
            outOfRangeCommsPartyMembers = 0;

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
                var (dmReaders, dmsFilteringOut) = _commsAudience.GetDMCommsAudience(sender, allPlayersAndDMs);
                mutedDMs = dmsFilteringOut;

                for (var member = GetFirstFactionMember(sender); GetIsObjectValid(member); member = GetNextFactionMember(sender))
                {
                    if (sender == member) continue;

                    if (_commsAudience.IsCommsReceiverInRange(sender, member))
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

            return recipients;
        }

        /// <summary>The message as one receiver perceives it: the Comms and relay prefixes, the language tag and each colored component.</summary>
        private string BuildFinalMessage(
            ChatChannel channel,
            uint sender,
            uint receiver,
            uint speaker,
            bool isHoloComRelay,
            SkillType language,
            List<ChatComponent> chatComponents)
        {
            var receiverId = GetObjectUUID(receiver);
            var dbReceiver = _db.Get<Player>(receiverId);

            // Generate the final message as perceived by obj.
            var finalMessage = new StringBuilder();

            if (channel == ChatChannel.PlayerParty)
            {
                if (GetIsDM(receiver) || GetIsDMPossessed(receiver))
                {
                    AppendPartyMemberSummary(finalMessage, sender);
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

            var (r, g, b) = _language.GetColor(language);

            if (dbReceiver?.Settings?.LanguageChatColors != null &&
                dbReceiver.Settings.LanguageChatColors.ContainsKey(language))
            {
                r = dbReceiver.Settings.LanguageChatColors[language].Red;
                g = dbReceiver.Settings.LanguageChatColors[language].Green;
                b = dbReceiver.Settings.LanguageChatColors[language].Blue;
            }

            if (language != SkillType.Basic)
            {
                var languageName = _language.GetName(language);
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
                    var (emoteRed, emoteGreen, emoteBlue) = ResolveEmoteColor(dbReceiver);
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

            return finalMessageColored;
        }

        /// <summary>Convenience for DMs - the first party members of the sender's faction, in braces.</summary>
        private void AppendPartyMemberSummary(StringBuilder finalMessage, uint sender)
        {
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

        /// <summary>The receiver's saved emote color, or the default.</summary>
        private (byte, byte, byte) ResolveEmoteColor(Player dbReceiver)
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

            return (emoteRed, emoteGreen, emoteBlue);
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

        private bool IsChatCommandMessage(string message)
        {
            return message.Length >= 2 &&
                   message[0] == '/' &&
                   message[1] != '/';
        }

        private uint GetEffectiveChatSpeaker(uint sender)
        {
            var hologramOwner = _holoCom.GetHoloGramOwner(sender);
            return GetIsObjectValid(hologramOwner)
                ? hologramOwner
                : sender;
        }


        // The three methods below call services that are not converted yet and whose static code reads the database or
        // the native engine for a real player. Each body is the original call, so a test subclass can answer them.

        /// <summary>The listener's version of a snippet: <c>Language.TranslateSnippetForListener</c>, whose rolls and skill reads are unchanged.</summary>
        protected virtual string TranslateSnippetForListener(uint speaker, uint listener, SkillType language, string snippet)
        {
            return _language.TranslateSnippetForListener(speaker, listener, language, snippet);
        }

        /// <summary>The colored chat name of <paramref name="target"/> as <paramref name="observer"/> sees it: <c>PlayerName.GetColoredChatDisplayName</c>.</summary>
        protected virtual string GetColoredChatDisplayName(uint observer, uint target)
        {
            return _playerName.GetColoredChatDisplayName(observer, target);
        }

        /// <summary>Runs <paramref name="sendMessage"/> with the per-observer chat name override in place: <c>PlayerName.SendChatMessageWithChatNameOverride</c>.</summary>
        protected virtual void SendChatMessageWithChatNameOverride(uint observer, uint target, Action sendMessage)
        {
            _playerName.SendChatMessageWithChatNameOverride(observer, target, sendMessage);
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
