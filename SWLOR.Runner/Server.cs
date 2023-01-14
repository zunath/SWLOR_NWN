using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Commands;
using Ductus.FluentDocker.Extensions;

namespace SWLOR.Runner
{
    internal class Server
    {
        private const string IniFileName = "./nwnpath.config";
        private const string DefaultNWNFolder = "%USERPROFILE%\\Documents\\Neverwinter Nights\\";
        private ICompositeService _service = null!;
        private readonly IHostService _docker;
        private readonly Dictionary<string, int> _containerLineCounts = new();

        public Server()
        {
            var hosts = new Hosts().Discover();
            _docker = hosts.FirstOrDefault(x => x.IsNative) ?? hosts.FirstOrDefault(x => x.Name == "default");
        }

        public void Run()
        {
            Console.WriteLine($"Starting server");
            RegisterEvents();

            var nwnPath = File.Exists(IniFileName)
                ? File.ReadAllText(IniFileName)
                : DefaultNWNFolder;
            var dockerComposePath = Environment.ExpandEnvironmentVariables(nwnPath) + "docker-compose.yml";
            
            using (_service = new Builder()
                       .UseContainer()
                       .UseCompose()
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
                            _containerLineCounts[container.Name] = 0;

                        using (var logs = _docker.Host.Logs(container.Id, showTimeStamps: true))
                        {
                            var lines = logs.ReadToEnd();

                            foreach (var line in lines.Skip(_containerLineCounts[container.Name]))
                            {
                                Console.WriteLine(line);
                            }

                            _containerLineCounts[container.Name] = lines.Count;
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
    }
}
