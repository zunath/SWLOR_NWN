using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Proves edits are lexically local: changing one field changes exactly that field's value
    /// line and nothing else. Guards against formatting bleed on save.
    /// </summary>
    public class EditLocalityTests
    {
        [TestCaseSource(nameof(SampleFilePerFolder))]
        public void ChangingOneIntegerField_ChangesExactlyOneLine(string file)
        {
            var original = File.ReadAllBytes(file);
            var document = JsonGffDocument.Parse(original);

            var field = FindFirstMutableInteger(document.Root);
            if (field == null)
                Assert.Ignore($"No mutable integer field in {file}");

            field.SetInteger(field.GetInteger() == 0 ? 1 : 0);

            var written = document.ToBytes();
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
        }

        private static JsonGffField? FindFirstMutableInteger(JsonGffStruct target)
        {
            foreach (var (_, field) in target.Entries)
            {
                switch (field.Type)
                {
                    case GffFieldType.Byte:
                    case GffFieldType.Word:
                    case GffFieldType.Short:
                    case GffFieldType.Dword:
                    case GffFieldType.Int:
                        return field;
                    case GffFieldType.Struct:
                        var fromStruct = FindFirstMutableInteger(field.Struct!);
                        if (fromStruct != null)
                            return fromStruct;
                        break;
                    case GffFieldType.List:
                        foreach (var element in field.Elements!)
                        {
                            var fromElement = FindFirstMutableInteger(element);
                            if (fromElement != null)
                                return fromElement;
                        }
                        break;
                }
            }

            return null;
        }

        private static IEnumerable<string> SampleFilePerFolder()
        {
            foreach (var folder in CorpusLocator.GffFolders)
            {
                var path = Path.Combine(CorpusLocator.ModuleDirectory, folder);
                if (!Directory.Exists(path))
                    continue;

                var file = Directory.EnumerateFiles(path, "*.json").FirstOrDefault();
                if (file != null)
                    yield return file;
            }
        }
    }
}
