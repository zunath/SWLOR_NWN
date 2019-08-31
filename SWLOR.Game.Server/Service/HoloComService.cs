using System;
using SWLOR.Game.Server.GameObject;

using NWN;
using static NWN._;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Messaging;


namespace SWLOR.Game.Server.Service
{
    public static class HoloComService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleChat>(message => OnModuleChat());
        }

        private static void OnModuleChat()
        {
            NWPlayer sender = (_.GetPCChatSpeaker());
            if (sender.GetLocalInt("HOLOCOM_CALL_CONNECTED") != 1) return;

            NWPlayer receiver = GetLocalObject(sender, "HOLOGRAM_DESTINATION");

            string text = _.GetPCChatMessage().Trim();

            if (text.StartsWith("/")) return;

            receiver.AssignCommand(() =>
            {
                ActionPlayAnimation(ANIMATION_LOOPING_TALK_NORMAL);
            });

            receiver.AssignCommand(() =>
            {
                ActionSpeakString(text, TALKVOLUME_TALK);
            });

            //AssignCommand(receiver, () => ActionPlayAnimation(ANIMATION_LOOPING_TALK_NORMAL));
            //AssignCommand(receiver, () => ActionSpeakString(text, TALKVOLUME_TALK));

            // Might be nice to loop through nearby players that are in a holocom chat and send the string as background noise. 
            // This could end up getting expensive though?
        }

    }
}
