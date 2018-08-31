using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWNX.Contracts;

namespace SWLOR.Game.Server.NWNX
{
    public class NWNXChat: NWNXBase, INWNXChat
    {
        public NWNXChat(INWScript script) 
            : base(script)
        {
        }

        // Sends a chat message. Channel is a NWNX_* constant.
        // If no target is provided, then it broadcasts to all eligible targets.
        // Returns TRUE if successful, FALSE otherwise.
        public int SendMessage(int channel, string message, NWObject sender, NWObject target)
        {
            NWNX_PushArgumentObject("NWNX_Chat", "SEND_MESSAGE", target.Object);
            NWNX_PushArgumentObject("NWNX_Chat", "SEND_MESSAGE", sender.Object);
            NWNX_PushArgumentString("NWNX_Chat", "SEND_MESSAGE", message);
            NWNX_PushArgumentInt("NWNX_Chat", "SEND_MESSAGE", channel);
            NWNX_CallFunction("NWNX_Chat", "SEND_MESSAGE");
            return NWNX_GetReturnValueInt("NWNX_Chat", "SEND_MESSAGE");
        }

        // Registers the script which receives all chat messages.
        // If a script was previously registered, this one will take over.
        public void RegisterChatScript(string script)
        {
            NWNX_PushArgumentString("NWNX_Chat", "REGISTER_CHAT_SCRIPT", script);
            NWNX_CallFunction("NWNX_Chat", "REGISTER_CHAT_SCRIPT");
        }

        // Skips the message.
        // Must be called from an chat or system script handler.
        public void SkipMessage()
        {
            NWNX_CallFunction("NWNX_Chat", "SKIP_MESSAGE");
        }

        // Gets the channel. Channel is a NWNX_* constant.
        // Must be called from an chat or system script handler.
        public int GetChannel()
        {
            NWNX_CallFunction("NWNX_Chat", "GET_CHANNEL");
            return NWNX_GetReturnValueInt("NWNX_Chat", "GET_CHANNEL");
        }

        // Gets the message.
        // Must be called from an chat or system script handler.
        public string GetMessage()
        {
            NWNX_CallFunction("NWNX_Chat", "GET_MESSAGE");
            return NWNX_GetReturnValueString("NWNX_Chat", "GET_MESSAGE");
        }

        // Gets the sender.
        // Must be called from an chat or system script handler.
        public NWObject GetSender()
        {
            NWNX_CallFunction("NWNX_Chat", "GET_SENDER");
            return NWObject.Wrap(NWNX_GetReturnValueObject("NWNX_Chat", "GET_SENDER"));
        }

        // Gets the target. May be OBJECT_INVALID if no target.
        // Must be called from an chat or system script handler.
        public NWObject GetTarget()
        {
            NWNX_CallFunction("NWNX_Chat", "GET_TARGET");
            return NWObject.Wrap(NWNX_GetReturnValueObject("NWNX_Chat", "GET_TARGET"));
        }

    }
}
