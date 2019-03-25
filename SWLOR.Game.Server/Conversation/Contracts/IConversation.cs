using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation.Contracts
{
    public interface IConversation
    {
        PlayerDialog SetUp(NWPlayer player);
        void Initialize();
        void DoAction(NWPlayer player, string pageName, int responseID);
        void Back(NWPlayer player, string beforeMovePage, string afterMovePage);
        void EndDialog();
    }
}
