using OpenCvSharp;

namespace VideoToAscii.RenderStrategy;

/// <summary>
/// Print each frame in the terminal using black and white ASCII characters
/// </summary>
public sealed class AsciiBWStrategy : AsciiStrategy
{
    /// <summary>
    /// Define a pixel parsing strategy to use grayscale chars
    /// </summary>
    /// <param name="pixel">BGR pixel data</param>
    /// <returns>ASCII representation without color</returns>
    protected override string ApplyPixelToAsciiStrategy(Vec3b pixel)
    {
        return ImageProcessor.PixelToAscii(pixel, false, 0);
    }
}
