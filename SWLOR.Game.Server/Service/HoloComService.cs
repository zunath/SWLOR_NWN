using System;
using SWLOR.Game.Server.GameObject;
using NWN;
using static NWN._;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Messaging;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWScript.Enumerations;

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
            var talkvolume = GetPCChatVolume();

            /*
            ChatChannelType channel = (ChatChannelType)NWNXChat.GetChannel();

            if (!IsInCall(sender)) return;
            if (channel != ChatChannelType.PlayerTalk) return;
            if (channel != ChatChannelType.PlayerWhisper) return;
            if (channel != ChatChannelType.PlayerParty) return;
            */
           
            if (talkvolume == TalkVolume.Shout) return;
            if (talkvolume == TalkVolume.Tell) return;
            if (talkvolume == TalkVolume.SilentShout) return;            
            if (talkvolume == TalkVolume.SilentTalk) return;

            NWPlayer receiver = GetHoloGram(sender);

            string text = _.GetPCChatMessage().Trim();

            if (text.StartsWith("/")) return;

            var animation = Animation.Talk_Normal;
            if (text.Contains("!")) animation = Animation.Talk_Forceful;
            if (text.Contains("?")) animation = Animation.Talk_Pleading;

            SetCommandable(true, receiver);
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
            if (player.GetLocalBoolean("HOLOCOM_CALL_CONNECTED") == true) return true;
            else return false;
        }
        public static void SetIsInCall(NWPlayer sender, NWPlayer receiver, bool value = true)
        {
            if (value) // START CALL
            {
                sender.SetLocalBoolean("HOLOCOM_CALL_CONNECTED", true);
                receiver.SetLocalBoolean("HOLOCOM_CALL_CONNECTED", true);

                sender.SetLocalObject("HOLOCOM_CALL_CONNECTED_WITH", receiver);
                receiver.SetLocalObject("HOLOCOM_CALL_CONNECTED_WITH", sender);
                
                string message = "Call Connected. (Use the HoloCom or the chat command /endcall to terminate the call)";
                SendMessageToPC(sender, message);
                SendMessageToPC(receiver, message);
                Effect effectImmobilized = EffectCutsceneImmobilize();
                TagEffect(effectImmobilized, "HOLOCOM_CALL_IMMOBILIZE");
                ApplyEffectToObject(DurationType.Permanent, effectImmobilized, sender);
                ApplyEffectToObject(DurationType.Permanent, effectImmobilized, receiver);

                var holosender = CopyObject(sender, VectorService.MoveLocation(receiver.Location, GetFacing(receiver), 2.0f, 180));
                var holoreceiver = CopyObject(receiver, VectorService.MoveLocation(sender.Location, GetFacing(sender), 2.0f, 180));

                ApplyEffectToObject(DurationType.Permanent, EffectVisualEffect(Vfx.Vfx_Dur_Ghostly_Visage_No_Sound, false), holosender);
                ApplyEffectToObject(DurationType.Permanent, EffectVisualEffect(Vfx.Vfx_Dur_Ghostly_Visage_No_Sound, false), holoreceiver);
                SetPlotFlag(holoreceiver, true);
                SetPlotFlag(holosender, true);
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
                    if (_.GetIsEffectValid(effect) == true)
                    {
                        var effectType = GetEffectType(effect);
                        if (effectType == EffectType.CutsceneImmobilize)
                        {
                            RemoveEffect(sender.Object, effect);
                        }
                    }
                }

                foreach (Effect effect in receiver.Effects)
                {
                    if (_.GetIsEffectValid(effect) == true)
                    {
                        var effectType = GetEffectType(effect);
                        if (effectType == EffectType.CutsceneImmobilize)
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
            if (player.GetLocalBoolean("HOLOCOM_CALL_SENDER") == true) return true;
            else return false;
        }
        public static void SetIsCallSender(NWPlayer player, bool value = true)
        {
            if (value) player.SetLocalBoolean("HOLOCOM_CALL_SENDER", true);
            else player.SetLocalBoolean("HOLOCOM_CALL_SENDER", false);
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
            if (player.GetLocalBoolean("HOLOCOM_CALL_RECEIVER") == true) return true;
            else return false;
        }
        public static void SetIsCallReceiver(NWPlayer player, bool value = true)
        {
            if (value) player.SetLocalBoolean("HOLOCOM_CALL_RECEIVER", true);
            else player.SetLocalBoolean("HOLOCOM_CALL_RECEIVER", false);
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
