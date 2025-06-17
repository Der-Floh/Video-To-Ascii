using OpenCvSharp;

namespace VideoToAscii.RenderStrategy;

/// <summary>
/// Print each frame in the terminal using colored ASCII fill characters
/// </summary>
public sealed class AsciiColorFilledStrategy : AsciiStrategy
{
    /// <summary>
    /// Define a pixel parsing strategy to use filled colored chars
    /// </summary>
    /// <param name="pixel">BGR pixel data</param>
    /// <returns>ASCII representation with color and fill characters</returns>
    protected override string ApplyPixelToAsciiStrategy(Vec3b pixel)
    {
        return ImageProcessor.PixelToAscii(pixel, true, 2);
    }
}
