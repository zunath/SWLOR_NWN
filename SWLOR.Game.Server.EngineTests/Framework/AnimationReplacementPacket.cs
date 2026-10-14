using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;

namespace SWLOR.Game.Server.EngineTests.Framework;

// Disconnected observers must negotiate the replacement payload. A default CNWSPlayer
// fails SatisfiesBuild and silently omits it (as in the older attack packet fixture).
internal sealed unsafe class AnimationReplacementPacket : IDisposable
{
    private const uint ReplacementUpdate = 0x1000000;
    private const uint Sentinel = 0xa57e913c;
    private static FunctionHook* _versionHook;
    private static void* _observer;
    private void* _memory;
    private CNWSMessage Message { get; }

    private AnimationReplacementPacket()
    {
        // The pinned engine's derived CNWSMessage is 0x70 bytes. The legacy SWIG
        // constructor allocates only its 0x68-byte base (see NeckAppearancePacket).
        var construct = (delegate* unmanaged<void*, void>)NativeLibrary.GetExport(
            NativeLibrary.GetMainProgramHandle(), "_ZN11CNWSMessageC1Ev");
        _memory = NativeMemory.AllocZeroed(0x70);
        construct(_memory);
        Message = CNWSMessage.FromPointer(_memory);
    }

    [UnmanagedCallersOnly]
    private static int SatisfiesBuild(void* observer, int major, int minor, int revision) =>
        observer == _observer ? 1 :
            ((delegate* unmanaged<void*, int, int, int, int>)_versionHook->m_trampoline)(observer, major, minor, revision);

    // Serializes several creature updates into ONE message, exercising the message's
    // fragment buffer too. No packet is sent to any connection.
    public static uint AssertRoundTrip(EngineTestContext ctx, IReadOnlyList<uint> creatures)
    {
        if (_versionHook == null)
        {
            delegate* unmanaged<void*, int, int, int, int> version = &SatisfiesBuild;
            _versionHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(),
                "_ZN10CNWSPlayer14SatisfiesBuildEiii"), (IntPtr)version, HookOrder.Early);
        }
        using var client = new CNWSPlayer(0xfffffffc);
        client.m_oidNWSObject = creatures[0];
        using var packet = new AnimationReplacementPacket();
        using var reader = new AnimationReplacementPacket();
        var expected = new List<Dictionary<string, string>>();
        _observer = (void*)client.Pointer;
        try
        {
            packet.Message.CreateWriteMessage(128, 0xffffffff, 1);
            foreach (var creature in creatures)
            {
                var obj = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(creature).AsNWSCreature();
                expected.Add(Native.WeaponAttackAnimation.ReadAnimationReplacements(obj));
                using var last = new CLastUpdateObject();
                packet.Message.WriteGameObjUpdate_UpdateObject(client, obj, last, ReplacementUpdate, 0);
                packet.Message.WriteDWORD(Sentinel, 32);
            }
            byte* bytes = null;
            uint size = 0;
            packet.Message.GetWriteMessage(&bytes, &size);
            ctx.Assert(size > 3, "Engine produced a replacement message");
            ctx.Assert(reader.Message.SetReadMessage(bytes + 3, size - 3, 0xffffffff, 1) != 0,
                "Native SetReadMessage accepted the fragment header");
            foreach (var mappings in expected)
            {
                ctx.Assert(reader.Message.ReadCHAR(8) == 0x55, "Creature update sub-message marker");
                reader.Message.ReadBYTE(8, 1);
                reader.Message.ReadOBJECTIDServer();
                ctx.AssertEqual(ReplacementUpdate, reader.Message.ReadDWORD(32), "Replacement update flag");
                ctx.AssertEqual((uint)mappings.Count, reader.Message.ReadDWORD(32), "Wire mapping count");
                var actual = new Dictionary<string, string>();
                for (var i = 0; i < mappings.Count; i++)
                {
                    using var oldName = reader.Message.ReadCResRef(16);
                    using var newName = reader.Message.ReadCResRef(16);
                    actual.Add(Marshal.PtrToStringAnsi((IntPtr)oldName.GetResRefStr()),
                        Marshal.PtrToStringAnsi((IntPtr)newName.GetResRefStr()));
                }
                foreach (var pair in mappings)
                    ctx.AssertEqual(pair.Value, actual.GetValueOrDefault(pair.Key), $"Wire mapping {pair.Key}");
                ctx.AssertEqual(Sentinel, reader.Message.ReadDWORD(32), "Following payload remains aligned");
            }
            ctx.Assert(reader.Message.MessageReadOverflow() == 0, "No native read overflow");
            return size;
        }
        finally { _observer = null; }
    }

    public void Dispose()
    {
        if (_memory == null) return;
        var destruct = (delegate* unmanaged<void*, void>)NativeLibrary.GetExport(
            NativeLibrary.GetMainProgramHandle(), "_ZN11CNWSMessageD1Ev");
        destruct(_memory);
        NativeMemory.Free(_memory);
        _memory = null;
        Message.Dispose();
    }
}
