using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SnippetService;

namespace SWLOR.Game.Server.Tests.Service;

public class SnippetActionExecutionTests
{
    [Test]
    public void ActionDelegateReportsWhetherTheOutcomeSucceeded()
    {
        typeof(SnippetActionDelegate).GetMethod("Invoke")!.ReturnType.Should().Be(typeof(bool));
    }

}
