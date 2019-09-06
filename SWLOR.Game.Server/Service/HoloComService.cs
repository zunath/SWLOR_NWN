﻿using System;
using SWLOR.Game.Server.GameObject;
using NWN;
using static NWN._;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Messaging;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.NWNX;

namespace SWLOR.Game.Server.Service
{
    public static class HoloComService
    {
        /*
         * 
        -- retest -- Languages shouldn't get translated by holocom.
        -- retest -- Need to test if you get language xp from your hologram.
        -- retest -- calling someone who's busy borks you.
        -- retest -- if player dies while in call they resurrect without cutscene immobile.
        -- retest -- only allow talk, whisper and party channels for holograms. NO DM or SHOUT 
        ??? onclientexit, end call for exiting player
        ??? can limbo holograms but they dont show in limbo.
         * 
         */

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleChat>(message => OnModuleChat());
            MessageHub.Instance.Subscribe<OnModuleDeath>(message => OnModuleDeath());
            MessageHub.Instance.Subscribe<OnModuleLeave>(message => OnModuleLeave());
        }

        private static void OnModuleDeath()
        {
            NWPlayer player = _.GetLastPlayerDied();
            if (HoloComService.IsInCall(player)) HoloComService.SetIsInCall(player, HoloComService.GetTargetForActiveCall(player), false);

        }
        private static void OnModuleLeave()
        {
            NWPlayer player = _.GetExitingObject();
            if (HoloComService.IsInCall(player)) HoloComService.SetIsInCall(player, HoloComService.GetTargetForActiveCall(player), false);

        }

        private static void OnModuleChat()
        {
            NWPlayer sender = GetPCChatSpeaker();
            int talkvolume = GetPCChatVolume();

            /*
            ChatChannelType channel = (ChatChannelType)NWNXChat.GetChannel();

            if (!IsInCall(sender)) return;
            if (channel != ChatChannelType.PlayerTalk) return;
            if (channel != ChatChannelType.PlayerWhisper) return;
            if (channel != ChatChannelType.PlayerParty) return;
            */
           
            if (talkvolume == TALKVOLUME_SHOUT) return;
            if (talkvolume == TALKVOLUME_TELL) return;
            if (talkvolume == TALKVOLUME_SILENT_SHOUT) return;            
            if (talkvolume == TALKVOLUME_SILENT_TALK) return;

            NWPlayer receiver = GetHoloGram(sender);

            string text = _.GetPCChatMessage().Trim();

            if (text.StartsWith("/")) return;

            int animation = ANIMATION_LOOPING_TALK_NORMAL;
            if (text.Contains("!")) animation = ANIMATION_LOOPING_TALK_FORCEFUL;
            if (text.Contains("?")) animation = ANIMATION_LOOPING_TALK_PLEADING;

            SetCommandable(TRUE, receiver);
            receiver.ClearAllActions();

            receiver.AssignCommand(() =>
            {
                ActionPlayAnimation(animation);
            });

            receiver.DelayAssignCommand(() =>
            {
                ActionSpeakString(text, talkvolume);
            },0.3f);

    }

