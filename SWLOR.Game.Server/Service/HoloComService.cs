using System;
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
        ??? can limbo holograms but they dont show in limbo.
         * 
         */

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleChat>(message => OnModuleChat());
            MessageHub.Instance.Subscribe<OnModuleDeath>(message => OnModuleDeath());
        }

        private static void OnModuleDeath()
        {
            NWPlayer player = _.GetLastPlayerDied();
            if (HoloComService.IsInCall(player)) HoloComService.SetIsInCall(player, HoloComService.GetTargetForActiveCall(player), false);

        }

        private static void OnModuleChat()
        {
            NWPlayer sender = GetPCChatSpeaker();
            int talkvolume = GetPCChatVolume();

            ChatChannelType channel = (ChatChannelType)NWNXChat.GetChannel();

            if (!IsInCall(sender)) return;
            if (channel != ChatChannelType.PlayerTalk) return;
            if (channel != ChatChannelType.PlayerWhisper) return;
            if (channel != ChatChannelType.PlayerParty) return;

            //if (talkvolume == TALKVOLUME_SHOUT) return;

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

            /* animation stuff
 
                                givemedeathToday at 5:20 PM
                                anyone know of a way to get a creature's current animation id?
 
                                sherincallToday at 5:27 PM
                                Yes. It's used for the NWNX_Creature_GetMovementType (or whatever it's called)
                                That includes all animations, including combat ones.
 
                                givemedeathToday at 5:28 PM
                                oh, nice!
                                thanks
 
                                sherincallToday at 5:28 PM
                                But the ID used by the engine is not the same as the script constants. There's a translation table somewhere.
 
                                givemedeathToday at 5:28 PM
                                if i'm copying them to another creature do i need the translation?
 
                                sherincallToday at 5:30 PM
                                Oh, you don't ever want to set the animation directly.
                                Reading it is okay, but never change it.
                                So many things are tied to an animation, and changing it directly causes all sorts of issues.
 
                                givemedeathToday at 5:30 PM
                                ah, ok. so i'll have to get the id using GetMovementType, translate, and then play translated value on the target
 
                                sherincallToday at 5:32 PM
                                GetMovementType only works for movement, you'll need a new NWNX function that reads the same field as that.
                                I think it might already exist.. lemme check.
                                No, there's just the translation layer for nwscript->engine animation constants.. https://github.com/nwnxee/unified/blob/master/Core/NWScript/nwnx_consts.nss#L10-L78
                                GitHub
                                nwnxee/unified
                                Binaries available under the Releases tab on Github: https://github.com/nwnxee/unified/releases - nwnxee/unified

 
                                givemedeathToday at 5:36 PM
                                hmm dang, i've no idea how to add a new nwnx function
 
                                sherincallToday at 5:36 PM
                                If you're already set up to build nwnx from source, it's pretty easy and there's a good tutorial.
                                If not, file an issue on github and Daz or Orth or someone can do it for you in 5 minutes.
                                https://github.com/nwnxee/unified/blob/master/Plugins/Creature/Creature.cpp#L1434 this is the field you want to read
                                GitHub
                                nwnxee/unified
                                Binaries available under the Releases tab on Github: https://github.com/nwnxee/unified/releases - nwnxee/unified

 
                                givemedeathToday at 5:37 PM
                                cool thanks! where can i find the tutorial?
 
                                sherincallToday at 5:37 PM
                                https://github.com/BhaalM/stuff/blob/master/simple_nwnxee_tutorial.md
                                GitHub
                                BhaalM/stuff
                                Contribute to BhaalM/stuff development by creating an account on GitHub.

 
                                sherincallToday at 5:38 PM
                                That one does a Set function, you're doing a Get, so look at one of the Get functions in nwnx_creature for how to return a value
                        */



        //SetCommandable(FALSE, receiver);

        //AssignCommand(receiver, () => ActionPlayAnimation(ANIMATION_LOOPING_TALK_NORMAL));
        //AssignCommand(receiver, () => ActionSpeakString(text, TALKVOLUME_TALK));

        // Might be nice to loop through nearby players that are in a holocom chat and send the string as background noise. 
        // This could end up getting expensive though?
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

                //Vector vSender = GetPosition(sender);
                //Vector vReceiver = GetPosition(receiver);

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
