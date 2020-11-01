using System;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.Event.Module;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Messaging;

namespace SWLOR.Game.Server.Legacy.Service
{
    public static class PlayerDescriptionService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleChat>(message => OnModuleChat());
        }

        private static void OnModuleChat()
        {
            NWPlayer sender = (NWScript.GetPCChatSpeaker());
            if (sender.GetLocalInt("LISTENING_FOR_DESCRIPTION") != 1) return;
            if (!sender.IsPlayer) return;

            var text = NWScript.GetPCChatMessage().Trim();
            sender.SetLocalString("NEW_DESCRIPTION_TO_SET", text);

            NWScript.SetPCChatMessage(string.Empty); // Skip the message

            NWScript.SendMessageToPC(sender.Object, "New description received. Please press the 'Next' button in the conversation window.");
        }

        public static void ChangePlayerDescription(NWPlayer player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));

            var newDescription = player.GetLocalString("NEW_DESCRIPTION_TO_SET");
            NWScript.SetDescription(player.Object, newDescription);
            NWScript.SetDescription(player.Object, newDescription, false);

            NWScript.FloatingTextStringOnCreature("New description set!", player.Object, false);
        }
    }
}
