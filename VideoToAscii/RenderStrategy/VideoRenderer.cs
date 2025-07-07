using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

using OpenCvSharp;

using VideoToAscii.FileConverter;
using VideoToAscii.Utils;

namespace VideoToAscii.RenderStrategy;

public sealed class VideoRenderer
{
    public AsciiStrategy Strategy { get; set; }
    public VideoCapture VideoCapture { get; set; }
    public string? Output { get; set; }
    public string? OutputFormat { get; set; }
    public bool WithAudio { get; set; }
    public string? AudioFilePath { get; set; }

    public VideoRenderer(AsciiStrategy strategy, VideoCapture videoCapture)
    {
        Strategy = strategy;
        VideoCapture = videoCapture;
    }

    public async Task Render()
    {
        if (string.IsNullOrEmpty(Output))
            RenderVideo();
        else
            RenderToFile();
    }

    public void RenderVideo()
    {
        VideoCapture.PosFrames = 0;
        var fps = VideoCapture.Fps <= 0 ? 30 : VideoCapture.Fps;

        const int MaxFrameSkip = 10;        // <- avoid "black-hole" skipping
        var frameDurationMs = 1000.0 / fps; // <- target budget

        var audioPlayer = SetupAudio();

        using var frame = new Mat();
        var sb = new StringBuilder();
        var writer = Console.Out;
        int prevCols = 0, prevRows = 0;

        var counter = 0;
        var asciiFrame = string.Empty;
        try
        {
            while (asciiFrame is not null)
            {
                var (cols, rows) = GetConsoleSize();
                if (prevCols != cols || prevRows != rows)
                    Console.Clear();
                prevCols = cols;
                prevRows = rows;

                (asciiFrame, var processingMs) = CalculateFrame(frame, cols, rows);
                if (asciiFrame is null)
                    continue;

                SyncAudio(counter, audioPlayer, fps);

                if (processingMs > frameDurationMs)
                {
                    // how many *additional* frames does this delay amount to?
                    var framesBehind = (int)(processingMs / frameDurationMs) - 1;
                    var framesToSkip = Math.Min(framesBehind, MaxFrameSkip);

                    for (var i = 0; i < framesToSkip && VideoCapture.Grab(); i++)
                    {
                        counter++;  // keep counter consistent
                    }
                }
                else if (processingMs < frameDurationMs)   // finished early → small sleep
                {
                    Thread.Sleep((int)(frameDurationMs - processingMs));
                }

                sb.Append("\u001b[0;0H");
                sb.Append(asciiFrame);
                writer.Write(sb);
                sb.Clear();

                counter++;
            }
        }
        finally
        {
            if (audioPlayer is not null)
            {
                try
                {
                    audioPlayer.Dispose();
                }
                catch { }
            }
            Console.Clear();
        }
    }

    public void RenderToFile()
    {
        VideoCapture.PosFrames = 0;
        var fps = VideoCapture.Fps <= 0 ? 30 : VideoCapture.Fps;

        var length = VideoCapture.FrameCount;

        IFileConverter? fileConverter = null;
        if (!string.IsNullOrEmpty(Output))
        {
            fileConverter = OutputFormat switch
            {
                ".sh" => new ShConverter(Output, fps),
                ".bat" => new BatConverter(Output, fps),
                ".ps1" => new Ps1Converter(Output, fps),
                ".json" => new JsonConverter(Output),
                _ => new TxtConverter(Output),
            };
        }

        using var frame = new Mat();
        var sb = new StringBuilder();
        var writer = Console.Out;
        var (cols, rows) = GetConsoleSize();

        var counter = 0;
        var asciiFrame = string.Empty;
        try
        {
            while (asciiFrame is not null)
            {
                ConsoleUtils.DisplayProgress(counter, length);
                (asciiFrame, _) = CalculateFrame(frame, cols, rows, true);
                if (asciiFrame is null)
                    continue;

                fileConverter?.WriteFrame(asciiFrame);

                counter++;
            }
        }
        finally
        {
            fileConverter?.Dispose();
            Console.Clear();
        }
    }

