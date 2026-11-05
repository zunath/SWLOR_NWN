using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Core;

[TestFixture]
[NonParallelizable]
public class AnimationPlayerServiceTests
{
    private FakeNWScript _engine = null!;
    private IDisposable _scope = null!;
    private readonly AnimationPlayerService _service = new();

    [SetUp]
    public void SetUp()
    {
        _engine = new FakeNWScript();
        _scope = NWScriptRuntime.Override(_engine);
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    [Test]
    public void Play_WithoutStoredEffect_AppliesNothing()
    {
        _engine.SetReturn("GetLocalInt", 0);

        _service.Play(7, AnimationEvent.CreatureOnDeath);

        _engine.CountOf("ApplyEffectToObject").Should().Be(0);
        _engine.CountOf("EffectVisualEffect").Should().Be(0);
    }

    [Test]
    public void Play_WithStoredEffect_AppliesItToTheObject()
    {
        _engine.SetReturn("GetLocalInt", 5);
        _engine.SetReturn("GetLocalFloat", 1.5f);

        _service.Play(7, AnimationEvent.CreatureOnDeath);

        _engine.CountOf("EffectVisualEffect").Should().Be(1);
        _engine.CountOf("ApplyEffectToObject").Should().Be(1);
    }
}
