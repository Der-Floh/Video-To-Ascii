using System.Text;

namespace VideoToAscii.FileConverter;

public abstract class FileConverterBase : IFileConverter
{
    public abstract string FileExtension { get; }

    protected const string ESC = "\u001b";
    protected TextWriter OutputWriter { get; }
    protected double FrameRate { get; }

    protected FileConverterBase(string filePath, double frameRate = 30)
    {
        OutputWriter = new StreamWriter(filePath, false, new UTF8Encoding(true));
        FrameRate = frameRate;
        BeginFile();
    }

    /// <summary>Called once from the base constructor so a subclass can write its header.</summary>
    protected virtual void BeginFile() { }

    /// <summary>Called once from Dispose so a subclass can write its footer.</summary>
    protected virtual void EndFile() { }

    public abstract void WriteFrame(string frame);

    public void Dispose()
    {
        EndFile();
        OutputWriter.Dispose();
    }
}
