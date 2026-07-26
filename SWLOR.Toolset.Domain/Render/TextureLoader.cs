// SPDX-License-Identifier: GPL-3.0-or-later
//
// The BioWare-DDS conversion below is adapted from Radoub.UI.Services.TextureService
// (https://github.com/LordOfMyatar/Radoub), which is GPL-3.0. That makes this file a derivative
// work: it is GPL-3.0 even though the rest of the SWLOR Toolset's own source is MIT. Dropping the
// Radoub reference would not change that - the logic would have to be clean-roomed from the DDS
// and BioWare header formats instead. See SWLOR.Toolset/LICENSE-NOTICE.md.
using Pfim;
using Radoub.Formats.Plt;
using Radoub.Formats.Tga;
using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>Which decoder produced a <see cref="TextureImage"/>.</summary>
    public enum TextureSourceFormat
    {
        Tga,
        Dds,
        Plt
    }

    /// <summary>Decoded RGBA texture pixel data (top-down, 4 bytes per pixel, as decoded).</summary>
    public sealed class TextureImage
    {
        public required int Width { get; init; }
        public required int Height { get; init; }

        /// <summary>Pixel data in RGBA format (4 bytes per pixel), <see cref="Width"/> * <see cref="Height"/> pixels.</summary>
        public required byte[] Pixels { get; init; }

        public required TextureSourceFormat SourceFormat { get; init; }
    }

    /// <summary>
    /// Resolves a texture resref to decoded RGBA pixels through a <see cref="ResourceIndex"/>,
    /// trying TGA, then DDS, then PLT (NWN's own texture-format resolution order). Headless
    /// Domain-level pipeline stage - no OpenGL/UI dependency - consumed later by both the GL
    /// model preview and the area renderer.
    ///
    /// The DDS path adapts <c>Radoub.UI.Services.TextureService</c>'s BioWare-DDS-to-standard-DDS
    /// conversion (same GPL boundary; Domain must not reference Radoub.UI, so the logic is
    /// duplicated here rather than shared).
    /// </summary>
    public static class TextureLoader
    {
        /// <summary>Try TGA, then DDS, then PLT (with default/neutral palette colors) for <paramref name="resRef"/>.</summary>
        public static TextureImage? Load(
            ResourceIndex index,
            string resRef,
            IReadOnlyDictionary<int, int>? layerColorIndices = null)
        {
            return LoadTga(index, resRef) ?? LoadDds(index, resRef) ??
                   LoadPlt(index, resRef, layerColorIndices);
        }

        public static TextureImage? LoadTga(ResourceIndex index, string resRef)
        {
            if (!TryGetBytes(index, resRef, ResourceIdentity.TypeFromExtension("tga"), out var data))
                return null;

            try
            {
                var image = TgaReader.Read(data);
                return new TextureImage
                {
                    Width = image.Width,
                    Height = image.Height,
                    Pixels = image.Pixels,
                    SourceFormat = TextureSourceFormat.Tga
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static TextureImage? LoadDds(ResourceIndex index, string resRef)
        {
            if (!TryGetBytes(index, resRef, ResourceIdentity.TypeFromExtension("dds"), out var data))
                return null;

            return DecodeDds(data);
        }

        /// <summary>
        /// Decode raw DDS bytes (standard Microsoft DDS or BioWare's proprietary variant, which
        /// lacks the "DDS " magic header) into RGBA pixels via Pfim.
        /// </summary>
        internal static TextureImage? DecodeDds(byte[] ddsData)
        {
            if (ddsData.Length < 20)
                return null;

            // Standard DDS starts with 0x44445320 ("DDS "); BioWare's variant starts directly
            // with width/height/channel-count fields instead.
            var isBiowareDds = !(ddsData[0] == 0x44 && ddsData[1] == 0x44 && ddsData[2] == 0x53 && ddsData[3] == 0x20);

            var decodableData = isBiowareDds ? ConvertBiowareDdsToStandard(ddsData) : ddsData;
            if (decodableData == null)
                return null;

            try
            {
                using var stream = new MemoryStream(decodableData);
                using var image = Pfimage.FromStream(stream);
                // Rows reversed so a DDS ends up in the same vertical convention as a TGA, which is
                // what the rest of the pipeline assumes. NWN's DDS files are stored top-down while its
                // TGAs are bottom-up, and both are sampled with the same bottom-up UVs, so leaving the
                // two in their own conventions makes one of them upside down. It hid because it only
                // shows on a texture whose top and bottom differ: PLC_JR1's chewyrug carries a black
                // strip near one edge, and sampling it inverted put that strip on the wookiee rug's
                // head - 15% of the mesh's area landed on near-black texels instead of 4%.
                var pixels = TextureOrientation.FlipRows(image.Width, image.Height, ConvertPfimToRgba(image));

                return new TextureImage
                {
                    Width = image.Width,
                    Height = image.Height,
                    Pixels = pixels,
                    SourceFormat = TextureSourceFormat.Dds
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Decode a PLT (palette-layered texture) using default (index 0) colors for every
        /// layer, unless <paramref name="layerColorIndices"/> supplies overrides (layer id -&gt;
        /// palette row). Palette TGAs (pal_skin01 etc.) are looked up through the same
        /// <paramref name="index"/>; a layer whose palette can't be resolved renders as
        /// grayscale for that layer (matches <see cref="PltReader.Render"/>'s own fallback), so
        /// this still succeeds against a hak-only index with no base-game palette files.
        /// </summary>
        public static TextureImage? LoadPlt(
            ResourceIndex index,
            string resRef,
            IReadOnlyDictionary<int, int>? layerColorIndices = null)
        {
            if (!TryGetBytes(index, resRef, ResourceIdentity.TypeFromExtension("plt"), out var data))
                return null;

            try
            {
                var pltFile = PltReader.Read(data);

                var palettes = new Dictionary<int, PaletteData>();
                for (var layerId = 0; layerId <= 9; layerId++)
                {
                    var paletteResRef = PltLayers.GetPaletteResRef(layerId);
                    if (!TryGetBytes(index, paletteResRef, ResourceIdentity.TypeFromExtension("tga"), out var paletteBytes))
                        continue;

                    try
                    {
                        var paletteTga = TgaReader.Read(paletteBytes);
                        palettes[layerId] = new PaletteData(paletteTga.Width, paletteTga.Height, paletteTga.Pixels);
                    }
                    catch (Exception)
                    {
                        // A malformed/unreadable palette just leaves that layer without one -
                        // PltReader.Render falls back to grayscale for it.
                    }
                }

                var colors = layerColorIndices ?? DefaultLayerColorIndices;
                var pixels = PltReader.Render(pltFile, palettes, new Dictionary<int, int>(colors));

                return new TextureImage
                {
                    Width = pltFile.Width,
                    Height = pltFile.Height,
                    Pixels = pixels,
                    SourceFormat = TextureSourceFormat.Plt
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static readonly IReadOnlyDictionary<int, int> DefaultLayerColorIndices =
            Enumerable.Range(0, 10).ToDictionary(layerId => layerId, _ => 0);

        private static bool TryGetBytes(ResourceIndex index, string resRef, ushort resourceType, out byte[] data)
        {
            data = Array.Empty<byte>();

            if (string.IsNullOrWhiteSpace(resRef))
                return false;

            var identity = new ResourceIdentity(resRef, resourceType);
            if (!index.TryLookup(identity, out var handle))
                return false;

            data = handle.GetBytes();
            return data.Length > 0;
        }

        /// <summary>
        /// Convert BioWare's proprietary DDS format to standard Microsoft DDS so Pfim can decode
        /// it. BioWare header (20 bytes): width(4), height(4), channels(4), pitch(4), alpha(4).
        /// Channels: 3 = DXT1 (RGB), 4 = DXT5 (RGBA).
        /// Adapted from <c>Radoub.UI.Services.TextureService.ConvertBiowareDdsToStandard</c>.
        /// </summary>
        internal static byte[]? ConvertBiowareDdsToStandard(byte[] biowareData)
        {
            if (biowareData.Length < 20)
                return null;

            var width = BitConverter.ToUInt32(biowareData, 0);
            var height = BitConverter.ToUInt32(biowareData, 4);
            var channels = BitConverter.ToUInt32(biowareData, 8);
            // pitch at offset 12, alpha at offset 16 - not needed for conversion.

            if (width == 0 || height == 0 || width > 4096 || height > 4096)
                return null;

            // 3 channels = DXT1 (BC1), 4 channels = DXT5 (BC3).
            var isDxt1 = channels == 3;
            var fourCc = isDxt1 ? "DXT1" : "DXT5";
            var blockSize = isDxt1 ? 8u : 16u;
            var mainImageSize = (width / 4) * (height / 4) * blockSize;

            var header = new byte[128];

            // Magic "DDS "
            header[0] = 0x44; header[1] = 0x44; header[2] = 0x53; header[3] = 0x20;

            // dwSize = 124
            BitConverter.GetBytes(124u).CopyTo(header, 4);

            // dwFlags: DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH | DDSD_PIXELFORMAT | DDSD_LINEARSIZE | DDSD_MIPMAPCOUNT
            BitConverter.GetBytes(0x000A1007u).CopyTo(header, 8);

            BitConverter.GetBytes(height).CopyTo(header, 12);
            BitConverter.GetBytes(width).CopyTo(header, 16);

            BitConverter.GetBytes(mainImageSize).CopyTo(header, 20);

            // dwDepth = 0 (offset 24). dwMipMapCount - calculate from dimensions.
            var mipCount = 1u;
            uint mw = width, mh = height;
            while (mw > 1 || mh > 1)
            {
                mw = Math.Max(1, mw / 2);
                mh = Math.Max(1, mh / 2);
                mipCount++;
            }
            BitConverter.GetBytes(mipCount).CopyTo(header, 28);

            // dwReserved1[11] = 0 (offsets 32-75).

            // Pixel format (offset 76, 32 bytes).
            BitConverter.GetBytes(32u).CopyTo(header, 76); // ddpf.dwSize
            BitConverter.GetBytes(4u).CopyTo(header, 80);  // ddpf.dwFlags = DDPF_FOURCC
            header[84] = (byte)fourCc[0];
            header[85] = (byte)fourCc[1];
            header[86] = (byte)fourCc[2];
            header[87] = (byte)fourCc[3];
            // ddpf remaining fields = 0 (offsets 88-107).

            // dwCaps = DDSCAPS_TEXTURE | DDSCAPS_MIPMAP | DDSCAPS_COMPLEX
            BitConverter.GetBytes(0x00401008u).CopyTo(header, 108);
            // dwCaps2-4 = 0, dwReserved2 = 0.

            var pixelDataLen = biowareData.Length - 20;
            var result = new byte[128 + pixelDataLen];
            header.CopyTo(result, 0);
            Array.Copy(biowareData, 20, result, 128, pixelDataLen);

            return result;
        }

        /// <summary>
        /// Copies Pfim's decoded pixels out as RGBA, putting the channels the right way round.
        /// </summary>
        /// <remarks>
        /// Pfim's <c>ImageFormat</c> names describe the DDS pixel format, not the byte order it hands
        /// back: <c>Rgba32</c> and <c>Rgb24</c> both arrive blue-first, so red and blue have to be
        /// exchanged on the way out.
        /// <para>
        /// This used to be done only for BioWare's DDS variant, on the theory that BioWare stored its
        /// DXT endpoints as BGR 5:6:5 while Pfim assumed RGB. That fixed the majority of the corpus by
        /// accident and left every standard DDS with its red and blue exchanged - which is invisible on
        /// the grey and desaturated artwork most of a tileset is made of, and glaring on anything with
        /// a real hue: PLC_JR1's chewyrug decoded to rgb(31,49,74) and drew as a blue pelt where Aurora
        /// shows brown fur. BioWare-variant textures are unaffected by the move, because doing the swap
        /// during the copy and doing it afterwards are the same swap.
        /// </para>
        /// </remarks>
        private static byte[] ConvertPfimToRgba(IImage image)
        {
            var width = image.Width;
            var height = image.Height;
            var output = new byte[width * height * 4];
            var src = image.Data;

            switch (image.Format)
            {
                case Pfim.ImageFormat.Rgba32:
                    for (int i = 0; i < output.Length && i < src.Length - 3; i += 4)
                    {
                        output[i] = src[i + 2];
                        output[i + 1] = src[i + 1];
                        output[i + 2] = src[i];
                        output[i + 3] = src[i + 3];
                    }
                    break;

                case Pfim.ImageFormat.Rgb24:
                    for (int i = 0, j = 0; i < output.Length && j < src.Length - 2; i += 4, j += 3)
                    {
                        output[i] = src[j + 2];
                        output[i + 1] = src[j + 1];
                        output[i + 2] = src[j];
                        output[i + 3] = 255;
                    }
                    break;

                case Pfim.ImageFormat.Rgb8:
                    for (int i = 0, j = 0; i < output.Length && j < src.Length; i += 4, j++)
                    {
                        output[i] = src[j];
                        output[i + 1] = src[j];
                        output[i + 2] = src[j];
                        output[i + 3] = 255;
                    }
                    break;

                default:
                    for (var i = 0; i < output.Length; i += 4)
                    {
                        output[i] = 128;
                        output[i + 1] = 128;
                        output[i + 2] = 128;
                        output[i + 3] = 255;
                    }
                    break;
            }

            return output;
        }

    }
}
