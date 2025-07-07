namespace VideoToAscii.FileConverter;

public interface IFileConverter : IDisposable
{
    string FileExtension { get; }
    void WriteFrame(string frame);
}
