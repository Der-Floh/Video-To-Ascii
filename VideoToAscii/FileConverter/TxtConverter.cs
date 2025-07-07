namespace VideoToAscii.FileConverter;

public sealed class TxtConverter : FileConverterBase
{
    public override string FileExtension => ".txt";

    public TxtConverter(string filePath) : base(filePath) { }

    public override void WriteFrame(string frame)
    {
        OutputWriter.WriteLine(frame);
    }
}
