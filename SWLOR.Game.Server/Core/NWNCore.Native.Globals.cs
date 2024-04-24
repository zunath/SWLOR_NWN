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
            public IntPtr PPExoBase;
            public IntPtr PPExoResMan;
            public IntPtr PPVirtualMachine;
            public IntPtr PPScriptCompiler;
            public IntPtr PPAppManager;
            public IntPtr PPTlkTable;
            public IntPtr PPRules;
            public IntPtr PPExoTaskManager;
            public IntPtr PBEnableCombatDebugging;
            public IntPtr PBEnableSavingThrowDebugging;
            public IntPtr PBEnableMovementSpeedDebugging;
            public IntPtr PBEnableHitDieDebugging;
            public IntPtr PBExitProgram;
        }
    }
}