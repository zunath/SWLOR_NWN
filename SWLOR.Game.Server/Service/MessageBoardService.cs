using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;

using static SWLOR.Game.Server.NWScript._;

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
            return sender.GetLocalBoolean("MESSAGE_BOARD_LISTENING") == true;
        }

        private static void OnModuleNWNXChat()
        {
            NWPlayer player = NWNXChat.GetSender();
            
            if (!CanHandleChat(player)) return;
            string message = NWNXChat.GetMessage();
            NWNXChat.SkipMessage();

            player.SetLocalString("MESSAGE_BOARD_TEXT", message);
            player.SendMessage("Please click the 'Set Title' or 'Set Message' option in the menu.");
        }
    }
}
