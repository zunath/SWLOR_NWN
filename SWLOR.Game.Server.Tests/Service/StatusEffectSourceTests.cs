using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Proves <see cref="StatusEffectSource"/> finds a status effect member in the file that declares it and keeps the text the
/// source-reading tests assert, so a member can move between the part files without a test noticing.
/// </summary>
[TestFixture]
public class StatusEffectSourceTests
{
    [Test]
    public void TheCombinedSourceHoldsEveryPartFileInOrder()
    {
        var paths = StatusEffectSource.Paths();

        paths.Select(path => path.Replace('\\', '/')).Should().HaveCount(StatusEffectSource.PartFiles.Count)
            .And.SatisfyRespectively(StatusEffectSource.PartFiles.Select(relative => (Action<string>)(path => path.Should().EndWith(relative))));
        paths.Should().OnlyContain(path => File.Exists(path));
        paths[0].Replace('\\', '/').Should().EndWith("SWLOR.Game.Server/Domain/Character/StatusEffectService.cs");
    }

    [Test]
    public void TheInterfaceFilesExistAndStartWithTheServiceInterface()
    {
        var text = StatusEffectSource.ReadInterfaces();

        text.Should().Contain("interface IStatusEffectService");
        StatusEffectSource.InterfaceFiles.Should().HaveCount(StatusEffectSource.PartFiles.Count);
        StatusEffectSource.PartInterfaces.Should().Contain("IStatusEffectService");
    }

    [Test]
    public void AMemberIsFoundWithItsBody()
    {
        StatusEffectSource.Method("GetStatAdjustment").ToString().Should().Contain("GetStatAdjustment(");
        StatusEffectSource.Members("RemoveCreature").Should().Contain("RemoveCreature(");
        StatusEffectSource.Read().Should().Contain("_creatureEffects");
    }

    [Test]
    public void MembersJoinsTheNamesInTheOrderGiven()
    {
        var text = StatusEffectSource.Members("RemoveCreature", "GetStatAdjustment");

        text.IndexOf("RemoveCreature(", StringComparison.Ordinal).Should()
            .BeLessThan(text.IndexOf("GetStatAdjustment(", StringComparison.Ordinal));
    }

    [Test]
    public void AMemberNoFileDeclaresIsAnError()
    {
        var read = () => StatusEffectSource.Method("NoSuchStatusEffectMember");

        read.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void ACallThroughAFieldOfAPartInterfaceLosesItsReceiver()
    {
        const string Text = "class C { private readonly IStatusEffectService _core; void M() { _core.RemoveCreature(1); _other.Do(2); } }";

        var normalised = StatSource.Normalise(Text, StatusEffectSource.PartInterfaces);

        normalised.Should().Contain("{ RemoveCreature(1);").And.Contain("_other.Do(2);");
    }

    [Test]
    public void TheStatusEffectFilesAreNotStatFiles()
    {
        StatusEffectSource.Paths().Should().OnlyContain(path => !StatSource.IsStatFile(path));
    }
}
