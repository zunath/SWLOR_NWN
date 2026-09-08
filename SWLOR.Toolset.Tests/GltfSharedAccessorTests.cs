using System.Buffers.Binary;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Animation;

namespace SWLOR.Toolset.Tests;

public class GltfSharedAccessorTests
{
    [TestCase(64, true)] [TestCase(67, false)]
    public void SharedTimestampsConsumeTheDecodedComponentBudgetOnlyOnce(int channels, bool fitsBudget)
    {
        var folder = Path.Combine(Path.GetTempPath(), "swlor-shared-gltf-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            const int keys = 30000;
            var payload = new byte[keys * 4 + channels * keys * 16];
            for (var i = 0; i < keys; i++) BinaryPrimitives.WriteSingleLittleEndian(payload.AsSpan(i * 4), i / 100f);
            for (var i = keys * 4; i < payload.Length; i += 16)
                BinaryPrimitives.WriteSingleLittleEndian(payload.AsSpan(i + 12), 1f);
            File.WriteAllBytes(Path.Combine(folder, "motion.bin"), payload);
            var views = new List<object> { new { buffer = 0, byteOffset = 0, byteLength = keys * 4 } };
            var accessors = new List<object> { new { bufferView = 0, componentType = 5126, count = keys, type = "SCALAR" } };
            for (var i = 0; i < channels; i++)
            {
                views.Add(new { buffer = 0, byteOffset = keys * 4 + i * keys * 16, byteLength = keys * 16 });
                accessors.Add(new { bufferView = i + 1, componentType = 5126, count = keys, type = "VEC4" });
            }
            var path = Path.Combine(folder, "motion.gltf");
            File.WriteAllText(path, JsonSerializer.Serialize(new
            {
                asset = new { version = "2.0" }, buffers = new[] { new { byteLength = payload.Length, uri = "motion.bin" } },
                bufferViews = views, accessors,
                nodes = Enumerable.Range(0, channels).Select(i => new { name = "joint" + i }),
                animations = new[] { new { name = "shared times",
                    samplers = Enumerable.Range(0, channels).Select(i => new { input = 0, output = i + 1 }),
                    channels = Enumerable.Range(0, channels).Select(i => new { sampler = i, target = new { node = i, path = "rotation" } }) } }
            }));
            if (fitsBudget)
            {
                var source = GltfAnimationSource.Load(path); var tracks = source.Animations.Single().Tracks;
                tracks.Should().HaveCount(channels);
                foreach (var track in tracks) track.Times.Should().BeSameAs(tracks[0].Times);
                source.Sample(0, 150).Should().HaveCount(channels);
            }
            else
            {
                Action load = () => GltfAnimationSource.Load(path);
                load.Should().Throw<InvalidDataException>().WithMessage("*too many keys*");
            }
        }
        finally { Directory.Delete(folder, true); }
    }
}
