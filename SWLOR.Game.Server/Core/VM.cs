using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SWLOR.Game.Server.Core
{
    public static partial class VM
    {
        static VM()
        {
            if (NWNCore.FunctionHandler == null)
            {
                throw new InvalidOperationException("Attempted to call a VM function before NWN.Core was initialised. Initialise NWN.Core first using NWNCore.Init()");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void StackPush(int value) => NWNCore.NativeFunctions.StackPushInteger(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void StackPush(float value) => NWNCore.NativeFunctions.StackPushFloat(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void StackPush(string value) => NWNCore.NativeFunctions.StackPushString(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void StackPush(uint value) => NWNCore.NativeFunctions.StackPushObject(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void StackPush(Vector3 vector) => NWNCore.NativeFunctions.StackPushVector(vector);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void StackPush(int engineType, IntPtr refValue) => NWNCore.NativeFunctions.StackPushGameDefinedStructure(engineType, refValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Call(int functionId) => NWNCore.NativeFunctions.CallBuiltIn(functionId);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static int StackPopInt() => NWNCore.NativeFunctions.StackPopInteger();

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static float StackPopFloat() => NWNCore.NativeFunctions.StackPopFloat();

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static string StackPopString() => NWNCore.NativeFunctions.StackPopString();

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static uint StackPopObject() => NWNCore.NativeFunctions.StackPopObject();

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static Vector3 StackPopVector() => NWNCore.NativeFunctions.StackPopVector();

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static IntPtr StackPopStruct(int engineType) => NWNCore.NativeFunctions.StackPopGameDefinedStructure(engineType);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void FreeGameDefinedStructure(int type, IntPtr str) => NWNCore.NativeFunctions.FreeGameDefinedStructure(type, str);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static IntPtr GetFunctionPointer(string name) => NWNCore.NativeFunctions.GetFunctionPointer(name);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static NWNCore.NWNXExportedGlobals GetNWNXExportedGlobals() => NWNCore.NativeFunctions.GetNWNXExportedGlobals();

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static int ClosureAssignCommand(uint oid, ulong eventId) => NWNCore.NativeFunctions.ClosureAssignCommand(oid, eventId);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static int ClosureDelayCommand(uint oid, float duration, ulong eventId) => NWNCore.NativeFunctions.ClosureDelayCommand(oid, duration, eventId);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static int ClosureActionDoCommand(uint oid, ulong eventId) => NWNCore.NativeFunctions.ClosureActionDoCommand(oid, eventId);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static IntPtr RequestHook(IntPtr address, IntPtr managedFuncPtr, int priority) => NWNCore.NativeFunctions.RequestHook(address, managedFuncPtr, priority);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void ReturnHook(IntPtr funcPtr) => NWNCore.NativeFunctions.ReturnHook(funcPtr);
    }
}