    public async Task RenderToFileAsync(int? degreeOfParallelism = null, CancellationToken ct = default)
    {
        VideoCapture.PosFrames = 0;
        var fps = VideoCapture.Fps <= 0 ? 30 : VideoCapture.Fps;
        var frameCount = VideoCapture.FrameCount;

        IFileConverter? fileConverter = null;
        if (!string.IsNullOrEmpty(Output))
        {
            fileConverter = OutputFormat switch
            {
                ".sh" => new ShConverter(Output, fps),
                ".bat" => new BatConverter(Output, fps),
                ".ps1" => new Ps1Converter(Output, fps),
                ".json" => new JsonConverter(Output),
                _ => new TxtConverter(Output),
            };
        }

        var (cols, rows) = GetConsoleSize();
        var dop = degreeOfParallelism ?? Environment.ProcessorCount;
        var asciiFrames = new string?[frameCount];

        var queue = new BlockingCollection<(int idx, Mat frame)>(boundedCapacity: dop * 4);

        // producer (decoding thread)
        var producer = Task.Run(() =>
        {
            var idx = 0;
            var frame = new Mat();

            try
            {
                while (!ct.IsCancellationRequested && VideoCapture.Read(frame))
                {
                    queue.Add((idx++, frame), ct);
                    frame = new Mat(); // hand off ownership
                }
            }
            finally
            {
                frame.Dispose();
                queue.CompleteAdding();
            }
        }, ct);

        // worker tasks (CPU-bound)
        var workers = Enumerable.Range(0, dop).Select(_ => Task.Run(() =>
        {
            foreach (var (idx, frame) in queue.GetConsumingEnumerable(ct))
            {
                using var resized = ResizeFrame(frame);
                var ascii = Strategy.ConvertFramePixelsToAscii(resized, (cols, rows), newLineChars: true);
                frame.Dispose();
                asciiFrames[idx] = ascii; // store result
                ConsoleUtils.DisplayProgress(idx + 1, frameCount);
            }
        }, ct)).ToArray();

        await Task.WhenAll(workers).ConfigureAwait(false);
        await producer.ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();

        for (var i = 0; i < frameCount; i++)
        {
            fileConverter?.WriteFrame(asciiFrames[i]!);
        }

        fileConverter?.Dispose();
        Console.Clear();
    }

    private (string?, double processingMs) CalculateFrame(Mat frame, int cols, int rows, bool newLineChars = false)
    {
        var frameStopwatch = Stopwatch.StartNew();

        if (!VideoCapture.Read(frame))
            return (null, -1);         // end of stream

        using var resized = ResizeFrame(frame);
        var asciiFrame = Strategy.ConvertFramePixelsToAscii(resized, (cols, rows), newLineChars);

        var processingMs = frameStopwatch.Elapsed.TotalMilliseconds;

        return (asciiFrame, processingMs);
    }

    private void SyncAudio(int counter, AudioPlayer? audioPlayer, double fps)
    {
        if (WithAudio && audioPlayer is not null && counter % 10 == 0)
        {
            var videoPos = counter / fps;
            audioPlayer.SyncWithVideo(videoPos);
        }
    }

    private AudioPlayer? SetupAudio()
    {
        AudioPlayer? audioPlayer = null;
        if (WithAudio && string.IsNullOrEmpty(Output) &&
            !string.IsNullOrEmpty(AudioFilePath) && File.Exists(AudioFilePath))
        {
            try
            {
                audioPlayer = new AudioPlayer();
                Console.WriteLine("Starting audio playback...");
                _ = Task.Run(() => audioPlayer.PlayAsync(AudioFilePath));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Audio playback error: {ex.Message}");
                audioPlayer?.Dispose();
                audioPlayer = null;
            }
        }

        return audioPlayer;
    }

    /// <summary>
    /// Resize a frame to meet the terminal dimensions
    /// </summary>
    private static Mat ResizeFrame(Mat frame, (int cols, int rows)? dimensions = null)
    {
        // Cache terminal size to avoid frequent recalculations
        var terminalSize = dimensions ?? GetConsoleSize();

        var height = frame.Height;
        var width = frame.Width;
        var cols = terminalSize.cols;
        var rows = terminalSize.rows;

        // Consider both width and height constraints
        // Each ASCII character takes 2 spaces horizontally for proper aspect ratio
        var widthReductionFactor = cols / 2.0 / width;
        var heightReductionFactor = rows / (double)height;

        // Use the smaller reduction factor to ensure the image fits in both dimensions
        var reductionFactor = Math.Min(widthReductionFactor, heightReductionFactor);

        var reducedWidth = (int)(width * reductionFactor);
        var reducedHeight = (int)(height * reductionFactor);

        // Ensure we have at least one pixel in each dimension
        reducedWidth = Math.Max(1, reducedWidth);
        reducedHeight = Math.Max(1, reducedHeight);

        // Create a new Mat buffer and use faster interpolation for better performance
        var resized = new Mat();
        Cv2.Resize(frame, resized, new OpenCvSharp.Size(reducedWidth, reducedHeight), 0, 0, InterpolationFlags.Nearest);
        return resized;
    }

    private static (int cols, int rows) GetConsoleSize() => (Console.WindowWidth, Console.WindowHeight);
}
