using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.NWN.API.Core;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class ChatPlugin
    {
        private const string PLUGIN_NAME = "NWNX_Chat";

        // Sends a chat message. Channel is a NWNX_* constant.
        // If no target is provided, then it broadcasts to all eligible targets.
        // Returns true if successful, false otherwise.
        public static int SendMessage(ChatChannel channel, string message, uint sender, uint target)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SendMessage");
            NWNXPInvoke.NWNXPushObject(target);
            NWNXPInvoke.NWNXPushObject(sender);
            NWNXPInvoke.NWNXPushString(message);
            NWNXPInvoke.NWNXPushInt((int)channel);
            NWNXPInvoke.NWNXCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Registers the script which receives all chat messages.
        // If a script was previously registered, this one will take over.
        public static void RegisterChatScript(string script)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "RegisterChatScript");
            NWNXPInvoke.NWNXPushString(script);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Skips the message.
        // Must be called from an chat or system script handler.
        public static void SkipMessage()
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SkipMessage");
            NWNXPInvoke.NWNXCallFunction();
        }

        // Gets the channel. Channel is a NWNX_* constant.
        // Must be called from an chat or system script handler.
        public static ChatChannel GetChannel()
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetChannel");
            NWNXPInvoke.NWNXCallFunction();
            return (ChatChannel)NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Gets the message.
        // Must be called from an chat or system script handler.
        public static string GetMessage()
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetMessage");
            NWNXPInvoke.NWNXCallFunction();
            return NWNXPInvoke.NWNXPopString();
        }

        // Gets the sender.
        // Must be called from an chat or system script handler.
        public static uint GetSender()
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetSender");
            NWNXPInvoke.NWNXCallFunction();
            return NWNCore.NativeFunctions.nwnxPopObject();
        }

        // Gets the target. May be OBJECT_INVALID if no target.
        // Must be called from an chat or system script handler.
        public static uint GetTarget()
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetTarget");
            NWNXPInvoke.NWNXCallFunction();
            return NWNCore.NativeFunctions.nwnxPopObject();
        }

        // Sets the distance with which the player hears talks or whispers.
        public static void SetChatHearingDistance(
            float distance, 
            uint? listener = null,
            ChatChannel channel = ChatChannel.PlayerTalk)
        {
            if (listener == null) listener = OBJECT_INVALID;
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetChatHearingDistance");
            NWNXPInvoke.NWNXPushInt((int)channel);
            NWNXPInvoke.NWNXPushObject((uint)listener);
            NWNCore.NativeFunctions.nwnxPushFloat(distance);
            NWNXPInvoke.NWNXCallFunction();
        }

        // Gets the distance with which the player hears talks or whisper
        public static float GetChatHearingDistance(uint? listener = null,
            ChatChannel channel = ChatChannel.PlayerTalk)
        {
            if (listener == null) listener = OBJECT_INVALID;
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetChatHearingDistance");
            NWNXPInvoke.NWNXPushInt((int)channel);
            NWNXPInvoke.NWNXPushObject((uint)listener);
            NWNXPInvoke.NWNXCallFunction();
            return NWNCore.NativeFunctions.nwnxPopFloat();
        }
    }
}