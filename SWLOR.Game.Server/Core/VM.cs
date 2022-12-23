using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SWLOR.Game.Server.Core
{
    public static partial class VM
    {
        public static readonly Encoding Cp1252Encoding;

        static VM()
        {
            if (NWNCore.FunctionHandler == null)
            {
                throw new InvalidOperationException("Attempted to call a VM function before NWN.Core was initialised. Initialise NWN.Core first using NWNCore.Init()");
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Cp1252Encoding = Encoding.GetEncoding("windows-1252");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void StackPush(int value) => NWNCore.NativeFunctions.StackPushInteger(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void StackPush(float value) => NWNCore.NativeFunctions.StackPushFloat(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void StackPush(string value)
        {
            IntPtr charPtr = GetNullTerminatedString(value);
            NWNCore.NativeFunctions.StackPushRawString(charPtr);
            Marshal.FreeHGlobal(charPtr);
        }

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
        public static string? StackPopString() => ReadNullTerminatedString(NWNCore.NativeFunctions.StackPopRawString());

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

        public static IntPtr GetNullTerminatedString(string? value)
        {
            if (value == null)
            {
                return IntPtr.Zero;
            }

            byte[] bytes = Cp1252Encoding.GetBytes(value);
            IntPtr buffer = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, buffer, bytes.Length);

            // Write null terminator
            Marshal.WriteByte(buffer + bytes.Length, 0);
            return buffer;
        }

        private static unsafe string? ReadNullTerminatedString(IntPtr cString)
        {
            if (cString == IntPtr.Zero)
            {
                return null;
            }

            byte* charPointer = (byte*)cString;
            return Cp1252Encoding.GetString(charPointer, GetStringLength(charPointer));
        }

        private static unsafe int GetStringLength(byte* cString)
        {
            byte* walk = cString;
            while (*walk != 0)
            {
                walk++;
            }

            return (int)(walk - cString);
        }
    }
}
