using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Service.Contracts;
using static NWN._;

namespace SWLOR.Game.Server.Service
{
    public class MessageBoardService : IMessageBoardService
    {
        public static bool CanHandleChat(NWObject sender)
        {
            return sender.GetLocalInt("MESSAGE_BOARD_LISTENING") == TRUE;
        }

        public void OnModuleNWNXChat()
        {
            NWPlayer player = NWNXChat.GetSender().Object;
            
            if (!CanHandleChat(player)) return;
            string message = NWNXChat.GetMessage();
            NWNXChat.SkipMessage();

            player.SetLocalString("MESSAGE_BOARD_TEXT", message);
            player.SendMessage("Please click the 'Set Title' or 'Set Message' option in the menu.");
        }
    }
}
