using System.Buffers.Binary;
using Avalonia;
using Avalonia.Headless.NUnit;
using Avalonia.Media.Imaging;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editors.Behaviors;
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

        [AvaloniaTest]
        public async Task NeverwinterPortraitChoiceHidesReservedBottomStrip()
        {
            var resourceDirectory = Path.Combine(
                Path.GetTempPath(),
                $"swlor-choice-preview-{Guid.NewGuid():N}");
            Directory.CreateDirectory(resourceDirectory);

            try
            {
                File.WriteAllBytes(
                    Path.Combine(resourceDirectory, "portrait.tga"),
                    OpaqueTga(width: 64, height: 128));
                var resources = new ResourceIndex(
                    baseLayer: null,
                    hakLayersInOrder:
                    [
                        new ResourceIndex.HakLayer("fixture", resourceDirectory)
                    ]);
                var service = new ChoicePreviewService(resources);
                var portrait = new BehaviorChoice(1, "Portrait", "portrait")
                {
                    ImageCrop = BehaviorChoiceImageCrop.NeverwinterPortrait
                };

                Bitmap? cropped = null;
                await service.RequestAsync(portrait, maxWidth: 192, bitmap => cropped = bitmap);
                var ordinary = await service.ResolveAsync("portrait", maxWidth: 192);

                cropped.Should().NotBeNull();
                cropped!.PixelSize.Should().Be(
                    new PixelSize(64, 100),
                    "NWN reserves the bottom 28 pixels of a medium portrait for engine-only data");
                ordinary.Should().NotBeNull();
                ordinary!.PixelSize.Should().Be(
                    new PixelSize(64, 128),
                    "cropping must only apply to portrait choices");
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

        private static byte[] OpaqueTga(ushort width, ushort height)
        {
            var bytes = new byte[18 + width * height * 4];
            bytes[2] = 2;
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(12, 2), width);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(14, 2), height);
            bytes[16] = 32;
            bytes[17] = 0x28; // Top-left origin, eight alpha bits.

            for (var offset = 18; offset < bytes.Length; offset += 4)
            {
                bytes[offset] = 30;
                bytes[offset + 1] = 60;
                bytes[offset + 2] = 90;
                bytes[offset + 3] = 255;
            }

            return bytes;
        }
    }
}
