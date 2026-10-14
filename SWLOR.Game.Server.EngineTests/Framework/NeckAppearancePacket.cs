using System;
using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;

namespace SWLOR.Game.Server.EngineTests.Framework;

// Test-only packets use a local observer and never send anything to a connection.
internal sealed unsafe class NeckAppearancePacket : IDisposable
{
    private void* _memory;
    private static FunctionHook* _versionHook;
    private static void* _observer;
    private CNWSMessage Message { get; }

    private NeckAppearancePacket()
    {
        // As in the animation packet fixture, the pinned engine needs 0x70 bytes;
        // the legacy SWIG constructor allocates only the 0x68-byte base class.
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

    public static (ushort[] Parts, byte[] Colors) Read(CNWSItem item)
    {
        if (_versionHook == null)
        {
            delegate* unmanaged<void*, int, int, int, int> version = &SatisfiesBuild;
            _versionHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(),
                "_ZN10CNWSPlayer14SatisfiesBuildEiii"), (IntPtr)version, HookOrder.Early);
        }
        using var client = new CNWSPlayer(0xfffffffd);
        using var packet = new NeckAppearancePacket();
        using var reader = new NeckAppearancePacket();
        // Select the EE 16-bit part format for this disconnected test observer only.
        _observer = (void*)client.Pointer;
        try
        {
            packet.Message.CreateWriteMessage(1024, 0xffffffff, 1);
            packet.Message.AddItemAppearanceToMessage(client, item);
            byte* bytes = null;
            uint size = 0;
            packet.Message.GetWriteMessage(&bytes, &size);
            if (size < 3) throw new InvalidOperationException("No item appearance packet");
            reader.Message.SetReadMessage(bytes + 3, size - 3, 0xffffffff, 1);
            reader.Message.ReadDWORD(32); // Base item.
            var parts = new ushort[19];
            var colors = new byte[120];
            for (var i = 0; i < parts.Length; i++) parts[i] = reader.Message.ReadWORD(16);
            for (var i = 0; i < colors.Length; i++) colors[i] = reader.Message.ReadBYTE(8, 1);
            return (parts, colors);
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
