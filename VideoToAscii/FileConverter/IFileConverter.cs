namespace VideoToAscii.FileConverter;

public interface IFileConverter : IDisposable
{
    void WriteFrame(string frame);
}
