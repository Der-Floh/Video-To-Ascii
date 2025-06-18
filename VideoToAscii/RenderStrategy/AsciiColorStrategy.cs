using OpenCvSharp;

namespace VideoToAscii.RenderStrategy;

/// <summary>
/// Print each frame in the terminal using colored ASCII characters
/// </summary>
public sealed class AsciiColorStrategy : AsciiStrategy
{
    /// <summary>
    /// Define a pixel parsing strategy to use colored chars
    /// </summary>
    /// <param name="pixel">BGR pixel data</param>
    /// <returns>ASCII representation with color</returns>
    protected override string ApplyPixelToAsciiStrategy(Vec3b pixel)
    {
        return ImageProcessor.PixelToAscii(pixel, true, 1);
    }
}
