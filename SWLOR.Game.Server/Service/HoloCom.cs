using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Service.PlayerListService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service
{
    public static class HoloCom
    {
        // Local variable name constants
        private const string HolocomCallConnected = "HOLOCOM_CALL_CONNECTED";
        private const string HolocomCallConnectedWith = "HOLOCOM_CALL_CONNECTED_WITH";
        private const string HolocomCallSender = "HOLOCOM_CALL_SENDER";
        private const string HolocomCallReceiver = "HOLOCOM_CALL_RECEIVER";
        private const string HolocomCallSenderObject = "HOLOCOM_CALL_SENDER_OBJECT";
        private const string HolocomCallReceiverObject = "HOLOCOM_CALL_RECEIVER_OBJECT";
        private const string HolocomCallAttempt = "HOLOCOM_CALL_ATTEMPT";
        private const string HolocomCallToken = "HOLOCOM_CALL_TOKEN";
        private const string HolocomHologram = "HOLOCOM_HOLOGRAM";
        private const string HologramOwner = "HOLOGRAM_OWNER";
        private const string IsHologramVariable = "HOLOCOM_IS_HOLOGRAM";

        /// <summary>
        /// The "Neutral" faction in Module/fac/repute.fac.json, which is neither hostile to nor hated by any faction.
        /// </summary>
        public const int HologramFactionId = 6;

        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void OnModuleDeath()
        {
            var player = GetLastPlayerDied();
            if (IsInCall(player))
                SetIsInCall(player, GetTargetForActiveCall(player), false);

        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void OnModuleEnter()
        {
            var player = GetEnteringObject();
            RemoveEffectByTag(player, PlayerActivityEffectTag.HoloComImmobilize);
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void OnModuleLeave()
        {
            var player = GetExitingObject();

            CleanupAllHoloComState(player);
        }

        [NWNEventHandler(ScriptName.OnModuleChat)]
        public static void OnModuleChat()
        {
            var sender = GetPCChatSpeaker();
            var talkVolume = GetPCChatVolume();

            /*
            ChatChannelType channel = (ChatChannelType)NWNXChat.GetChannel();

            if (!IsInCall(sender)) return;
            if (channel != ChatChannelType.PlayerTalk) return;
            if (channel != ChatChannelType.PlayerWhisper) return;
            if (channel != ChatChannelType.PlayerParty) return;
            */

            if (talkVolume == TalkVolume.Shout) return;
            if (talkVolume == TalkVolume.Tell) return;
            if (talkVolume == TalkVolume.SilentShout) return;
            if (talkVolume == TalkVolume.SilentTalk) return;

            var receiver = GetHoloGram(sender);
            if (!GetIsObjectValid(receiver)) return;

            var text = GetPCChatMessage().Trim();

            if (text.StartsWith("/")) return;

            var animation = Animation.LoopingTalkNormal;
            if (text.Contains("!")) animation = Animation.LoopingTalkForceful;
            if (text.Contains("?")) animation = Animation.LoopingTalkPleading;

            SetCommandable(true, receiver);
            AssignCommand(receiver, () => ClearAllActions());

            AssignCommand(receiver, () =>
            {
                ActionPlayAnimation(animation);
            });

            AssignCommand(receiver, () => ActionSpeakString(text, talkVolume));
        }

        private static int _nextCallToken;

        /// <summary>
        /// HoloCom is a player system: neither end of a call may be a DM or a DM-controlled creature.
        /// </summary>
        private static bool IsDMInvolved(uint first, uint second = OBJECT_INVALID)
        {
            return DMPresence.IsControlledByDM(first) ||
                   (GetIsObjectValid(second) && DMPresence.IsControlledByDM(second));
        }

        /// <summary>
        /// Where a player stands in a call: none, ringing someone, being rung, or connected.
        /// </summary>
        public static PlayerListCallState GetCallState(uint player)
        {
            if (IsInCall(player))
                return PlayerListCallState.Connected;
            if (IsCallReceiver(player))
                return PlayerListCallState.Incoming;
            if (IsCallSender(player))
                return PlayerListCallState.Outgoing;

            return PlayerListCallState.None;
        }

        /// <summary>
        /// Starts ringing another player. The caller checks <see cref="PlayerListRules"/> first.
        /// </summary>
        public static void StartCall(uint sender, uint receiver)
        {
            if (IsDMInvolved(sender, receiver) ||
                GetCallState(sender) != PlayerListCallState.None ||
                GetCallState(receiver) != PlayerListCallState.None ||
                Space.IsPlayerInSpaceMode(sender) ||
                Space.IsPlayerInSpaceMode(receiver))
                return;

            // Each attempt gets its own token so a delayed callback from a cancelled attempt cannot ring for a newer one.
            var token = ++_nextCallToken;
            SetLocalInt(sender, HolocomCallToken, token);
            SetIsCallSender(sender);
            PlayerList.QueueRefresh();
            DelayCommand(1.0f, () =>
            {
                CallPlayer(sender, receiver, token);
            });
        }

        /// <summary>
        /// Connects an incoming call. Answering is refused while either player is in space.
        /// </summary>
        public static void AnswerCall(uint player)
        {
            if (!IsCallReceiver(player) || IsInCall(player) || DMPresence.IsControlledByDM(player))
                return;

            var callSender = GetCallSender(player);
            if (Space.IsPlayerInSpaceMode(player))
            {
                SendMessageToPC(player, ColorToken.Red("HoloCom cannot be used in space."));
                return;
            }

            if (!GetIsObjectValid(callSender) || Space.IsPlayerInSpaceMode(callSender) || IsDMInvolved(callSender))
            {
                if (GetIsObjectValid(callSender))
                    SendMessageToPC(callSender, "Your HoloCom call went unanswered.");

                SendMessageToPC(player, "Your HoloCom stops buzzing.");
                CleanupCallAttempt(callSender, player);
                return;
            }

            SetIsInCall(player, callSender, true);
        }

        /// <summary>
        /// Declines an incoming call and tells the caller.
        /// </summary>
        public static void DeclineCall(uint player)
        {
            if (!IsCallReceiver(player) || IsInCall(player) || DMPresence.IsControlledByDM(player))
                return;

            var callSender = GetCallSender(player);
            SendMessageToPC(callSender, "Your HoloCom call was declined.");
            CleanupCallAttempt(callSender, player);
        }

        /// <summary>
        /// Ends a connected call or cancels an outgoing attempt.
        /// </summary>
        /// <returns>False when the player has neither.</returns>
        public static bool EndCall(uint player)
        {
            if (IsInCall(player))
            {
                SetIsInCall(player, GetTargetForActiveCall(player), false);
                SendMessageToPC(player, "You end your HoloCom call.");
                return true;
            }

            if (IsCallSender(player))
            {
                var callReceiver = GetCallReceiver(player);
                if (GetIsObjectValid(callReceiver))
                    SendMessageToPC(callReceiver, "Your HoloCom stops buzzing.");

                CleanupCallAttempt(player, callReceiver);
                SendMessageToPC(player, "You cancel your HoloCom call.");
                return true;
            }

            return false;
        }

        private static void CallPlayer(uint sender, uint receiver, int token)
        {
            // A cancelled or replaced attempt has a different token, so its delayed callbacks stop here.
            if (!GetIsObjectValid(sender) || GetLocalInt(sender, HolocomCallToken) != token)
                return;

            if (!GetIsObjectValid(receiver))
            {
                CleanupCallAttempt(sender, receiver);
                SendMessageToPC(sender, "Your HoloCom call went unanswered.");
                return;
            }

            if (IsInCall(sender) || !IsCallSender(sender)) return;

            if (IsDMInvolved(sender, receiver))
            {
                EndAttempt(sender, receiver);
                return;
            }

            if (Space.IsPlayerInSpaceMode(sender) || Space.IsPlayerInSpaceMode(receiver))
            {
                EndAttempt(sender, receiver);
                SendMessageToPC(sender, ColorToken.Red("HoloCom cannot be used in space."));
                return;
            }

            // The receiver may ring from this very attempt on a retry; any other call state means a busy line.
            var receiverState = GetCallState(receiver);
            var isRingingFromSender = receiverState == PlayerListCallState.Incoming && GetCallSender(receiver) == sender;
            if (receiverState != PlayerListCallState.None && !isRingingFromSender)
            {
                EndAttempt(sender, receiver);
                SendMessageToPC(sender, "Their HoloCom line is busy.");
                return;
            }

            var receiverName = PlayerName.GetDisplayName(sender, receiver);
            SendMessageToPC(sender, "You wait for " + receiverName + " to answer their HoloCom.");

            SetIsCallSender(sender);
            SetIsCallSender(receiver, false);
            SetCallSender(sender, sender);
            SetCallSender(receiver, sender);
            SetIsCallReceiver(sender, false);
            SetIsCallReceiver(receiver);
            SetCallReceiver(sender, receiver);
            SetCallReceiver(receiver, receiver);
            PlayerList.QueueRefresh();

            var message = "Your HoloCom buzzes as you are receiving a call. Open the Player List to answer or decline, or use /answer or /decline.";
            if (Random(10) == 1)
            {
                message += " " + ColorToken.Green("Maybe you should answer it.");
            }
            SendMessageToPC(receiver, message);
            if (GetCallAttempt(sender) % 5 == 0)
            {
                FloatingTextStringOnCreature(message, receiver);
            }

            if (GetCallAttempt(sender) <= 15)
            {
                SetCallAttempt(sender, GetCallAttempt(sender) + 1);
                DelayCommand(5.0f, () => { CallPlayer(sender, receiver, token); });
            }
            else
            {
                SendMessageToPC(sender, "Your HoloCom call went unanswered.");
                SendMessageToPC(receiver, "Your HoloCom stops buzzing.");

                // the following call cleans everything up even if a call isn't currently connected.
                SetIsInCall(sender, receiver, false);
            }
        }

        public static bool IsInCall(uint player)
        {
            if (GetLocalBool(player, HolocomCallConnected) == true) return true;
            else return false;
        }
        public static void SetIsInCall(uint sender, uint receiver, bool value = true)
        {
            if (value) // START CALL
            {
                SetLocalBool(sender, HolocomCallConnected, true);
                SetLocalBool(receiver, HolocomCallConnected, true);

                SetLocalObject(sender, HolocomCallConnectedWith, receiver);
                SetLocalObject(receiver, HolocomCallConnectedWith, sender);

                var message = "Call Connected. (Use End Call in the Player List or the chat command /endcall to terminate the call)";
                SendMessageToPC(sender, message);
                SendMessageToPC(receiver, message);
                var effectImmobilized = TagEffect(
                    EffectCutsceneImmobilize(),
                    PlayerActivityEffectTag.HoloComImmobilize);
                ApplyEffectToObject(DurationType.Permanent, effectImmobilized, sender);
                ApplyEffectToObject(DurationType.Permanent, effectImmobilized, receiver);

                var receiverLocation = GetLocation(receiver);
                var senderLocation = GetLocation(sender);
                var holoSender = CopyObject(sender, BiowareVector.MoveLocation(receiverLocation, GetFacing(receiver), 2.0f, 180));
                var holoReceiver = CopyObject(receiver, BiowareVector.MoveLocation(senderLocation, GetFacing(sender), 2.0f, 180));
                SetName(holoSender, "HoloCom Hologram");
                SetName(holoReceiver, "HoloCom Hologram");

                ApplyEffectToObject(DurationType.Instant, EffectHeal(GetMaxHitPoints(holoSender)), holoSender);
                ApplyEffectToObject(DurationType.Instant, EffectHeal(GetMaxHitPoints(holoReceiver)), holoReceiver);

                ApplyEffectToObject(DurationType.Permanent, EffectVisualEffect(VisualEffect.Vfx_Dur_Ghostly_Visage_No_Sound, false), holoSender);
                ApplyEffectToObject(DurationType.Permanent, EffectVisualEffect(VisualEffect.Vfx_Dur_Ghostly_Visage_No_Sound, false), holoReceiver);
                SetPlotFlag(holoReceiver, true);
                SetPlotFlag(holoSender, true);
                MakeNonCombatant(holoSender);
                MakeNonCombatant(holoReceiver);
                SetLocalObject(sender, HolocomHologram, holoSender);
                SetLocalObject(receiver, HolocomHologram, holoReceiver);

                SetLocalObject(holoSender, HologramOwner, sender);
                SetLocalObject(holoReceiver, HologramOwner, receiver);

                AssignCommand(sender, () =>
                {
                    PlaySound("hologram_on");
                });
                AssignCommand(receiver, () =>
                {
                    PlaySound("hologram_on");
                });
            }
            else // END CALL
            {
                RemoveEffectByTag(sender, PlayerActivityEffectTag.HoloComImmobilize);
                RemoveEffectByTag(receiver, PlayerActivityEffectTag.HoloComImmobilize);

                AssignCommand(sender, () =>
                {
                    PlaySound("hologram_off");
                });
                AssignCommand(receiver, () =>
                {
                    PlaySound("hologram_off");
                });

                // Destroy holograms if they are valid
                var senderHologram = GetHoloGram(sender);
                var receiverHologram = GetHoloGram(receiver);

                if (GetIsObjectValid(senderHologram))
                {
                    DestroyObject(senderHologram);
                }
                if (GetIsObjectValid(receiverHologram))
                {
                    DestroyObject(receiverHologram);
                }

                DeleteLocalInt(sender, HolocomCallConnected);
                DeleteLocalInt(receiver, HolocomCallConnected);

                DeleteLocalInt(sender, HolocomCallSender);
                DeleteLocalInt(receiver, HolocomCallSender);

                DeleteLocalInt(sender, HolocomCallReceiver);
                DeleteLocalInt(receiver, HolocomCallReceiver);

                DeleteLocalObject(sender, HolocomCallConnectedWith);
                DeleteLocalObject(receiver, HolocomCallConnectedWith);

                DeleteLocalObject(sender, HolocomHologram);
                DeleteLocalObject(receiver, HolocomHologram);

                DeleteLocalInt(sender, HolocomCallAttempt);
                DeleteLocalInt(receiver, HolocomCallAttempt);
                DeleteLocalInt(sender, HolocomCallToken);
                DeleteLocalInt(receiver, HolocomCallToken);

                DeleteLocalObject(sender, HolocomCallReceiverObject);
                DeleteLocalObject(receiver, HolocomCallReceiverObject);

                DeleteLocalObject(sender, HolocomCallSenderObject);
                DeleteLocalObject(receiver, HolocomCallSenderObject);
            }

            PlayerList.QueueRefresh();
        }
        /// <summary>
        /// Removes a call hologram from combat: it leaves the copied player's faction for one no creature
        /// is hostile toward, so hostile abilities and aggro never select it, and its combat state is cleared.
        /// </summary>
        private static void MakeNonCombatant(uint hologram)
        {
            SetLocalBool(hologram, IsHologramVariable, true);
            CreaturePlugin.SetFaction(hologram, HologramFactionId);
            AssignCommand(hologram, () => ClearAllActions(true));
        }

        /// <summary>
        /// Determines whether a creature is a HoloCom call hologram.
        /// </summary>
        public static bool IsHologram(uint creature)
        {
            return GetLocalBool(creature, IsHologramVariable);
        }

        public static uint GetHoloGram(uint player)
        {
            return GetLocalObject(player, HolocomHologram);
        }
        public static uint GetHoloGramOwner(uint hologram)
        {
            return GetLocalObject(hologram, HologramOwner);
        }
        public static uint GetTargetForActiveCall(uint player)
        {
            return GetLocalObject(player, HolocomCallConnectedWith);
        }
        public static bool IsCallSender(uint player)
        {
            if (GetLocalBool(player, HolocomCallSender) == true) return true;
            else return false;
        }
        public static void SetIsCallSender(uint player, bool value = true)
        {
            if (value) SetLocalBool(player, HolocomCallSender, true);
            else SetLocalBool(player, HolocomCallSender, false);
        }
        public static uint GetCallSender(uint player)
        {
            return GetLocalObject(player, HolocomCallSenderObject);
        }
        public static void SetCallSender(uint player, uint sender)
        {
            SetLocalObject(player, HolocomCallSenderObject, sender);
        }

        public static bool IsCallReceiver(uint player)
        {
            if (GetLocalBool(player, HolocomCallReceiver) == true) return true;
            else return false;
        }
        public static void SetIsCallReceiver(uint player, bool value = true)
        {
            if (value) SetLocalBool(player, HolocomCallReceiver, true);
            else SetLocalBool(player, HolocomCallReceiver, false);
        }
        public static uint GetCallReceiver(uint player)
        {
            return GetLocalObject(player, HolocomCallReceiverObject);
        }
        public static void SetCallReceiver(uint player, uint receiver)
        {
            SetLocalObject(player, HolocomCallReceiverObject, receiver);
        }
        public static int GetCallAttempt(uint player)
        {
            return GetLocalInt(player, HolocomCallAttempt);
        }
        public static void SetCallAttempt(uint player, int value = 0)
        {
            SetLocalInt(player, HolocomCallAttempt, value);
        }

        // Ends one attempt without disturbing a receiver who is on, or ringing from, a different call.
        private static void EndAttempt(uint sender, uint receiver)
        {
            var isRingingFromSender = IsCallReceiver(receiver) && GetCallSender(receiver) == sender;
            if (isRingingFromSender)
            {
                SendMessageToPC(receiver, "Your HoloCom stops buzzing.");
                CleanupCallAttempt(sender, receiver);
                return;
            }

            CleanupCallAttempt(sender, OBJECT_INVALID);
        }

        /// <summary>
        /// Cleans up call attempt state for both sender and receiver
        /// </summary>
        /// <param name="sender">The player who initiated the call</param>
        /// <param name="receiver">The player who was being called</param>
        public static void CleanupCallAttempt(uint sender, uint receiver)
        {
            if (GetIsObjectValid(receiver))
            {
                // Clean up the receiver's call state
                SetIsCallReceiver(receiver, false);
                DeleteLocalObject(receiver, HolocomCallReceiverObject);
                DeleteLocalObject(receiver, HolocomCallSenderObject);
                DeleteLocalInt(receiver, HolocomCallAttempt);
            }

            // Clean up the sender's call state
            SetIsCallSender(sender, false);
            DeleteLocalObject(sender, HolocomCallSenderObject);
            DeleteLocalObject(sender, HolocomCallReceiverObject);
            DeleteLocalInt(sender, HolocomCallAttempt);
            DeleteLocalInt(sender, HolocomCallToken);
            PlayerList.QueueRefresh();
        }

        /// <summary>
        /// Comprehensive cleanup of all HoloCom state for a player
        /// </summary>
        /// <param name="player">The player to clean up</param>
        public static void CleanupAllHoloComState(uint player)
        {
            // Clean up call sender state
            if (IsCallSender(player))
            {
                var receiver = GetCallReceiver(player);
                if (GetIsObjectValid(receiver))
                {
                    // Notify the receiver that the call attempt has ended
                    SendMessageToPC(receiver, "Your HoloCom stops buzzing.");

                    // Clean up receiver's state
                    SetIsCallReceiver(receiver, false);
                    DeleteLocalObject(receiver, HolocomCallReceiverObject);
                    DeleteLocalObject(receiver, HolocomCallSenderObject);
                    DeleteLocalInt(receiver, HolocomCallAttempt);
                }

                // Clean up sender's state
                SetIsCallSender(player, false);
                DeleteLocalObject(player, HolocomCallSenderObject);
                DeleteLocalObject(player, HolocomCallReceiverObject);
                DeleteLocalInt(player, HolocomCallAttempt);
            }

            // Clean up call receiver state
            if (IsCallReceiver(player))
            {
                var sender = GetCallSender(player);
                if (GetIsObjectValid(sender))
                {
                    // Notify the sender that the call attempt has ended
                    SendMessageToPC(sender, "Your HoloCom call went unanswered.");

                    // Clean up sender's state
                    SetIsCallSender(sender, false);
                    DeleteLocalObject(sender, HolocomCallSenderObject);
                    DeleteLocalObject(sender, HolocomCallReceiverObject);
                    DeleteLocalInt(sender, HolocomCallAttempt);
                }

                // Clean up receiver's state
                SetIsCallReceiver(player, false);
                DeleteLocalObject(player, HolocomCallReceiverObject);
                DeleteLocalObject(player, HolocomCallSenderObject);
                DeleteLocalInt(player, HolocomCallAttempt);
            }

            // Clean up active call state
            if (IsInCall(player))
            {
                var callTarget = GetTargetForActiveCall(player);
                if (GetIsObjectValid(callTarget))
                {
                    SetIsInCall(player, callTarget, false);
                }
                else
                {
                    // If target is no longer valid, just clean up this player's state
                    DeleteLocalInt(player, HolocomCallConnected);
                    DeleteLocalObject(player, HolocomCallConnectedWith);
                    DeleteLocalObject(player, HolocomHologram);
                }
            }
        }
    }
}
