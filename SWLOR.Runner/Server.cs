using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Commands;
using Ductus.FluentDocker.Extensions;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.OLE.Interop;
using SWLOR.Shared.Core.Extension;

namespace SWLOR.Runner
{
    internal class Server
    {
        private const string ServerContainerName = "debugserver-swlor-server-1";
        private ICompositeService _service = null!;
        private readonly IHostService _docker;
        private readonly Dictionary<string, ContainerLogger> _containerLineCounts = new();
        private volatile bool _isDebuggerRunning;

        public Server()
        {
            var hosts = new Hosts().Discover();
            _docker = hosts.FirstOrDefault(x => x.IsNative) ?? hosts.FirstOrDefault(x => x.Name == "default");
        }

        public async Task RunAsync()
        {
            Console.WriteLine($"Starting server");
            RegisterEvents();

            var debugServerPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\..\\"));
            var dockerComposePath = debugServerPath + "debugserver/docker-compose.yml";

            using (_service = new Builder()
                       .UseContainer()
                       .UseCompose()
                       .ForceRecreate()
                       .FromFile(dockerComposePath)
                       .RemoveOrphans()
                       .Build()
                       .Start())
            {
                while (_service.State != ServiceRunningState.Stopped)
                {
                    foreach (var container in _service.Containers)
                    {
                        if (!_containerLineCounts.ContainsKey(container.Name))
                            _containerLineCounts[container.Name] = new ContainerLogger();

                        using (var logs = _docker.Host.Logs(container.Id, showTimeStamps: true))
                        {
                            var lines = logs.ReadToEnd();
                            var containerLogger = _containerLineCounts[container.Name];

                            foreach (var line in lines.Skip(containerLogger.LineCount))
                            {
                                Console.ForegroundColor = containerLogger.Color;
                                Console.WriteLine(line);
                            }

                            _containerLineCounts[container.Name].LineCount = lines.Count;
                        }

                        if (container.Name == ServerContainerName &&
                            !_isDebuggerRunning &&
                            container.State == ServiceRunningState.Running)
                        {
                            StartVisualStudioDebugger();
                        }

                    }
                }
            }

            Console.WriteLine($"Shutting down...");
        }

        private void RegisterEvents()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
            {
                Console.WriteLine("Closing app");
                _service.Stop();
            };
            Console.CancelKeyPress += (sender, args) =>
            {
                Console.WriteLine("Ctrl+C close app");
                _service.Stop();
            };
        }

        private void StartVisualStudioDebugger()
        {
            var dte = GetInstances().FirstOrDefault();

            if (dte == null)
            {
                Console.WriteLine("Visual studio could not be located. Debugger will NOT be started.");
                return;
            }

            try
            {
                var window = dte.Windows.Item(EnvDTE.Constants.vsWindowKindCommandWindow);
                window.Activate();

                var commandWindow = window.Object as CommandWindow;

                var command2 = "DebugAdapterHost.Launch " +
                               "/LaunchJson:\"./SWLOR.Runner/launch.json\" " +
                               "/EngineGuid:541B8A8A-6081-4506-9F0A-1CE771DEBC04";

                commandWindow.SendInput(command2, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start Visual Studio debugger. Exception: {ex.ToMessageAndCompleteStacktrace()}");
            }


            _isDebuggerRunning = true;
        }

        private static IEnumerable<DTE> GetInstances()
        {
            var retVal = GetRunningObjectTable(0, out var rot);

            if (retVal == 0)
            {
                rot.EnumRunning(out var enumMoniker);

                var moniker = new IMoniker[1];
                while (enumMoniker.Next(1, moniker, out _) == 0)
                {
                    CreateBindCtx(0, out var bindCtx);
                    moniker[0].GetDisplayName(bindCtx, null, out var displayName);
                    var isVisualStudio = displayName.StartsWith("!VisualStudio");
                    if (isVisualStudio)
                    {
                        rot.GetObject(moniker[0], out var obj);
                        var dte = obj as DTE;
                        yield return dte;
                    }
                }
            }
        }

        [DllImport("ole32.dll")]
        private static extern void CreateBindCtx(int reserved, out IBindCtx ppbc);

        [DllImport("ole32.dll")]
        private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);
    }

}
