using NUnit.Framework;
using SWLOR.DiscordBot.Configuration;
using SWLOR.DiscordBot.Core;

namespace SWLOR.DiscordBot.Tests;

[TestFixture]
public sealed class TemplateRendererTests
{
    [TestCase("", "large")]
    [TestCase("large", "")]
    [TestCase("   ", "large")]
    [TestCase("large", " \t ")]
    [TestCase("\U0001F600", "large")]
    [TestCase("N", "\U0001F600")]
    public void OmittedFieldDoesNotConsumeLaterFieldOrEmbedBudget(string name, string value)
    {
        var almostFull = name == "\U0001F600" || value == "\U0001F600";
        var used = almostFull ? 5998 : 0;
        var first = new CommunityEmbed(null, used > 0 ? new string('d', 4096) : null, null, 0,
            used > 0 ? [new(new string('n', 256), new string('v', 1024)), new("N", new string('v', 621))] : []);
        var invalid = new CommunityEmbed(null, null, null, 0,
            Enumerable.Range(0, 8).Select(_ => new CommunityEmbedField(
                name == "large" ? new string('n', 256) : name,
                value == "large" ? new string('v', 1024) : value)).ToArray());
        var valid = new CommunityEmbed("Z", null, "https://example.com/kept", 42, []);
        var output = TemplateRenderer.EnforceMessageLimits(new("", [first, invalid, valid]));
        Assert.Multiple(() =>
        {
            Assert.That(output.Embeds, Has.Count.EqualTo(used > 0 ? 2 : 1), "Unusable fields must be omitted as a whole.");
            Assert.That(output.Embeds.Last().Title, Is.EqualTo("Z"));
            Assert.That(output.Embeds.Last().Url, Is.EqualTo("https://example.com/kept"));
            Assert.That(output.Embeds.Last().Color, Is.EqualTo(42));
            Assert.That(output.Embeds.Sum(embed => (embed.Title?.Length ?? 0) + (embed.Description?.Length ?? 0) +
                embed.Fields.Sum(field => field.Name.Length + field.Value.Length)), Is.EqualTo(used + 1));
        });
    }

    [Test]
    public void MissingArgumentFieldLeavesFullBudgetForLaterValidFields()
    {
        var conditional = TemplateRenderer.RenderEmbed(new AnswerEmbed
        {
            Fields = Enumerable.Range(0, 6).Select(_ => new SWLOR.DiscordBot.Configuration.EmbedField
            { Name = "{1}", Value = new string('v', 1024) }).ToArray()
        }, 7, "Server", []);
        var valid = new CommunityEmbed(null, null, null, 0,
            Enumerable.Range(0, 6).Select(_ => new CommunityEmbedField("N", new string('v', 1000))).ToArray());
        var output = TemplateRenderer.EnforceMessageLimits(new("", [conditional, valid]));
        Assert.That(output.Embeds, Has.Count.EqualTo(1));
        Assert.That(output.Embeds.Single().Fields, Has.Count.EqualTo(6));
        Assert.That(output.Embeds.Single().Fields.Sum(field => field.Name.Length + field.Value.Length), Is.EqualTo(6000));
    }
}