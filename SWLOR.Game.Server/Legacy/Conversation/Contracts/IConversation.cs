using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.ValueObject.Dialog;

namespace SWLOR.Game.Server.Legacy.Conversation.Contracts
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
