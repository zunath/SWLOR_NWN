using System;
using static SWLOR.Game.Server.NWNX.NWNXCore;

namespace SWLOR.Game.Server.NWNX
{
    public static class NWNXRedis
    {
        public struct PubSubMessageData
        {
            public string channel;
            public string message;
        };

        public static int Set(string key, string value, string condition = "")
        {
            NWNX_PushArgumentString("NWNX_Redis", "Deferred", "SET");
            NWNX_PushArgumentString("NWNX_Redis", "Deferred", key);
            NWNX_PushArgumentString("NWNX_Redis", "Deferred", value);
            if (condition != "") NWNX_PushArgumentString("NWNX_Redis", "Deferred", condition);
            NWNX_CallFunction("NWNX_Redis", "Deferred");
            return NWNX_GetReturnValueInt("NWNX_Redis", "Deferred");
        }

        public static string Get(string key)
        {
            NWNX_PushArgumentString("NWNX_Redis", "Deferred", "GET");
            NWNX_PushArgumentString("NWNX_Redis", "Deferred", key);
            NWNX_CallFunction("NWNX_Redis", "Deferred");
            var resultID = NWNX_GetReturnValueInt("NWNX_Redis", "Deferred");
            return GetResultAsString(resultID);
        }

        public static int Delete(string key)
        {
            NWNX_PushArgumentString("NWNX_Redis", "Deferred", "DEL");
            NWNX_PushArgumentString("NWNX_Redis", "Deferred", key);
            NWNX_CallFunction("NWNX_Redis", "Deferred");
            return NWNX_GetReturnValueInt("NWNX_Redis", "Deferred");
        }

        public static bool Exists(string key)
        {
            NWNX_PushArgumentString("NWNX_Redis", "Deferred", "EXISTS");
            NWNX_PushArgumentString("NWNX_Redis", "Deferred", key);
            NWNX_CallFunction("NWNX_Redis", "Deferred");
            var result = NWNX_GetReturnValueInt("NWNX_Redis", "Deferred");

            return Convert.ToBoolean(GetResultAsInt(result));
        }

        private static int GetResultAsInt(int resultId)
        {
            NWNX_PushArgumentInt("NWNX_Redis", "GetResultAsString", resultId);
            NWNX_CallFunction("NWNX_Redis", "GetResultAsString");
            return Convert.ToInt32(NWNX_GetReturnValueString("NWNX_Redis", "GetResultAsString"));
        }

        private static string GetResultAsString(int resultId)
        {
            NWNX_PushArgumentInt("NWNX_Redis", "GetResultAsString", resultId);
            NWNX_CallFunction("NWNX_Redis", "GetResultAsString");
            return NWNX_GetReturnValueString("NWNX_Redis", "GetResultAsString");
        }
    }
}
