using static Jewel.JPMS.Api.Features.Sales.Imagine.ImagineWording;

namespace Jewel.JPMS.Api.Features.Sales.Imagine;

/// <summary>A prospect's photo as bytes the store will take: decoded from the page's data URL,
/// bounded, and typed from its own header rather than the client's word for it.</summary>
internal static class ImaginePhotoDecoding
{
    public static (byte[] Bytes, string ContentType) Decode(ImaginePhotoUpload upload)
    {
        var data = upload.Base64 ?? "";
        var comma = data.IndexOf(',');
        if (data.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma > 0) data = data[(comma + 1)..];
        byte[] bytes;
        try { bytes = Convert.FromBase64String(data); }
        catch (FormatException) { throw new InvalidOperationException($"{upload.FileName} couldn't be read — please try that photo again."); }
        if (bytes.Length < 64) throw new InvalidOperationException($"{upload.FileName} is empty.");
        if (bytes.Length > ImagineLimits.MaxPhotoBytes)
            throw new InvalidOperationException($"{upload.FileName} is too large after resizing — please try a smaller photo.");
        var contentType = Sniff(bytes) ?? throw new InvalidOperationException($"{upload.FileName} isn't a JPEG, PNG or WebP photo.");
        return (bytes, contentType);
    }

    /// <summary>The type from the bytes, never from the client's word for it.</summary>
    private static string? Sniff(byte[] bytes)
    {
        if (bytes.Length > 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) return "image/jpeg";
        if (bytes.Length > 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) return "image/png";
        if (bytes.Length > 12 && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46
            && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50) return "image/webp";
        return null;
    }
}
