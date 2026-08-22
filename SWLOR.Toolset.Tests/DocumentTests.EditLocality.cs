using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Proves a typed document setter is as lexically local as the underlying field mutation:
    /// changing one named property changes exactly that field's value line. Mirrors the pattern
    /// in EditLocalityTests, but drives the edit through the Documents layer's typed API.
    /// </summary>
    public class DocumentEditLocalityTests
    {
        [Test]
        public void AreDocument_SettingTag_ChangesExactlyOneLine()
        {
            var path = Path.Combine(CorpusLocator.ModuleDirectory, "are", "bank.are.json");
            var original = File.ReadAllBytes(path);
            var document = AreDocument.Parse(original);

            document.Tag = "bank_edited";
            var written = document.ToBytes();

            AssertSingleLineChange(original, written, "bank_edited");
        }

        [Test]
        public void UtcDocument_SettingFactionId_ChangesExactlyOneLine()
        {
            var path = Path.Combine(CorpusLocator.ModuleDirectory, "utc", "zomb_guard.utc.json");
            var original = File.ReadAllBytes(path);
            var document = UtcDocument.Parse(original);

            document.FactionID = 42;
            var written = document.ToBytes();

            AssertSingleLineChange(original, written, "42");
        }

        [Test]
        public void VarTable_SetIntOnExistingEntry_ChangesExactlyOneLine()
        {
            var path = Path.Combine(CorpusLocator.ModuleDirectory, "utc", "bf_butcher.utc.json");
            var original = File.ReadAllBytes(path);
            var document = UtcDocument.Parse(original);

            document.VarTable.SetInt("QUEST_NPC_GROUP_ID", 123);
            var written = document.ToBytes();

            AssertSingleLineChange(original, written, "123");
        }

        private static void AssertSingleLineChange(byte[] original, byte[] written, string expectedNewToken)
        {
            var originalLines = Encoding.UTF8.GetString(original).Split('\n');
            var writtenLines = Encoding.UTF8.GetString(written).Split('\n');

            writtenLines.Length.Should().Be(originalLines.Length, "an in-place edit must not add or remove lines");

            var changedLines = new List<int>();
            for (var i = 0; i < originalLines.Length; i++)
            {
                if (originalLines[i] != writtenLines[i])
                    changedLines.Add(i);
            }

            changedLines.Should().HaveCount(1,
                $"exactly one line should change. Changed lines: " +
                string.Join(", ", changedLines.Select(i => $"{i}: '{originalLines[i]}' -> '{writtenLines[i]}'")));
            writtenLines[changedLines[0]].Should().Contain("\"value\":");
            writtenLines[changedLines[0]].Should().Contain(expectedNewToken);
        }
    }
}
