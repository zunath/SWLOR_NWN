// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Common;

namespace SWLOR.NWN.Formats.Corpus.Tests;

[Category(MdlCorpusScopeTests.CorpusCategory)]
public sealed class AsciiMdlScopeInventoryTests
{
    [Test]
    public void RequiredAsciiMdlGrammarSurfaceIsInventoried()
    {
        var resources = LicensedCorpus.KeyResources(ResourceTypes.FromExtension("mdl"))
            .Where(resource =>
                BinaryPrimitives.ReadUInt32LittleEndian(LicensedCorpus.ReadPrefix(resource, 4)) != 0)
            .Select(resource => (
                Identity: $"{resource.KeyName}:{resource.ResRef}.mdl",
                Read: (Func<byte[]>)(() => LicensedCorpus.Read(resource))))
            .ToList();
        foreach (var path in LicensedCorpus.HakSourceDirectories()
                     .SelectMany(directory => Directory.EnumerateFiles(directory, "*.mdl", SearchOption.AllDirectories))
                     .Where(IsAsciiMdl))
        {
            var captured = path;
            resources.Add((captured, () => File.ReadAllBytes(captured)));
        }
        resources.Should().NotBeEmpty();

        var directives = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var nodeTypes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var requested = resources.Count;
        var executed = 0;
        var failures = new List<string>();
        foreach (var resource in resources)
        {
            try
            {
                var text = Encoding.Latin1.GetString(resource.Read());
                using var lines = new StringReader(text);
                while (lines.ReadLine() is { } line)
                {
                    var trimmed = line.Trim();
                    if (trimmed.Length == 0 || trimmed[0] == '#')
                        continue;
                    var tokens = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length == 0 || !char.IsLetter(tokens[0][0]))
                        continue;

                    Increment(directives, tokens[0]);
                    if (tokens[0].Equals("node", StringComparison.OrdinalIgnoreCase) && tokens.Length >= 2)
                        Increment(nodeTypes, tokens[1]);
                }
                executed++;
            }
            catch (Exception ex)
            {
                failures.Add(
                    $"{resource.Identity}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        var directiveSummary = string.Join(
            ", ",
            directives.OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Take(100)
                .Select(pair => $"{pair.Key}={pair.Value}"));
        var nodeSummary = string.Join(
            ", ",
            nodeTypes.OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair => $"{pair.Key}={pair.Value}"));
        TestContext.Out.WriteLine(
            $"ASCII MDL inventory requested={requested} executed={executed} " +
            $"failed={failures.Count} skipped=0 unique-directives={directives.Count}");
        TestContext.Out.WriteLine($"ASCII MDL node types: {nodeSummary}");
        TestContext.Out.WriteLine($"ASCII MDL top directives: {directiveSummary}");
        if (failures.Count > 0)
            TestContext.Out.WriteLine(string.Join(Environment.NewLine, failures.Take(25)));

        failures.Should().BeEmpty();
        executed.Should().Be(requested);
        nodeTypes.Should().ContainKey("trimesh");
        directives.Should().ContainKeys(
            "newmodel",
            "setsupermodel",
            "beginmodelgeom",
            "endmodelgeom",
            "donemodel");
    }

    private static void Increment(IDictionary<string, int> counts, string key)
    {
        counts.TryGetValue(key, out var count);
        counts[key] = count + 1;
    }

    private static bool IsAsciiMdl(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        Span<byte> prefix = stackalloc byte[4];
        return stream.Read(prefix) == 4 && BinaryPrimitives.ReadUInt32LittleEndian(prefix) != 0;
    }
}
