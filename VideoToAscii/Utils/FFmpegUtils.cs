using FFMpegCore;
using FFMpegCore.Exceptions;
using FFMpegCore.Helpers;

namespace VideoToAscii.Utils;

public static class FFmpegUtils
{
    public static bool IsFFmpegInstalled()
    {
        try
        {
            FFMpegHelper.VerifyFFMpegExists(new FFOptions());
            var path = GlobalFFOptions.GetFFMpegBinaryPath();
            return File.Exists(path);
        }
        catch (FFMpegException)
        {
            return false;
        }
    }
}

