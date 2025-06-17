namespace VideoToAscii.FileConverter;

public sealed class ShConverter : FileConverterBase
{
    public ShConverter(string filePath, double frameRate) : base(filePath, frameRate) { }

    protected override void BeginFile()
    {
        OutputWriter.WriteLine("#!/bin/bash");
        OutputWriter.WriteLine("echo '\\033[2J'");
        OutputWriter.WriteLine("echo '\\u001b[0;0H'");
    }

    public override void WriteFrame(string frame)
    {
        var sleepSeconds = 1.0 / FrameRate;
        OutputWriter.WriteLine($"sleep {sleepSeconds:0.###}");
        OutputWriter.WriteLine($"echo '{frame}'");
        OutputWriter.WriteLine("echo '\\u001b[0;0H'");
    }
}
