using System.Reflection;
using System.Runtime.Loader;

namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>Lists the assemblies of every load context.</summary>
public sealed class LoadContextAllScanner
{
    public IEnumerable<Assembly> Scan() => AssemblyLoadContext.All.SelectMany(context => context.Assemblies);
}
