namespace VideoToAscii.FileConverter;

public sealed class TxtConverter : FileConverterBase
{
    public TxtConverter(string filePath) : base(filePath) { }

    public override void WriteFrame(string frame)
    {
        OutputWriter.WriteLine(frame);
    }
}
