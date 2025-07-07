using System.Text;

using OpenCvSharp;

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
    public virtual async Task Render(VideoCapture cap, string? output = null, string? outputFormat = null, bool withAudio = false, string? audioFilePath = null)
    {
        var videoRenderer = new VideoRenderer(this, cap)
        {
            Output = output,
            OutputFormat = outputFormat,
            WithAudio = withAudio,
            AudioFilePath = audioFilePath
        };
        await videoRenderer.Render();
    }
}
