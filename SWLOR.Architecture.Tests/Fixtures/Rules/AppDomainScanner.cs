using System.Reflection;

namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>Lists every assembly of the app domain.</summary>
public sealed class AppDomainScanner
{
    public Assembly[] Scan() => AppDomain.CurrentDomain.GetAssemblies();
}
