using System.Reflection;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// The handler discovery exactly as <c>ScriptRegistry.LoadHandlersFromAssembly</c> wrote it before the
/// <c>LoadHandlersFromAssemblies</c> seam existed, reduced to "which method is registered for which script".
/// It exists only so <see cref="ScriptHandlerSeamTests"/> can show the seam changed nothing; it is not used to
/// produce the order snapshot.
/// </summary>
public static class LegacyHandlerDiscovery
{
    public static IReadOnlyList<(string Script, bool Conditional, MethodInfo Method)> Discover(IEnumerable<Assembly> assemblies)
    {
        var registered = new List<(string Script, bool Conditional, MethodInfo Method)>();

        var handlers = assemblies
            .SelectMany(a => a.GetTypes())
            .SelectMany(t => t.GetMethods())
            .Where(m => m.GetCustomAttributes(typeof(NWNEventHandler), false).Length > 0)
            .ToArray();

        foreach (var mi in handlers)
        {
            foreach (var attr in mi.GetCustomAttributes(typeof(NWNEventHandler), false))
            {
                var script = ((NWNEventHandler)attr).Script;
                if (script.Length > 16 || script.Length == 0)
                    throw new ApplicationException();

                if (mi.ReturnType == typeof(bool))
                    registered.Add((script, true, mi));
                else if (mi.ReturnType == typeof(void))
                    registered.Add((script, false, mi));
            }
        }

        return registered;
    }
}
