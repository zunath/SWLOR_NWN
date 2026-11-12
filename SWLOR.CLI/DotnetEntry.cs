using System;
using System.Collections.Generic;
using System.IO;

namespace SWLOR.CLI
{
    /// <summary>
    /// The NWNX .NET entry a server boots through, named by <c>SWLOR_DOTNET_ENTRY</c> in its <c>swlor.env</c>, with the
    /// <c>NWNX_DOTNET_*</c> lines and the <c>dotnet/</c> layout that go with it.
    /// </summary>
    internal sealed class DotnetEntry
    {
        public const string Variable = "SWLOR_DOTNET_ENTRY";

        /// <summary>
        /// SWLOR.Game.Host in <c>dotnet/</c>, which loads the game build from <c>dotnet/game/</c> as a generation. The
        /// development server's default.
        /// </summary>
        public static readonly DotnetEntry Host = new(
            "host",
            "/nwn/home/dotnet/SWLOR.Game.Host",
            "SWLOR.Game.Host.Bootstrap.NativeBootstrap",
            "Initialize",
            "game");

        /// <summary>SWLOR.Game.Server's own <c>ServerManager.Bootstrap</c>, the game build in <c>dotnet/</c>.</summary>
        public static readonly DotnetEntry Native = new(
            "native",
            "/nwn/home/dotnet/SWLOR.Game.Server",
            "SWLOR.Game.Server.Core.ServerManager",
            "Bootstrap",
            string.Empty);

        private DotnetEntry(string name, string assembly, string entryPoint, string method, string gameFolder)
        {
            Name = name;
            Assembly = assembly;
            EntryPoint = entryPoint;
            Method = method;
            GameFolder = gameFolder;
        }

        /// <summary>The value of <c>SWLOR_DOTNET_ENTRY</c>.</summary>
        public string Name { get; }

        /// <summary><c>NWNX_DOTNET_ASSEMBLY</c>.</summary>
        public string Assembly { get; }

        /// <summary><c>NWNX_DOTNET_ENTRYPOINT</c>.</summary>
        public string EntryPoint { get; }

        /// <summary><c>NWNX_DOTNET_METHOD</c>.</summary>
        public string Method { get; }

        /// <summary>Where the game build goes under <c>dotnet/</c>: <c>game</c> for the host, the folder itself for native.</summary>
        public string GameFolder { get; }

        /// <summary>The <c>swlor.env</c> lines this entry needs, in the order they are written.</summary>
        public IReadOnlyList<KeyValuePair<string, string>> EnvironmentSettings => new[]
        {
            new KeyValuePair<string, string>(Variable, Name),
            new KeyValuePair<string, string>("NWNX_DOTNET_ASSEMBLY", Assembly),
            new KeyValuePair<string, string>("NWNX_DOTNET_ENTRYPOINT", EntryPoint),
            new KeyValuePair<string, string>("NWNX_DOTNET_METHOD", Method)
        };

        /// <summary>
        /// The entry <paramref name="envPath"/> names. A file without the setting, or with it blank, means the host: the
        /// development server moved to it in P4-10. An unknown value stops the deploy.
        /// </summary>
        public static DotnetEntry Read(string envPath)
        {
            var value = string.Empty;
            if (File.Exists(envPath))
            {
                var prefix = Variable + "=";
                foreach (var line in File.ReadAllLines(envPath))
                {
                    if (!line.StartsWith(prefix, StringComparison.Ordinal))
                        continue;

                    value = line.Substring(prefix.Length).Trim();
                    break;
                }
            }

            return value.ToLowerInvariant() switch
            {
                "" or "host" => Host,
                "native" => Native,
                _ => throw new InvalidOperationException($"{envPath} sets {Variable}={value}; use host or native.")
            };
        }
    }
}
