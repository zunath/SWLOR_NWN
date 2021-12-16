using System;
using System.Runtime.InteropServices;
using NWN.Native.API;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Native
{
    public static unsafe class ResolveAttackRoll
    {
        internal delegate void ResolveAttackRollHook(void* thisPtr, void* pTarget);

        // ReSharper disable once NotAccessField.Local
        private static ResolveAttackRollHook _callOriginal;

        [NWNEventHandler("mod_load")]
        public static void RegisterHook()
        {
            delegate* unmanaged<void*, void*, void> pHook = &OnResolveAttackRoll;

            var hookPtr = Internal.NativeFunctions.RequestHook(new IntPtr(FunctionsLinux._ZN12CNWSCreature17ResolveAttackRollEP10CNWSObject), (IntPtr)pHook, -1000000);
            _callOriginal = Marshal.GetDelegateForFunctionPointer<ResolveAttackRollHook>(hookPtr);
        }

        [UnmanagedCallersOnly]
        private static void OnResolveAttackRoll(void* thisPtr, void* pTarget)
        {
            Console.WriteLine("Running OnResolveAttackRoll");
        }
    }
}
