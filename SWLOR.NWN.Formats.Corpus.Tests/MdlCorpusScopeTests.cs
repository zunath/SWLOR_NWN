// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Common;

namespace SWLOR.NWN.Formats.Corpus.Tests;

[Category(CorpusCategory)]
public sealed class MdlCorpusScopeTests
{
    public const string CorpusCategory = "LicensedCorpus";

    [Test]
    public void RequiredMdlCorpusFormatsAreReported()
    {
        var sources = new List<(string Source, string Identity, string Path, long Offset)>();

        AddLooseModels(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Neverwinter Nights", "override"),
            "override",
            sources);
        foreach (var directory in LicensedCorpus.HakSourceDirectories())
            AddLooseModels(directory, $"hak-source:{Path.GetFileName(directory)}", sources);
        foreach (var resource in LicensedCorpus.KeyResources(ResourceTypes.FromExtension("mdl")))
        {
            sources.Add((
                $"key:{resource.KeyName}",
                $"{resource.ResRef}.mdl",
                resource.BifPath,
                resource.Offset));
        }

        sources.Should().NotBeEmpty("the required licensed MDL corpus must execute, not silently skip");
        var binary = new List<(string Source, string Identity)>();
        var text = new List<(string Source, string Identity)>();
        var prefix = new byte[4];
        foreach (var item in sources)
        {
            using var stream = new FileStream(item.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
            stream.Seek(item.Offset, SeekOrigin.Begin);
            var read = stream.Read(prefix);
            if (read < prefix.Length)
                throw new InvalidDataException($"Licensed MDL '{item.Identity}' is shorter than four bytes.");

            var target = BinaryPrimitives.ReadUInt32LittleEndian(prefix) == 0 ? binary : text;
            target.Add((item.Source, item.Identity));
        }

        foreach (var group in sources.GroupBy(item => item.Source).OrderBy(group => group.Key))
        {
            var requested = group.Count();
            var executed = group.Count(item =>
                binary.Any(match => match.Source == item.Source && match.Identity == item.Identity) ||
                text.Any(match => match.Source == item.Source && match.Identity == item.Identity));
            TestContext.Out.WriteLine(
                $"MDL corpus source={group.Key} requested={requested} executed={executed} " +
                $"failed=0 skipped=0 binary={group.Count(item => binary.Contains((item.Source, item.Identity)))} " +
                $"ascii={group.Count(item => text.Contains((item.Source, item.Identity)))}");
        }
        TestContext.Out.WriteLine(
            $"MDL corpus total requested={sources.Count} executed={binary.Count + text.Count} " +
            $"failed=0 skipped=0 binary={binary.Count} ascii={text.Count}");
        TestContext.Out.WriteLine(
            $"MDL sample: {string.Join(", ", sources.Take(12).Select(item => $"{item.Source}:{item.Identity}"))}");

        binary.Should().NotBeEmpty();
        text.Should().NotBeEmpty(
            "the scope spike found required ASCII MDLs and the implementation plan records the expansion gate");
    }

    private static void AddLooseModels(
        string directory,
        string source,
        ICollection<(string Source, string Identity, string Path, long Offset)> models)
    {
        if (!Directory.Exists(directory))
            return;
        foreach (var path in Directory.EnumerateFiles(directory, "*.mdl", SearchOption.AllDirectories))
            models.Add((source, Path.GetFileNameWithoutExtension(path), path, 0));
    }

}
