using SWLOR.Game.Server.Core;

namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>An event handler on a static class, which is not an instance class of a service.</summary>
public static class HandlerOnStaticClass
{
    [NWNEventHandler("fixture_script")]
    public static void Handle()
    {
    }
}
