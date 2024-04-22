using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Paket;

namespace SWLOR.Core.Plugin.Paket
{
    internal sealed class PaketPluginSource : IPluginSource
    {
        private static readonly ApplicationSettings _appSettings = ApplicationSettings.Get();
        
        // https://docs.microsoft.com/en-us/nuget/create-packages/supporting-multiple-target-frameworks#architecture-specific-folders
        private static readonly string[] NativeDllPackagePaths = { "runtimes/linux-x64/native" };

        private readonly FSharpList<string> frameworks = ListModule.OfArray(Assemblies.TargetFrameworks);
        private readonly string linkFilePathFormat = Path.Combine(_appSettings.PluginDirectory, ".paket/load/{0}/main.group.csx");
        private readonly string packagesFolderPath = Path.Combine(_appSettings.PluginDirectory, "packages");

        private readonly string paketFilePath = Path.Combine(_appSettings.PluginDirectory, "paket.dependencies");

        private readonly PluginManager pluginManager;
        private readonly FSharpList<string> scriptTypes = ListModule.OfArray(new[] { "csx" });

        public PaketPluginSource(PluginManager pluginManager)
        {
            this.pluginManager = pluginManager;
        }

        public IEnumerable<Plugin> Bootstrap()
        {
            Dependencies? dependencies = GetDependencies();
            if (dependencies == null)
            {
                return Enumerable.Empty<Plugin>();
            }

            Logging.@event.Publish.AddHandler(OnLogEvent);

            InstallPackages(dependencies);
            return CreatePlugins(dependencies);
        }

        private IEnumerable<Plugin> CreatePlugins(Dependencies dependencies)
        {
            PaketAssemblyLoadFile? loadFile = null;

            // Our target frameworks are sorted by preference, so we early out once we find a valid link file.
            foreach (string framework in Assemblies.TargetFrameworks)
            {
                string linkFilePath = string.Format(linkFilePathFormat, framework);
                if (File.Exists(linkFilePath))
                {
                    loadFile = new PaketAssemblyLoadFile(linkFilePath);
                    break;
                }
            }

            if (loadFile == null)
            {
                throw new InvalidOperationException("Could not locate link file.");
            }

            Dictionary<string, string> nativeAssemblyPaths = GetNativeAssemblyPaths();

            List<Plugin> plugins = new List<Plugin>();
            FSharpList<Tuple<string, string, string>> entries = dependencies.GetDirectDependencies();

            Console.WriteLine($"Loading {entries.Length} DotNET plugin/s from: {_appSettings.PluginDirectory}");
            foreach ((string _, string pluginName, string _) in entries)
            {
                if (Assemblies.IsReservedName(pluginName))
                {
                    Console.WriteLine($"Skipping plugin {pluginName} as it uses a reserved name");
                    continue;
                }

                if (!loadFile.AssemblyPaths.TryGetValue(pluginName, out string? pluginPath))
                {
                    Console.WriteLine($"Cannot find path for plugin assembly {pluginName}");
                    continue;
                }

                if (!File.Exists(pluginPath))
                {
                    Console.WriteLine($"Cannot find plugin assembly {pluginPath}");
                    continue;
                }

                Plugin plugin = new Plugin(pluginManager, pluginPath)
                {
                    AdditionalAssemblyPaths = loadFile.AssemblyPaths,
                    UnmanagedAssemblyPaths = nativeAssemblyPaths,
                };

                plugins.Add(plugin);
            }

            return plugins;
        }

        private Dependencies? GetDependencies()
        {
            if (!File.Exists(paketFilePath))
            {
                Console.WriteLine($"Skipping initialization of Paket as {paketFilePath} does not exist");
                return null;
            }

            return Dependencies.Locate(paketFilePath);
        }

        private Dictionary<string, string> GetNativeAssemblyPaths()
        {
            Dictionary<string, string> nativeAssemblyPaths = new Dictionary<string, string>();

            string[] packageFolders = Directory.GetDirectories(packagesFolderPath);
            foreach (string nativeSubDir in NativeDllPackagePaths)
            {
                foreach (string packageFolder in packageFolders)
                {
                    string path = Path.Combine(packageFolder, nativeSubDir);
                    if (Directory.Exists(path))
                    {
                        foreach (string assembly in Directory.GetFiles(path))
                        {
                            nativeAssemblyPaths[Path.GetFileNameWithoutExtension(assembly)] = assembly;
                        }
                    }
                }
            }

            return nativeAssemblyPaths;
        }

        private void InstallPackages(Dependencies dependencies)
        {
            dependencies.Install(false, true, false, false, false, SemVerUpdateMode.NoRestriction, false, true, frameworks, scriptTypes, FSharpOption<string>.None);
        }

        private void OnLogEvent(object sender, Logging.Trace args)
        {
            switch (args.Level)
            {
                case TraceLevel.Error:
                    Console.WriteLine(args.Text);
                    break;
                case TraceLevel.Warning:
                    Console.WriteLine(args.Text);
                    break;
                default:
                    Console.WriteLine(args.Text);
                    break;
            }
        }
    }
}
