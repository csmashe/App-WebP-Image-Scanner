namespace WebPScanner.Core.Utilities;

/// <summary>
/// Utility class for formatting byte sizes into human-readable strings.
/// </summary>
public static class ByteFormatUtility
{
    private static readonly string[] SizeSuffixes = ["B", "KB", "MB", "GB", "TB"];

    /// <summary>
    /// Formats a byte count into a human-readable string (e.g., "1.5 MB", "256 KB").
    /// </summary>
    /// <param name="bytes">The number of bytes to format.</param>
    /// <returns>A formatted string representation of the byte size.</returns>
    public static string FormatBytes(long bytes)
    {
        if (bytes == 0)
            return "0 B";

        var isNegative = bytes < 0;
        // Handle long.MinValue specially since Math.Abs would overflow
        // (|long.MinValue| = 2^63, but max positive long is 2^63 - 1)
        var absoluteBytes = bytes == long.MinValue
            ? (ulong)long.MaxValue + 1
            : (ulong)(isNegative ? -bytes : bytes);

        var order = 0;
        var size = (double)absoluteBytes;

        while (size >= 1024 && order < SizeSuffixes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        // Use 1 decimal place for KB, 2 for MB and above
        var format = order <= 1 ? "0.#" : "0.##";
        var formatted = $"{size.ToString(format)} {SizeSuffixes[order]}";

        return isNegative ? $"-{formatted}" : formatted;
    }
}
