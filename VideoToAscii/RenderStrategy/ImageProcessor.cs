using System.Numerics;
using System.Runtime.CompilerServices;

using OpenCvSharp;

namespace VideoToAscii.RenderStrategy;

public sealed class ImageProcessor
{
    // Different character sets for varying density levels
    private static readonly char[][] DensityChars =
    [
        [' ', ' ', '.', ':', '!', '+', '*', 'e', '$', '@', '8'], // Light
        ['.', '*', 'e', 's', '◍'],                               // Color
        ['░', '▒', '▓', '█']                                     // Filled
    ];

    private const int BrightnessLevels = 256; // 0-255

    // Pre-computed brightness-to-ASCII tables (one per density level)
    private readonly char[][] _brightnessLookupTables;

    public ImageProcessor()
    {
        // Initialize brightness lookup tables for quick access
        _brightnessLookupTables = new char[DensityChars.Length][];

        for (var densityIndex = 0; densityIndex < DensityChars.Length; densityIndex++)
        {
            _brightnessLookupTables[densityIndex] = new char[BrightnessLevels];
            var chars = DensityChars[densityIndex];
            var maxIndex = chars.Length - 1;

            for (var b = 0; b < BrightnessLevels; b++)
            {
                var charIndex = (int)(maxIndex * b / 255.0);
                _brightnessLookupTables[densityIndex][b] = chars[charIndex];
            }
        }
    }

    /// <summary>
    /// Convert brightness level to ASCII character using lookup table
    /// </summary>
    /// <param name="brightness">Brightness value (0-255)</param>
    /// <param name="density">Character density level (0-2)</param>
    /// <returns>ASCII character representing the brightness</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public char BrightnessToAscii(int brightness, int density = 0)
    {
        // Clamp values to valid ranges for safety
        brightness = Math.Clamp(brightness, 0, BrightnessLevels - 1);
        density = Math.Clamp(density, 0, _brightnessLookupTables.Length - 1);

        // Direct lookup from pre-computed table
        return _brightnessLookupTables[density][brightness];
    }

    /// <summary>
    /// Convert a pixel to colored ASCII characters
    /// </summary>
    /// <param name="pixel">BGR pixel data</param>
    /// <param name="colored">Whether to use color</param>
    /// <param name="density">Character density level (0-2)</param>
    /// <returns>ASCII representation</returns>
    public string PixelToAscii(Vec3b pixel, bool colored = true, int density = 0)
    {
        var (b, g, r) = (pixel.Item0, pixel.Item1, pixel.Item2);

        if (colored)
        {
            var brightness = RgbToBrightness(r, g, b);
            var (sr, sg, sb) = IncreaseSaturation(r, g, b);
            var ascii = BrightnessToAscii(brightness, density);
            var colorEscape = GetAnsiColorCode(sr, sg, sb);
            return $"{colorEscape}{ascii}{ascii}\u001b[0m";
        }
        else
        {
            var brightness = RgbToBrightness(r, g, b, grayscale: true);
            var ascii = BrightnessToAscii(brightness, density);
            return $"{ascii}{ascii}";
        }
    }

    private static (byte r, byte g, byte b) IncreaseSaturation(byte r, byte g, byte b)
    {
        // Convert to HSV
        const float inv255 = 1f / 255f; // pre-computed reciprocal
        var rf = r * inv255;
        var gf = g * inv255;
        var bf = b * inv255;

        var max = Math.Max(rf, Math.Max(gf, bf));
        var min = Math.Min(rf, Math.Min(gf, bf));
        var delta = max - min;

        // Calculate HSV
        float h = 0f;
        if (delta != 0f)
        {
            if (max == rf)
                h = (gf - bf) / delta % 6f;
            else if (max == gf)
                h = ((bf - rf) / delta) + 2f;
            else
                h = ((rf - gf) / delta) + 4f;

            h *= 60f;
            if (h < 0f)
                h += 360f;
        }

        var s = max == 0f ? 0f : delta / max;
        var v = max;

        // Increase saturation
        s = Math.Min(s + 0.3f, 1f);

        // Convert back to RGB
        var c = v * s;
        var x = c * (1f - Math.Abs((h / 60f % 2f) - 1f));
        var m = v - c;

        float r1, g1, b1;

        if (h < 60f)
        {
            r1 = c;
            g1 = x;
            b1 = 0f;
        }
        else if (h < 120f)
        {
            r1 = x;
            g1 = c;
            b1 = 0f;
        }
        else if (h < 180f)
        {
            r1 = 0f;
            g1 = c;
            b1 = x;
        }
        else if (h < 240f)
        {
            r1 = 0f;
            g1 = x;
            b1 = c;
        }
        else if (h < 300f)
        {
            r1 = x;
            g1 = 0f;
            b1 = c;
        }
        else
        {
            r1 = c;
            g1 = 0f;
            b1 = x;
        }

        return (
            (byte)Math.Round(Math.Clamp((r1 + m) * 255f, 0f, 255f)),
            (byte)Math.Round(Math.Clamp((g1 + m) * 255f, 0f, 255f)),
            (byte)Math.Round(Math.Clamp((b1 + m) * 255f, 0f, 255f))
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int RgbToBrightness(byte r, byte g, byte b, bool grayscale = false)
    {
        if (Vector.IsHardwareAccelerated && grayscale)
        {
            // Use SIMD if available for the standard grayscale formula
            var weights = new Vector3(0.2126f, 0.7152f, 0.0722f);
            var rgb = new Vector3(r, g, b);
            return (int)Vector3.Dot(rgb, weights);
        }
        else if (grayscale)
        {
            // Standard grayscale formula
            return (int)((0.2126 * r) + (0.7152 * g) + (0.0722 * b));
        }
        else
        {
            // Custom brightness formula from original code
            return (int)((0.267 * r) + (0.642 * g) + (0.091 * b));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetAnsiColorCode(byte r, byte g, byte b) => $"\u001b[38;2;{r};{g};{b}m";
}
