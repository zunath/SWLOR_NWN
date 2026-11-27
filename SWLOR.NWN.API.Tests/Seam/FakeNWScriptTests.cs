using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.NWN.API.Tests.Seam;

/// <summary>The recording fake: what it records, what it answers by default, and how tests configure answers.</summary>
[TestFixture]
public class FakeNWScriptTests
{
    private const uint ObjectInvalid = 0x7F000000;

    [Test]
    public void Calls_AreRecordedInOrderWithTheirArguments()
    {
        INWScript engine = new FakeNWScript();

        engine.SetLocalInt(1001u, "KEY", 5);
        engine.SpeakString("hello");

        var fake = (FakeNWScript)engine;
        fake.Calls.Select(c => c.ToString()).Should().Equal("SetLocalInt(1001, \"KEY\", 5)", "SpeakString(\"hello\", Talk)");
        fake.CountOf("SetLocalInt").Should().Be(1);
        fake.CallsTo("SpeakString").Single().Arguments[0].Should().Be("hello");
    }

    [Test]
    public void DefaultAnswers_EndTypicalEngineLoops()
    {
        INWScript engine = new FakeNWScript();

        engine.GetTag(1001u).Should().BeEmpty();
        engine.GetLocalInt(1001u, "KEY").Should().Be(0);
        engine.GetIsObjectValid(1001u).Should().BeFalse();
        engine.GetFirstObjectInArea().Should().Be(ObjectInvalid, "every uint return is an object id");
        engine.GetDistanceBetween(1001u, 1002u).Should().Be(0f);
        engine.OBJECT_SELF.Should().Be(ObjectInvalid);

        var effect = engine.GetFirstEffect(1001u);
        effect.Should().NotBeNull();
        effect.Handle.Should().Be(0);
        engine.GetIsEffectValid(effect).Should().BeFalse();
    }

    [Test]
    public void SetReturn_AnswersEveryCallAndConvertsNumbers()
    {
        var fake = new FakeNWScript();
        fake.SetReturn("GetLocalInt", 7);
        fake.SetReturn("GetMaster", 4242);
        fake.SetReturn("GetTag", "TAG");
        INWScript engine = fake;

        engine.GetLocalInt(1u, "A").Should().Be(7);
        engine.GetLocalInt(2u, "B").Should().Be(7);
        engine.GetMaster(1u).Should().Be(4242u, "an int configured for a uint member is converted");
        engine.GetTag(1u).Should().Be("TAG");
    }

    [Test]
    public void SetReturnSequence_AnswersOnePerCallThenFallsBackToTheDefault()
    {
        var fake = new FakeNWScript();
        fake.SetReturnSequence("GetNextObjectInArea", 1001u, 1002u);
        INWScript engine = fake;

        engine.GetNextObjectInArea().Should().Be(1001u);
        engine.GetNextObjectInArea().Should().Be(1002u);
        engine.GetNextObjectInArea().Should().Be(ObjectInvalid);
    }

    [Test]
    public void SetReturnHandler_ComputesTheAnswerFromTheArguments()
    {
        var fake = new FakeNWScript();
        fake.SetReturnHandler("GetLocalInt", args => ((string)args[1]!).Length);
        INWScript engine = fake;

        engine.GetLocalInt(1u, "ABC").Should().Be(3);
        engine.GetLocalInt(1u, "ABCDE").Should().Be(5);
    }

    [Test]
    public void SetReturn_WithTheWrongType_ExplainsWhichMemberFailed()
    {
        var fake = new FakeNWScript();
        fake.SetReturn("GetTag", 5);
        INWScript engine = fake;

        var action = () => engine.GetTag(1u);

        action.Should().Throw<InvalidOperationException>().WithMessage("*GetTag*Int32*String*");
    }

    [Test]
    public void ClearCalls_ForgetsCallsButKeepsConfiguredAnswers()
    {
        var fake = new FakeNWScript();
        fake.SetReturn("GetTag", "KEPT");
        INWScript engine = fake;
        engine.GetTag(1u);

        fake.ClearCalls();

        fake.Calls.Should().BeEmpty();
        engine.GetTag(1u).Should().Be("KEPT");
    }

    [Test]
    public void ParamsArrays_AreRecordedAsOneArgument()
    {
        INWScript engine = new FakeNWScript();
        var tags = new[] { "A", "B" };

        engine.HasEffectByTag(1u, tags);

        ((FakeNWScript)engine).Calls.Single().Arguments[1].Should().BeSameAs(tags);
    }

    [Test]
    public void EngineStructures_AreCreatedWithoutAFinalizerThatNeedsTheEngine()
    {
        var effect = EngineStructures.Create<Effect>(12);

        effect.Handle.Should().Be(12);
        // The finalizer of a real structure would call into the native game; suppressed instances are safe to collect.
        effect = null!;
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
