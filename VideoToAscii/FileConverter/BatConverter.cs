using System.Text;

namespace VideoToAscii.FileConverter;

public sealed class BatConverter : FileConverterBase
{
    public override string FileExtension => ".bat";

    public BatConverter(string filePath, double frameRate) : base(filePath, frameRate) { }

    protected override void BeginFile()
    {
        OutputWriter.WriteLine(":: dummy line to absorb BOM");
        OutputWriter.WriteLine("@echo off");
        OutputWriter.WriteLine("setlocal EnableDelayedExpansion");
        OutputWriter.WriteLine("chcp 65001 >nul");
        OutputWriter.WriteLine($"echo {ESC}[?25l");
        OutputWriter.WriteLine("cls");
        OutputWriter.WriteLine($"echo {ESC}[H");
    }

    public override void WriteFrame(string frame)
    {
        var sb = new StringBuilder();

        var delayMs = (int)Math.Round(1000.0 / FrameRate);
        if (delayMs > 1)
            sb.AppendLine($"ping -n 1 -w {delayMs} 127.0.0.1 >nul");

        sb.AppendLine($"echo {ESC}[H");

        var lines = frame.Split(["\r\n", "\n"], StringSplitOptions.None);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var sanitizedLine = EscapeForBatch(line);
            sb.AppendLine($"echo {sanitizedLine}");
        }

        OutputWriter.Write(sb);
    }

    private static string EscapeForBatch(string input)
    {
        return input
            .Replace("^", "^^")   // Escape escape character
            .Replace("&", "^&")   // Command separator
            .Replace("|", "^|")   // Pipe
            .Replace(">", "^>")   // Redirection
            .Replace("<", "^<")   // Redirection
            .Replace("%", "%%")   // Environment variables
            .Replace("!", "^^!"); // Delayed expansion
    }
}
