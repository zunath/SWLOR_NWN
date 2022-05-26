using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security;

namespace SWLOR.Game.Server.Core
{
    public static partial class NWNCore
    {
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr GetFunctionPointerDelegate(string name);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void RegisterHandlersDelegate(IntPtr handlers, uint size);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void CallBuiltInDelegate(int id);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void StackPushIntegerDelegate(int value);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void StackPushFloatDelegate(float value);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void StackPushStringDelegate(string value);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void StackPushStringUTF8Delegate(string value);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void StackPushObjectDelegate(uint value);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void StackPushVectorDelegate(Vector3 value);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void StackPushGameDefinedStructureDelegate(int type, IntPtr str);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int StackPopIntegerDelegate();

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float StackPopFloatDelegate();

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate string StackPopStringDelegate();

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate string StackPopStringUTF8Delegate();

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate uint StackPopObjectDelegate();

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate Vector3 StackPopVectorDelegate();

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr StackPopGameDefinedStructureDelegate(int type);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void FreeEffectDelegate(IntPtr ptr);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void FreeEventDelegate(IntPtr ptr);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void FreeLocationDelegate(IntPtr ptr);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void FreeTalentDelegate(IntPtr ptr);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void FreeItemPropertyDelegate(IntPtr ptr);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr FreeGameDefinedStructureDelegate(int type, IntPtr str);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int ClosureAssignCommandDelegate(uint oid, ulong eventId);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int ClosureDelayCommandDelegate(uint oid, float duration, ulong eventId);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int ClosureActionDoCommandDelegate(uint oid, ulong eventId);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NWNXSetFunctionDelegate(string plugin, string function);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NWNXPushIntDelegate(int n);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NWNXPushFloatDelegate(float f);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NWNXPushObjectDelegate(uint o);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NWNXPushStringDelegate(string s);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NWNXPushStringUTF8Delegate(string s);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NWNXPushEffectDelegate(IntPtr e);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NWNXPushItemPropertyDelegate(IntPtr ip);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int NWNXPopIntDelegate();

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float NWNXPopFloatDelegate();

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate uint NWNXPopObjectDelegate();

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate string NWNXPopStringDelegate();

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate string NWNXPopStringUTF8Delegate();

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr NWNXPopEffectDelegate();

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr NWNXPopItemPropertyDelegate();

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NWNXCallFunctionDelegate();

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr RequestHookDelegate(IntPtr address, IntPtr managedFuncPtr, int priority);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ReturnHookDelegate(IntPtr hook);

        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void StackPushRawStringDelegate(IntPtr charPtr);
        
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr StackPopRawStringDelegate();
        
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NWNXPushRawStringDelegate(IntPtr charPtr);
        
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr NWNXPopRawStringDelegate();

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct NativeHandles
        {
            public readonly GetFunctionPointerDelegate GetFunctionPointer;
            public readonly RegisterHandlersDelegate RegisterHandlers;
            public readonly CallBuiltInDelegate CallBuiltIn;
            public readonly StackPushIntegerDelegate StackPushInteger;
            public readonly StackPushFloatDelegate StackPushFloat;
            public readonly StackPushStringDelegate StackPushString;
            public readonly StackPushStringUTF8Delegate StackPushStringUTF8;
            public readonly StackPushObjectDelegate StackPushObject;
            public readonly StackPushVectorDelegate StackPushVector;
            public readonly StackPushGameDefinedStructureDelegate StackPushGameDefinedStructure;
            public readonly StackPopIntegerDelegate StackPopInteger;
            public readonly StackPopFloatDelegate StackPopFloat;
            public readonly StackPopStringDelegate StackPopString;
            public readonly StackPopStringUTF8Delegate StackPopStringUTF8;
            public readonly StackPopObjectDelegate StackPopObject;
            public readonly StackPopVectorDelegate StackPopVector;
            public readonly StackPopGameDefinedStructureDelegate StackPopGameDefinedStructure;
            public readonly FreeGameDefinedStructureDelegate FreeGameDefinedStructure;
            public readonly ClosureAssignCommandDelegate ClosureAssignCommand;
            public readonly ClosureDelayCommandDelegate ClosureDelayCommand;
            public readonly ClosureActionDoCommandDelegate ClosureActionDoCommand;
            public readonly NWNXSetFunctionDelegate nwnxSetFunction;
            public readonly NWNXPushIntDelegate nwnxPushInt;
            public readonly NWNXPushFloatDelegate nwnxPushFloat;
            public readonly NWNXPushObjectDelegate nwnxPushObject;
            public readonly NWNXPushStringDelegate nwnxPushString;
            public readonly NWNXPushStringUTF8Delegate nwnxPushStringUTF8;
            public readonly NWNXPushEffectDelegate nwnxPushEffect;
            public readonly NWNXPushItemPropertyDelegate nwnxPushItemProperty;
            public readonly NWNXPopIntDelegate nwnxPopInt;
            public readonly NWNXPopFloatDelegate nwnxPopFloat;
            public readonly NWNXPopObjectDelegate nwnxPopObject;
            public readonly NWNXPopStringDelegate nwnxPopString;
            public readonly NWNXPopStringUTF8Delegate nwnxPopStringUTF8;
            public readonly NWNXPopEffectDelegate nwnxPopEffect;
            public readonly NWNXPopItemPropertyDelegate nwnxPopItemProperty;
            public readonly NWNXCallFunctionDelegate nwnxCallFunction;
            public readonly GetNWNXExportedGlobalsDelegate GetNWNXExportedGlobals;
            public readonly RequestHookDelegate RequestHook;
            public readonly ReturnHookDelegate ReturnHook;
            public readonly StackPushRawStringDelegate StackPushRawString;
            public readonly StackPopRawStringDelegate StackPopRawString;
            public readonly NWNXPushRawStringDelegate nwnxPushRawString;
            public readonly NWNXPopRawStringDelegate nwnxPopRawString;
        }
    }
}
