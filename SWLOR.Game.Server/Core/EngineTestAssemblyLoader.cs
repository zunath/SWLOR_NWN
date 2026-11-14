using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// Loads the optional in-engine test assembly (<c>SWLOR.Game.Server.EngineTests</c>) into a load context from its
    /// bytes, the way the host loads the game build. The context keeps no open handle on the file, so a later build
    /// can replace it, and a collectible context unloads the test code with its generation: a reload brings the test
    /// code of the new build, never the old one.
    /// </summary>
    public static class EngineTestAssemblyLoader
    {
        /// <summary>The simple name of the engine-test assembly.</summary>
        public const string AssemblyName = "SWLOR.Game.Server.EngineTests";

        /// <summary>The path of the engine-test assembly in the first directory that has it, or null. <paramref name="probed"/> receives each path tried.</summary>
        public static string FindPath(IEnumerable<string> candidateDirectories, ICollection<string> probed)
        {
            foreach (var directory in candidateDirectories)
            {
                if (string.IsNullOrWhiteSpace(directory))
                    continue;

                var path = Path.Combine(directory, AssemblyName + ".dll");
                probed.Add(path);
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        /// <summary>Loads the assembly at <paramref name="path"/> (and its symbols, when the .pdb is beside it) into <paramref name="context"/> from bytes.</summary>
        public static Assembly Load(AssemblyLoadContext context, string path)
        {
            var assemblyBytes = File.ReadAllBytes(path);
            var symbolsPath = Path.ChangeExtension(path, ".pdb");

            using var assemblyStream = new MemoryStream(assemblyBytes, false);
            if (!File.Exists(symbolsPath))
                return context.LoadFromStream(assemblyStream);

            using var symbolsStream = new MemoryStream(File.ReadAllBytes(symbolsPath), false);
            return context.LoadFromStream(assemblyStream, symbolsStream);
        }

        /// <summary>
        /// The instances the instance handlers of <paramref name="assembly"/> run on: each class that declares a public instance
        /// method with <see cref="NWNEventHandler"/> is made content, built once by <paramref name="content"/> with its services.
        /// The bootstrapper hands them to the script registry after the container's own singletons.
        /// </summary>
        public static IReadOnlyDictionary<Type, object> CreateHandlerOwners(Assembly assembly, IContentFactory content)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            return assembly.GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract &&
                               type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                   .Any(method => method.GetCustomAttributes(typeof(NWNEventHandler), false).Length > 0))
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToDictionary(type => type, type => content.Create<object>(type));
        }
    }
}
