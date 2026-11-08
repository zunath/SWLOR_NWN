using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The poison coating instance: when a coated weapon hit spends a charge, the internal cooldown, the venom application
/// or refresh and the credit, and the tier tables. The engine and the events plugin are recording fakes; the venom and
/// combat point calls are answered by a subclass.
/// </summary>
[TestFixture]
public class PoisonsServiceTests
{
    private const uint Attacker = 11;
    private const uint Defender = 22;
    private const uint Weapon = 33;
    private const uint ObjectInvalid = 0x7F000000;
    private static readonly DateTime Epoch = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly List<IDisposable> _scopes = new();
    private FakeNWScript _engine = null!;
    private FakeEventsPlugin _events = null!;
    private LocalVariables _locals = null!;
    private TestPoisons _poisons = null!;
    private bool _defenderDead;
    private bool _weaponValid;

    [SetUp]
    public void SetUp()
    {
        _defenderDead = false;
        _weaponValid = true;
        _engine = new FakeNWScript();
        _locals = new LocalVariables(_engine);
        _engine.SetReturn("OBJECT_SELF", Attacker);
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid && ((uint)args[0]! != Weapon || _weaponValid));
        _engine.SetReturnHandler("GetIsDead", args => (uint)args[0]! == Defender && _defenderDead);
        _engine.SetReturnHandler("StringToObject", args => args[0] switch
        {
            "DEFENDER" => Defender,
            "WEAPON" => Weapon,
            _ => ObjectInvalid
        });
        _engine.SetReturnHandler("GetName", _ => "Vibroblade");
        _scopes.Add(NWScriptRuntime.Override(_engine));

        _events = new FakeEventsPlugin();
        _events.SetReturnHandler("GetEventData", args => (string)args[0]!);
        _scopes.Add(EventsPluginRuntime.Override(_events));

