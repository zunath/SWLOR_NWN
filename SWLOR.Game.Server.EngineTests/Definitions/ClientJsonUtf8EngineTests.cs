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
    /// Hands the engine NUI messages whose JSON text is not valid UTF-8, as a client can send them. Without
    /// <see cref="ClientJsonUtf8Guard"/> the engine throws while logging the message and the server aborts.
    /// </summary>
    public static class ClientJsonUtf8EngineTests
    {
        private const byte NuiEventMinor = 0x03;
        private const byte NuiBindUpdateMinor = 0x04;
        private const int UnknownWindowToken = 987654;

        // {"text": "Sl\xE1ine", "k\xE9y": ["Caf\xE9f", "é", 5], "nested": {"n\x92": "\xC0\xAF"}}
        // One invalid byte in a string value, an object key, an array element and a nested key, plus the two
        // bytes of an overlong sequence. The valid "é" (C3 A9) must survive.
        private const int InvalidBytesInPayload = 6;

        [EngineTest("A NUI event whose payload text is not valid UTF-8 is repaired instead of aborting the server", Category = "ClientJsonUtf8", TimeoutSeconds = 60f)]
        public static async Task NuiEventPayloadIsRepaired(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var before = ClientJsonUtf8Guard.ReplacedByteCount;

            await ctx.ExecuteInCreatureContextAsync(fixture.Creature, () =>
                SendNuiMessage(fixture.Creature, NuiEventMinor, BuildEventMessage(BuildInvalidPayload())));

            ctx.AssertEqual(before + InvalidBytesInPayload, ClientJsonUtf8Guard.ReplacedByteCount,
                "invalid bytes replaced in the event payload");
        }

        [EngineTest("A NUI bind update whose text is not valid UTF-8 is repaired instead of aborting the server", Category = "ClientJsonUtf8", TimeoutSeconds = 60f)]
        public static async Task NuiBindUpdateIsRepaired(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var before = ClientJsonUtf8Guard.ReplacedByteCount;

            await ctx.ExecuteInCreatureContextAsync(fixture.Creature, () =>
                SendNuiMessage(fixture.Creature, NuiBindUpdateMinor, BuildBindUpdateMessage(BuildInvalidPayload())));

            ctx.AssertEqual(before + InvalidBytesInPayload, ClientJsonUtf8Guard.ReplacedByteCount,
                "invalid bytes replaced in the bind value");
        }

        [EngineTest("A NUI event with valid UTF-8 text is passed through unchanged", Category = "ClientJsonUtf8", TimeoutSeconds = 60f)]
        public static async Task ValidNuiEventIsUntouched(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var before = ClientJsonUtf8Guard.ReplacedByteCount;
            var payload = Concat(
                Bytes('{'),
                UbjsonKey(Encoding.UTF8.GetBytes("text")),
                UbjsonString(Encoding.UTF8.GetBytes("Sláine - café ✓")),
                Bytes('}'));

            await ctx.ExecuteInCreatureContextAsync(fixture.Creature, () =>
                SendNuiMessage(fixture.Creature, NuiEventMinor, BuildEventMessage(payload)));

            ctx.AssertEqual(before, ClientJsonUtf8Guard.ReplacedByteCount, "bytes replaced in valid text");
        }

        private static byte[] BuildInvalidPayload()
        {
            return Concat(
                Bytes('{'),
                UbjsonKey(Encoding.ASCII.GetBytes("text")),
                UbjsonString(new byte[] { (byte)'S', (byte)'l', 0xE1, (byte)'i', (byte)'n', (byte)'e' }),
                UbjsonKey(new byte[] { (byte)'k', 0xE9, (byte)'y' }),
                Bytes('['),
                UbjsonString(new byte[] { (byte)'C', (byte)'a', (byte)'f', 0xE9, (byte)'f' }),
                UbjsonString(new byte[] { 0xC3, 0xA9 }),
                new byte[] { (byte)'i', 5 },
                Bytes(']'),
                UbjsonKey(Encoding.ASCII.GetBytes("nested")),
                Bytes('{'),
                UbjsonKey(new byte[] { (byte)'n', 0x92 }),
                UbjsonString(new byte[] { 0xC0, 0xAF }),
                Bytes('}'),
                Bytes('}'));
        }

        // Event message: entry count, then window token, event name, element ID, array index and JSON payload.
        private static byte[] BuildEventMessage(byte[] payload)
        {
            return Concat(
                Int(1),
                Int(UnknownWindowToken),
                Text("click"),
                Text("client_json_utf8_probe"),
                Int(-1),
                Json(payload));
        }

        // Bind update message: entry count, then window token, bind name, JSON value and array index.
        private static byte[] BuildBindUpdateMessage(byte[] payload)
        {
            return Concat(
                Int(1),
                Int(UnknownWindowToken),
                Text("client_json_utf8_probe"),
                Json(payload),
                Int(-1));
        }

        /// <summary>
        /// Hands the engine a NUI message as if the player's client sent it.
        /// </summary>
        private static unsafe void SendNuiMessage(uint sender, byte minor, byte[] section)
        {
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var player = server.GetClientObjectByObjectId(sender);
            var message = server.GetNWSMessage();
            // The read buffer opens with its byte-section length plus 3, then the section, then one bit-fragment byte.
            var size = 4 + section.Length + 1;
            var buffer = (byte*)NativeMemory.AllocZeroed((nuint)size);
            try
            {
                *(uint*)buffer = (uint)(4 + section.Length + 3);
                Marshal.Copy(section, 0, (IntPtr)(buffer + 4), section.Length);
                message.SetReadMessage(buffer, (uint)size, player.m_nPlayerID, 1);
                message.HandlePlayerToServerNuiEvent(player, minor);
            }
            finally
            {
                message.SetReadMessage(null, 0, player.m_nPlayerID, 1);
                NativeMemory.Free(buffer);
            }
        }

        private static byte[] Int(int value) => BitConverter.GetBytes(value);

        private static byte[] Text(string value)
        {
            var bytes = Encoding.ASCII.GetBytes(value);
            return Concat(Int(bytes.Length), bytes);
        }

        private static byte[] Json(byte[] ubjson) => Concat(Int(ubjson.Length), ubjson);

        // UBJSON, the binary form the client uses for NUI JSON. Lengths here stay under 128 bytes.
        private static byte[] UbjsonKey(byte[] key) => Concat(new byte[] { (byte)'i', (byte)key.Length }, key);

        private static byte[] UbjsonString(byte[] value) => Concat(Bytes('S'), UbjsonKey(value));

        private static byte[] Bytes(char marker) => new[] { (byte)marker };

        private static byte[] Concat(params byte[][] parts) => parts.SelectMany(part => part).ToArray();
    }
}
