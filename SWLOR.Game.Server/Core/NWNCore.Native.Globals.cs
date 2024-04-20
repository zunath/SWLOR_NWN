using System;
using System.Runtime.InteropServices;
using System.Security;

namespace SWLOR.Game.Server.Core
{
    public static partial class NWNCore
    {
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate NWNXExportedGlobals GetNWNXExportedGlobalsDelegate();

        [StructLayout(LayoutKind.Sequential)]
        public struct NWNXExportedGlobals
        {
            public IntPtr PSBuildNumber;
            public IntPtr PSBuildRevision;
            public IntPtr PExoBase;
            public IntPtr PExoResMan;
            public IntPtr PVirtualMachine;
            public IntPtr PScriptCompiler;
            public IntPtr PAppManager;
            public IntPtr PTlkTable;
            public IntPtr PRules;
            public IntPtr PExoTaskManager;
            public IntPtr PBEnableCombatDebugging;
            public IntPtr PBEnableSavingThrowDebugging;
            public IntPtr PBEnableMovementSpeedDebugging;
            public IntPtr PBEnableHitDieDebugging;
            public IntPtr PBExitProgram;
        }
    }
}