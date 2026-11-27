using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.Testing;
using SWLOR.NWN.API.Tests.Support;

namespace SWLOR.NWN.API.Tests.Seam;

/// <summary>
/// The behaviour-neutral proof for the NWNX plugin seams: for every plugin, each static wrapper method has a matching member
/// on the plugin's interface, and calling the wrapper makes exactly one call on the installed engine with the same arguments,
/// returning what the engine returned.
/// </summary>
[TestFixture]
public class PluginForwardingTests
{
    public static IEnumerable<PluginSeam> Seams => PluginSeam.All;

    [Test]
    public void EveryPluginWrapperHasASeam()
    {
        // PluginSeam.All finds the static wrappers and throws when a generated type is missing.
        PluginSeam.All.Select(s => s.Name).Should().Contain(new[] { "ChatPlugin", "CreaturePlugin", "PlayerPlugin", "UtilPlugin", "WeaponPlugin" });
        PluginSeam.All.Should().HaveCountGreaterThanOrEqualTo(16);
    }

    [TestCaseSource(nameof(Seams))]
    public void StaticWrapper_HoldsOnlyMethodsAndDataOnlyNestedTypes(PluginSeam seam)
    {
        ForwardingProof.UncoveredMembers(seam.Wrapper).Should().BeEmpty("a static wrapper member the seam does not cover would bypass the engine interface");
        ForwardingProof.StaticMethods(seam.Wrapper).Should().NotBeEmpty();
    }

    [TestCaseSource(nameof(Seams))]
    public void EveryStaticMethod_HasAMatchingInterfaceMember(PluginSeam seam)
    {
        ForwardingProof.SignatureProblems(seam.Wrapper, seam.Interface).Should().BeEmpty();
    }

    [TestCaseSource(nameof(Seams))]
    public void EveryInterfaceMember_HasAStaticWrapper(PluginSeam seam)
    {
        ForwardingProof.OrphanInterfaceMembers(seam.Wrapper, seam.Interface).Should().BeEmpty();
    }

    [TestCaseSource(nameof(Seams))]
    public void EveryStaticMethod_ForwardsOnceToTheEngine_WithTheSameArgumentsAndReturnValue(PluginSeam seam)
    {
        var problems = ForwardingProof.ForwardingProblems(seam.Wrapper, () =>
        {
            var fake = seam.NewFake();
            return (fake, seam.Override(fake));
        });

        problems.Should().BeEmpty();
    }

    [TestCaseSource(nameof(Seams))]
    public void TheFake_ImplementsTheInterfaceAndRecordsThroughTheSharedBase(PluginSeam seam)
    {
        seam.Interface.IsAssignableFrom(seam.Fake).Should().BeTrue();
        typeof(FakeEngine).IsAssignableFrom(seam.Fake).Should().BeTrue();
        seam.Interface.IsAssignableFrom(seam.PassThrough).Should().BeTrue();
    }

    [Test]
    public void OverridingOnePlugin_LeavesTheOthersOnTheirDefaultEngines()
    {
        var chat = new FakeChatPlugin();
        using var scope = ChatPluginRuntime.Override(chat);

        ChatPlugin.GetMessage();

        chat.CountOf("GetMessage").Should().Be(1);
        PlayerPluginRuntime.Current.Should().BeOfType<NwnCorePlayerPlugin>();
        CreaturePluginRuntime.Current.Should().BeOfType<NwnCoreCreaturePlugin>();
    }
}
