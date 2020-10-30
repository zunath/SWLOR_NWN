using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Service
{
    public static class MessageBoardService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleNWNXChat>(message => OnModuleNWNXChat());
        }

        public static bool CanHandleChat(NWObject sender)
        {
            return sender.GetLocalBool("MESSAGE_BOARD_LISTENING") == true;
        }

        private static void OnModuleNWNXChat()
        {
            NWPlayer player = Chat.GetSender();
            
            if (!CanHandleChat(player)) return;
            var message = Chat.GetMessage();
            Chat.SkipMessage();

            player.SetLocalString("MESSAGE_BOARD_TEXT", message);
            player.SendMessage("Please click the 'Set Title' or 'Set Message' option in the menu.");
        }
    }
}
