using System;
using System.IO;
using System.Text;

// Standard DDS blocks stored bottom-up, as required by NWN's texture loader.
// Returning a normalized TGA buffer lets the artwork audits inspect decoded pixels.
public static class GameplayIconTexture
{
    public static byte[] ReadBottomLeftTga(string path)
    {
        var data = File.ReadAllBytes(path);
        if (!path.EndsWith(".dds", StringComparison.OrdinalIgnoreCase)) return data;
        if (data.Length < 128 || Encoding.ASCII.GetString(data, 0, 4) != "DDS " ||
            U32(data, 4) != 124 || U32(data, 76) != 32 || U32(data, 80) != 4)
            throw new InvalidDataException("Invalid standard DDS header: " + path);
        var width = checked((int)U32(data, 16));
        var height = checked((int)U32(data, 12));
        var format = Encoding.ASCII.GetString(data, 84, 4);
        if (width < 1 || height < 1 || width > 256 || height > 256 ||
            (format != "DXT1" && format != "DXT5") || U32(data, 28) != 1 ||
            (U32(data, 8) & 0xA1007) != 0xA1007 || U32(data, 108) != 0x1000 || U32(data, 112) != 0)
            throw new InvalidDataException("Expected one-level DXT1/DXT5 icon DDS: " + path);
        var blockSize = format == "DXT1" ? 8 : 16;
        var payloadSize = checked(((width + 3) / 4) * ((height + 3) / 4) * blockSize);
        if (data.Length != 128 + payloadSize || U32(data, 20) != payloadSize)
            throw new InvalidDataException("DDS payload size mismatch: " + path);
        var result = new byte[18 + width * height * 4];
        result[2] = 2; result[12] = (byte)width; result[13] = (byte)(width >> 8);
        result[14] = (byte)height; result[15] = (byte)(height >> 8);
        result[16] = 32; result[17] = 8;
        var offset = 128;
        for (var by = 0; by < height; by += 4)
        for (var bx = 0; bx < width; bx += 4)
        {
            var alpha = new byte[16];
            for (var i = 0; i < 16; i++) alpha[i] = 255;
            if (format == "DXT5")
            {
                var levels = new byte[8]; levels[0] = data[offset]; levels[1] = data[offset + 1];
                var count = levels[0] > levels[1] ? 7 : 5;
                for (var i = 1; i < count; i++)
                    levels[i + 1] = (byte)(((count - i) * levels[0] + i * levels[1]) / count);
                if (count == 5) { levels[6] = 0; levels[7] = 255; }
                ulong bits = 0;
                for (var i = 0; i < 6; i++) bits |= (ulong)data[offset + 2 + i] << (i * 8);
                for (var i = 0; i < 16; i++) alpha[i] = levels[(int)((bits >> (i * 3)) & 7)];
                offset += 8;
            }
            var c0 = U16(data, offset); var c1 = U16(data, offset + 2);
            var colors = new byte[4][];
            colors[0] = Color565(c0); colors[1] = Color565(c1);
            colors[2] = new byte[4]; colors[3] = new byte[4];
            if (c0 > c1 || format == "DXT5")
            {
                for (var c = 0; c < 3; c++)
                {
                    colors[2][c] = (byte)((2 * colors[0][c] + colors[1][c]) / 3);
                    colors[3][c] = (byte)((colors[0][c] + 2 * colors[1][c]) / 3);
                }
                colors[2][3] = colors[3][3] = 255;
            }
            else
            {
                for (var c = 0; c < 3; c++) colors[2][c] = (byte)((colors[0][c] + colors[1][c]) / 2);
                colors[2][3] = 255;
            }
            var indices = U32(data, offset + 4); offset += 8;
            for (var i = 0; i < 16; i++)
            {
                var x = bx + i % 4; var y = by + i / 4;
                if (x >= width || y >= height) continue;
                var color = colors[(int)((indices >> (2 * i)) & 3)];
                var pixel = 18 + (y * width + x) * 4;
                result[pixel] = color[0]; result[pixel + 1] = color[1]; result[pixel + 2] = color[2];
                result[pixel + 3] = format == "DXT5" ? alpha[i] : color[3];
            }
        }
        return result;
    }

    private static byte[] Color565(int color)
    {
        var r = (color >> 11) & 31; var g = (color >> 5) & 63; var b = color & 31;
        return new byte[] { (byte)((b << 3) | (b >> 2)), (byte)((g << 2) | (g >> 4)),
            (byte)((r << 3) | (r >> 2)), 255 };
    }
    private static uint U32(byte[] data, int offset) { return BitConverter.ToUInt32(data, offset); }
    private static int U16(byte[] data, int offset) { return BitConverter.ToUInt16(data, offset); }
}
