using System;
using NWN.Native.API;
using SWLOR.NWN.API.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Extensions;
using SWLOR.Shared.Core.Extension;
using SWLOR.Shared.Core.Log.LogGroup;

namespace SWLOR.App.Server.Server
{
    public class ScriptExecutor : IScriptExecutor
    {
        private CVirtualMachine _virtualMachine;

        private const int ScriptHandled = 0;
        private const int ScriptNotHandled = -1;

        private readonly ILogger _logger;
        private readonly IClosureManager _closureManager;
        private readonly IProfilerPluginService _profilerPlugin;
        private readonly IEventAggregator _eventAggregator;
        private readonly ScriptToEventMapper _scriptToEventMapper;

        public ScriptExecutor(
            ILogger logger,
            IClosureManager closureManager, 
            IProfilerPluginService profilerPlugin,
            IEventAggregator eventAggregator,
            ScriptToEventMapper scriptToEventMapper)
        {
            _logger = logger;
            _closureManager = closureManager;
            _profilerPlugin = profilerPlugin;
            _eventAggregator = eventAggregator;
            _scriptToEventMapper = scriptToEventMapper;
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
            var hasEventType = _scriptToEventMapper.HasEventType(script);

            if (!hasEventType)
            {
                return ScriptNotHandled;
            }

            // Publish event to IEventAggregator - all event handling now goes through the event aggregator
            PublishEventToAggregator(script);

            return ScriptHandled;
        }

        private void PublishEventToAggregator(string script)
        {
            try
            {
                var eventType = _scriptToEventMapper.GetEventType(script);
                if (eventType != null)
                {
                    _profilerPlugin.PushPerfScope(OBJECT_SELF, $"Event:{eventType.Name}");
                    try
                    {
                        // Create an instance of the event
                        var eventInstance = Activator.CreateInstance(eventType) as IEvent;
                        if (eventInstance != null)
                        {
                            // Get the target object (OBJECT_SELF)
                            var target = _closureManager.ObjectSelf;
                            
                            // Publish to IEventAggregator using reflection since we don't know the type at compile time
                            var publishMethod = typeof(IEventAggregator)
                                .GetMethod(nameof(IEventAggregator.Publish))
                                .MakeGenericMethod(eventType);
                            
                            publishMethod.Invoke(_eventAggregator, new object[] { eventInstance, target });
                        }
                    }
                    finally
                    {
                        _profilerPlugin.PopPerfScope();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Write<ErrorLogGroup>($"Error publishing event for script '{script}': {ex.ToMessageAndCompleteStacktrace()}");
            }
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
