using SWLOR.Game.Server.Core;

namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>An event handler where it belongs: a static method of the facade.</summary>
public static class HandlerOnFacade
{
    [NWNEventHandler("fixture_script")]
    public static void Handle()
    {
    }
}
