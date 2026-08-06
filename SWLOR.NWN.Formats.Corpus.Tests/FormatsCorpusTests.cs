// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Common;
using SWLOR.NWN.Formats.Gff;
using SWLOR.NWN.Formats.Key;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.NWN.Formats.Plt;
using SWLOR.NWN.Formats.Tga;
using SWLOR.NWN.Formats.Tlk;
using SWLOR.NWN.Formats.TwoDA;

namespace SWLOR.NWN.Formats.Corpus.Tests;

[Category(MdlCorpusScopeTests.CorpusCategory)]
public sealed class FormatsCorpusTests
{
    [Test]
    public void EveryInstalledKeyAndReferencedBifIsReadable()
    {
        var keyPaths = Directory.EnumerateFiles(
                LicensedCorpus.DataDirectory,
                "*.key",
                SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        keyPaths.Should().NotBeEmpty();

        var declaredResources = 0;
        var declaredBifs = 0;
        using var inputHash = new CorpusHash();
        using var semanticHash = new CorpusHash();
        foreach (var path in keyPaths)
        {
            var bytes = File.ReadAllBytes(path);
            CorpusSemanticHash.AddInput(inputHash, $"key:{Path.GetFileName(path)}", bytes);
            var key = KeyReader.Read(bytes);
            key.ResourceEntries.Should().NotBeEmpty();
            declaredResources += key.ResourceEntries.Count;
            declaredBifs += key.BifEntries.Count;
        }

        var resources = LicensedCorpus.KeyResources();
        resources.Should().HaveCount(declaredResources);
        var uniqueBifs = resources.Select(resource => resource.BifPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var sample = EvenSample(resources, 1_000).ToList();
        foreach (var first in resources
                     .GroupBy(resource => resource.BifPath, StringComparer.OrdinalIgnoreCase)
                     .Select(group => group.First()))
        {
            if (!sample.Contains(first))
                sample.Add(first);
        }

        var executed = 0;
        var failures = new List<string>();
        foreach (var resource in sample)
        {
            try
            {
                var bytes = LicensedCorpus.Read(resource);
                bytes.Should().HaveCount(checked((int)resource.Size));
                var identity =
                    $"{resource.KeyName}:{resource.ResRef}:{resource.ResourceType}:{Path.GetFileName(resource.BifPath)}";
                CorpusSemanticHash.AddInput(inputHash, identity, bytes);
                semanticHash.AddString(identity);
                semanticHash.AddUInt32(resource.Size);
                semanticHash.AddBytes(SHA256.HashData(bytes));
                executed++;
            }
            catch (Exception ex)
            {
                failures.Add(
                    $"{resource.KeyName}:{resource.ResRef}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        TestContext.Out.WriteLine(
            $"KEY/BIF corpus keys={keyPaths.Length} declared-bifs={declaredBifs} " +
            $"unique-bifs={uniqueBifs.Length} resources={resources.Count} requested={sample.Count} " +
            $"executed={executed} failed={failures.Count} skipped=0 " +
            $"input-sha256={inputHash.Finish()} semantic-sha256={semanticHash.Finish()}");
        if (failures.Count > 0)
            TestContext.Out.WriteLine(string.Join(Environment.NewLine, failures.Take(25)));
        failures.Should().BeEmpty();
        executed.Should().Be(sample.Count);
    }

    [Test]
    public void EveryRepositoryTwoDaAndEveryBaseGameTwoDaIsAccountedFor()
    {
        var requested = 0;
        var executed = 0;
        var failures = new List<string>();
        var expectedInvalid = new List<string>();
        using var inputHash = new CorpusHash();
        using var semanticHash = new CorpusHash();

        foreach (var path in LicensedCorpus.HakSourceDirectories()
                     .SelectMany(directory => Directory.EnumerateFiles(directory, "*.2da", SearchOption.AllDirectories))
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            requested++;
            var identity = "hak:" + Path.GetRelativePath(LicensedCorpus.HaksRoot, path)
                .Replace('\\', '/');
            var bytes = File.ReadAllBytes(path);
            CorpusSemanticHash.AddInput(inputHash, identity, bytes);
            try
            {
                var table = TwoDAReader.Read(bytes);
                semanticHash.AddString(identity);
                CorpusSemanticHash.AddTwoDa(semanticHash, table);
                executed++;
            }
            catch (NwnFormatException ex) when (
                Path.GetFileName(path).Equals("iprp_spells past.2da", StringComparison.OrdinalIgnoreCase))
            {
                expectedInvalid.Add($"{path}: {ex.Message}");
                semanticHash.AddString(identity);
                semanticHash.AddString("expected-invalid");
                executed++;
            }
            catch (Exception ex)
            {
                failures.Add($"{path}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        var baseResources = LicensedCorpus.KeyResources(ResourceTypes.FromExtension("2da"));
        foreach (var resource in baseResources)
        {
            requested++;
            var identity = $"{resource.KeyName}:{resource.ResRef}.2da";
            var bytes = LicensedCorpus.Read(resource);
            CorpusSemanticHash.AddInput(inputHash, identity, bytes);
            try
            {
                var table = TwoDAReader.Read(bytes);
                semanticHash.AddString(identity);
                CorpusSemanticHash.AddTwoDa(semanticHash, table);
                executed++;
            }
            catch (Exception ex)
            {
                failures.Add(
                    $"{resource.KeyName}:{resource.ResRef}.2da: {ex.GetType().Name}: {ex.Message}");
            }
        }

        TestContext.Out.WriteLine(
            $"2DA corpus requested={requested} executed={executed} failed={failures.Count} skipped=0 " +
            $"expected-invalid={expectedInvalid.Count} input-sha256={inputHash.Finish()} " +
            $"semantic-sha256={semanticHash.Finish()}");
        if (expectedInvalid.Count > 0)
            TestContext.Out.WriteLine($"Expected invalid 2DA: {expectedInvalid[0]}");
        if (failures.Count > 0)
            TestContext.Out.WriteLine(string.Join(Environment.NewLine, failures.Take(25)));

        requested.Should().BeGreaterThan(0);
        executed.Should().Be(requested);
        failures.Should().BeEmpty();
    }

    [Test]
    public void RequiredTalkTablesParseWithNonzeroContent()
    {
        var paths = Directory.EnumerateFiles(LicensedCorpus.HaksRoot, "*.tlk", SearchOption.AllDirectories)
            .Append(Path.Combine(LicensedCorpus.InstallRoot, "lang", "en", "data", "dialog.tlk"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        paths.Should().NotBeEmpty();
        paths = paths.OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase).ToArray();

        var failures = new List<string>();
        var executed = 0;
        using var inputHash = new CorpusHash();
        using var semanticHash = new CorpusHash();
        foreach (var path in paths)
        {
            try
            {
                File.Exists(path).Should().BeTrue($"required TLK '{path}' must exist");
                var bytes = File.ReadAllBytes(path);
                var identity = Path.GetFileName(path);
                CorpusSemanticHash.AddInput(inputHash, identity, bytes);
                var file = TlkReader.Read(bytes);
                file.Entries.Should().NotBeEmpty();
                file.Entries.Any(entry => !string.IsNullOrEmpty(entry.Text)).Should().BeTrue();
                semanticHash.AddString(identity);
                semanticHash.AddUInt32(file.LanguageId);
                semanticHash.AddInt32(file.Entries.Count);
                foreach (var entry in file.Entries)
                {
                    semanticHash.AddUInt32(entry.Flags);
                    semanticHash.AddString(entry.SoundResRef);
                    semanticHash.AddSingle(entry.SoundLength);
                    semanticHash.AddString(entry.Text);
                }
                executed++;
                TestContext.Out.WriteLine(
                    $"TLK corpus file={Path.GetFileName(path)} entries={file.Entries.Count}");
            }
            catch (Exception ex)
            {
                failures.Add($"{path}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        TestContext.Out.WriteLine(
            $"TLK corpus requested={paths.Length} executed={executed} failed={failures.Count} skipped=0 " +
            $"input-sha256={inputHash.Finish()} semantic-sha256={semanticHash.Finish()}");
        failures.Should().BeEmpty();
        executed.Should().Be(paths.Length);
    }

    [Test]
    public void EveryBaseGameItpParsesAsGff()
    {
        var resources = LicensedCorpus.KeyResources(ResourceTypes.FromExtension("itp"));
        resources.Should().NotBeEmpty();
        var failures = new List<string>();
        var executed = 0;
        using var inputHash = new CorpusHash();
        using var semanticHash = new CorpusHash();
        foreach (var resource in resources)
        {
            try
            {
                var bytes = LicensedCorpus.Read(resource);
                var identity = $"{resource.KeyName}:{resource.ResRef}.itp";
                CorpusSemanticHash.AddInput(inputHash, identity, bytes);
                var file = GffReader.Read(bytes);
                file.FileVersion.Should().Be("V3.2");
                semanticHash.AddString(identity);
                CorpusSemanticHash.AddGff(semanticHash, file);
                executed++;
            }
            catch (Exception ex)
            {
                failures.Add(
                    $"{resource.KeyName}:{resource.ResRef}.itp: {ex.GetType().Name}: {ex.Message}");
            }
        }

        TestContext.Out.WriteLine(
            $"GFF/ITP corpus requested={resources.Count} executed={executed} failed={failures.Count} skipped=0 " +
            $"input-sha256={inputHash.Finish()} semantic-sha256={semanticHash.Finish()}");
        if (failures.Count > 0)
            TestContext.Out.WriteLine(string.Join(Environment.NewLine, failures.Take(25)));
        failures.Should().BeEmpty();
        executed.Should().Be(resources.Count);
    }

    [TestCase("tga", 300)]
    [TestCase("plt", 300)]
    public void TextureReadersParseDeterministicLicensedSamples(string extension, int maximumPerSource)
    {
        var loose = LicensedCorpus.HakSourceDirectories()
            .SelectMany(directory => Directory.EnumerateFiles(directory, $"*.{extension}", SearchOption.AllDirectories))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var archived = LicensedCorpus.KeyResources(ResourceTypes.FromExtension(extension))
            .OrderBy(resource => resource.KeyName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(resource => resource.ResRef, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var looseSample = EvenSample(loose, maximumPerSource).ToList();
        if (extension.Equals("plt", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var corrupt in loose.Where(path => IsKnownInvalidPlt(path)))
            {
                if (!looseSample.Contains(corrupt, StringComparer.OrdinalIgnoreCase))
                    looseSample.Add(corrupt);
            }
        }
        var archiveSample = EvenSample(archived, maximumPerSource);
        var requested = looseSample.Count + archiveSample.Count;
        requested.Should().BeGreaterThan(0);
        var executed = 0;
        var failures = new List<string>();
        var expectedInvalid = new List<string>();
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        using var inputHash = new CorpusHash();

        foreach (var path in looseSample)
        {
            var bytes = File.ReadAllBytes(path);
            var identity = "hak:" + Path.GetRelativePath(LicensedCorpus.HaksRoot, path)
                .Replace('\\', '/');
            CorpusSemanticHash.AddInput(inputHash, identity, bytes);
            try
            {
                VerifyTexture(extension, bytes, hash);
                executed++;
            }
            catch (NwnFormatException ex) when (
                extension.Equals("plt", StringComparison.OrdinalIgnoreCase) && IsKnownInvalidPlt(path))
            {
                expectedInvalid.Add($"{path}: {ex.Message}");
                executed++;
            }
            catch (Exception ex)
            {
                failures.Add($"{path}: {ex.GetType().Name}: {ex.Message}");
            }
        }
        foreach (var resource in archiveSample)
        {
            var bytes = LicensedCorpus.Read(resource);
            var identity = $"{resource.KeyName}:{resource.ResRef}.{extension}";
            CorpusSemanticHash.AddInput(inputHash, identity, bytes);
            try
            {
                VerifyTexture(extension, bytes, hash);
                executed++;
            }
            catch (Exception ex)
            {
                failures.Add(
                    $"{resource.KeyName}:{resource.ResRef}.{extension}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        var semanticHash = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
        TestContext.Out.WriteLine(
            $"{extension.ToUpperInvariant()} corpus available-loose={loose.Length} " +
            $"available-archive={archived.Length} requested={requested} executed={executed} " +
            $"failed={failures.Count} skipped=0 expected-invalid={expectedInvalid.Count} " +
            $"input-sha256={inputHash.Finish()} semantic-sha256={semanticHash}");
        if (expectedInvalid.Count > 0)
            TestContext.Out.WriteLine(string.Join(Environment.NewLine, expectedInvalid));
        if (failures.Count > 0)
            TestContext.Out.WriteLine(string.Join(Environment.NewLine, failures.Take(25)));
        failures.Should().BeEmpty();
        executed.Should().Be(requested);
    }

    [Test]
    public void BinaryMdlReaderParsesRepresentativeLicensedModels()
    {
        var available = LicensedCorpus.KeyResources(ResourceTypes.FromExtension("mdl"));
        var binaryAvailable = available
            .Where(resource => BitConverter.ToUInt32(LicensedCorpus.ReadPrefix(resource, 4)) == 0)
            .ToArray();
        var binary = EvenSample(binaryAvailable, 600);
        binary.Should().HaveCount(600);

        var failures = new List<string>();
        var executed = 0;
        var meshes = 0;
        var skins = 0;
        var emitters = 0;
        var animations = 0;
        using var inputHash = new CorpusHash();
        using var semanticHash = new CorpusHash();
        foreach (var resource in binary)
        {
            try
            {
                var bytes = LicensedCorpus.Read(resource);
                var identity = $"{resource.KeyName}:{resource.ResRef}.mdl";
                CorpusSemanticHash.AddInput(inputHash, identity, bytes);
                var model = new MdlReader().Parse(bytes);
                semanticHash.AddString(identity);
                CorpusSemanticHash.AddModel(semanticHash, model);
                var nodes = EnumerateNodes(model).ToArray();
                meshes += nodes.OfType<MdlTrimeshNode>().Count();
                skins += nodes.OfType<MdlSkinmeshNode>().Count();
                emitters += nodes.OfType<MdlEmitterNode>().Count();
                animations += model.Animations.Count;
                executed++;
            }
            catch (Exception ex)
            {
                failures.Add(
                    $"{resource.KeyName}:{resource.ResRef}.mdl: {ex.GetType().Name}: {ex.Message}");
            }
        }

        TestContext.Out.WriteLine(
            $"Binary MDL corpus available={available.Count} binary={binaryAvailable.Length} " +
            $"requested={binary.Count} executed={executed} " +
            $"failed={failures.Count} skipped=0 meshes={meshes} skins={skins} emitters={emitters} " +
            $"animations={animations} input-sha256={inputHash.Finish()} " +
            $"semantic-sha256={semanticHash.Finish()}");
        if (failures.Count > 0)
            TestContext.Out.WriteLine(string.Join(Environment.NewLine, failures.Take(25)));
        failures.Should().BeEmpty();
        executed.Should().Be(binary.Count);
        meshes.Should().BeGreaterThan(0);
    }

    [Test]
    public void EveryAsciiMdlParsesWithoutSkipping()
    {
        var resources = LicensedCorpus.KeyResources(ResourceTypes.FromExtension("mdl"))
            .Where(resource => BitConverter.ToUInt32(LicensedCorpus.ReadPrefix(resource, 4)) != 0)
            .Select(resource => (
                Identity: $"{resource.KeyName}:{resource.ResRef}.mdl",
                Read: (Func<byte[]>)(() => LicensedCorpus.Read(resource))))
            .ToList();
        foreach (var path in LicensedCorpus.HakSourceDirectories()
                     .SelectMany(directory => Directory.EnumerateFiles(directory, "*.mdl", SearchOption.AllDirectories))
                     .Where(path => BitConverter.ToUInt32(LicensedCorpus.ReadPrefix(path, 4)) != 0))
        {
            var captured = path;
            var identity = "hak:" + Path.GetRelativePath(LicensedCorpus.HaksRoot, captured)
                .Replace('\\', '/');
            resources.Add((identity, () => File.ReadAllBytes(captured)));
        }
        resources = resources.OrderBy(resource => resource.Identity, StringComparer.OrdinalIgnoreCase).ToList();
        resources.Should().NotBeEmpty();
        var semanticSample = EvenSample(resources, 600);
        var sampledIdentities = semanticSample
            .Select(resource => resource.Identity)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var failures = new List<string>();
        var executed = 0;
        var meshes = 0;
        var skins = 0;
        var emitters = 0;
        var animations = 0;
        using var inputHash = new CorpusHash();
        using var semanticHash = new CorpusHash();
        foreach (var resource in resources)
        {
            try
            {
                var bytes = resource.Read();
                var model = new MdlReader().Parse(bytes);
                if (sampledIdentities.Contains(resource.Identity))
                {
                    CorpusSemanticHash.AddInput(inputHash, resource.Identity, bytes);
                    semanticHash.AddString(resource.Identity);
                    CorpusSemanticHash.AddModel(semanticHash, model);
                }
                var nodes = EnumerateNodes(model).ToArray();
                meshes += nodes.OfType<MdlTrimeshNode>().Count();
                skins += nodes.OfType<MdlSkinmeshNode>().Count();
                emitters += nodes.OfType<MdlEmitterNode>().Count();
                animations += model.Animations.Count;
                executed++;
            }
            catch (Exception ex)
            {
                failures.Add($"{resource.Identity}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        TestContext.Out.WriteLine(
            $"ASCII MDL corpus requested={resources.Count} executed={executed} " +
            $"failed={failures.Count} skipped=0 meshes={meshes} skins={skins} emitters={emitters} " +
            $"animations={animations} semantic-sample={semanticSample.Count} " +
            $"input-sha256={inputHash.Finish()} semantic-sha256={semanticHash.Finish()}");
        if (failures.Count > 0)
            TestContext.Out.WriteLine(string.Join(Environment.NewLine, failures.Take(50)));
        failures.Should().BeEmpty();
        executed.Should().Be(resources.Count);
        meshes.Should().BeGreaterThan(0);
    }

    [Test]
    public void FullModuleJsonInputManifestIsDeterministic()
    {
        var moduleRoot = Path.Combine(LicensedCorpus.RepositoryRoot, "Module");
        var paths = Directory.EnumerateFiles(moduleRoot, "*.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        paths.Should().NotBeEmpty();

        long totalBytes = 0;
        using var inputHash = new CorpusHash();
        foreach (var path in paths)
        {
            var bytes = File.ReadAllBytes(path);
            totalBytes = checked(totalBytes + bytes.LongLength);
            var identity = Path.GetRelativePath(moduleRoot, path).Replace('\\', '/');
            CorpusSemanticHash.AddInput(inputHash, identity, bytes);
        }

        TestContext.Out.WriteLine(
            $"Module JSON corpus requested={paths.Length} executed={paths.Length} failed=0 skipped=0 " +
            $"bytes={totalBytes} input-sha256={inputHash.Finish()}");
    }

    private static void VerifyTexture(string extension, byte[] bytes, IncrementalHash hash)
    {
        if (extension.Equals("tga", StringComparison.OrdinalIgnoreCase))
        {
            var image = TgaReader.Read(bytes);
            image.Width.Should().BeGreaterThan(0);
            image.Height.Should().BeGreaterThan(0);
            image.Pixels.Should().HaveCount(checked(image.Width * image.Height * 4));
            hash.AppendData(BitConverter.GetBytes(image.Width));
            hash.AppendData(BitConverter.GetBytes(image.Height));
            hash.AppendData(image.Pixels);
            return;
        }

        var plt = PltReader.Read(bytes);
        plt.Width.Should().BeGreaterThan(0);
        plt.Height.Should().BeGreaterThan(0);
        plt.Pixels.Should().HaveCount(checked(plt.Width * plt.Height));
        hash.AppendData(BitConverter.GetBytes(plt.Width));
        hash.AppendData(BitConverter.GetBytes(plt.Height));
        foreach (var pixel in plt.Pixels)
        {
            hash.AppendData(new[] { pixel.Intensity, pixel.Layer });
        }
    }

    private static IReadOnlyList<T> EvenSample<T>(IReadOnlyList<T> source, int maximum)
    {
        if (source.Count <= maximum)
            return source.ToArray();
        var result = new T[maximum];
        for (var index = 0; index < maximum; index++)
            result[index] = source[(int)((long)index * source.Count / maximum)];
        return result;
    }

    private static bool IsKnownInvalidPlt(string path)
    {
        var name = Path.GetFileName(path);
        return name.Equals("ipf_shol197.plt", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("ipf_shor197.plt", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<MdlNode> EnumerateNodes(MdlModel model)
    {
        if (model.GeometryRoot != null)
        {
            foreach (var node in EnumerateTree(model.GeometryRoot))
                yield return node;
        }
        foreach (var animation in model.Animations)
        {
            if (animation.GeometryRoot == null)
                continue;
            foreach (var node in EnumerateTree(animation.GeometryRoot))
                yield return node;
        }
    }

    private static IEnumerable<MdlNode> EnumerateTree(MdlNode root)
    {
        var pending = new Stack<MdlNode>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var node = pending.Pop();
            yield return node;
            for (var index = node.Children.Count - 1; index >= 0; index--)
                pending.Push(node.Children[index]);
        }
    }
}