        _poisons = new TestPoisons { Now = Epoch.AddSeconds(100) };
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var scope in _scopes)
            scope.Dispose();
        _scopes.Clear();
    }

    private void Coat(int charges, int tier = 3, int potency = 7)
    {
        _engine.SetLocalInt(Weapon, Poisons.CoatingChargesVariable, charges);
        _engine.SetLocalInt(Weapon, Poisons.CoatingTierVariable, tier);
        _engine.SetLocalInt(Weapon, Poisons.CoatingPotencyVariable, potency);
    }

    [Test]
    public void ConsumePoisonCoating_IgnoresAnInvalidOrDeadDefender()
    {
        Coat(3);
        _defenderDead = true;
        _poisons.ConsumePoisonCoating();
        _poisons.Applied.Should().BeEmpty();

        _defenderDead = false;
        _events.SetReturnHandler("GetEventData", _ => "nobody");
        _poisons.ConsumePoisonCoating();
        _poisons.Applied.Should().BeEmpty();
        _locals.Int(Weapon, Poisons.CoatingChargesVariable).Should().Be(3);
    }

    [Test]
    public void ConsumePoisonCoating_NeedsAValidWeaponWithAtLeastOneCharge()
    {
        Coat(0);
        _poisons.ConsumePoisonCoating();
        _poisons.Applied.Should().BeEmpty();

        Coat(2);
        _weaponValid = false;
        _poisons.ConsumePoisonCoating();
        _poisons.Applied.Should().BeEmpty();
    }

    [Test]
    public void ConsumePoisonCoating_WaitsOutTheInternalCooldown()
    {
        Coat(3);
        _engine.SetLocalInt(Attacker, "POISON_COATING_NEXT_APPLY", 101);

        _poisons.ConsumePoisonCoating();

        _poisons.Applied.Should().BeEmpty();
        _locals.Int(Weapon, Poisons.CoatingChargesVariable).Should().Be(3);
    }

    [Test]
    public void ConsumePoisonCoating_AppliesVenomByTierSpendsACharge_AndCreditsEspionage()
    {
        Coat(3, tier: 4, potency: 9);

        _poisons.ConsumePoisonCoating();

        var applied = _poisons.Applied.Should().ContainSingle().Subject;
        applied.Source.Should().Be(Attacker);
        applied.Target.Should().Be(Defender);
        applied.Effect.Should().BeOfType<VenomStatusEffect>();
        applied.Duration.Should().Be(30f);
        _poisons.CombatPoints.Should().Equal((Attacker, Defender));
        _locals.Int(Weapon, Poisons.CoatingChargesVariable).Should().Be(2);
        _locals.Int(Attacker, "POISON_COATING_NEXT_APPLY").Should().Be(100 + Poisons.InternalCooldownSeconds);
        _engine.CountOf("SendMessageToPC").Should().Be(0);
    }

    [Test]
    public void ConsumePoisonCoating_DoesNotCreditWhenTheVenomIsNotApplied()
    {
        Coat(3);
        _poisons.ApplyResult = false;

        _poisons.ConsumePoisonCoating();

        _poisons.CombatPoints.Should().BeEmpty();
        _locals.Int(Weapon, Poisons.CoatingChargesVariable).Should().Be(2);
    }

    [Test]
    public void ConsumePoisonCoating_RefreshesAnExistingVenomOfTheSameSource()
    {
        Coat(3, tier: 2, potency: 5);
        _poisons.Existing = new VenomStatusEffect(1, 1);

        _poisons.ConsumePoisonCoating();

        _poisons.Applied.Should().BeEmpty();
        _poisons.Refreshed.Should().Equal((Defender, typeof(VenomStatusEffect), Attacker, 18f));
        _poisons.CombatPoints.Should().ContainSingle();
    }

    [Test]
    public void ConsumePoisonCoating_ReplacesAVenomThatIsFlaggedForRemoval()
    {
        Coat(3);
        var venom = new VenomStatusEffect(1, 1);
        typeof(StatusEffectBase).GetProperty(nameof(IStatusEffect.IsFlaggedForRemoval),
                BindingFlags.Public | BindingFlags.Instance)!
            .GetSetMethod(true)!.Invoke(venom, [true]);
        _poisons.Existing = venom;

        _poisons.ConsumePoisonCoating();

        _poisons.Refreshed.Should().BeEmpty();
        _poisons.Applied.Should().ContainSingle();
    }

    [Test]
    public void ConsumePoisonCoating_RemovesTheCoatingAndTellsThePlayerOnTheLastCharge()
    {
        Coat(1);

        _poisons.ConsumePoisonCoating();

        _locals.Has(Weapon, Poisons.CoatingChargesVariable).Should().BeFalse();
        _locals.Has(Weapon, Poisons.CoatingTierVariable).Should().BeFalse();
        _locals.Has(Weapon, Poisons.CoatingPotencyVariable).Should().BeFalse();
        _engine.CallsTo("SendMessageToPC").Should().ContainSingle()
            .Which.Arguments[1].Should().Be("The venom coating on Vibroblade has worn off.");
    }

    [TestCase(-3, 8)]
    [TestCase(0, 8)]
    [TestCase(1, 8)]
    [TestCase(2, 12)]
    [TestCase(3, 16)]
    [TestCase(4, 21)]
    [TestCase(5, 26)]
    [TestCase(9, 26)]
    public void GetCoatingVenomDamagePerTick_ClampsTheTier(int tier, int damage)
    {
        _poisons.GetCoatingVenomDamagePerTick(tier).Should().Be(damage);
    }

    [TestCase(0, 12f)]
    [TestCase(1, 12f)]
    [TestCase(3, 24f)]
    [TestCase(5, 36f)]
    [TestCase(8, 36f)]
    public void GetVenomDurationSeconds_ClampsTheTier(int tier, float seconds)
    {
        _poisons.GetVenomDurationSeconds(tier).Should().Be(seconds);
    }

    [Test]
    public void TheTierTablesAreReadWithoutAllocating()
    {
        var sum = 0;
        for (var i = 0; i < 100; i++)
            sum += _poisons.GetCoatingVenomDamagePerTick(i % 6) + (int)_poisons.GetVenomDurationSeconds(i % 6);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
            sum += _poisons.GetCoatingVenomDamagePerTick(i % 6) + (int)_poisons.GetVenomDurationSeconds(i % 6);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        allocated.Should().Be(0, "the damage table is an instance field, not a getter that rebuilds it");
        sum.Should().BeGreaterThan(0);
    }

    private sealed class TestPoisons : PoisonsService
    {
        public TestPoisons()
            : base(FacadeForwarder.For<IStatusEffectService>(), FacadeForwarder.For<ICombatPointService>())
        {
        }

        public DateTime Now { get; set; }
        public IStatusEffect? Existing { get; set; }
        public bool ApplyResult { get; set; } = true;
        public List<(uint Source, uint Target, IStatusEffect Effect, float Duration)> Applied { get; } = new();
        public List<(uint Target, Type Type, uint Source, float Duration)> Refreshed { get; } = new();
        public List<(uint Player, uint Creature)> CombatPoints { get; } = new();

        protected override DateTime UtcNow() => Now;

        protected override IStatusEffect GetStatusEffect(uint creature, Type statusEffectClass, uint source) => Existing!;

        protected override bool RefreshStatusEffectDuration(uint creature, Type statusEffectClass, uint source, float durationSeconds)
        {
            Refreshed.Add((creature, statusEffectClass, source, durationSeconds));
            return ApplyResult;
        }

        protected override bool ApplyStatusEffect(uint source, uint creature, IStatusEffect statusEffect, float durationSeconds)
        {
            Applied.Add((source, creature, statusEffect, durationSeconds));
            return ApplyResult;
        }

        protected override void AddEspionageCombatPoint(uint player, uint creature) =>
            CombatPoints.Add((player, creature));
    }
}
