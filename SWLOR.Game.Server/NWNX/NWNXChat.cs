using NWN;
using static SWLOR.Game.Server.NWNX.NWNXCore;

namespace SWLOR.Game.Server.NWNX
{
    public static class NWNXChat
    {
        private const string NWNX_Chat = "NWNX_Chat"; 

        // Sends a chat message. Channel is a NWNX_* constant.
        // If no target is provided, then it broadcasts to all eligible targets.
        // Returns true if successful, false otherwise.
        public static int SendMessage(int channel, string message, NWGameObject sender, NWGameObject target)
        {
            string sFunc = "SEND_MESSAGE";

            NWNX_PushArgumentObject(NWNX_Chat, sFunc, target);
            NWNX_PushArgumentObject(NWNX_Chat, sFunc, sender);
            NWNX_PushArgumentString(NWNX_Chat, sFunc, message);
            NWNX_PushArgumentInt(NWNX_Chat, sFunc, channel);
            NWNX_CallFunction(NWNX_Chat, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Chat, sFunc);
        }

        // Registers the script which receives all chat messages.
        // If a script was previously registered, this one will take over.
        public static void RegisterChatScript(string script)
        {
            string sFunc = "RegisterChatScript";

            NWNX_PushArgumentString(NWNX_Chat, sFunc, script);
            NWNX_CallFunction(NWNX_Chat, sFunc);
        }

        // Skips the message.
        // Must be called from an chat or system script handler.
        public static void SkipMessage()
        {
            string sFunc = "SkipMessage";

            NWNX_CallFunction(NWNX_Chat, sFunc);
        }

        // Gets the channel. Channel is a NWNX_* constant.
        // Must be called from an chat or system script handler.
        public static NWNXChatChannel GetChannel()
        {
            string sFunc = "GetChannel";

            NWNX_CallFunction(NWNX_Chat, sFunc);
            return (NWNXChatChannel)NWNX_GetReturnValueInt(NWNX_Chat, sFunc);
        }

        // Gets the message.
        // Must be called from an chat or system script handler.
        public static string GetMessage()
        {
            string sFunc = "GetMessage";

            NWNX_CallFunction(NWNX_Chat, sFunc);
            return NWNX_GetReturnValueString(NWNX_Chat, sFunc);
        }

        // Gets the sender.
        // Must be called from an chat or system script handler.
        public static NWGameObject GetSender()
        {
            string sFunc = "GetSender";

            NWNX_CallFunction(NWNX_Chat, sFunc);
            return NWNX_GetReturnValueObject(NWNX_Chat, sFunc);
        }

        // Gets the target. May be OBJECT_INVALID if no target.
        // Must be called from an chat or system script handler.
        public static NWGameObject GetTarget()
        {
            string sFunc = "GetTarget";

            NWNX_CallFunction(NWNX_Chat, sFunc);
            return NWNX_GetReturnValueObject(NWNX_Chat, sFunc);
        }

        /// <summary>
        /// Sets the distance with which the player hears talks or whispers.
        /// </summary>
        /// <param name="distance">The distance in meters.</param>
        /// <param name="listener">The listener, if OBJECT_INVALID then it will be set server wide.</param>
        /// <param name="channel">The "channel" to modify the distance heard. Only applicable for talk and whisper.</param>
        public static void SetChatHearingDistance(float distance, NWGameObject listener = null, NWNXChatChannel channel = NWNXChatChannel.PlayerTalk)
        {
            if(listener == null)
                listener = NWGameObject.OBJECT_INVALID;

            string sFunc = "SetChatHearingDistance";

            NWNX_PushArgumentInt(NWNX_Chat, sFunc, (int)channel);
            NWNX_PushArgumentObject(NWNX_Chat, sFunc, listener);
            NWNX_PushArgumentFloat(NWNX_Chat, sFunc, distance);
            NWNX_CallFunction(NWNX_Chat, sFunc);
        }

        /// <summary>
        /// Gets the distance with which the player hears talks or whisper
        /// </summary>
        /// <param name="listener">The listener, if OBJECT_INVALID then will return server wide setting.</param>
        /// <param name="channel">The "channel". Only applicable for talk and whisper.</param>
        /// <returns>The hearing distance</returns>
        public static float GetChatHearingDistance(NWGameObject listener = null, NWNXChatChannel channel = NWNXChatChannel.PlayerTalk)
        {
            if(listener == null)
                listener = NWGameObject.OBJECT_INVALID;

            string sFunc = "GetChatHearingDistance";

            NWNX_PushArgumentInt(NWNX_Chat, sFunc, (int)channel);
            NWNX_PushArgumentObject(NWNX_Chat, sFunc, listener);
            NWNX_CallFunction(NWNX_Chat, sFunc);
            return NWNX_GetReturnValueFloat(NWNX_Chat, sFunc);
        }
    }
}
