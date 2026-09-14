using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Native
{
    /// <summary>Keeps saved item stacks intact while a player file is being loaded.</summary>
    internal static unsafe class SavedPlayerInventory
    {
        private static readonly byte[] IsPCLabel = System.Text.Encoding.ASCII.GetBytes("IsPC\0");
        private static FunctionHook* _compareHook;
        [ThreadStatic] private static int _loadDepth;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHook()
        {
            if (_compareHook != null)
                return;
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
        {
            // Native loading can combine differently priced blueprints before migrations
            // see them. Keep each saved stack; ordinary gameplay merging stays unchanged.
            if (_loadDepth > 0)
                return 0;
            var original = (delegate* unmanaged<void*, void*, int>)_compareHook->m_trampoline;
            return original(item, other);
        }
    }
}
