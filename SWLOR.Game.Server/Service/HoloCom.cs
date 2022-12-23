using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service
{
    public static class HoloCom
    {
        [NWNEventHandler("mod_death")]
        public static void OnModuleDeath()
        {
            var player = GetLastPlayerDied();
            if (IsInCall(player)) 
                SetIsInCall(player, GetTargetForActiveCall(player), false);

        }

        [NWNEventHandler("mod_enter")]
        public static void OnModuleEnter()
        {
            var player = GetEnteringObject();
            RemoveEffectByTag(player, "HOLOCOM_CALL_IMMOBILIZE");
        }

        [NWNEventHandler("mod_exit")]
        public static void OnModuleLeave()
        {
            var player = GetExitingObject();
            if (IsInCall(player)) 
                SetIsInCall(player, GetTargetForActiveCall(player), false);

        }

        [NWNEventHandler("mod_chat")]
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
            if (GetLocalBool(player, "HOLOCOM_CALL_CONNECTED") == true) return true;
            else return false;
        }
        public static void SetIsInCall(uint sender, uint receiver, bool value = true)
        {
            if (value) // START CALL
            {
                SetLocalBool(sender, "HOLOCOM_CALL_CONNECTED", true);
                SetLocalBool(receiver, "HOLOCOM_CALL_CONNECTED", true);

                SetLocalObject(sender, "HOLOCOM_CALL_CONNECTED_WITH", receiver);
                SetLocalObject(receiver, "HOLOCOM_CALL_CONNECTED_WITH", sender);

                var message = "Call Connected. (Use the HoloCom or the chat command /endcall to terminate the call)";
                SendMessageToPC(sender, message);
                SendMessageToPC(receiver, message);
                var effectImmobilized = EffectCutsceneImmobilize();
                TagEffect(effectImmobilized, "HOLOCOM_CALL_IMMOBILIZE");
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
                SetLocalObject(sender, "HOLOCOM_HOLOGRAM", holoSender);
                SetLocalObject(receiver, "HOLOCOM_HOLOGRAM", holoReceiver);

                SetLocalObject(holoSender, "HOLOGRAM_OWNER", sender);
                SetLocalObject(holoReceiver, "HOLOGRAM_OWNER", receiver);

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

                DestroyObject(GetHoloGram(sender));
                DestroyObject(GetHoloGram(receiver));

                DeleteLocalInt(sender, "HOLOCOM_CALL_CONNECTED");
                DeleteLocalInt(receiver, "HOLOCOM_CALL_CONNECTED");

                DeleteLocalInt(sender, "HOLOCOM_CALL_SENDER");
                DeleteLocalInt(receiver, "HOLOCOM_CALL_SENDER");

                DeleteLocalInt(sender, "HOLOCOM_CALL_RECEIVER");
                DeleteLocalInt(receiver, "HOLOCOM_CALL_RECEIVER");

                DeleteLocalObject(sender, "HOLOCOM_CALL_CONNECTED_WITH");
                DeleteLocalObject(receiver, "HOLOCOM_CALL_CONNECTED_WITH");

                DeleteLocalObject(sender, "HOLOCOM_HOLOGRAM");
                DeleteLocalObject(receiver, "HOLOCOM_HOLOGRAM");

                DeleteLocalInt(sender, "HOLOCOM_CALL_ATTEMPT");
                DeleteLocalInt(receiver, "HOLOCOM_CALL_ATTEMPT");

                DeleteLocalObject(sender, "HOLOCOM_CALL_RECEIVER_OBJECT");
                DeleteLocalObject(receiver, "HOLOCOM_CALL_RECEIVER_OBJECT");

                DeleteLocalObject(sender, "HOLOCOM_CALL_SENDER_OBJECT");
                DeleteLocalObject(receiver, "HOLOCOM_CALL_SENDER_OBJECT");
            }
        }
        public static uint GetHoloGram(uint player)
        {
            return GetLocalObject(player, "HOLOCOM_HOLOGRAM");
        }
        public static uint GetHoloGramOwner(uint hologram)
        {
            return GetLocalObject(hologram, "HOLOGRAM_OWNER");
        }
        public static uint GetTargetForActiveCall(uint player)
        {
            return GetLocalObject(player, "HOLOCOM_CALL_CONNECTED_WITH");
        }
        public static bool IsCallSender(uint player)
        {
            if (GetLocalBool(player, "HOLOCOM_CALL_SENDER") == true) return true;
            else return false;
        }
        public static void SetIsCallSender(uint player, bool value = true)
        {
            if (value) SetLocalBool(player, "HOLOCOM_CALL_SENDER", true);
            else SetLocalBool(player, "HOLOCOM_CALL_SENDER", false);
        }
        public static uint GetCallSender(uint player)
        {
            return GetLocalObject(player, "HOLOCOM_CALL_SENDER_OBJECT");
        }
        public static void SetCallSender(uint player, uint sender)
        {
            SetLocalObject(player, "HOLOCOM_CALL_SENDER_OBJECT", sender);
        }

        public static bool IsCallReceiver(uint player)
        {
            if (GetLocalBool(player, "HOLOCOM_CALL_RECEIVER") == true) return true;
            else return false;
        }
        public static void SetIsCallReceiver(uint player, bool value = true)
        {
            if (value) SetLocalBool(player, "HOLOCOM_CALL_RECEIVER", true);
            else SetLocalBool(player, "HOLOCOM_CALL_RECEIVER", false);
        }
        public static uint GetCallReceiver(uint player)
        {
            return GetLocalObject(player, "HOLOCOM_CALL_RECEIVER_OBJECT");
        }
        public static void SetCallReceiver(uint player, uint receiver)
        {
            SetLocalObject(player, "HOLOCOM_CALL_RECEIVER_OBJECT", receiver);
        }
        public static int GetCallAttempt(uint player)
        {
            return GetLocalInt(player, "HOLOCOM_CALL_ATTEMPT");
        }
        public static void SetCallAttempt(uint player, int value = 0)
        {
            SetLocalInt(player, "HOLOCOM_CALL_ATTEMPT", value);
        }
    }
}
