using OpenCvSharp;

using VideoToAscii.RenderStrategy;

namespace VideoToAscii;

public sealed class VideoEngine
{
    private IRenderStrategy _renderStrategy;
    private VideoCapture? _readBuffer;
    public bool WithAudio { get; set; }
    public string? AudioFilePath { get; set; }

    public VideoEngine(string strategy = "default")
    {
        _renderStrategy = StrategyFactory.GetStrategy(strategy);
        WithAudio = false;
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
        _readBuffer = new VideoCapture(filename);
        if (!_readBuffer.IsOpened())
        {
            throw new Exception($"Could not open video file: {filename}");
        }
    }

    /// <summary>
    /// Play the video captured using an specific render strategy
    /// </summary>
    public void Play(string? output = null, string? outputFormat = null)
    {
        Console.CursorVisible = false;
        _renderStrategy.Render(_readBuffer!, output, outputFormat, WithAudio, AudioFilePath);
        Console.CursorVisible = true;
    }
}
