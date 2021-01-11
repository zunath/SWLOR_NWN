using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class Chat
    {
        private const string PLUGIN_NAME = "NWNX_Chat";

        // Sends a chat message. Channel is a NWNX_* constant.
        // If no target is provided, then it broadcasts to all eligible targets.
        // Returns true if successful, false otherwise.
        public static int SendMessage(ChatChannel channel, string message, uint sender, uint target)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SendMessage");
            Internal.NativeFunctions.nwnxPushObject(target);
            Internal.NativeFunctions.nwnxPushObject(sender);
            Internal.NativeFunctions.nwnxPushString(message);
            Internal.NativeFunctions.nwnxPushInt((int)channel);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Registers the script which receives all chat messages.
        // If a script was previously registered, this one will take over.
        public static void RegisterChatScript(string script)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RegisterChatScript");
            Internal.NativeFunctions.nwnxPushString(script);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Skips the message.
        // Must be called from an chat or system script handler.
        public static void SkipMessage()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SkipMessage");
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Gets the channel. Channel is a NWNX_* constant.
        // Must be called from an chat or system script handler.
        public static ChatChannel GetChannel()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetChannel");
            Internal.NativeFunctions.nwnxCallFunction();
            return (ChatChannel)Internal.NativeFunctions.nwnxPopInt();
        }

        // Gets the message.
        // Must be called from an chat or system script handler.
        public static string GetMessage()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetMessage");
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopString();
        }

        // Gets the sender.
        // Must be called from an chat or system script handler.
        public static uint GetSender()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetSender");
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopObject();
        }

        // Gets the target. May be OBJECT_INVALID if no target.
        // Must be called from an chat or system script handler.
        public static uint GetTarget()
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetTarget");
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopObject();
        }

        // Sets the distance with which the player hears talks or whispers.
        public static void SetChatHearingDistance(
            float distance, 
            uint? listener = null,
            ChatChannel channel = ChatChannel.PlayerTalk)
        {
            if (listener == null) listener = Internal.OBJECT_INVALID;
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetChatHearingDistance");
            Internal.NativeFunctions.nwnxPushInt((int)channel);
            Internal.NativeFunctions.nwnxPushObject((uint)listener);
            Internal.NativeFunctions.nwnxPushFloat(distance);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Gets the distance with which the player hears talks or whisper
        public static float GetChatHearingDistance(uint? listener = null,
            ChatChannel channel = ChatChannel.PlayerTalk)
        {
            if (listener == null) listener = Internal.OBJECT_INVALID;
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetChatHearingDistance");
            Internal.NativeFunctions.nwnxPushInt((int)channel);
            Internal.NativeFunctions.nwnxPushObject((uint)listener);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopFloat();
        }
    }
}