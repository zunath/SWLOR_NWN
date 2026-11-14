using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SWLOR.CLI
{
    internal class DeployBuild
    {
        private const string DebugServerPath = "../debugserver/";
        private const string DotnetPath = DebugServerPath + "dotnet";
        private const string HakPath = DebugServerPath + "hak";
        private const string ModulesPath = DebugServerPath + "modules";
        private const string TlkPath = DebugServerPath + "tlk";
        private const string DebugServerEnvPath = DebugServerPath + "swlor.env";
        private const string SourceEnvPath = "../SWLOR.Game.Server/Docker/swlor.env";
        private const string MaterialNameNullTweakVariable = "NWNX_TWEAKS_MATERIAL_NAME_NULL_IS_ALL";
        private const string MaterialNameNullTweakValue = "true";

        private readonly HakBuilder _hakBuilder = new();

        public void Process(string serverOutputPath = null, string hostOutputPath = null)
        {
            // RunCLI builds Release for standalone invocations. Post-build deployment
            // explicitly passes the initiating build's TargetDir (including Debug/custom output).
            var source = new DirectoryInfo(string.IsNullOrWhiteSpace(serverOutputPath)
                ? "../SWLOR.Game.Server/bin/Release/net10.0/"
                : serverOutputPath);
            RequireBuildOutput(source, "SWLOR.Game.Server", "Server");

            // The entry the debug server's swlor.env names (the host when it names none); the repository's swlor.env
            // seeds a server that has none yet.
            var entry = DotnetEntry.Read(File.Exists(DebugServerEnvPath) ? DebugServerEnvPath : SourceEnvPath);
            DirectoryInfo host = null;
            if (entry == DotnetEntry.Host)
            {
                host = new DirectoryInfo(string.IsNullOrWhiteSpace(hostOutputPath)
                    ? "../SWLOR.Game.Host/bin/Release/net10.0/"
                    : hostOutputPath);
                RequireBuildOutput(host, "SWLOR.Game.Host", "Host");
            }

            CreateDebugServerDirectory(entry);
            CopyBinaries(entry, source, host);
            BuildHaks();
            BuildModule();
        }

        private static void RequireBuildOutput(DirectoryInfo output, string assemblyName, string label)
        {
            foreach (var filename in new[]
                     {
                         assemblyName + ".dll", assemblyName + ".deps.json", assemblyName + ".runtimeconfig.json"
                     })
            {
                if (!File.Exists(Path.Combine(output.FullName, filename)))
                    throw new FileNotFoundException($"{label} build output is incomplete: {Path.Combine(output.FullName, filename)}");
            }
        }

        private void CreateDebugServerDirectory(DotnetEntry entry)
        {
            Directory.CreateDirectory(DebugServerPath);
            Directory.CreateDirectory(DotnetPath);
            Directory.CreateDirectory(HakPath);
            Directory.CreateDirectory(ModulesPath);
            Directory.CreateDirectory(TlkPath);

            var source = new DirectoryInfo("../SWLOR.Game.Server/Docker");
            var target = new DirectoryInfo(DebugServerPath);

            CopyAll(source, target, "swlor.env");
            EnsureEnvironmentSetting(
                DebugServerEnvPath,
                MaterialNameNullTweakVariable,
                MaterialNameNullTweakValue);

            // The NWNX entry lines follow SWLOR_DOTNET_ENTRY, so that one setting decides the entry.
            foreach (var setting in entry.EnvironmentSettings)
                EnsureEnvironmentSetting(DebugServerEnvPath, setting.Key, setting.Value);
        }

        private void CopyBinaries(DotnetEntry entry, DirectoryInfo source, DirectoryInfo host)
        {
            var dotnet = new DirectoryInfo(DotnetPath);
            if (host != null)
            {
                Console.WriteLine($"Deploying host binaries ({entry.Name} entry): {host.FullName} -> {dotnet.FullName}");
                CopyAll(host, dotnet, string.Empty);
            }

            var target = new DirectoryInfo(Path.Combine(DotnetPath, entry.GameFolder));
            Console.WriteLine($"Deploying server binaries ({entry.Name} entry): {source.FullName} -> {target.FullName}");
            CopyAll(source, target, string.Empty);
        }

        private void BuildHaks()
        {
            var config = _hakBuilder.GetConfig();
            var missingInputs = config.HakList
                .Where(hak => hak != null && !string.IsNullOrWhiteSpace(hak.Name))
                .Select(hak => hak.Path)
                .Where(path => !Directory.Exists(path))
                .ToList();
            if (!File.Exists(config.TlkPath))
                missingInputs.Add(config.TlkPath);

            if (missingInputs.Count > 0)
            {
                Console.WriteLine($"warning : Skipping HAK/TLK deployment: missing inputs: {string.Join(", ", missingInputs)}. Initialize the SWLOR_Haks submodule and build its TLK. Existing deployed assets are preserved; server binaries were updated.");
                return;
            }

            _hakBuilder.Process(config);
        }

        private void BuildModule()
        {
            var modulePath = "../Module/Star Wars LOR v2.mod";
            if (!File.Exists(modulePath))
            {
                Console.WriteLine("warning : Skipping module deployment: Module/Star Wars LOR v2.mod is missing. Run PackModule.cmd from the Module directory, then rebuild. Any existing deployed module is preserved; server binaries were updated.");
                return;
            }
            File.Copy(modulePath, ModulesPath + "/Star Wars LOR v2.mod", true);
        }

        private static void CopyAll(DirectoryInfo source, DirectoryInfo target, string excludeFile)
        {
            Directory.CreateDirectory(target.FullName);
            foreach (var fi in source.GetFiles())
            {
                var targetPath = Path.Combine(target.FullName, fi.Name);

                if (File.Exists(targetPath) && fi.Name == excludeFile)
                    continue;

                fi.CopyTo(targetPath, true);
            }
            foreach (var diSourceSubDir in source.GetDirectories())
            {
                var nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir, excludeFile);
            }
        }

        private static void EnsureEnvironmentSetting(string path, string key, string value)
        {
            var prefix = key + "=";
            var updatedLines = new List<string>();
            var found = false;

            foreach (var line in File.ReadAllLines(path))
            {
                if (!line.StartsWith(prefix, StringComparison.Ordinal))
                {
                    updatedLines.Add(line);
                    continue;
                }

                if (!found)
                {
                    updatedLines.Add(prefix + value);
                    found = true;
                }
            }

            if (!found)
                updatedLines.Add(prefix + value);

            File.WriteAllLines(path, updatedLines);
        }
    }
}
