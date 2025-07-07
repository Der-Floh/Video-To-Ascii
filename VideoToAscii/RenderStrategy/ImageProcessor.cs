using System.Collections.Concurrent;
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
        ['.', '*', 'e', 's', '◍'], // Color
        ['░', '▒', '▓', '█'] // Filled
    ];

    // Brightness to ASCII lookup tables for each density level
    private readonly char[][] _brightnessLookupTables;
    private const int BRIGHTNESS_LEVELS = 256; // 0-255

    // Cache for color to ASCII mappings to avoid redundant calculations
    // Using tuple as key: (R, G, B, colored, density)
    private readonly ConcurrentDictionary<(byte R, byte G, byte B, bool Colored, int Density), string> _colorCache = new();

    // ANSI color code cache for frequent colors
    private readonly ConcurrentDictionary<(byte R, byte G, byte B), string> _ansiColorCache = new();

    public ImageProcessor()
    {
        // Initialize brightness lookup tables for quick access
        _brightnessLookupTables = new char[DensityChars.Length][];

        for (var densityIndex = 0; densityIndex < DensityChars.Length; densityIndex++)
        {
            _brightnessLookupTables[densityIndex] = new char[BRIGHTNESS_LEVELS];
            var chars = DensityChars[densityIndex];
            var maxIndex = chars.Length - 1;

            // Pre-compute ASCII character for each brightness level (0-255)
            for (var brightness = 0; brightness < BRIGHTNESS_LEVELS; brightness++)
            {
                var index = (int)(maxIndex * brightness / 255.0);
                _brightnessLookupTables[densityIndex][brightness] = chars[index];
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
        brightness = Math.Clamp(brightness, 0, BRIGHTNESS_LEVELS - 1);
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
        var b = pixel.Item0;
        var g = pixel.Item1;
        var r = pixel.Item2;

        // Use cache to avoid redundant calculations and string allocations
        return _colorCache.GetOrAdd((r, g, b, colored, density), key =>
        {
            var (r1, g1, b1, isColored, d) = key;

            if (isColored)
            {
                var brightness = RgbToBrightness(r1, g1, b1);
                (r1, g1, b1) = IncreaseSaturation(r1, g1, b1); // Enhance colors
                var asciiChar = BrightnessToAscii(brightness, d);

                // ANSI color escape sequence
                var colorCode = GetAnsiColorCode(r1, g1, b1);
                return $"{colorCode}{asciiChar}{asciiChar}\u001b[0m";
            }
            else
            {
                var brightness = RgbToBrightness(r1, g1, b1, true); // Use grayscale formula
                var asciiChar = BrightnessToAscii(brightness, d);
                return $"{asciiChar}{asciiChar}";
            }
        });
    }

    /// <summary>
    /// Increase the saturation of an RGB color
    /// </summary>
    /// <param name="r">Red (0-255)</param>
    /// <param name="g">Green (0-255)</param>
    /// <param name="b">Blue (0-255)</param>
    /// <returns>RGB tuple with increased saturation</returns>
    private (byte r, byte g, byte b) IncreaseSaturation(byte r, byte g, byte b)
    {
        // Convert to HSV
        var rf = r / 255.0;
        var gf = g / 255.0;
        var bf = b / 255.0;

        var max = Math.Max(rf, Math.Max(gf, bf));
        var min = Math.Min(rf, Math.Min(gf, bf));
        var delta = max - min;

        // Calculate HSV
        double h = 0;
        if (delta != 0)
        {
            if (max == rf)
                h = ((gf - bf) / delta) % 6;
            else if (max == gf)
                h = (bf - rf) / delta + 2;
            else
                h = (rf - gf) / delta + 4;

            h *= 60;
            if (h < 0)
                h += 360;
        }

        var s = max == 0 ? 0 : delta / max;
        var v = max;

        // Increase saturation
        s = Math.Min(s + 0.3, 1.0);

        // Convert back to RGB
        var c = v * s;
        var x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        var m = v - c;

        double r1, g1, b1;

        if (h < 60)
        {
            r1 = c;
            g1 = x;
            b1 = 0;
        }
        else if (h < 120)
        {
            r1 = x;
            g1 = c;
            b1 = 0;
        }
        else if (h < 180)
        {
            r1 = 0;
            g1 = c;
            b1 = x;
        }
        else if (h < 240)
        {
            r1 = 0;
            g1 = x;
            b1 = c;
        }
        else if (h < 300)
        {
            r1 = x;
            g1 = 0;
            b1 = c;
        }
        else
        {
            r1 = c;
            g1 = 0;
            b1 = x;
        }

        return (
            (byte)Math.Round(Math.Clamp((r1 + m) * 255, 0, 255)),
            (byte)Math.Round(Math.Clamp((g1 + m) * 255, 0, 255)),
            (byte)Math.Round(Math.Clamp((b1 + m) * 255, 0, 255))
        );
    }

    /// <summary>
    /// Calculate brightness from RGB color with optimized path using Vector
    /// </summary>
    /// <param name="r">Red (0-255)</param>
    /// <param name="g">Green (0-255)</param>
    /// <param name="b">Blue (0-255)</param>
    /// <param name="grayscale">Use grayscale formula</param>
    /// <returns>Brightness value (0-255)</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int RgbToBrightness(byte r, byte g, byte b, bool grayscale = false)
    {
        if (Vector.IsHardwareAccelerated && grayscale)
        {
            // Use SIMD if available for the standard grayscale formula
            Vector3 weights = new Vector3(0.2126f, 0.7152f, 0.0722f);
            Vector3 rgb = new Vector3(r, g, b);
            return (int)Vector3.Dot(rgb, weights);
        }
        else if (grayscale)
        {
            // Standard grayscale formula
            return (int)(0.2126 * r + 0.7152 * g + 0.0722 * b);
        }
        else
        {
            // Custom brightness formula from original code
            return (int)(0.267 * r + 0.642 * g + 0.091 * b);
        }
    }

    /// <summary>
    /// Get ANSI color code for an RGB color with caching
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string GetAnsiColorCode(byte r, byte g, byte b)
    {
        return _ansiColorCache.GetOrAdd((r, g, b), rgb =>
        {
            // Using 24-bit true color ANSI escape sequence
            return $"\u001b[38;2;{rgb.R};{rgb.G};{rgb.B}m";
        });
    }
}
