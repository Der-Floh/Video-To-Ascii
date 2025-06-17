using System.Diagnostics;
using System.Text;

using OpenCvSharp;

using VideoToAscii.FileConverter;
using VideoToAscii.Utils;

namespace VideoToAscii.RenderStrategy;

public class AsciiStrategy : IRenderStrategy
{
    protected readonly ImageProcessor ImageProcessor;

    public AsciiStrategy()
    {
        ImageProcessor = new ImageProcessor();
    }

    /// <summary>
    /// Replace all pixels with colored chars and return the resulting string
    /// </summary>
    /// <param name="frame">A single video frame</param>
    /// <param name="dimensions">Printing area dimensions [cols, rows]</param>
    /// <param name="newLineChars">Whether to add newline characters</param>
    /// <returns>String with ASCII representation</returns>
    public virtual string ConvertFramePixelsToAscii(Mat frame, (int cols, int rows) dimensions, bool newLineChars = false)
    {
        var cols = dimensions.cols;
        var rows = dimensions.rows;
        var h = frame.Height;
        var w = frame.Width;

        var printingWidth = Math.Min(cols, w * 2) / 2;
        var pad = Math.Max(cols - (printingWidth * 2), 0);

        // Reuse this space for padding
        var padSpaces = pad > 0 ? new string(' ', pad) : string.Empty;

        // Create a string array for each row to allow parallel processing
        var rowStrings = new string[h - 1];

        // Process rows in parallel
        Parallel.For(0, h - 1, j =>
        {
            // Pre-calculate the estimated capacity for this row
            // Each pixel typically results in ~10 chars (2 ASCII chars + color codes)
            var rowCapacity = (printingWidth * 10) + pad + 2; // +2 for newlines
            var rowSb = new StringBuilder(rowCapacity);

            // Process each pixel in this row
            for (var i = 0; i < printingWidth; i++)
            {
                var pixel = frame.At<Vec3b>(j, i);
                rowSb.Append(ApplyPixelToAsciiStrategy(pixel));
            }

            // Handle row endings
            if (newLineChars)
            {
                rowSb.Append('\n');
            }
            else if (pad > 0)
            {
                rowSb.Append(padSpaces);
            }

            // Store the completed row
            rowStrings[j] = rowSb.ToString();
        });

        // Combine all rows into final result
        var finalCapacity = ((h - 1) * ((printingWidth * 10) + pad + 2)) + 2; // +2 for final newline
        var finalSb = new StringBuilder(finalCapacity);

        foreach (var rowString in rowStrings)
        {
            finalSb.Append(rowString);
        }

        finalSb.Append("\r\n");
        return finalSb.ToString();
    }

    /// <summary>
    /// Apply strategy to convert a pixel to ASCII
    /// </summary>
    /// <param name="pixel">BGR pixel data</param>
    /// <returns>ASCII representation</returns>
    protected virtual string ApplyPixelToAsciiStrategy(Vec3b pixel) => ImageProcessor.PixelToAscii(pixel);

