using SWLOR.Game.Server.Core;

namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>An event handler on an instance class, which the script registry cannot bind.</summary>
public class HandlerOnInstance
{
    [NWNEventHandler("fixture_script")]
    public void Handle()
    {
    }
}
