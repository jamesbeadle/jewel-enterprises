using System.Buffers.Binary;
using System.IO.Compression;

namespace Jewel.JPMS.Api.Features.Ai.Scans;

/// <summary>
/// The smallest PNG writer that does the job: 8-bit RGB, no filter, one IDAT, zlib through the
/// framework's own ZLibStream. Exists so a rendered page can be shown to the assistant without
/// taking an imaging library into the API for one call.
/// </summary>
internal static class PngEncoder
{
    private static readonly byte[] Signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private const byte ColourTypeRgb = 2;
    private const byte BitDepth = 8;
    private const int BytesPerPixel = 3;

    /// <summary>Encodes a BGRA buffer (pdfium's output) as an RGB PNG; the alpha is dropped over white.</summary>
    public static byte[] FromBgra(byte[] bgra, int width, int height)
    {
        var raw = RowsWithFilterBytes(bgra, width, height);
        using var output = new MemoryStream();
        output.Write(Signature);
        WriteChunk(output, "IHDR", Header(width, height));
        WriteChunk(output, "IDAT", Deflate(raw));
        WriteChunk(output, "IEND", Array.Empty<byte>());
        return output.ToArray();
    }

    private static byte[] Header(int width, int height)
    {
        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0), width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = BitDepth;
        header[9] = ColourTypeRgb;
        return header;
    }

    private static byte[] RowsWithFilterBytes(byte[] bgra, int width, int height)
    {
        var stride = width * BytesPerPixel + 1;
        var raw = new byte[stride * height];
        for (var y = 0; y < height; y++)
        {
            var rowStart = y * stride;
            raw[rowStart] = 0;
            for (var x = 0; x < width; x++)
            {
                var source = (y * width + x) * 4;
                var alpha = bgra[source + 3];
                var target = rowStart + 1 + x * BytesPerPixel;
                raw[target] = OverWhite(bgra[source + 2], alpha);
                raw[target + 1] = OverWhite(bgra[source + 1], alpha);
                raw[target + 2] = OverWhite(bgra[source], alpha);
            }
        }
        return raw;
    }

    private static byte OverWhite(byte channel, byte alpha) =>
        alpha == 255 ? channel : (byte)((channel * alpha + 255 * (255 - alpha)) / 255);

    private static byte[] Deflate(byte[] raw)
    {
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
            zlib.Write(raw);
        return compressed.ToArray();
    }

    private static void WriteChunk(Stream output, string type, byte[] data)
    {
        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        var length = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        output.Write(length);
        output.Write(typeBytes);
        output.Write(data);
        var crc = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crc, Crc32.Of(typeBytes, data));
        output.Write(crc);
    }

    private static class Crc32
    {
        private static readonly uint[] Table = BuildTable();

        public static uint Of(byte[] first, byte[] second)
        {
            var crc = 0xFFFFFFFFu;
            foreach (var b in first) crc = Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
            foreach (var b in second) crc = Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
            return crc ^ 0xFFFFFFFFu;
        }

        private static uint[] BuildTable()
        {
            var table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                var c = n;
                for (var k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                table[n] = c;
            }
            return table;
        }
    }
}
