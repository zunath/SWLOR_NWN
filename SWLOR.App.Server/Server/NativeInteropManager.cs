using System;
using System.Runtime.InteropServices;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Events.Events.Infrastructure;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.App.Server.Server
{
    public unsafe class NativeInteropManager : INativeInteropManager
    {
        private readonly IMainLoopProcessor _mainLoopProcessor;
        private readonly IScriptExecutor _scriptExecutor;
        private readonly IClosureManager _closureManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly IEventRegistrationService _eventRegistration;
        private readonly IServiceInitializer _serviceInitializer;

        public NativeInteropManager(
            IMainLoopProcessor mainLoopProcessor, 
            IScriptExecutor scriptExecutor, 
            IClosureManager closureManager, 
            IEventAggregator eventAggregator,
            IEventRegistrationService eventRegistration,
            IServiceInitializer serviceInitializer)
        {
            _mainLoopProcessor = mainLoopProcessor;
            _scriptExecutor = scriptExecutor;
            _closureManager = closureManager;
            _eventAggregator = eventAggregator;
            _eventRegistration = eventRegistration;
            _serviceInitializer = serviceInitializer;
        }

        public void RegisterHandlers()
        {
            NWNXAPI.RegisterMainLoopHandler(&OnMainLoop);
            NWNXAPI.RegisterRunScriptHandler(&OnRunScript);
            NWNXAPI.RegisterClosureHandler(&OnClosure);
            NWNXAPI.RegisterSignalHandler(&OnSignal);
        }

        [UnmanagedCallersOnly]
        private static void OnMainLoop(ulong frame)
        {
            try
            {
                var instance = ServiceContainer.GetService<INativeInteropManager>() as NativeInteropManager;
                instance?._mainLoopProcessor.ProcessMainLoop(frame);
            }
            catch (Exception e)
            {
                var logger = ServiceContainer.GetService<ILogger>();
                logger?.Write<ErrorLogGroup>($"MainLoop exception: {e}", true);
            }
        }

        [UnmanagedCallersOnly]
        private static int OnRunScript(IntPtr scriptPtr, uint oidSelf)
        {
            try
            {
                var instance = ServiceContainer.GetService<INativeInteropManager>() as NativeInteropManager;
                var scriptName = scriptPtr.ReadNullTerminatedString();
                return instance?._scriptExecutor.ProcessRunScript(scriptName, oidSelf) ?? -1;
            }
            catch (Exception e)
            {
                var logger = ServiceContainer.GetService<ILogger>();
                logger?.Write<ErrorLogGroup>($"RunScript exception: {e}", true);
                return -1;
            }
        }

        [UnmanagedCallersOnly]
        private static void OnSignal(IntPtr signalPtr)
        {
            try
            {
                var instance = ServiceContainer.GetService<INativeInteropManager>() as NativeInteropManager;
                var signal = signalPtr.ReadNullTerminatedString();
                instance?.ProcessSignal(signal);
            }
            catch (Exception e)
            {
                var logger = ServiceContainer.GetService<ILogger>();
                logger?.Write<ErrorLogGroup>($"Signal processing exception: {e}", true);
            }
        }

        [UnmanagedCallersOnly]
        private static void OnClosure(ulong eid, uint oidSelf)
        {
            try
            {
                var instance = ServiceContainer.GetService<INativeInteropManager>() as NativeInteropManager;
                instance?._closureManager.OnClosure(eid, oidSelf);
            }
            catch (Exception e)
            {
                var logger = ServiceContainer.GetService<ILogger>();
                logger?.Write<ErrorLogGroup>($"Closure exception: {e}", true);
            }
        }


        private void ProcessSignal(string signal)
        {
            switch (signal)
            {
                case "ON_MODULE_LOAD_FINISH":
                    _scriptExecutor.Initialize();
                    InitializeServices();
                    _eventRegistration.RegisterEvents();
                    RunPreModuleLoadEvents();
                    break;
            }
        }

        /// <summary>
        /// Initializes all services that require post-construction setup
        /// </summary>
        private void InitializeServices()
        {
            try
            {
                _serviceInitializer.InitializeAllServices();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during service initialization: {ex.Message}");
                throw;
            }
        }

        private void RunPreModuleLoadEvents()
        {
            _eventAggregator.Publish(new OnServerLoaded(), GetModule());
            _eventAggregator.Publish(new OnModuleCacheBefore(), GetModule());
            _eventAggregator.Publish(new OnModuleCacheAfter(), GetModule());
        }
    }
}
