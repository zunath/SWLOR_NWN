using System;
using System.Runtime.InteropServices;

namespace SWLOR.Game.Server.Core
{
    public static partial class NWNCore
    {
        internal static global::NWN.Core.ICoreFunctionHandler FunctionHandler;

        internal static NativeHandles NativeFunctions;
        private static NativeEventHandles _eventHandles;

        public static int Init(IntPtr nativeHandlesPtr, int nativeHandlesLength, global::NWN.Core.ICoreFunctionHandler functionHandler, ICoreEventHandler eventHandler)
        {
            int result = Init(nativeHandlesPtr, nativeHandlesLength, functionHandler);
            if (result == 0)
            {
                RegisterEventHandles(eventHandler);
            }

            return result;
        }

        public static int Init(IntPtr nativeHandlesPtr, int nativeHandlesLength, global::NWN.Core.ICoreFunctionHandler functionHandler)
        {
            FunctionHandler = functionHandler;

            if (nativeHandlesPtr == IntPtr.Zero)
            {
                Console.WriteLine("Received NULL bootstrap structure");
                return 1;
            }

            int expectedLength = Marshal.SizeOf(typeof(NativeHandles));
            if (nativeHandlesLength < expectedLength)
            {
                Console.WriteLine($"Received bootstrap structure too small - actual={nativeHandlesLength}, expected={expectedLength}");
                return 1;
            }

            if (nativeHandlesLength > expectedLength)
            {
                Console.WriteLine($"WARNING: Received bootstrap structure bigger than expected - actual={nativeHandlesLength}, expected={expectedLength}");
                Console.WriteLine("         This usually means that native code version is ahead of the managed code");
            }

            NativeFunctions = Marshal.PtrToStructure<NativeHandles>(nativeHandlesPtr);
            return 0;
        }

        public static int Init(IntPtr nativeHandlesPtr, int nativeHandlesLength, out CoreGameManager coreGameManager)
        {
            coreGameManager = new CoreGameManager();
            return Init(nativeHandlesPtr, nativeHandlesLength, coreGameManager, coreGameManager);
        }

        private static void RegisterEventHandles(ICoreEventHandler eventHandler)
        {
            _eventHandles.MainLoop = eventHandler.OnMainLoop;
            _eventHandles.RunScript = eventHandler.OnRunScript;
            _eventHandles.Closure = eventHandler.OnClosure;
            _eventHandles.Signal = eventHandler.OnSignal;

            int size = Marshal.SizeOf(typeof(NativeEventHandles));
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(_eventHandles, ptr, false);
            NativeFunctions.RegisterHandlers(ptr, (uint)size);
            Marshal.FreeHGlobal(ptr);
        }
    }
}
