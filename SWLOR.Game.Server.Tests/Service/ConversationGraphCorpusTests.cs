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

        resources.Should().HaveCount(341,
            "all active authored graphs must be embedded; retired dialogues and native DMFI/crafting resources are excluded");
        resources.Should().NotContain(resource =>
            resource.EndsWith(".dmfi_universal.conversation.json", StringComparison.OrdinalIgnoreCase));
        resources.Should().NotContain(resource =>
            resource.EndsWith(".x0_skill_ctrap.conversation.json", StringComparison.OrdinalIgnoreCase));
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
