using System.Reflection;
using System.Runtime.Loader;

namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>Lists the assemblies of the default load context.</summary>
public sealed class LoadContextDefaultScanner
{
    public IEnumerable<Assembly> Scan() => AssemblyLoadContext.Default.Assemblies;
}
