using OpenCvSharp;

using VideoToAscii.RenderStrategy;
using VideoToAscii.Utils;

namespace VideoToAscii;

public sealed class VideoEngine
{
    private IRenderStrategy _renderStrategy;
    private VideoCapture? _videoCapture;
    public bool WithAudio { get; set; }
    public string? AudioFilePath { get; set; }

    public VideoEngine(string strategy = "default")
    {
        _renderStrategy = StrategyFactory.GetStrategy(strategy);
    }

    /// <summary>
    /// Set a render strategy
    /// </summary>
    public void SetStrategy(string strategy)
    {
        _renderStrategy = StrategyFactory.GetStrategy(strategy);
    }

    /// <summary>
    /// Load a video file into the engine and set a read buffer
    /// </summary>
    public void LoadVideoFromFile(string filename)
    {
        _videoCapture = OpenCVUtils.TryOpenWithHardwareAcc(filename);
        if (!_videoCapture.IsOpened())
        {
            throw new Exception($"Could not open video file: {filename}");
        }
    }

    /// <summary>
    /// Play the video captured using an specific render strategy
    /// </summary>
    public async Task Play(string? output = null, string? outputFormat = null)
    {
        Console.CursorVisible = false;
        await _renderStrategy.Render(_videoCapture!, output, outputFormat, WithAudio, AudioFilePath);
        Console.CursorVisible = true;
    }
}