    /// <summary>
    /// Render the video using the ASCII strategy
    /// </summary>
    public virtual void Render(VideoCapture cap, string? output = null, string? outputFormat = null, bool withAudio = false, string? audioFilePath = null)
    {
        double length = cap.FrameCount;
        var fps = cap.Fps;
        if (fps <= 0)
            fps = 30;

        const int MaxFrameSkip = 10;        // <- avoid "black-hole" skipping
        var frameDurationMs = 1000.0 / fps; // <- target budget

        IFileConverter? fileConverter = null;
        if (!string.IsNullOrEmpty(output))
        {
            fileConverter = outputFormat switch
            {
                ".sh" => new ShConverter(output, fps),
                ".bat" => new BatConverter(output, fps),
                ".ps1" => new Ps1Converter(output, fps),
                ".json" => new JsonConverter(output),
                _ => new TxtConverter(output),
            };
        }

        var counter = 0;
        var timeDelta = 1.0 / fps;

        (var cols, var rows) = GetConsoleSize();

        /* ---------- AUDIO SETUP ---------- */
        AudioPlayer? audioPlayer = null;
        if (withAudio && string.IsNullOrEmpty(output) &&
            !string.IsNullOrEmpty(audioFilePath) && File.Exists(audioFilePath))
        {
            try
            {
                audioPlayer = new AudioPlayer();
                Console.WriteLine("Starting audio playback...");
                _ = Task.Run(() => audioPlayer.PlayAsync(audioFilePath));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Audio playback error: {ex.Message}");
                audioPlayer?.Dispose();
                audioPlayer = null;
            }
        }

        try
        {
            using var frame = new Mat();
            var sb = new StringBuilder();
            var writer = Console.Out;
            int prevCols = 0, prevRows = 0;
            while (true)
            {
                /* ---------- START of a frame ---------- */
                var frameStopwatch = Stopwatch.StartNew();

                // Always re-query terminal size
                (cols, rows) = GetConsoleSize();
                if (prevCols != cols || prevRows != rows)
                    Console.Clear();
                prevCols = cols;
                prevRows = rows;

                if (!cap.Read(frame))
                    break;         // end of stream

                /* ---------- LIVE TERMINAL RENDER ---------- */
                if (string.IsNullOrEmpty(output))
                {
                    sb.Append("\u001b[0;0H");      // cursor to (0,0)

                    using var resized = ResizeFrame(frame, (cols, rows));
                    var asciiFrame = ConvertFramePixelsToAscii(resized, (cols, rows));
                    sb.Append(asciiFrame);

                    /* sync audio every 10 frames */
                    if (withAudio && audioPlayer is not null && counter % 10 == 0)
                    {
                        var videoPos = counter / fps;
                        audioPlayer.SyncWithVideo(videoPos);
                    }

                    //Console.Write(sb);
                    writer.Write(sb);

                    /* ---------- MEASURE + FRAME SKIP ---------- */
                    var processingMs = frameStopwatch.Elapsed.TotalMilliseconds;

                    if (processingMs > frameDurationMs)
                    {
                        // how many *additional* frames does this delay amount to?
                        var framesBehind = (int)(processingMs / frameDurationMs) - 1;
                        var framesToSkip = Math.Min(framesBehind, MaxFrameSkip);

                        for (var s = 0; s < framesToSkip && cap.Grab(); s++)
                        {
                            counter++;  // keep counter consistent
                        }
                    }
                    else if (processingMs < frameDurationMs)   // finished early → small sleep
                    {
                        Thread.Sleep((int)(frameDurationMs - processingMs));
                    }
                }
                /* ---------- FILE OUTPUT ---------- */
                else
                {
                    ConsoleUtils.DisplayProgress(counter, (int)length);
                    using var resized = ResizeFrame(frame);
                    var asciiFrame = ConvertFramePixelsToAscii(resized, (cols, rows), true);
                    fileConverter?.WriteFrame(asciiFrame);
                }

                counter++;
                sb.Clear();
            }
        }
        finally
        {
            /* ---------- cleanup ---------- */
            if (audioPlayer is not null)
            {
                try
                {
                    audioPlayer.Dispose();
                }
                catch { /* ignore */ }
            }
            fileConverter?.Dispose();
            Console.Clear();
        }
    }

    /// <summary>
    /// Resize a frame to meet the terminal dimensions
    /// </summary>
    protected static Mat ResizeFrame(Mat frame, (int cols, int rows)? dimensions = null)
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
        Cv2.Resize(frame, resized, new Size(reducedWidth, reducedHeight), 0, 0, InterpolationFlags.Nearest);
        return resized;
    }

    /// <summary>
    /// Get console size
    /// </summary>
    private static (int cols, int rows) GetConsoleSize() => (Console.WindowWidth, Console.WindowHeight);
}
