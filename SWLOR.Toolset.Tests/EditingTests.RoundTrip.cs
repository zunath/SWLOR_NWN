using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// WP1.5: proves a mutation made through a DocumentTransaction, once undone, reproduces the
    /// original file byte-for-byte, and that redo reproduces the post-edit bytes byte-for-byte.
    /// Never writes to disk: files are only read, and the mutated document is only ever
    /// serialized to an in-memory byte array via ToBytes().
    /// </summary>
    public class EditingTestsRoundTrip
    {
        [Test]
        public void UndoAfterTransaction_RestoresOriginalBytesExactly()
        {
            var path = CorpusFiles.FindFileWithMutableInteger("utc");
            var original = File.ReadAllBytes(path);
            var document = JsonGffDocument.Parse(original);

            using var session = new DocumentSession(path, document);

            var field = CorpusFiles.FindFirstMutableInteger(document.Root);
            field.Should().NotBeNull("the sample file should contain at least one mutable integer field");

            var before = field!.GetInteger();

            using (var tx = session.Begin("bump integer"))
            {
                field.SetInteger(before == 0 ? 1 : 0);
            }

            var mutated = document.ToBytes();
            mutated.AsSpan().SequenceEqual(original).Should().BeFalse("the mutation must change the serialized bytes");

            session.UndoStack.CanUndo.Should().BeTrue();
            session.UndoStack.Undo();

            var afterUndo = document.ToBytes();
            afterUndo.AsSpan().SequenceEqual(original).Should().BeTrue(
                "undo must restore the document to the exact original bytes");

            session.UndoStack.CanRedo.Should().BeTrue();
            session.UndoStack.Redo();

            var afterRedo = document.ToBytes();
            afterRedo.AsSpan().SequenceEqual(mutated).Should().BeTrue(
                "redo must reproduce the exact post-edit bytes");
        }

        [Test]
        public void UndoAfterStringFieldEdit_RestoresOriginalBytesExactly()
        {
            var path = CorpusFiles.FindFileWithMutableString("utc");
            var original = File.ReadAllBytes(path);
            var document = JsonGffDocument.Parse(original);

            using var session = new DocumentSession(path, document);

            var field = CorpusFiles.FindFirstMutableString(document.Root);
            field.Should().NotBeNull("the sample file should contain at least one mutable string field");

            var before = field!.GetString();

            using (var tx = session.Begin("change string"))
            {
                field.SetString(before + "_edited");
            }

            document.ToBytes().AsSpan().SequenceEqual(original).Should().BeFalse();

            session.UndoStack.Undo();
            document.ToBytes().AsSpan().SequenceEqual(original).Should().BeTrue(
                "undo must restore the exact original bytes for a string field edit too");
        }
    }

    /// <summary>Shared corpus lookup helpers for the WP1.5 editing tests.</summary>
    internal static class CorpusFiles
    {
        public static string FindFileWithMutableInteger(string folder)
        {
            foreach (var file in EnumerateFolder(folder))
            {
                var document = JsonGffDocument.Parse(File.ReadAllBytes(file));
                if (FindFirstMutableInteger(document.Root) != null)
                    return file;
            }

            throw new InvalidOperationException($"No file with a mutable integer field found under '{folder}'.");
        }

        public static string FindFileWithMutableString(string folder)
        {
            foreach (var file in EnumerateFolder(folder))
            {
                var document = JsonGffDocument.Parse(File.ReadAllBytes(file));
                if (FindFirstMutableString(document.Root) != null)
                    return file;
            }

            throw new InvalidOperationException($"No file with a mutable string field found under '{folder}'.");
        }

        public static string FindFileWithEmptyLocStringField(string folder, string fieldName)
        {
            foreach (var file in EnumerateFolder(folder))
            {
                var document = JsonGffDocument.Parse(File.ReadAllBytes(file));
                if (document.Root.TryGet(fieldName, out var field)
                    && field.Type == GffFieldType.CExoLocString
                    && field.LocStringEntries!.Count == 0)
                {
                    return file;
                }
            }

            throw new InvalidOperationException(
                $"No file with an empty '{fieldName}' cexolocstring field found under '{folder}'.");
        }

        public static string FindFileWithListOfSize(string folder, int minimumElementCount, out string listFieldName)
        {
            foreach (var file in EnumerateFolder(folder))
            {
                var document = JsonGffDocument.Parse(File.ReadAllBytes(file));
                var name = FindFirstListFieldName(document.Root, minimumElementCount);
                if (name != null)
                {
                    listFieldName = name;
                    return file;
                }
            }

            throw new InvalidOperationException(
                $"No file with a list field of at least {minimumElementCount} elements found under '{folder}'.");
        }

        public static JsonGffField? FindFirstMutableInteger(JsonGffStruct target)
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

        public static JsonGffField? FindFirstMutableString(JsonGffStruct target)
        {
            foreach (var (_, field) in target.Entries)
            {
                if (field.Type == GffFieldType.CExoString)
                    return field;
            }

            return null;
        }

        public static string? FindFirstListFieldName(JsonGffStruct target, int minimumElementCount)
        {
            foreach (var (name, field) in target.Entries)
            {
                if (field.Type == GffFieldType.List && field.Elements!.Count >= minimumElementCount)
                    return name;
            }

            return null;
        }

        private static IEnumerable<string> EnumerateFolder(string folder)
        {
            var path = Path.Combine(CorpusLocator.ModuleDirectory, folder);
            return Directory.EnumerateFiles(path, "*.json");
        }
    }
}
