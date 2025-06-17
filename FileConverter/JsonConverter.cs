using System.Text;
using System.Text.Json;

using VideoToAscii.Extensions;

namespace VideoToAscii.FileConverter;

public sealed class JsonConverter : FileConverterBase
{
    private bool _isFirstFrame = true;

    public JsonConverter(string filePath) : base(filePath) { }

    protected override void BeginFile() => OutputWriter.WriteLine("[");
    protected override void EndFile()
    {
        OutputWriter.WriteLine();
        OutputWriter.WriteLine("]");
    }

    public override void WriteFrame(string frame)
    {
        var lines = frame.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);

        // Remove last line breaks
        if (lines.Length > 2)
            Array.Resize(ref lines, lines.Length - 2);

        if (lines.Length == 0)
            return;

        var sb = new StringBuilder();

        var frameSB = new StringBuilder();
        foreach (var line in lines)
        {
            var escapedLine = JsonSerializer.Serialize(line);
            frameSB.AppendLine($"\t\t{escapedLine},");
        }
        frameSB.TrimEnd('\r', '\n', ',');
        frameSB.AppendLine();

        if (!_isFirstFrame)
        {
            sb.AppendLine(",");
            sb.Append('\t');
        }
        else
        {
            sb.Append('\t');
            _isFirstFrame = false;
        }

        sb.AppendLine("[");
        sb.Append(frameSB);
        sb.Append("\t]");
        OutputWriter.Write(sb);
    }
}
