using System.Text;

namespace VideoToAscii.FileConverter;

public sealed class Ps1Converter : FileConverterBase
{
    public Ps1Converter(string filePath, double frameRate) : base(filePath, frameRate) { }

    protected override void BeginFile()
    {
        OutputWriter.WriteLine($"Write-Host \"{ESC}[?25l\" -NoNewline");
        OutputWriter.WriteLine("Clear-Host");
        OutputWriter.WriteLine("[System.Console]::SetCursorPosition(0, 0)");
    }

    public override void WriteFrame(string frame)
    {
        var lines = frame.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);

        // Remove last line breaks
        if (lines.Length > 2)
            Array.Resize(ref lines, lines.Length - 2);

        if (lines.Length == 0)
            return;

        frame = string.Join(Environment.NewLine, lines);

        var sb = new StringBuilder();

        var delayMs = (int)Math.Round(1000.0 / FrameRate);
        if (delayMs > 1)
            sb.AppendLine($"Start-Sleep -Milliseconds {delayMs}");

        sb.AppendLine("[System.Console]::SetCursorPosition(0, 0)");

        sb.AppendLine("Write-Host @'");
        sb.AppendLine(frame);
        sb.AppendLine("'@ -NoNewline");

        OutputWriter.WriteLine(sb);
    }
}
