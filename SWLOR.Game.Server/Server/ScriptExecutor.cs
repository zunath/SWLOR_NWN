using System;
using NWN.Native.API;
using SWLOR.NWN.API.NWNX;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Extension;
using SWLOR.Shared.Core.Log.LogGroup;

namespace SWLOR.Game.Server.Server
{
    public class ScriptExecutor : IScriptExecutor
    {
        private CVirtualMachine _virtualMachine;

        private const int ScriptHandled = 0;
        private const int ScriptNotHandled = -1;

        private readonly ILogger _logger;
        private readonly IClosureManager _closureManager;
        private readonly IScriptRegistry _scriptRegistry;

        public ScriptExecutor(
            ILogger logger,
            IClosureManager closureManager, 
            IScriptRegistry scriptRegistry)
        {
            _logger = logger;
            _closureManager = closureManager;
            _scriptRegistry = scriptRegistry;
            Console.WriteLine($"{nameof(ScriptExecutor)} initialized.");
        }

        public void Initialize()
        {
            _virtualMachine = NWNXLib.VirtualMachine();
            Console.WriteLine($"Virtual machine initialized");
        }

        public int ProcessRunScript(string scriptName, uint objectSelf)
        {
            var oldObjectSelf = _closureManager.ObjectSelf;
            _closureManager.ObjectSelf = objectSelf;

            try
            {
                var retVal = RunScripts(scriptName);
                return retVal == -1 ? ScriptNotHandled : retVal;
            }
            finally
            {
                _closureManager.ObjectSelf = oldObjectSelf;
            }
        }

        private int RunScripts(string script)
        {
            var hasConditionalScripts = _scriptRegistry.HasConditionalScript(script);
            var hasActionScripts = _scriptRegistry.HasScript(script);

            if (!hasConditionalScripts && !hasActionScripts)
            {
                return ScriptNotHandled;
            }

            // Execute conditional scripts first if they exist
            if (hasConditionalScripts)
            {
                var conditionalResult = ExecuteConditionalScripts(script);
                // If conditional scripts return false, don't execute action scripts
                if (conditionalResult == 0)
                {
                    return 0;
                }
            }

            // Execute action scripts if they exist
            if (hasActionScripts)
            {
                ExecuteActionScripts(script);
            }

            return ScriptHandled;
        }

        private int ExecuteConditionalScripts(string script)
        {
            var result = true;

            foreach (var (action, name) in _scriptRegistry.GetConditionalScripts(script))
            {
                ProfilerPlugin.PushPerfScope(OBJECT_SELF, name);
                try
                {
                    var actionResult = action.Invoke();
                    if (result)
                        result = actionResult;
                }
                catch (Exception ex)
                {
                    // Error is already logged by ScriptMethodInvoker, no need to log again
                    result = false; // If any conditional script fails, return false
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
            foreach (var (action, name) in _scriptRegistry.GetActionScripts(script))
            {
                try
                {
                    ProfilerPlugin.PushPerfScope(OBJECT_SELF, name);
                    action();
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
            var oldObjectSelf = _closureManager.ObjectSelf;
            var spBefore = PushScriptContext(objectId, scriptEventId);
            try
            {
                action();
            }
            finally
            {
                var spAfter = PopScriptContext();
                _closureManager.ObjectSelf = oldObjectSelf;
                if (spAfter != spBefore)
                {
                    _logger.Write<ErrorLogGroup>($"VM stack is invalid ({spBefore} != {spAfter}) after script context invocation: {action.Method.GetFullName()}", true);
                }
            }
        }
        public T ExecuteInScriptContext<T>(Func<T> action, uint objectId = OBJECT_INVALID, int scriptEventId = 0)
        {
            var oldObjectSelf = _closureManager.ObjectSelf;
            var spBefore = PushScriptContext(objectId, scriptEventId);

            try
            {
                return action();
            }
            finally
            {
                var spAfter = PopScriptContext();
                _closureManager.ObjectSelf = oldObjectSelf;
                if (spAfter != spBefore)
                {
                    _logger.Write<ErrorLogGroup>($"VM stack is invalid ({spBefore} != {spAfter}) after script context invocation: {action.Method.GetFullName()}", true);
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

            // Update the ClosureManager's ObjectSelf to match the script context
            _closureManager.ObjectSelf = oid;

            return _virtualMachine.m_cRunTimeStack.GetStackPointer();
        }
    }
}
