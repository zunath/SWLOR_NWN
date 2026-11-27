using System.Runtime.Loader;

namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>Uses the same types as the offenders without subscribing to a process-wide event or starting a thread.</summary>
public sealed class ProcessNeutralUser
{
    public object Use()
    {
        Thread.Sleep(0);
        Console.WriteLine(AppDomain.CurrentDomain.FriendlyName);
        Console.WriteLine(AssemblyLoadContext.Default.Name);
        return Thread.CurrentThread;
    }
}
