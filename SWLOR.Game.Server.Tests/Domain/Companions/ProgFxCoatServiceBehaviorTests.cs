using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Companions;

/// <summary>
/// The coat lookups of <see cref="ProgFxCoatService"/> over a scripted engine: which visual effects coat, which coat a
/// creature wears, what gets written to its materials, and the queued refresh that follows an effect.
/// </summary>
[TestFixture]
public class ProgFxCoatServiceBehaviorTests
{
    private const uint Creature = 10;
    private const int PetrifyProgFx = 1050;

    private TintMapWorld _world = null!;
    private FacadeContainer _container = null!;
    private IDisposable _installed = null!;
    private ProgFxCoatService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _world = new TintMapWorld();
        _container = new FacadeContainer();
        _installed = _container.Install();
        _world.Add(Creature, ObjectType.Creature);
        // visualeffects row 1 coats with bark (progfx 5), row 2 has an environment-less progfx row, row 3 names no progfx row.
        _world.Engine.SetReturnHandler("Get2DARowCount", _ => 4);
        _world.Set2DA("visualeffects", "ProgFX_Duration", 1, "5");
        _world.Set2DA("visualeffects", "ProgFX_Duration", 2, "6");
        _world.Set2DA("visualeffects", "ProgFX_Duration", 3, "****");
        _world.Set2DA("progfx", "Type", 5, "1");
        _world.Set2DA("progfx", "Param1", 5, "vdu_tex_bark");
        _world.Set2DA("progfx", "Type", 6, "11");
        _world.Set2DA("progfx", "Param1", 6, "vdu_tex_bark");
        _world.Set2DA("progfx", "Type", PetrifyProgFx, "1");
        _world.Set2DA("progfx", "Param1", PetrifyProgFx, "vdu_tex_stone");
        _service = new ProgFxCoatService();
    }

    [TearDown]
    public void TearDown()
    {
        _installed.Dispose();
        _container.Dispose();
        _world.Dispose();
    }

    private void GiveEffects(params (EffectTypeScript Type, int Integer)[] effects)
    {
        var cursor = 0;
        _world.Engine.SetReturnHandler("GetFirstEffect", _ => { cursor = 0; return EngineStructures.Create<Effect>(effects.Length > 0 ? 1 : 0); });
        _world.Engine.SetReturnHandler("GetNextEffect", _ => { cursor++; return EngineStructures.Create<Effect>(cursor < effects.Length ? cursor + 1 : 0); });
        _world.Engine.SetReturnHandler("GetIsEffectValid", args => ((Effect)args[0]!).Handle != 0);
        _world.Engine.SetReturnHandler("GetEffectType", _ => effects[cursor].Type);
        _world.Engine.SetReturnHandler("GetEffectInteger", _ => effects[cursor].Integer);
    }

    private static TintMapMaterialSelection Selection(string material, bool wornHelmet = false) =>
        new(wornHelmet ? "helm_test" : "model", new TintMapMaterialDefinition("Material", material, TintMapLayerType.Cloth1), 11, Creature,
            usesItemColors: true, AppearanceArmor.Torso);

    [Test]
    public void LoadCoatingVisualEffects_FindsOnlyTheRowsThatCoat()
    {
        _service.LoadCoatingVisualEffects();

        _service.IsCoatingVisualEffect(1).Should().BeTrue();
        _service.IsCoatingVisualEffect(0).Should().BeFalse();
        _service.IsCoatingVisualEffect(2).Should().BeFalse("its progfx row is not an environment map");
        _service.IsCoatingVisualEffect(3).Should().BeFalse();
    }

    [Test]
    public void ARowBeforeTheLoadIsNotCoating()
    {
        _service.IsCoatingVisualEffect(1).Should().BeFalse();
    }

    [Test]
    public void GetCoatForVisualEffect_IsTheAtlasCellPlusOneAndIgnoresNegativeRows()
    {
        _service.GetCoatForVisualEffect(1).Should().Be(3, "vdu_tex_bark is the third atlas cell");
        _service.GetCoatForVisualEffect(1).Should().Be(3, "the answer is cached");
        _service.GetCoatForVisualEffect(3).Should().Be(0);
        _service.GetCoatForVisualEffect(-1).Should().Be(0);
    }

    [Test]
    public void GetCoatForProgFx_ReadsTheProgFxRowAndIgnoresNegativeRows()
    {
        _service.GetCoatForProgFx(5).Should().Be(3);
        _service.GetCoatForProgFx(6).Should().Be(0);
        _service.GetCoatForProgFx(-2).Should().Be(0);
    }

    [Test]
    public void GetCoat_IsZeroWithoutEffects()
    {
        GiveEffects();

        _service.GetCoat(Creature).Should().Be(0);
    }

    [Test]
    public void GetCoat_TheMostRecentCoatingWinsAndOtherEffectsAreSkipped()
    {
        GiveEffects((EffectTypeScript.Visualeffect, 1), (EffectTypeScript.Haste, 0), (EffectTypeScript.Petrify, 0));

        _service.GetCoat(Creature).Should().Be(1, "the petrify stone coat came last");
    }

    [Test]
    public void GetCoat_ReadsAVisualEffectCoat()
    {
        GiveEffects((EffectTypeScript.Visualeffect, 2), (EffectTypeScript.Visualeffect, 1));

        _service.GetCoat(Creature).Should().Be(3);
    }

    [Test]
    public void Apply_WritesTheCoatToEveryMaterialExceptWornHelmetsOnce()
    {
        GiveEffects((EffectTypeScript.Visualeffect, 1));
        var selections = new[] { Selection("mat_a"), Selection("MAT_A"), Selection("mat_b"), Selection("mat_helm", wornHelmet: true) };

        _service.Apply(Creature, selections, shaderOverridesReset: true);

        _world.UniformWrites().Should().Equal((Creature, "mat_a", ProgFxCoatService.UniformName), (Creature, "mat_b", ProgFxCoatService.UniformName));
        _world.Engine.CallsTo("SetMaterialShaderUniformVec4").Select(call => (float)call.Arguments[3]!).Should().OnlyContain(value => value == 3f);
    }

    [Test]
    public void Apply_AfterAResetWithNoCoatWritesNothing()
    {
        GiveEffects();

        _service.Apply(Creature, new[] { Selection("mat_a") }, shaderOverridesReset: true);

        _world.UniformWrites().Should().BeEmpty();
    }

    [Test]
    public void Apply_WithoutAResetClearsAStaleCoat()
    {
        GiveEffects();

        _service.Apply(Creature, new[] { Selection("mat_a") }, shaderOverridesReset: false);

        _world.Engine.CallsTo("SetMaterialShaderUniformVec4").Should().ContainSingle().Which.Arguments[3].Should().Be(0f);
    }

    [Test]
    public void QueueCoatRefresh_WaitsThenSkipsAnObjectThatIsGoneOrHasNoTintState()
    {
        var apply = new TintMapApplyService(ScriptedProxy.Create<IDroidService>(out _), _service,
            ScriptedProxy.Create<ITwinPistolEquipmentService>(out _));

        apply.QueueCoatRefresh(Creature);
        apply.QueueCoatRefresh(999);
        _world.Engine.CallsTo("DelayCommand").Should().HaveCount(2);
        _world.RunDelayed();

        _world.Engine.CallsTo("ResetMaterialShaderUniforms").Should().BeEmpty("a creature with no tint selections is left to the client's own coat");
    }
}
