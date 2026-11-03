using System.Collections.Generic;
using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Repairs invalid UTF-8 in the JSON that clients send with NUI events and bind updates. The engine decodes
    /// that JSON without checking its text, then serializes every received payload for a debug log line with
    /// strict UTF-8 handling. A string holding a single Windows-1252 byte made the serializer throw, nothing caught
    /// the exception, and the server aborted. Repairing the decoded JSON keeps every later use of it safe.
    /// </summary>
    public static unsafe class ClientJsonUtf8Guard
    {
        private const string ReadJsonSymbol = "_ZN10CNWMessage8ReadJSONB5cxx11Ev";

        // nlohmann::json (libstdc++): a type byte, then an 8-byte value at offset 8. Objects point at a std::map,
        // arrays at a std::vector of json, strings at a std::string (data pointer, then length).
        private const byte JsonObject = 1;
        private const byte JsonArray = 2;
        private const byte JsonString = 3;
        private const int JsonSize = 16;
        private const int JsonValueOffset = 8;
        private const int VectorEndOffset = 8;
        private const int StringLengthOffset = 8;

        // std::map: the empty comparator, then the tree header (color, parent = root, left, right), then the count.
        // Each node holds color, parent, left and right, then the key std::string and its json value.
        private const int MapRootOffset = 16;
        private const int NodeLeftOffset = 16;
        private const int NodeRightOffset = 24;
        private const int NodeKeyOffset = 32;
        private const int NodeValueOffset = 64;

        private static FunctionHook* _readJsonHook;

        /// <summary>The total number of invalid bytes replaced since the module loaded.</summary>
        public static long ReplacedByteCount { get; private set; }

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHook()
        {
            if (_readJsonHook != null)
                return;

            delegate* unmanaged<void*, void*, void*> readJson = &ReadJson;
            _readJsonHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(), ReadJsonSymbol),
                (IntPtr)readJson, HookOrder.Earliest);
        }

        // ReadJSON returns the decoded json through a hidden result pointer, ahead of the message pointer.
        [UnmanagedCallersOnly]
        private static void* ReadJson(void* result, void* message)
        {
            var original = (delegate* unmanaged<void*, void*, void*>)_readJsonHook->m_trampoline;
            var json = original(result, message);
            try
            {
                var replaced = ReplaceInvalidText((byte*)json);
                if (replaced > 0)
                {
                    ReplacedByteCount += replaced;
                    var playerId = CNWMessage.FromPointer((IntPtr)message).m_dwPlayerID;
                    // Native hooks only read engine state; look the player up once the frame continues.
                    Scheduler.Schedule(() => LogReplacement(playerId, replaced), TimeSpan.Zero);
                }
            }
            catch (Exception exception)
            {
                Log.WriteError(exception, "Could not check client JSON for invalid UTF-8");
            }

            return json;
        }

        /// <summary>
        /// Replaces invalid UTF-8 in every string value and object key of a decoded json, including nested ones.
        /// </summary>
        /// <param name="root">The json to repair.</param>
        /// <returns>The number of bytes replaced.</returns>
        private static int ReplaceInvalidText(byte* root)
        {
            var rootType = root[0];
            if (rootType != JsonObject && rootType != JsonArray && rootType != JsonString)
                return 0;

            var replaced = 0;
            var values = new Stack<IntPtr>();
            var nodes = new Stack<IntPtr>();
            values.Push((IntPtr)root);
            while (values.Count > 0)
            {
                var json = (byte*)values.Pop();
                var value = *(byte**)(json + JsonValueOffset);
                if (value == null)
                    continue;

                switch (json[0])
                {
                    case JsonString:
                        replaced += ReplaceInvalidBytes(value);
                        break;
                    case JsonArray:
                        var end = *(byte**)(value + VectorEndOffset);
                        for (var element = *(byte**)value; element < end; element += JsonSize)
                            values.Push((IntPtr)element);
                        break;
                    case JsonObject:
                        var treeRoot = *(byte**)(value + MapRootOffset);
                        if (treeRoot != null)
                            nodes.Push((IntPtr)treeRoot);
                        while (nodes.Count > 0)
                        {
                            var node = (byte*)nodes.Pop();
                            // A repaired key can sort differently, but only keys that were invalid change.
                            replaced += ReplaceInvalidBytes(node + NodeKeyOffset);
                            values.Push((IntPtr)(node + NodeValueOffset));
                            var left = *(byte**)(node + NodeLeftOffset);
                            var right = *(byte**)(node + NodeRightOffset);
                            if (left != null)
                                nodes.Push((IntPtr)left);
                            if (right != null)
                                nodes.Push((IntPtr)right);
                        }

                        break;
                }
            }

            return replaced;
        }

        private static int ReplaceInvalidBytes(byte* text)
        {
            var data = *(byte**)text;
            var length = *(ulong*)(text + StringLengthOffset);
            if (data == null || length == 0 || length > int.MaxValue)
                return 0;

            return Utf8TextRepair.ReplaceInvalidBytes(new Span<byte>(data, (int)length));
        }

        private static void LogReplacement(uint playerId, int replaced)
        {
            var client = NWNXLib.g_pAppManager.m_pServerExoApp.GetClientObjectByPlayerId(playerId);
            var player = client?.m_oidPCObject ?? OBJECT_INVALID;
            var sender = GetIsObjectValid(player)
                ? $"{PlayerName.GetAuditName(player)} - {GetPCPlayerName(player)} - {GetPCPublicCDKey(player)}"
                : $"client {playerId}";
            Log.Write(LogGroup.Server, $"Replaced {replaced} invalid UTF-8 byte(s) in NUI data sent by {sender}.");
        }
    }
}
