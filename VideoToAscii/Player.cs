using System.Diagnostics;

using VideoToAscii.Utils;

namespace VideoToAscii;

public sealed class Player
{
    public string VideoFilePath { get; set; }
    public string Strategy { get; set; }
    public string? OutputFilePath { get; set; }
    public bool PlayAudio { get; set; }

    public Player(string videoFilePath, string strategy)
    {
        VideoFilePath = videoFilePath;
        Strategy = strategy;
    }

    /// <summary>
    /// Play or export a video from a file by default using ascii chars in terminal
    /// </summary>
    /// <param name="filename">Path to the video file</param>
    /// <param name="strategy">Rendering strategy to use</param>
    /// <param name="output">Output file path if exporting</param>
    /// <param name="playAudio">Whether to play audio</param>
    /// <param name="debug">Enable debug mode with timing logs</param>
    public async Task Play()
    {
        var engine = new VideoEngine();
        engine.LoadVideoFromFile(VideoFilePath);

        string? audioFilePath = null;
        if (PlayAudio && string.IsNullOrEmpty(OutputFilePath)) // Only process audio for terminal playback, not for file output
        {
            audioFilePath = ExtractAudio(VideoFilePath);
            if (audioFilePath is not null)
            {
                engine.WithAudio = true;
                engine.AudioFilePath = audioFilePath;
            }
        }

        if (string.IsNullOrEmpty(Strategy))
            throw new ArgumentException("No rendering strategy specified");

        engine.SetStrategy(Strategy);

        try
        {
            var outputFormat = OutputFilePath is null ? null : Path.GetExtension(OutputFilePath);
            await engine.Play(OutputFilePath, outputFormat);
        }
        finally
        {
            // Clean up temp audio file if it exists
            if (audioFilePath is not null && File.Exists(audioFilePath))
            {
                try
                {
                    File.Delete(audioFilePath);
                }
                catch { /* Ignore cleanup errors */ }
            }
        }
    }

    /// <summary>
    /// Extract audio from video file using FFmpeg
    /// </summary>
    /// <param name="videoFile">Path to the video file</param>
    /// <returns>Path to the extracted audio file or null if extraction fails</returns>
    private static string? ExtractAudio(string videoFile)
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"vta_audio_{Path.GetFileNameWithoutExtension(videoFile)}_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

        Console.WriteLine("Extracting audio from video...");
        var stopwatch = Stopwatch.StartNew();

        if (!FFmpegUtils.IsFFmpegInstalled())
        {
            Console.WriteLine("FFmpeg is not installed. Continuing without audio.");
            return null;
        }

        var success = AudioPlayer.ExtractAudioFromVideo(videoFile, tempFilePath);

        if (success)
        {
            Console.WriteLine($"Audio extraction completed in {stopwatch.ElapsedMilliseconds}ms");
            return tempFilePath;
        }
        else
        {
            Console.WriteLine("Audio extraction failed. Continuing without audio.");
            return null;
        }
    }
}
