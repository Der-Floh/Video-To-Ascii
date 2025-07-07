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
            return true;
        }
        catch (FFMpegException)
        {
            return false;
        }
    }
}

