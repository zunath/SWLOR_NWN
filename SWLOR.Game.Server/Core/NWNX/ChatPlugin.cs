using SWLOR.Game.Server.Core.NWNX.Enum;

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
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SendMessage");
            NWNCore.NativeFunctions.nwnxPushObject(target);
            NWNCore.NativeFunctions.nwnxPushObject(sender);
            NWNCore.NativeFunctions.nwnxPushString(message);
            NWNCore.NativeFunctions.nwnxPushInt((int)channel);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Registers the script which receives all chat messages.
        // If a script was previously registered, this one will take over.
        public static void RegisterChatScript(string script)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RegisterChatScript");
            NWNCore.NativeFunctions.nwnxPushString(script);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Skips the message.
        // Must be called from an chat or system script handler.
        public static void SkipMessage()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SkipMessage");
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Gets the channel. Channel is a NWNX_* constant.
        // Must be called from an chat or system script handler.
        public static ChatChannel GetChannel()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetChannel");
            NWNCore.NativeFunctions.nwnxCallFunction();
            return (ChatChannel)NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Gets the message.
        // Must be called from an chat or system script handler.
        public static string GetMessage()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMessage");
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopString();
        }

        // Gets the sender.
        // Must be called from an chat or system script handler.
        public static uint GetSender()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetSender");
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopObject();
        }

        // Gets the target. May be OBJECT_INVALID if no target.
        // Must be called from an chat or system script handler.
        public static uint GetTarget()
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetTarget");
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopObject();
        }

        // Sets the distance with which the player hears talks or whispers.
        public static void SetChatHearingDistance(
            float distance, 
            uint? listener = null,
            ChatChannel channel = ChatChannel.PlayerTalk)
        {
            if (listener == null) listener = OBJECT_INVALID;
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetChatHearingDistance");
            NWNCore.NativeFunctions.nwnxPushInt((int)channel);
            NWNCore.NativeFunctions.nwnxPushObject((uint)listener);
            NWNCore.NativeFunctions.nwnxPushFloat(distance);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Gets the distance with which the player hears talks or whisper
        public static float GetChatHearingDistance(uint? listener = null,
            ChatChannel channel = ChatChannel.PlayerTalk)
        {
            if (listener == null) listener = OBJECT_INVALID;
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetChatHearingDistance");
            NWNCore.NativeFunctions.nwnxPushInt((int)channel);
            NWNCore.NativeFunctions.nwnxPushObject((uint)listener);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopFloat();
        }
    }
}