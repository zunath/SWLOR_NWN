using System;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Messaging;


namespace SWLOR.Game.Server.Service
{
    public static class PlayerDescriptionService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleChat>(message => OnModuleChat());
        }

        private static void OnModuleChat()
        {
            NWPlayer sender = (_.GetPCChatSpeaker());
            if (sender.GetLocalInt("LISTENING_FOR_DESCRIPTION") != 1) return;
            if (!sender.IsPlayer) return;

            string text = _.GetPCChatMessage().Trim();
            sender.SetLocalString("NEW_DESCRIPTION_TO_SET", text);

            _.SetPCChatMessage(string.Empty); // Skip the message

            _.SendMessageToPC(sender.Object, "New description received. Please press the 'Next' button in the conversation window.");
        }

        public static void ChangePlayerDescription(NWPlayer player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (player.Object == null) throw new ArgumentNullException(nameof(player.Object));

            string newDescription = player.GetLocalString("NEW_DESCRIPTION_TO_SET");
            _.SetDescription(player.Object, newDescription);
            _.SetDescription(player.Object, newDescription, _.FALSE);

            _.FloatingTextStringOnCreature("New description set!", player.Object, _.FALSE);
        }
    }
}
