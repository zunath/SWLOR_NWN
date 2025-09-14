using NWN.Native.API;
using SWLOR.Game.Server.Core.Extensions;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWNX;
using System;

namespace SWLOR.Game.Server.Core
{
    public class ScriptExecutor
    {
        private CVirtualMachine _virtualMachine;

        private const int ScriptHandled = 0;
        private const int ScriptNotHandled = -1;

        internal void Initialize()
        {
            _virtualMachine = NWNXLib.VirtualMachine();
            Console.WriteLine($"{nameof(ScriptExecutor)} initialized.");
        }

        public int ProcessRunScript(string scriptName, uint objectSelf)
        {
            var oldObjectSelf = ServerManager.Bootstrapper.ClosureManager.ObjectSelf;
            ServerManager.Bootstrapper.ClosureManager.ObjectSelf = objectSelf;

            try
            {
                var retVal = RunScripts(scriptName);
                return retVal == -1 ? ScriptNotHandled : retVal;
            }
            finally
            {
                ServerManager.Bootstrapper.ClosureManager.ObjectSelf = oldObjectSelf;
            }
        }

        private int RunScripts(string script)
        {
            if (ServerManager.Scripts.HasConditionalScript(script))
            {
                return ExecuteConditionalScripts(script);
            }
            else if (ServerManager.Scripts.HasScript(script))
            {
                return ExecuteActionScripts(script);
            }

            return ScriptNotHandled;
        }

        private int ExecuteConditionalScripts(string script)
        {
            var result = true;

            foreach (var (action, name) in ServerManager.Scripts.GetConditionalScripts(script))
            {
                ProfilerPlugin.PushPerfScope(OBJECT_SELF, name);
                try
                {
                    var actionResult = action.Invoke();
                    if (result)
                        result = actionResult;
                }
                finally
                {
                    ProfilerPlugin.PopPerfScope();
                }
            }

            return result ? 1 : 0;
        }

        private int ExecuteActionScripts(string script)
        {
            foreach (var (action, name) in ServerManager.Scripts.GetActionScripts(script))
            {
                try
                {
                    ProfilerPlugin.PushPerfScope(OBJECT_SELF, name);
                    action();
                }
                catch (Exception ex)
                {
                    Log.Write(LogGroup.Error, $"C# Script '{script}' threw an exception. Details: {Environment.NewLine}{Environment.NewLine}{ex.ToMessageAndCompleteStacktrace()}", true);
                }
                finally
                {
                    ProfilerPlugin.PopPerfScope();
                }
            }

            return ScriptHandled;
        }

        public void ExecuteInScriptContext(Action action, uint objectId = OBJECT_INVALID, int scriptEventId = 0)
        {
            var spBefore = PushScriptContext(objectId, scriptEventId);
            try
            {
                action();
            }
            finally
            {
                var spAfter = PopScriptContext();
                if (spAfter != spBefore)
                {
                    Log.Write(LogGroup.Error, $"VM stack is invalid ({spBefore} != {spAfter}) after script context invocation: {action.Method.GetFullName()}");
                }
            }
        }
        public T ExecuteInScriptContext<T>(Func<T> action, uint objectId = OBJECT_INVALID, int scriptEventId = 0)
        {
            var spBefore = PushScriptContext(objectId, scriptEventId);

            try
            {
                return action();
            }
            finally
            {
                var spAfter = PopScriptContext();
                if (spAfter != spBefore)
                {
                    Log.Write(LogGroup.Error, $"VM stack is invalid ({spBefore} != {spAfter}) after script context invocation: {action.Method.GetFullName()}");
                }
            }
        }

        private int PopScriptContext()
        {
            var cmd = CNWSVirtualMachineCommands.FromPointer(_virtualMachine.m_pCmdImplementer.Pointer);

            if (--_virtualMachine.m_nRecursionLevel != -1)
            {
                cmd.m_oidObjectRunScript = _virtualMachine.m_oidObjectRunScript[_virtualMachine.m_nRecursionLevel];
                cmd.m_bValidObjectRunScript = _virtualMachine.m_bValidObjectRunScript[_virtualMachine.m_nRecursionLevel];
            }

            return _virtualMachine.m_cRunTimeStack.GetStackPointer();
        }

        private int PushScriptContext(uint oid, int scriptEventId)
        {
            if(_virtualMachine == null)
                Console.WriteLine($"VM is null!!!");

            var cmd = CNWSVirtualMachineCommands.FromPointer(_virtualMachine.m_pCmdImplementer.Pointer);
            var valid = NWNXUtils.GetGameObject(oid) != IntPtr.Zero;

            if (_virtualMachine.m_nRecursionLevel++ == -1)
            {
                _virtualMachine.m_cRunTimeStack.InitializeStack();
                _virtualMachine.m_cRunTimeStack.m_pVMachine = _virtualMachine;
                _virtualMachine.m_nInstructPtrLevel = 0;
                _virtualMachine.m_nInstructionsExecuted = 0;
            }

            _virtualMachine.m_oidObjectRunScript[_virtualMachine.m_nRecursionLevel] = oid;
            _virtualMachine.m_bValidObjectRunScript[_virtualMachine.m_nRecursionLevel] = valid ? 1 : 0;

            var script = _virtualMachine.m_pVirtualMachineScript[_virtualMachine.m_nRecursionLevel];
            script.m_nScriptEventID = scriptEventId;

            _virtualMachine.m_pVirtualMachineScript[_virtualMachine.m_nRecursionLevel] = script;
            cmd.m_oidObjectRunScript = _virtualMachine.m_oidObjectRunScript[_virtualMachine.m_nRecursionLevel];
            cmd.m_bValidObjectRunScript = _virtualMachine.m_bValidObjectRunScript[_virtualMachine.m_nRecursionLevel];

            return _virtualMachine.m_cRunTimeStack.GetStackPointer();
        }
    }
}