    public static bool IsInCall(NWPlayer player)
        {
            if (player.GetLocalInt("HOLOCOM_CALL_CONNECTED") == TRUE) return true;
            else return false;
        }
        public static void SetIsInCall(NWPlayer sender, NWPlayer receiver, bool value = true)
        {
            if (value) // START CALL
            {
                sender.SetLocalInt("HOLOCOM_CALL_CONNECTED", TRUE);
                receiver.SetLocalInt("HOLOCOM_CALL_CONNECTED", TRUE);

                sender.SetLocalObject("HOLOCOM_CALL_CONNECTED_WITH", receiver);
                receiver.SetLocalObject("HOLOCOM_CALL_CONNECTED_WITH", sender);
                
                string message = "Call Connected. (Use the HoloCom or the chat command /endcall to terminate the call)";
                SendMessageToPC(sender, message);
                SendMessageToPC(receiver, message);
                Effect effectImmobilized = EffectCutsceneImmobilize();
                TagEffect(effectImmobilized, "HOLOCOM_CALL_IMMOBILIZE");
                ApplyEffectToObject(DURATION_TYPE_PERMANENT, effectImmobilized, sender);
                ApplyEffectToObject(DURATION_TYPE_PERMANENT, effectImmobilized, receiver);

                var holosender = CopyObject(sender, VectorService.MoveLocation(receiver.Location, GetFacing(receiver), 2.0f, 180));
                var holoreceiver = CopyObject(receiver, VectorService.MoveLocation(sender.Location, GetFacing(sender), 2.0f, 180));

                ApplyEffectToObject(DURATION_TYPE_PERMANENT, EffectVisualEffect(VFX_DUR_GHOSTLY_VISAGE_NO_SOUND, FALSE), holosender);
                ApplyEffectToObject(DURATION_TYPE_PERMANENT, EffectVisualEffect(VFX_DUR_GHOSTLY_VISAGE_NO_SOUND, FALSE), holoreceiver);
                SetPlotFlag(holoreceiver, TRUE);
                SetPlotFlag(holosender, TRUE);
                sender.SetLocalObject("HOLOCOM_HOLOGRAM", holosender);
                receiver.SetLocalObject("HOLOCOM_HOLOGRAM", holoreceiver);

                SetLocalObject(holosender, "HOLOGRAM_OWNER", sender);
                SetLocalObject(holoreceiver, "HOLOGRAM_OWNER", receiver);

                sender.AssignCommand(() =>
                {
                    PlaySound("hologram_on");
                });
                receiver.AssignCommand(() =>
                {
                    PlaySound("hologram_on");
                });
                
                /*
                Console.WriteLine("SENDERS PERSPECTIVE:");
                Console.WriteLine("Sender Name:            " + HoloComService.GetCallSender(sender).Name);
                Console.WriteLine("Receiver Name:          " + HoloComService.GetCallReceiver(sender).Name);
                Console.WriteLine("Sender Call Attempts:   " + HoloComService.GetCallAttempt(sender));
                Console.WriteLine("Sender Connected With:  " + HoloComService.GetTargetForActiveCall(sender));
                Console.WriteLine("RECEIVERS PERSPECTIVE:");
                Console.WriteLine("Sender Name:            " + HoloComService.GetCallSender(receiver).Name);
                Console.WriteLine("Receiver Name:          " + HoloComService.GetCallReceiver(receiver).Name);
                Console.WriteLine("Sender Call Attempts:   " + HoloComService.GetCallAttempt(GetCallSender(receiver)));
                Console.WriteLine("Receiver Connected With:" + HoloComService.GetTargetForActiveCall(receiver));
                */
            }
            else // END CALL
            {
                foreach (Effect effect in sender.Effects)
                {
                    if (_.GetIsEffectValid(effect) == TRUE)
                    {
                        int effectType = GetEffectType(effect);
                        if (effectType == EFFECT_TYPE_CUTSCENEIMMOBILIZE)
                        {
                            RemoveEffect(sender.Object, effect);
                        }
                    }
                }

                foreach (Effect effect in receiver.Effects)
                {
                    if (_.GetIsEffectValid(effect) == TRUE)
                    {
                        int effectType = GetEffectType(effect);
                        if (effectType == EFFECT_TYPE_CUTSCENEIMMOBILIZE)
                        {
                            RemoveEffect(receiver.Object, effect);
                        }
                    }
                }

                sender.AssignCommand(() =>
                {
                    PlaySound("hologram_off");
                });
                receiver.AssignCommand(() =>
                {
                    PlaySound("hologram_off");
                });

                DestroyObject(GetHoloGram(sender));
                DestroyObject(GetHoloGram(receiver));

                sender.DeleteLocalInt("HOLOCOM_CALL_CONNECTED");
                receiver.DeleteLocalInt("HOLOCOM_CALL_CONNECTED");

                sender.DeleteLocalInt("HOLOCOM_CALL_SENDER");
                receiver.DeleteLocalInt("HOLOCOM_CALL_SENDER");

                sender.DeleteLocalInt("HOLOCOM_CALL_RECEIVER");
                receiver.DeleteLocalInt("HOLOCOM_CALL_RECEIVER");

                sender.DeleteLocalObject("HOLOCOM_CALL_CONNECTED_WITH");
                receiver.DeleteLocalObject("HOLOCOM_CALL_CONNECTED_WITH");

                sender.DeleteLocalObject("HOLOCOM_HOLOGRAM");
                receiver.DeleteLocalObject("HOLOCOM_HOLOGRAM");

                sender.DeleteLocalInt("HOLOCOM_CALL_ATTEMPT");
                receiver.DeleteLocalInt("HOLOCOM_CALL_ATTEMPT");

                sender.DeleteLocalObject("HOLOCOM_CALL_RECEIVER_OBJECT");
                receiver.DeleteLocalObject("HOLOCOM_CALL_RECEIVER_OBJECT");

                sender.DeleteLocalObject("HOLOCOM_CALL_SENDER_OBJECT");
                receiver.DeleteLocalObject("HOLOCOM_CALL_SENDER_OBJECT");
            }
        }
        public static NWPlayer GetHoloGram(NWPlayer player)
        {
            return player.GetLocalObject("HOLOCOM_HOLOGRAM");
        }
        public static NWPlayer GetHoloGramOwner(NWObject hologram)
        {
            return hologram.GetLocalObject("HOLOGRAM_OWNER");
        }
        public static NWPlayer GetTargetForActiveCall(NWPlayer player)
        {
            return player.GetLocalObject("HOLOCOM_CALL_CONNECTED_WITH");
        }
        public static bool IsCallSender(NWPlayer player)
        {
            if (player.GetLocalInt("HOLOCOM_CALL_SENDER") == TRUE) return true;
            else return false;
        }
        public static void SetIsCallSender(NWPlayer player, bool value = true)
        {
            if (value) player.SetLocalInt("HOLOCOM_CALL_SENDER", TRUE);
            else player.SetLocalInt("HOLOCOM_CALL_SENDER", FALSE);
        }
        public static NWPlayer GetCallSender(NWPlayer player)
        {
            return player.GetLocalObject("HOLOCOM_CALL_SENDER_OBJECT");
        }
        public static void SetCallSender(NWPlayer player, NWPlayer sender)
        {
            player.SetLocalObject("HOLOCOM_CALL_SENDER_OBJECT", sender);
        }

        public static bool IsCallReceiver(NWPlayer player)
        {
            if (player.GetLocalInt("HOLOCOM_CALL_RECEIVER") == TRUE) return true;
            else return false;
        }
        public static void SetIsCallReceiver(NWPlayer player, bool value = true)
        {
            if (value) player.SetLocalInt("HOLOCOM_CALL_RECEIVER", TRUE);
            else player.SetLocalInt("HOLOCOM_CALL_RECEIVER", FALSE);
        }        
        public static NWPlayer GetCallReceiver(NWPlayer player)
        {
            return player.GetLocalObject("HOLOCOM_CALL_RECEIVER_OBJECT");
        }
        public static void SetCallReceiver(NWPlayer player, NWPlayer receiver)
        {
            player.SetLocalObject("HOLOCOM_CALL_RECEIVER_OBJECT", receiver);
        }
        public static int GetCallAttempt(NWPlayer player)
        {
            return player.GetLocalInt("HOLOCOM_CALL_ATTEMPT");
        }
        public static void SetCallAttempt(NWPlayer player, int value = 0)
        {
            player.SetLocalInt("HOLOCOM_CALL_ATTEMPT", value);
        }
    }
}
