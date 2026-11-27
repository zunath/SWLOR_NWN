using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// The assemblies a discovery scan (handlers, definitions, engine tests) may search for types.
    /// Only the assemblies loaded into the same <see cref="AssemblyLoadContext"/> as the game assembly count, so a
    /// retired build that is still loaded in its own context while a newer one boots is never scanned.
    /// In a process with a single context (today's server) these are the assemblies
    /// <c>AppDomain.CurrentDomain.GetAssemblies()</c> returns, in the same order.
    /// This is the only place the server enumerates assemblies; an architecture test fails any other use.
    /// </summary>
    public static class GameAssemblies
    {
        /// <summary>The assemblies of the game build's own load context, in the order the app domain lists them.</summary>
        public static Assembly[] GetAssemblies()
        {
            var gameContext = AssemblyLoadContext.GetLoadContext(typeof(GameAssemblies).Assembly);
            var all = AppDomain.CurrentDomain.GetAssemblies();
            var result = new List<Assembly>(all.Length);
            foreach (var assembly in all)
            {
                if (IsInContext(assembly, gameContext))
                    result.Add(assembly);
            }

            return result.ToArray();
        }

        private static bool IsInContext(Assembly assembly, AssemblyLoadContext gameContext)
        {
            try
            {
                return ReferenceEquals(AssemblyLoadContext.GetLoadContext(assembly), gameContext);
            }
            catch (ArgumentException)
            {
                // An assembly the runtime cannot place in a context (some dynamic ones) belongs to no build.
                return false;
            }
        }
    }
}
