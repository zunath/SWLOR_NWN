using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.NativeSinks;

namespace SWLOR.Game.Server.Native
{
    /// <summary>Installs the hook that lets <see cref="SavedPlayerInventorySink"/> keep saved item stacks intact while a player file is being loaded.</summary>
    internal static unsafe class SavedPlayerInventory
    {
        private static readonly byte[] IsPCLabel = System.Text.Encoding.ASCII.GetBytes("IsPC\0");
        private static FunctionHook* _compareHook;
        private static ISavedPlayerInventorySink _sink;
        [ThreadStatic] private static int _loadDepth;

        /// <summary>How many player files this thread is loading right now.</summary>
        internal static int LoadDepth => _loadDepth;

        [BootPhase(BootReload.NativeHook)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHook()
        {
            if (_compareHook != null)
                return;
            _sink ??= new SavedPlayerInventorySink();
            delegate* unmanaged<void*, void*, int> compare = &Compare;
            _compareHook = NWNXAPI.RequestFunctionHook(NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(),
                "_ZN8CNWSItem11CompareItemEPS_"), (IntPtr)compare, HookOrder.Early);
        }

        internal readonly struct LoadScope : IDisposable
        {
            private readonly bool _playerFile;

            internal LoadScope(bool playerFile)
            {
                _playerFile = playerFile;
                if (playerFile) _loadDepth++;
            }

            public void Dispose()
            {
                if (_playerFile) _loadDepth--;
            }
        }

        internal static LoadScope BeginLoad(CResGFF file, CResStruct root)
        {
            var found = 0;
            fixed (byte* label = IsPCLabel)
                return new LoadScope(file.ReadFieldBYTE(root, label, &found, 0) == 1 && found != 0);
        }

        [UnmanagedCallersOnly]
        private static int Compare(void* item, void* other)
            => _sink.CompareItem(item, other, (nint)_compareHook->m_trampoline);
    }
}
