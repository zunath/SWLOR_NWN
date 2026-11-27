using System.Reflection;
using SWLOR.Game.Server.Feature.GuiDefinition.PerksWindow;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A perks host whose every member returns its default (player 0, no mode, empty lists), for tests that call a perks service directly.
/// </summary>
public class PerksHostStub : DispatchProxy
{
    public static IPerksHost Create() => Create<IPerksHost, PerksHostStub>();

    protected override object Invoke(MethodInfo targetMethod, object[] args)
    {
        var type = targetMethod.ReturnType;
        return type == typeof(void) || !type.IsValueType ? null : Activator.CreateInstance(type);
    }
}
