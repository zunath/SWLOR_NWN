using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using static NWN._;

namespace SWLOR.Game.Server.Service
{
    public class MessageBoardService : IMessageBoardService
    {
        
        private readonly INWNXChat _nwnxChat;

        public MessageBoardService(
            
            INWNXChat nwnxChat)
        {
            
            _nwnxChat = nwnxChat;
        }

        public static bool CanHandleChat(NWObject sender)
        {
            return sender.GetLocalInt("MESSAGE_BOARD_LISTENING") == TRUE;
        }

        public void OnModuleNWNXChat()
        {
            NWPlayer player = _nwnxChat.GetSender().Object;
            
            if (!CanHandleChat(player)) return;
            string message = _nwnxChat.GetMessage();
            _nwnxChat.SkipMessage();

            player.SetLocalString("MESSAGE_BOARD_TEXT", message);
            player.SendMessage("Please click the 'Set Title' or 'Set Message' option in the menu.");
        }
    }
}
