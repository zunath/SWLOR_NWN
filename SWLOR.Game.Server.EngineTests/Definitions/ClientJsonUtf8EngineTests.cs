using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Native;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Hands the engine client NUI messages whose JSON it could not decode or log safely.
    /// </summary>
    public static class ClientJsonUtf8EngineTests
    {
        private const byte NuiEventMinor = 0x03;
        private const byte NuiBindUpdateMinor = 0x04;
        private const int UnknownWindowToken = 987654;
        private const string ReadJsonSymbol = "_ZN10CNWMessage8ReadJSONB5cxx11Ev";
        private const string DestroyJsonSymbol =
            "_ZN8nlohmann10basic_jsonISt3mapSt6vectorNSt7__cxx1112basic_stringIcSt11char_traitsIcESaIcEEEblmdSaNS_14adl_serializerES2_IhSaIhEEE10json_value7destroyENS_6detail7value_tE";

        // nlohmann::json and libstdc++ layouts, as ClientJsonUtf8Guard reads them.
        private const byte JsonObject = 1;
        private const byte JsonString = 3;

        // {"text": "Sl\xE1ine", "k\xE9y": ["Caf\xE9f", "é", 5], "nested": {"n\x92": "\xC0\xAF"}} holds 6 invalid bytes.
        private const int InvalidBytesInPayload = 6;

        [EngineTest("A NUI event whose payload text is not valid UTF-8 is repaired instead of aborting the server", Category = "ClientJsonUtf8", TimeoutSeconds = 60f)]
        public static async Task NuiEventPayloadIsRepaired(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var before = Counts();

            await ctx.ExecuteInCreatureContextAsync(fixture.Creature, () =>
                SendNuiMessage(fixture.Creature, NuiEventMinor, BuildEventMessage(Json(BuildInvalidPayload()))));

            ctx.AssertEqual((before.Replaced + InvalidBytesInPayload, before.Rejected), Counts(), "replaced and rejected counts");
        }

        [EngineTest("A NUI bind update whose text is not valid UTF-8 is repaired instead of aborting the server", Category = "ClientJsonUtf8", TimeoutSeconds = 60f)]
        public static async Task NuiBindUpdateIsRepaired(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var before = Counts();

            await ctx.ExecuteInCreatureContextAsync(fixture.Creature, () =>
                SendNuiMessage(fixture.Creature, NuiBindUpdateMinor, BuildBindUpdateMessage(Json(BuildInvalidPayload()))));

            ctx.AssertEqual((before.Replaced + InvalidBytesInPayload, before.Rejected), Counts(), "replaced and rejected counts");
        }

        [EngineTest("Repaired object keys that collide decode as one entry, so the engine's map stays consistent", Category = "ClientJsonUtf8", TimeoutSeconds = 60f)]
        public static async Task CollidingKeysDecodeAsOneEntry(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            // {"mouse_btn": 1, "\x80": "a", "\x81": "b", "typed": {$S#2 "k\xE9": "x", "\x82": "y"}}
            var payload = Concat(
                Bytes('{'),
                UbjsonKey(Ascii("mouse_btn")), new byte[] { (byte)'i', 1 },
                UbjsonKey(new byte[] { 0x80 }), UbjsonString(Ascii("a")),
                UbjsonKey(new byte[] { 0x81 }), UbjsonString(Ascii("b")),
                UbjsonKey(Ascii("typed")),
                Bytes('{'), Bytes('$'), Bytes('S'), Bytes('#'), new byte[] { (byte)'i', 2 },
                UbjsonKey(new byte[] { (byte)'k', 0xE9 }), UbjsonKey(Ascii("x")),
                UbjsonKey(new byte[] { 0x82 }), UbjsonKey(Ascii("y")),
                Bytes('}'));
            List<(string Key, string Value)> entries = null;

            await ctx.ExecuteInCreatureContextAsync(fixture.Creature, () =>
                entries = DecodeWithEngine(fixture.Creature, payload, ReadObjectEntries));

            // Repaired after decoding instead, "\x80" and "\x81" would both stay in the map as "?".
            ctx.AssertEqual("?=b|mouse_btn={type 5}|typed={object}", string.Join("|", entries.Select(entry => $"{entry.Key}={entry.Value}")),
                "decoded keys in map order");
        }

        [EngineTest("A NUI event with valid UTF-8 text decodes unchanged", Category = "ClientJsonUtf8", TimeoutSeconds = 60f)]
        public static async Task ValidNuiEventIsUntouched(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var text = "Sláine - café ✓";
            var payload = Concat(Bytes('{'), UbjsonKey(Ascii("text")), UbjsonString(Encoding.UTF8.GetBytes(text)), Bytes('}'));
            var before = Counts();
            List<(string Key, string Value)> entries = null;

            await ctx.ExecuteInCreatureContextAsync(fixture.Creature, () =>
            {
                SendNuiMessage(fixture.Creature, NuiEventMinor, BuildEventMessage(Json(payload)));
                entries = DecodeWithEngine(fixture.Creature, payload, ReadObjectEntries);
            });

            ctx.AssertEqual(before, Counts(), "replaced and rejected counts");
            ctx.AssertEqual($"text={text}", string.Join("|", entries.Select(entry => $"{entry.Key}={entry.Value}")), "decoded payload");
        }

        [EngineTest("Client NUI payloads the engine cannot decode safely are rejected instead of crashing the server", Category = "ClientJsonUtf8", TimeoutSeconds = 60f)]
        public static async Task UnsafePayloadsAreRejected(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var nested = Concat(Enumerable.Repeat((byte)'[', UbjsonTextRepair.MaxDepth + 1).ToArray(),
                Enumerable.Repeat((byte)']', UbjsonTextRepair.MaxDepth + 1).ToArray());
            var oneMillionNulls = new byte[] { (byte)'[', (byte)'$', (byte)'Z', (byte)'#', (byte)'l', 0x00, 0x10, 0x00, 0x00 };
            var formatText = new byte[] { (byte)'H', (byte)'i', 2, (byte)'%', (byte)'n' };
            var unclosedKey = Concat(Bytes('{'), UbjsonKey(new byte[] { 0x80 }));
            var cases = new (string Name, byte[] Section)[]
            {
                ("length past the data", JsonWithLength(Ascii("SiAabc"), 1000)),
                ("length that wraps", JsonWithLength(Ascii("Si"), 0xFFFFFFFC)),
                ("nesting past the limit", Json(nested)),
                ("a huge typed array of nulls", Json(oneMillionNulls)),
                ("high-precision number text", Json(formatText)),
                ("data that ends early", Json(unclosedKey)),
            };

            foreach (var (name, section) in cases)
            {
                var before = Counts();
                await ctx.ExecuteInCreatureContextAsync(fixture.Creature, () =>
                    SendNuiMessage(fixture.Creature, NuiEventMinor, BuildEventMessage(section)));
                ctx.AssertEqual((before.Replaced, before.Rejected + 1), Counts(), $"counts after {name}");
            }
        }

        [EngineTest("An invalid object key at the deepest allowed nesting is repaired", Category = "ClientJsonUtf8", TimeoutSeconds = 60f)]
        public static async Task DeepestAllowedKeyIsRepaired(EngineTestContext ctx)
        {
            var depth = UbjsonTextRepair.MaxDepth - 1;
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var before = Counts();
            var payload = Concat(
                Enumerable.Repeat((byte)'[', depth).ToArray(),
                Bytes('{'), UbjsonKey(new byte[] { 0x80 }), Bytes('Z'), Bytes('}'),
                Enumerable.Repeat((byte)']', depth).ToArray());

            await ctx.ExecuteInCreatureContextAsync(fixture.Creature, () =>
                SendNuiMessage(fixture.Creature, NuiEventMinor, BuildEventMessage(Json(payload))));

            ctx.AssertEqual((before.Replaced + 1, before.Rejected), Counts(), "replaced and rejected counts");
        }

        private static (long Replaced, long Rejected) Counts() =>
            (ClientJsonUtf8Guard.ReplacedByteCount, ClientJsonUtf8Guard.RejectedPayloadCount);

        private static byte[] BuildInvalidPayload()
        {
            return Concat(
                Bytes('{'),
                UbjsonKey(Ascii("text")),
                UbjsonString(new byte[] { (byte)'S', (byte)'l', 0xE1, (byte)'i', (byte)'n', (byte)'e' }),
                UbjsonKey(new byte[] { (byte)'k', 0xE9, (byte)'y' }),
                Bytes('['),
                UbjsonString(new byte[] { (byte)'C', (byte)'a', (byte)'f', 0xE9, (byte)'f' }),
                UbjsonString(new byte[] { 0xC3, 0xA9 }),
                new byte[] { (byte)'i', 5 },
                Bytes(']'),
                UbjsonKey(Ascii("nested")),
                Bytes('{'),
                UbjsonKey(new byte[] { (byte)'n', 0x92 }),
                UbjsonString(new byte[] { 0xC0, 0xAF }),
                Bytes('}'),
                Bytes('}'));
        }

        // Event message: entry count, then window token, event name, element ID, array index and JSON payload.
        private static byte[] BuildEventMessage(byte[] json)
        {
            return Concat(Int(1), Int(UnknownWindowToken), Text("click"), Text("client_json_utf8_probe"), Int(-1), json);
        }

        // Bind update message: entry count, then window token, bind name, JSON value and array index.
        private static byte[] BuildBindUpdateMessage(byte[] json)
        {
            return Concat(Int(1), Int(UnknownWindowToken), Text("client_json_utf8_probe"), json, Int(-1));
        }

        /// <summary>
        /// Hands the engine a NUI message as if the player's client sent it.
        /// </summary>
        private static unsafe void SendNuiMessage(uint sender, byte minor, byte[] section)
        {
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var player = server.GetClientObjectByObjectId(sender);
            var message = server.GetNWSMessage();
            var buffer = AllocateReadBuffer(section, out var size);
            try
            {
                message.SetReadMessage(buffer, size, player.m_nPlayerID, 1);
                message.HandlePlayerToServerNuiEvent(player, minor);
            }
            finally
            {
                message.SetReadMessage(null, 0, player.m_nPlayerID, 1);
                NativeMemory.Free(buffer);
            }
        }

        /// <summary>
        /// Decodes UBJSON with the engine's ReadJSON, guard included, and inspects the result before freeing it.
        /// </summary>
        private static unsafe T DecodeWithEngine<T>(uint sender, byte[] ubjson, Func<IntPtr, T> inspect)
        {
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var player = server.GetClientObjectByObjectId(sender);
            var message = server.GetNWSMessage();
            var program = NativeLibrary.GetMainProgramHandle();
            var readJson = (delegate* unmanaged<void*, void*, void*>)NativeLibrary.GetExport(program, ReadJsonSymbol);
            var destroy = (delegate* unmanaged<void*, int, void>)NativeLibrary.GetExport(program, DestroyJsonSymbol);
            var buffer = AllocateReadBuffer(Json(ubjson), out var size);
            var json = (byte*)NativeMemory.AllocZeroed(16);
            try
            {
                message.SetReadMessage(buffer, size, player.m_nPlayerID, 1);
                readJson(json, (void*)message.Pointer);
                return inspect((IntPtr)json);
            }
            finally
            {
                destroy(json + 8, json[0]);
                message.SetReadMessage(null, 0, player.m_nPlayerID, 1);
                NativeMemory.Free(json);
                NativeMemory.Free(buffer);
            }
        }

        // The read buffer opens with its byte-section length plus 3, then the section, then one bit-fragment byte.
        private static unsafe byte* AllocateReadBuffer(byte[] section, out uint size)
        {
            size = (uint)(4 + section.Length + 1);
            var buffer = (byte*)NativeMemory.AllocZeroed(size);
            *(uint*)buffer = (uint)(4 + section.Length + 3);
            Marshal.Copy(section, 0, (IntPtr)(buffer + 4), section.Length);
            return buffer;
        }

        /// <summary>
        /// Lists a decoded object's entries in map order, with string values as text and other values by type.
        /// </summary>
        private static unsafe List<(string Key, string Value)> ReadObjectEntries(IntPtr json)
        {
            var root = (byte*)json;
            if (root[0] != JsonObject)
                return new List<(string Key, string Value)> { ("type", root[0].ToString()) };

            var entries = new List<(string Key, string Value)>();
            var map = *(byte**)(root + 8);
            // In-order walk of the red-black tree: root at +16; each node's left +16, right +24, key +32, value +64.
            var pending = new Stack<IntPtr>();
            var node = *(byte**)(map + 16);
            while (node != null || pending.Count > 0)
            {
                while (node != null)
                {
                    pending.Push((IntPtr)node);
                    node = *(byte**)(node + 16);
                }

                node = (byte*)pending.Pop();
                var value = node + 64;
                var text = value[0] switch
                {
                    JsonString => ReadStdString(*(byte**)(value + 8)),
                    JsonObject => "{object}",
                    _ => $"{{type {value[0]}}}"
                };
                entries.Add((ReadStdString(node + 32), text));
                node = *(byte**)(node + 24);
            }

            return entries;
        }

        private static unsafe string ReadStdString(byte* text)
        {
            return Encoding.UTF8.GetString(*(byte**)text, (int)*(ulong*)(text + 8));
        }

        private static byte[] Int(int value) => BitConverter.GetBytes(value);

        private static byte[] Text(string value) => Concat(Int(value.Length), Ascii(value));

        private static byte[] Json(byte[] ubjson) => Concat(Int(ubjson.Length), ubjson);

        // A JSON field whose declared length does not match the bytes that follow.
        private static byte[] JsonWithLength(byte[] ubjson, uint declaredLength) =>
            Concat(BitConverter.GetBytes(declaredLength), ubjson);

        // UBJSON, the binary form the client uses for NUI JSON. Lengths here stay under 128 bytes.
        private static byte[] UbjsonKey(byte[] key) => Concat(new byte[] { (byte)'i', (byte)key.Length }, key);

        private static byte[] UbjsonString(byte[] value) => Concat(Bytes('S'), UbjsonKey(value));

        private static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);

        private static byte[] Bytes(char marker) => new[] { (byte)marker };

        private static byte[] Concat(params byte[][] parts) => parts.SelectMany(part => part).ToArray();
    }
}
