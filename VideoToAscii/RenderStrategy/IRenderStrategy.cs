using OpenCvSharp;

namespace VideoToAscii.RenderStrategy;

public interface IRenderStrategy
{
    /// <summary>
    /// Render the video using the specific strategy
    /// </summary>
    /// <param name="capture">The video capture to render</param>
    /// <param name="output">Optional output file path</param>
    /// <param name="outputFormat">Output format if saving to file</param>
    /// <param name="withAudio">Whether to play audio</param>
    /// <param name="audioFilePath">Path to the audio file to play alongside video</param>
    void Render(VideoCapture capture, string? output = null, string? outputFormat = null, bool withAudio = false, string? audioFilePath = null);
}
