using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.NWNX.Contracts
{
    public interface INWNXChat
    {
        int SendMessage(int channel, string message, NWObject sender, NWObject target);
        void RegisterChatScript(string script);
        void SkipMessage();
        int GetChannel();
        string GetMessage();
        NWObject GetSender();
        NWObject GetTarget();
    }
}