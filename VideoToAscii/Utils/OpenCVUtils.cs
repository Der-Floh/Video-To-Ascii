using System.Runtime.InteropServices;

using OpenCvSharp;

namespace VideoToAscii.Utils;

public static class OpenCVUtils
{
    public static VideoCapture TryOpenWithHardwareAcc(string path)
    {
        foreach (var (backend, accel) in GetCandidates())
        {
            VideoCapture? cap = null;
            try
            {
                cap = accel == VideoAccelerationType.None
                    ? new VideoCapture(path, backend) // software
                    : new VideoCapture(path, backend,
                    [
                        (int)VideoCaptureProperties.HwAcceleration, (int)accel,
                        (int)VideoCaptureProperties.HwDevice, 0 // first GPU
                    ]);

                if (cap.IsOpened())
                    return cap;
            }
            catch { }
            finally
            {
                // dispose half-opened capture to release FDs / GPU handles
                if (cap is not null && !cap.IsOpened() && !cap.IsDisposed)
                    cap.Dispose();
            }
        }

        throw new InvalidOperationException($"Unable to open \"{path}\" with any video-IO backend.");
    }

    private static IEnumerable<(VideoCaptureAPIs Backend, VideoAccelerationType Accel)> GetCandidates()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            yield return (VideoCaptureAPIs.FFMPEG, VideoAccelerationType.D3D11);
            yield return (VideoCaptureAPIs.MSMF, VideoAccelerationType.D3D11);
            yield return (VideoCaptureAPIs.FFMPEG, VideoAccelerationType.MFX);   // Intel iGPU
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            yield return (VideoCaptureAPIs.FFMPEG, VideoAccelerationType.VAAPI);
            yield return (VideoCaptureAPIs.GSTREAMER, VideoAccelerationType.VAAPI);
            yield return (VideoCaptureAPIs.FFMPEG, VideoAccelerationType.MFX);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // FFmpeg uses VideoToolbox internally when available
            yield return (VideoCaptureAPIs.FFMPEG, VideoAccelerationType.Any);
        }

        // cross-platform “try anything the build supports”
        yield return (VideoCaptureAPIs.FFMPEG, VideoAccelerationType.Any);

        // final fallback – fully software, backend auto-selection
        yield return (VideoCaptureAPIs.ANY, VideoAccelerationType.None);
    }
}
