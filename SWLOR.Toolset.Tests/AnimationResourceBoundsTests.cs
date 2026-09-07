using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Bif;
using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.Tests;

public class AnimationResourceBoundsTests
{
    private string _folder = null!;
    private const int PayloadSize = 8 * 1024 * 1024;
    [SetUp] public void Setup() { _folder = Path.Combine(Path.GetTempPath(), "swlor-resource-bounds-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(_folder); }
    [TearDown] public void Teardown() { Directory.Delete(_folder, true); }

    [TestCase(false)] [TestCase(true)]
    public void MountedHakResourcesRejectOversizeBeforeAllocationWithoutFallingThrough(bool packed)
    {
        var top = Path.Combine(_folder, "top"); var lower = Path.Combine(_folder, "lower");
        Directory.CreateDirectory(top); Directory.CreateDirectory(lower);
        File.WriteAllText(Path.Combine(lower, "sample.mdl"), "shadowed base resource");
        string layer;
        if (packed)
        {
            layer = Path.Combine(_folder, "top.hak");
            ResourceIndexTests.WriteSingleResourceHak(layer, "sample", "mdl", []);
            using var stream = File.OpenWrite(layer);
            stream.Position = 188; stream.Write(BitConverter.GetBytes((uint)PayloadSize));
            stream.SetLength(192L + PayloadSize);
        }
        else
        {
            layer = top;
            using var stream = File.Create(Path.Combine(top, "sample.mdl")); stream.SetLength(PayloadSize);
        }
        var index = new ResourceIndex(null, [new("top", layer), new("lower", lower)]);
        index.TryLookup(ResourceIdentity.FromFileName("sample.mdl"), out var resource).Should().BeTrue();
        AssertBounded(resource);
        resource.GetBytes(PayloadSize).Should().HaveCount(PayloadSize);
        resource.Provenance.LayerName.Should().Be("top");
    }

    [Test] public void MountedBaseGameResourcesApplyTheCallerLimitBeforeExtraction()
    {
        var data = Path.Combine(_folder, "data"); Directory.CreateDirectory(data);
        WriteBif(Path.Combine(data, "sample.bif"));
        using (var stream = File.Create(Path.Combine(data, "nwn_base.key")))
        using (var writer = new BinaryWriter(stream, Encoding.ASCII))
        {
            writer.Write("KEY "u8); writer.Write("V1  "u8);
            writer.Write(1u); writer.Write(1u); writer.Write(64u); writer.Write(76u);
            writer.Write(126u); writer.Write(1u); writer.Write(new byte[32]);
            const string filename = @"data\sample.bif";
            writer.Write((uint)(PayloadSize + 36)); writer.Write(98u);
            writer.Write((ushort)filename.Length); writer.Write((ushort)1);
            writer.Write("sample"u8); writer.Write(new byte[10]); writer.Write((ushort)2002); writer.Write(0u);
            writer.Write(Encoding.ASCII.GetBytes(filename));
        }
        var index = new ResourceIndex(KeyBifCatalog.Load(data), []);
        index.TryLookup(ResourceIdentity.FromFileName("sample.mdl"), out var resource).Should().BeTrue();
        AssertBounded(resource);
        resource.GetBytes(PayloadSize).Should().HaveCount(PayloadSize);
    }

    [TestCase(false)] [TestCase(true)]
    public void BifExtractionLimitAlsoAppliesToMemoryAndFileBackedArchives(bool memory)
    {
        var path = Path.Combine(_folder, "sample.bif"); WriteBif(path);
        var bif = memory ? BifReader.Read(File.ReadAllBytes(path)) : BifReader.ReadMetadataOnly(path);
        var before = GC.GetAllocatedBytesForCurrentThread();
        Action read = () => bif.ExtractVariableResource(0, 1024);
        read.Should().Throw<Exception>().Where(e => e.Message.Contains("1024-byte"));
        (GC.GetAllocatedBytesForCurrentThread() - before).Should().BeLessThan(1024 * 1024);
    }

    private static void AssertBounded(ResourceHandle resource)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        Action read = () => resource.GetBytes(1024);
        read.Should().Throw<Exception>().Where(e => e.GetBaseException().Message.Contains("1024-byte"));
        (GC.GetAllocatedBytesForCurrentThread() - before).Should().BeLessThan(1024 * 1024,
            "the winning resource must be rejected before its eight-megabyte payload is allocated");
    }

    private static void WriteBif(string path)
    {
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.Write("BIFF"u8); writer.Write("V1  "u8); writer.Write(1u); writer.Write(0u); writer.Write(20u);
        writer.Write(0u); writer.Write(36u); writer.Write((uint)PayloadSize); writer.Write(2002u);
        writer.Flush(); stream.SetLength(36L + PayloadSize);
    }
}
