using System.Buffers.Binary;
using Avalonia;
using Avalonia.Headless.NUnit;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Editors.Behaviors;

namespace SWLOR.Toolset.Tests
{
    public class ChoicePreviewServiceTests
    {
        [AvaloniaTest]
        public async Task TransparentCanvasCanBeCroppedWithoutChangingOrdinaryPreview()
        {
            var resourceDirectory = Path.Combine(
                Path.GetTempPath(),
                $"swlor-choice-preview-{Guid.NewGuid():N}");
            Directory.CreateDirectory(resourceDirectory);

            try
            {
                File.WriteAllBytes(
                    Path.Combine(resourceDirectory, "part.tga"),
                    TransparentPartTga(width: 4, height: 6));
                var resources = new ResourceIndex(
                    baseLayer: null,
                    hakLayersInOrder:
                    [
                        new ResourceIndex.HakLayer("fixture", resourceDirectory)
                    ]);
                var service = new ChoicePreviewService(resources);

                var ordinary = await service.ResolveAsync("part", maxWidth: 192);
                var cropped = await service.ResolveAsync(
                    "part",
                    maxWidth: 192,
                    cropTransparentCanvas: true);

                ordinary.Should().NotBeNull();
                ordinary!.PixelSize.Should().Be(new PixelSize(4, 6));
                cropped.Should().NotBeNull();
                cropped!.PixelSize.Should().Be(
                    new PixelSize(2, 2),
                    "the two-by-two visible part should fill its thumbnail instead of retaining the full canvas");
            }
            finally
            {
                Directory.Delete(resourceDirectory, recursive: true);
            }
        }

        private static byte[] TransparentPartTga(ushort width, ushort height)
        {
            var bytes = new byte[18 + width * height * 4];
            bytes[2] = 2;
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(12, 2), width);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(14, 2), height);
            bytes[16] = 32;
            bytes[17] = 0x28; // Top-left origin, eight alpha bits.

            for (var y = 2; y <= 3; y++)
            {
                for (var x = 1; x <= 2; x++)
                {
                    var offset = 18 + (y * width + x) * 4;
                    bytes[offset] = 30;
                    bytes[offset + 1] = 60;
                    bytes[offset + 2] = 90;
                    bytes[offset + 3] = 255;
                }
            }

            return bytes;
        }
    }
}
