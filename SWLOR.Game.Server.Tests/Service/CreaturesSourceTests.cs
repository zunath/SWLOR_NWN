using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Proves <see cref="CreaturesSource"/> finds a Creatures member in the file that declares it and keeps the attributes the
/// source-reading tests assert, so a member can move between the part files without a test noticing.
/// </summary>
[TestFixture]
public class CreaturesSourceTests
{
    [Test]
    public void AMemberIsFoundInTheFileThatDeclaresIt()
    {
        CreaturesSource.RelativePath("CreatureBlocked").Should().Be("Domain/Creatures/AICreatureEventService.cs");
        CreaturesSource.RelativePath("CreatureAggroEnter").Should().Be("Domain/Creatures/AIAggroService.cs");
        CreaturesSource.RelativePath("ProcessFlags").Should().Be("Domain/Creatures/AIService.cs");
        CreaturesSource.RelativePath("ProcessProfileTrigger").Should().Be("Domain/Creatures/AIDecisionService.cs");
        CreaturesSource.RelativePath("ResolveTarget").Should().Be("Domain/Creatures/AIActionService.cs");
        CreaturesSource.RelativePath("PeekAuthorizedTarget").Should().Be("Domain/Creatures/CompanionTargetAuthorityService.cs");
        CreaturesSource.RelativePath("ProcessSpawnSystem").Should().Be("Domain/Creatures/SpawnService.cs");
    }

    [Test]
    public void AMemberKeepsItsAttributesAndBody()
    {
        CreaturesSource.Member("CreatureSpawn").Should()
            .Contain("[NWNEventHandler(ScriptName.OnCreatureSpawnAfter)]")
            .And.Contain("public void CreatureSpawn()");
    }

    [Test]
    public void MembersJoinsTheNamesInTheOrderGiven()
    {
        var text = CreaturesSource.Members("CreatureSpawn", "CreaturePhysicalAttacked");

        text.IndexOf("CreatureSpawn()", StringComparison.Ordinal).Should()
            .BeLessThan(text.IndexOf("CreaturePhysicalAttacked()", StringComparison.Ordinal));
    }

    [Test]
    public void AMemberNoFileDeclaresIsAnError()
    {
        var read = () => CreaturesSource.Member("NoSuchCreaturesMember");

        read.Should().Throw<InvalidOperationException>();
    }
}
