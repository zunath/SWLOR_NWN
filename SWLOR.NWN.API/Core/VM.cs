using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SWLOR.NWN.API.Core
{
    public static partial class VM
    {
        public static readonly Encoding Cp1252Encoding;

        static VM()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Cp1252Encoding = Encoding.GetEncoding("windows-1252");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void StackPush(int value) => NWNXPInvoke.StackPushInteger(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void StackPush(float value) => NWNXPInvoke.StackPushFloat(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void StackPush(string value)
        {
            IntPtr charPtr = GetNullTerminatedString(value);
            NWNXPInvoke.StackPushRawString(charPtr);
            Marshal.FreeHGlobal(charPtr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void StackPush(uint value) => NWNXPInvoke.StackPushObject(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void StackPush(Vector3 vector) => NWNXPInvoke.StackPushVector(vector);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void StackPush(int engineType, IntPtr refValue) => NWNXPInvoke.StackPushGameDefinedStructure(engineType, refValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void Call(int functionId) => NWNXPInvoke.CallBuiltIn(functionId);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static int StackPopInt() => NWNXPInvoke.StackPopInteger();

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static float StackPopFloat() => NWNXPInvoke.StackPopFloat();

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static string? StackPopString() => ReadNullTerminatedString(NWNXPInvoke.StackPopRawString());

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static uint StackPopObject() => NWNXPInvoke.StackPopObject();

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static Vector3 StackPopVector() => NWNXPInvoke.StackPopVector();

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static IntPtr StackPopStruct(int engineType) => NWNXPInvoke.StackPopGameDefinedStructure(engineType);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void FreeGameDefinedStructure(int type, IntPtr str) => NWNXPInvoke.FreeGameDefinedStructure(type, str);

        // Legacy methods - these are no longer available in the new NWNX DotNET API
        // GetFunctionPointer and GetNWNXExportedGlobals are not available in the new bootstrap approach

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static int ClosureAssignCommand(uint oid, ulong eventId) => NWNXPInvoke.ClosureAssignCommand(oid, eventId);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static int ClosureDelayCommand(uint oid, float duration, ulong eventId) => NWNXPInvoke.ClosureDelayCommand(oid, duration, eventId);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static int ClosureActionDoCommand(uint oid, ulong eventId) => NWNXPInvoke.ClosureActionDoCommand(oid, eventId);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static IntPtr RequestHook(IntPtr address, IntPtr managedFuncPtr, int priority) => NWNXPInvoke.RequestHook(address, managedFuncPtr, priority);

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static void ReturnHook(IntPtr funcPtr) => NWNXPInvoke.ReturnHook(funcPtr);

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
