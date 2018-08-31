using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation.Contracts
{
    internal interface IConversation
    {
        PlayerDialog SetUp(NWPlayer player);
        void Initialize();
        void DoAction(NWPlayer player, string pageName, int responseID);
        void EndDialog();
    }
}
