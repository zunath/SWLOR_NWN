using NWN.Native.API;
using SWLOR.Game.Contracts;
using SWLOR.Game.NativeHooks;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.NativeSinks;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Enables the hook that lets <see cref="SavedPlayerInventorySink"/> keep saved item stacks intact while a player file
    /// is being loaded. The count of player files the thread is loading lives beside the trampolines
    /// (<see cref="PlayerFileLoadDepth"/>), since the engine thread outlives every game build.
    /// </summary>
    internal static unsafe class SavedPlayerInventory
    {
        private static readonly byte[] IsPCLabel = System.Text.Encoding.ASCII.GetBytes("IsPC\0");

        /// <summary>How many player files this thread is loading right now.</summary>
        internal static int LoadDepth => PlayerFileLoadDepth.Current;

        [BootPhase(BootReload.NativeHook)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHook()
        {
            NativeHookAccess.Hooks.Enable(NativeHookId.SavedPlayerInventory);
        }

        internal readonly struct LoadScope : IDisposable
        {
            private readonly bool _playerFile;

            internal LoadScope(bool playerFile)
            {
                _playerFile = playerFile;
                if (playerFile) PlayerFileLoadDepth.Enter();
            }

            public void Dispose()
            {
                if (_playerFile) PlayerFileLoadDepth.Exit();
            }
        }

        internal static LoadScope BeginLoad(CResGFF file, CResStruct root)
        {
            var found = 0;
            fixed (byte* label = IsPCLabel)
                return new LoadScope(file.ReadFieldBYTE(root, label, &found, 0) == 1 && found != 0);
        }
    }
}
