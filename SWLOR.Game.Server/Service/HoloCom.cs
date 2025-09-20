
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Bioware;
using SWLOR.Shared.Core.Event;

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
        private const string HolocomHologram = "HOLOCOM_HOLOGRAM";
        private const string HologramOwner = "HOLOGRAM_OWNER";
        private const string HolocomCallImmobilize = "HOLOCOM_CALL_IMMOBILIZE";

        [ScriptHandler(ScriptName.OnModuleDeath)]
        public static void OnModuleDeath()
        {
            var player = GetLastPlayerDied();
            if (IsInCall(player)) 
                SetIsInCall(player, GetTargetForActiveCall(player), false);

        }

        [ScriptHandler(ScriptName.OnModuleEnter)]
        public static void OnModuleEnter()
        {
            var player = GetEnteringObject();
            RemoveEffectByTag(player, HolocomCallImmobilize);
        }

        [ScriptHandler(ScriptName.OnModuleExit)]
        public static void OnModuleLeave()
        {
            var player = GetExitingObject();

            CleanupAllHoloComState(player);
        }

        [ScriptHandler(ScriptName.OnModuleChat)]
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

                var message = "Call Connected. (Use the HoloCom or the chat command /endcall to terminate the call)";
                SendMessageToPC(sender, message);
                SendMessageToPC(receiver, message);
                var effectImmobilized = EffectCutsceneImmobilize();
                TagEffect(effectImmobilized, HolocomCallImmobilize);
                ApplyEffectToObject(DurationType.Permanent, effectImmobilized, sender);
                ApplyEffectToObject(DurationType.Permanent, effectImmobilized, receiver);

                var receiverLocation = GetLocation(receiver);
                var senderLocation = GetLocation(sender);
                var holoSender = CopyObject(sender, BiowareVector.MoveLocation(receiverLocation, GetFacing(receiver), 2.0f, 180));
                var holoReceiver = CopyObject(receiver, BiowareVector.MoveLocation(senderLocation, GetFacing(sender), 2.0f, 180));

                ApplyEffectToObject(DurationType.Instant, EffectHeal(GetMaxHitPoints(holoSender)), holoSender);
                ApplyEffectToObject(DurationType.Instant, EffectHeal(GetMaxHitPoints(holoReceiver)), holoReceiver);

                ApplyEffectToObject(DurationType.Permanent, EffectVisualEffect(VisualEffect.Vfx_Dur_Ghostly_Visage_No_Sound, false), holoSender);
                ApplyEffectToObject(DurationType.Permanent, EffectVisualEffect(VisualEffect.Vfx_Dur_Ghostly_Visage_No_Sound, false), holoReceiver);
                SetPlotFlag(holoReceiver, true);
                SetPlotFlag(holoSender, true);
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
                for(var effect = GetFirstEffect(sender); GetIsEffectValid(effect); effect = GetNextEffect(sender))
                {
                    if (GetIsEffectValid(effect))
                    {
                        var effectType = GetEffectType(effect);
                        if (effectType == EffectTypeScript.CutsceneImmobilize)
                        {
                            RemoveEffect(sender, effect);
                        }
                    }
                }

                for (var effect = GetFirstEffect(receiver); GetIsEffectValid(effect); effect = GetNextEffect(receiver))
                {
                    if (GetIsEffectValid(effect))
                    {
                        var effectType = GetEffectType(effect);
                        if (effectType == EffectTypeScript.CutsceneImmobilize)
                        {
                            RemoveEffect(receiver, effect);
                        }
                    }
                }

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

                DeleteLocalObject(sender, HolocomCallReceiverObject);
                DeleteLocalObject(receiver, HolocomCallReceiverObject);

                DeleteLocalObject(sender, HolocomCallSenderObject);
                DeleteLocalObject(receiver, HolocomCallSenderObject);
            }
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
