using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>The concealment read the status effects use, which reads only the engine.</summary>
[TestFixture]
public class ConcealmentRulesTests
{
    private const uint Creature = 1001;
    private const uint ObjectInvalid = 0x7F000000;

    private FakeNWScript _engine = null!;
    private IDisposable _scope = null!;
    private bool _stealth;

    [SetUp]
    public void SetUp()
    {
        _stealth = false;
        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid);
        _engine.SetReturnHandler("GetActionMode", args => (ActionMode)args[1]! == ActionMode.Stealth && _stealth);
        _scope = NWScriptRuntime.Override(_engine);
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    [Test]
    public void ACreatureThatIsGoneIsNeverConcealed()
    {
        _stealth = true;

        ConcealmentRules.IsConcealed(ObjectInvalid).Should().BeFalse();
    }

    [Test]
    public void AStealthedCreatureIsConcealedWithoutLookingAtItsEffects()
    {
        _stealth = true;

        ConcealmentRules.IsConcealed(Creature).Should().BeTrue();
        _engine.CountOf("GetFirstEffect").Should().Be(0);
    }

    [TestCase(EffectTypeScript.Invisibility, true)]
    [TestCase(EffectTypeScript.ImprovedInvisibility, true)]
    [TestCase(EffectTypeScript.Slow, false)]
    public void AnInvisibleCreatureIsConcealed(EffectTypeScript carried, bool concealed)
    {
        _engine.SetReturnSequence("GetIsEffectValid", true, false);
        _engine.SetReturnHandler("GetEffectType", _ => carried);

        ConcealmentRules.IsConcealed(Creature).Should().Be(concealed);
    }

    [Test]
    public void ACreatureWithNeitherIsNotConcealed()
    {
        _engine.SetReturn("GetIsEffectValid", false);

        ConcealmentRules.IsConcealed(Creature).Should().BeFalse();
    }
}
