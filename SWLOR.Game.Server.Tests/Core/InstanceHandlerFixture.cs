using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// A class with event handlers on instance methods, one action and one conditional, for
/// <see cref="ScriptRegistryInstanceHandlerTests"/>. The container does not register it, so only a resolver of the test's
/// own binds it (the scans of the whole AppDomain use <see cref="ScriptHandlerTestSupport.OwnersAndTestFixtures"/>).
/// </summary>
public class InstanceHandlerFixture
{
    public const string ActionScript = "zz_inst_act";
    public const string ConditionalScript = "zz_inst_cond";

    public int Runs { get; private set; }

    public bool Answer { get; set; } = true;

    [NWNEventHandler(ActionScript)]
    public void Run()
    {
        Runs++;
    }

    [NWNEventHandler(ConditionalScript)]
    public bool Allow()
    {
        return Answer;
    }
}
