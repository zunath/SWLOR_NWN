using System.Reflection;
using System.Runtime.Loader;

namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>Finds the load context of one assembly and loads one from a path: neither lists assemblies.</summary>
public sealed class LoadContextLookup
{
    public AssemblyLoadContext? Find(Assembly assembly) => AssemblyLoadContext.GetLoadContext(assembly);

    public Assembly Load(string path) => AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
}
