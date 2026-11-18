using System.Collections;
using System.Reflection;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// A byte-for-byte dump of every player guide topic (name, category, summary, rail summary, blocks, questions and related topics,
/// in list order), captured on the code before the guide's topics move into data types (P5-16h, PR 3) and compared after it.
/// Regenerate with <c>SWLOR_UPDATE_SNAPSHOTS=1</c> only for a deliberate content change.
/// </summary>
public class PlayerGuideTopicDumpTests : SkillFacadeFixture
{
    private const string FixturePath = "SWLOR.Game.Server.Tests/Fixtures/ViewModelSplit/PlayerGuideTopics.txt";

    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void InstallFacades() => _facadeScope = _facades.Install();

    [TearDown]
    public void RemoveFacades() => _facadeScope.Dispose();

    [Test]
    public void EveryTopicIsPublishedExactlyAsCaptured()
    {
        var actual = Dump();
        var path = Path.Combine(FindRepositoryRoot(), FixturePath.Replace('/', Path.DirectorySeparatorChar));

        if (Environment.GetEnvironmentVariable("SWLOR_UPDATE_SNAPSHOTS") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, new UTF8Encoding(false).GetBytes(actual));
        }

        File.Exists(path).Should().BeTrue("the dump is committed beside the tests");
        var expected = new UTF8Encoding(false).GetString(File.ReadAllBytes(path)).Replace("\r\n", "\n");
        if (expected != actual)
        {
            var expectedLines = expected.Split('\n');
            var actualLines = actual.Split('\n');
            var index = 0;
            while (index < expectedLines.Length && index < actualLines.Length && expectedLines[index] == actualLines[index])
                index++;
            Assert.Fail($"The guide dump differs at line {index + 1}: expected '{(index < expectedLines.Length ? expectedLines[index] : "<end>")}' " +
                        $"but got '{(index < actualLines.Length ? actualLines[index] : "<end>")}'.");
        }
    }

    private static string Dump()
    {
        var topics = FindTopics();
        var text = new StringBuilder();
        text.Append("topics ").Append(topics.Count).Append('\n');
        for (var index = 0; index < topics.Count; index++)
        {
            var topic = topics[index];
            text.Append("== topic ").Append(index).Append('\n');
            Line(text, "name", Text(topic, "Name"));
            Line(text, "category", Text(topic, "Category"));
            Line(text, "summary", Text(topic, "Summary"));
            Line(text, "rail", Text(topic, "RailSummary"));
            var blocks = Items(topic, "Blocks");
            text.Append("blocks ").Append(blocks.Count).Append('\n');
            foreach (var block in blocks)
            {
                Line(text, "block-title", Text(block, "Title"));
                Line(text, "block-body", Text(block, "Body"));
            }

            var questions = Items(topic, "Questions");
            text.Append("questions ").Append(questions.Count).Append('\n');
            foreach (var question in questions)
            {
                Line(text, "question", Text(question, "Question"));
                Line(text, "answer", Text(question, "Answer"));
            }

            var related = Items(topic, "RelatedTopics");
            text.Append("related ").Append(related.Count).Append('\n');
            foreach (var name in related)
                Line(text, "related-name", (string)name);
        }

        return text.ToString();
    }

    // Each value is written with its length so a change of one character, including whitespace, cannot hide at a line break.
    private static void Line(StringBuilder text, string label, string value) =>
        text.Append(label).Append(' ').Append(value.Length).Append(" [").Append(value).Append("]\n");

    private static List<object> FindTopics()
    {
        // The list is a static member named Topics on the view model today and on the topic catalog after the split.
        foreach (var typeName in new[] { "PlayerGuideViewModel", "PlayerGuideTopicCatalog" })
        {
            var type = typeof(PlayerGuideViewModel).Assembly.GetTypes().FirstOrDefault(candidate => candidate.Name == typeName);
            var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var value = type?.GetField("Topics", flags)?.GetValue(null) ?? type?.GetProperty("Topics", flags)?.GetValue(null);
            if (value is IEnumerable topics)
                return topics.Cast<object>().ToList();
        }

        throw new InvalidOperationException("No static Topics list was found on PlayerGuideViewModel or PlayerGuideTopicCatalog.");
    }

    private static string Text(object source, string property) =>
        source.GetType().GetProperty(property)!.GetValue(source) as string ?? string.Empty;

    private static List<object> Items(object source, string property) =>
        ((IEnumerable)source.GetType().GetProperty(property)!.GetValue(source)!).Cast<object>().ToList();

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate SWLOR.Game.Server.sln.");
    }
}
