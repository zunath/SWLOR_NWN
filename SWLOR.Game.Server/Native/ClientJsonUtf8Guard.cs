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
    /// Makes the JSON that clients send with NUI events and bind updates safe for the engine to decode and log.
    /// </summary>
    public static unsafe class ClientJsonUtf8Guard
    {
        private const string ReadJsonSymbol = "_ZN10CNWMessage8ReadJSONB5cxx11Ev";

        // CNWMessage read state, as CNWMessage::ReadINT uses it.
        private const int ReadBufferOffset = 0x38;
        private const int ReadBufferSizeOffset = 0x40;
        private const int ReadBufferPositionOffset = 0x44;

        // No UBJSON value starts with a zero byte, so the engine's reader rejects data that does.
        private const byte UnreadableMarker = 0;

        // nlohmann::json: a type byte, then its value at +8. Strings are std::string (data, then length).
        private const byte JsonObject = 1;
        private const byte JsonArray = 2;
        private const byte JsonString = 3;
        private const int JsonSize = 16;
        private const int JsonValueOffset = 8;
        private const int VectorEndOffset = 8;
        private const int StringLengthOffset = 8;

        // libstdc++ std::map: root node at +16; each node holds left +16, right +24 and its json value at +64.
        private const int MapRootOffset = 16;
        private const int NodeLeftOffset = 16;
        private const int NodeRightOffset = 24;
        private const int NodeValueOffset = 64;

        private static readonly TimeSpan ReportDelay = TimeSpan.FromSeconds(10);
        private static readonly Dictionary<uint, (string Account, uint Player, int Replaced, int Rejected)> PendingReports = new();
        private static bool _reportScheduled;
        private static FunctionHook* _readJsonHook;

        /// <summary>The number of invalid UTF-8 bytes replaced since the module loaded.</summary>
        public static long ReplacedByteCount { get; private set; }

        /// <summary>The number of client JSON payloads made unreadable since the module loaded.</summary>
        public static long RejectedPayloadCount { get; private set; }

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
            var replaced = 0;
            bool rejected;
            try
            {
                rejected = !RepairPendingUbjson((byte*)message, out replaced);
            }
            catch (Exception exception)
            {
                rejected = true;
                MakePendingUbjsonUnreadable((byte*)message);
                Log.WriteError(exception, "Could not check client UBJSON for invalid UTF-8");
            }

            var original = (delegate* unmanaged<void*, void*, void*>)_readJsonHook->m_trampoline;
            var json = original(result, message);
            try
            {
                // A rejected payload decodes to the engine's error text, so string values are still checked.
                replaced += ReplaceInvalidStringValues((byte*)json);
                if (replaced > 0 || rejected)
                    Record(CNWMessage.FromPointer((IntPtr)message).m_dwPlayerID, replaced, rejected);
            }
            catch (Exception exception)
            {
                Log.WriteError(exception, "Could not check client JSON for invalid UTF-8");
            }

            return json;
        }

        /// <summary>
        /// Repairs the UBJSON that ReadJSON is about to decode, or makes it unreadable. Returns false when rejected.
        /// </summary>
        private static bool RepairPendingUbjson(byte* message, out int replaced)
        {
            replaced = 0;
            if (!TryGetPendingUbjson(message, out var lengthField, out var ubjson, out var overruns))
                return true;

            // The engine would read past the data, so it reads nothing instead.
            if (overruns)
            {
                *(uint*)lengthField = 0;
                return false;
            }

            replaced = UbjsonTextRepair.RepairStrings(ubjson, out var complete);
            if (complete)
                return true;

            replaced = 0;
            ubjson[0] = UnreadableMarker;
            return false;
        }

        private static void MakePendingUbjsonUnreadable(byte* message)
        {
            if (!TryGetPendingUbjson(message, out var lengthField, out var ubjson, out var overruns))
                return;

            if (overruns)
                *(uint*)lengthField = 0;
            else
                ubjson[0] = UnreadableMarker;
        }

        /// <summary>
        /// Finds the 4-byte length and the UBJSON bytes at the read position. False when there is nothing to check.
        /// </summary>
        private static bool TryGetPendingUbjson(byte* message, out byte* lengthField, out Span<byte> ubjson, out bool overruns)
        {
            lengthField = null;
            ubjson = default;
            overruns = false;

            // Without multiplayer the engine also reads type bytes here; nwserver always runs multiplayer.
            if (NWNXLib.g_pAppManager.m_bMultiplayerEnabled == 0)
                return false;

            var buffer = *(byte**)(message + ReadBufferOffset);
            var size = (long)*(uint*)(message + ReadBufferSizeOffset);
            var position = (long)*(uint*)(message + ReadBufferPositionOffset);
            if (buffer == null || position + sizeof(uint) > size)
                return false;

            lengthField = buffer + position;
            var length = (long)*(uint*)lengthField;
            var start = position + sizeof(uint);
            if (length > size - start)
            {
                overruns = true;
                return true;
            }

            if (length == 0)
                return false;

            ubjson = new Span<byte>(buffer + start, (int)length);
            return true;
        }

        /// <summary>
        /// Replaces invalid UTF-8 in every string value of a decoded json. Keys are left alone, keeping the map sorted.
        /// </summary>
        private static int ReplaceInvalidStringValues(byte* root)
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

        /// <summary>
        /// Counts a repair or rejection and adds it to the sender's next report, written at most once per delay.
        /// </summary>
        private static void Record(uint playerId, int replaced, bool rejected)
        {
            ReplacedByteCount += replaced;
            if (rejected)
                RejectedPayloadCount++;

            if (!PendingReports.TryGetValue(playerId, out var report))
            {
                // Read the sender now: the client may leave before the report is written.
                var client = NWNXLib.g_pAppManager.m_pServerExoApp.GetClientObjectByPlayerId(playerId);
                report = (client?.GetPlayerName()?.ToString() ?? $"client {playerId}", client?.m_oidPCObject ?? OBJECT_INVALID, 0, 0);
            }

            PendingReports[playerId] = (report.Account, report.Player, report.Replaced + replaced, report.Rejected + (rejected ? 1 : 0));
            if (_reportScheduled)
                return;

            _reportScheduled = true;
            Scheduler.Schedule(WriteReports, ReportDelay);
        }

        private static void WriteReports()
        {
            _reportScheduled = false;
            foreach (var (_, report) in PendingReports)
            {
                var character = GetIsObjectValid(report.Player)
                    ? $"{PlayerName.GetAuditName(report.Player)} - {GetPCPublicCDKey(report.Player)}"
                    : "no character";
                Log.Write(LogGroup.Server, $"Client NUI data from {report.Account} ({character}): " +
                                           $"replaced {report.Replaced} invalid UTF-8 byte(s), rejected {report.Rejected} unreadable payload(s).");
            }

            PendingReports.Clear();
        }
    }
}
