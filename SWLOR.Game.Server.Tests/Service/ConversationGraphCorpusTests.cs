using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;

namespace SWLOR.Game.Server.Tests.Service;

[NonParallelizable]
public sealed class ConversationGraphCorpusTests
{
    [Test]
    public void EmbeddedAuthoredGraphs_AreStructurallyValidAndUseRegisteredSnippets()
    {
        Snippet.CacheData();
        Conversation.CacheData();

        var assembly = typeof(Conversation).Assembly;
        var resources = assembly.GetManifestResourceNames()
            .Where(name => name.Contains(".ConversationData.", StringComparison.Ordinal) &&
                           name.EndsWith(".conversation.json", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var repositoryRoot = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (repositoryRoot != null && !File.Exists(Path.Combine(repositoryRoot.FullName, "SWLOR.Game.Server.sln")))
            repositoryRoot = repositoryRoot.Parent;
        repositoryRoot.Should().NotBeNull("the authored conversation corpus must be available to the test");
        var authoredGraphCount = Directory.EnumerateFiles(
            Path.Combine(repositoryRoot!.FullName, "SWLOR.Game.Server", "ConversationData"),
            "*.conversation.json").Count();

        resources.Should().HaveCount(authoredGraphCount,
            "every active authored graph must be embedded; retired quest dialogues and native DMFI are excluded");
        resources.Should().NotContain(resource =>
            resource.EndsWith(".dmfi_universal.conversation.json", StringComparison.OrdinalIgnoreCase));
        foreach (var resource in resources)
        {
            using var stream = assembly.GetManifestResourceStream(resource);
            using var reader = new StreamReader(stream!);
            var graph = JsonConvert.DeserializeObject<ConversationGraph>(reader.ReadToEnd());

            graph.Should().NotBeNull(resource);
            ConversationGraphValidator.Validate(graph!).Should().BeEmpty(resource);
            Conversation.TryGetGraph(graph!.Id, out _).Should().BeTrue(resource);
        }
    }
}
