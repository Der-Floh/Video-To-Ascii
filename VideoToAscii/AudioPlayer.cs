using System.Globalization;

using FFMpegCore;

using Iso639;

using NAudio.Wave;

using SoundTouch;
using SoundTouch.Net.NAudioSupport;

using VideoToAscii.Utils;

namespace VideoToAscii;

public sealed class AudioPlayer : IDisposable
{
    private WaveOutEvent? _outputDevice;
    private AudioFileReader? _sourceStream;
    private SoundTouchProcessor? _processor;
    private SoundTouchWaveProvider? _soundTouchProvider;
    private readonly object _syncLock = new();
    private const double SyncThresholdSeconds = 0.05;

    /// <summary>
    /// Asynchronously plays the supplied audio file. The task completes when playback finishes
    /// or is cancelled. Call <see cref="SyncWithVideo"/> any time during playback to keep the
    /// audio clock aligned with an external video clock.
    /// </summary>
    /// <param name="audioFilePath">Full path to the audio file (any NAudio‑supported format).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public async Task PlayAsync(string audioFilePath, CancellationToken cancellationToken = default)
    {
        StopInternal();

        _sourceStream = new AudioFileReader(audioFilePath);
        _processor = new SoundTouchProcessor
        {
            SampleRate = _sourceStream.WaveFormat.SampleRate,
            Channels = _sourceStream.WaveFormat.Channels,
            Tempo = 1f
        };
        _soundTouchProvider = new SoundTouchWaveProvider(_sourceStream, _processor);

        _outputDevice = new WaveOutEvent();
        _outputDevice.Init(_soundTouchProvider);
        _outputDevice.Play();

        try
        {
            await WaitUntilFinishedAsync(cancellationToken);
        }
        finally
        {
            StopInternal();
        }
    }

    /// <summary>
    /// Synchronizes the audio timeline with the given <paramref name="videoSeconds"/>,
    /// gently speeding up or slowing down until the clocks converge.
    /// </summary>
    /// <param name="videoSeconds">Current video clock time in seconds.</param>
    public void SyncWithVideo(double videoSeconds)
    {
        if (_sourceStream == null || _processor == null)
            return;

        var audioSeconds = GetCurrentAudioTimeSeconds();
        var diff = audioSeconds - videoSeconds;    // +ve: audio ahead; -ve: behind

        float newTempo;
        if (Math.Abs(diff) <= SyncThresholdSeconds)
        {
            newTempo = 1f; // already in sync
        }
        else if (diff < 0)
        {
            // Audio is lagging -> speed up proportionally (capped)
            newTempo = (float)Math.Clamp(1 + (-diff * 0.25), 1f, 1.5f);
        }
        else // diff > 0 → audio leads
        {
            // Audio is ahead -> slow down
            newTempo = (float)Math.Clamp(1 - (diff * 0.25), 0.5f, 1f);
        }

        lock (_syncLock)
        {
            if (Math.Abs(_processor.Tempo - newTempo) > 0.01f)
            {
                _processor.Tempo = newTempo;
            }
        }
    }

    /// <summary>Returns current audio play head time in seconds.</summary>
    private double GetCurrentAudioTimeSeconds() =>
        _sourceStream is null
            ? 0
            : (double)_sourceStream.Position / _sourceStream.WaveFormat.AverageBytesPerSecond;

    /// <summary>Waits until playback naturally ends or cancellation is requested.</summary>
    private async Task WaitUntilFinishedAsync(CancellationToken ct)
    {
        while (_outputDevice?.PlaybackState == PlaybackState.Playing)
        {
            await Task.Delay(100, ct);
        }
    }

    private void StopInternal()
    {
        _outputDevice?.Stop();
        _outputDevice?.Dispose();
        _outputDevice = null;

        _sourceStream?.Dispose();
        _sourceStream = null;

        _soundTouchProvider = null;
        _processor = null;
    }

    public void Dispose() => StopInternal();

    /// <summary>
    /// Extract audio from a video file using ffmpeg
    /// </summary>
    /// <param name="videoFilePath">Path to video file</param>
    /// <param name="outputAudioPath">Path to save the extracted audio</param>
    /// <returns>True if extraction was successful, false otherwise</returns>
    public static bool ExtractAudioFromVideo(string videoFilePath, string outputAudioPath, CultureInfo? lang = null)
    {
        try
        {
            if (!File.Exists(videoFilePath))
            {
                Console.WriteLine("Video file does not exist.");
                return false;
            }

            var mediaInfo = FFProbe.Analyse(videoFilePath);
            var audioIndex = RetrieveAudioChannelByLang(mediaInfo, lang);

            var canCopy =
               mediaInfo.PrimaryAudioStream?.CodecName == "pcm_s16le" &&
               mediaInfo.PrimaryAudioStream?.SampleRateHz == 44100 &&
               mediaInfo.PrimaryAudioStream?.Channels == 2;

            static void uiProgress(double percent) => ConsoleUtils.DisplayProgress(percent, "Extracting audio from video...");

            FFMpegArguments
               .FromFileInput(videoFilePath)
               .OutputToFile(outputAudioPath, overwrite: true, options =>
               {
                   options.WithCustomArgument($"-map 0:a:{audioIndex} -vn")       // only audio
                          .WithCustomArgument("-threads 2");

                   if (canCopy)
                   {
                       // near-instant remux
                       options.WithAudioCodec("copy")
                              .ForceFormat("wav");
                   }
                   else
                   {
                       options.WithAudioCodec("pcm_s16le");

                       // resample only if we really have to
                       if (mediaInfo.PrimaryAudioStream?.SampleRateHz != 44100)
                           options.WithAudioSamplingRate(44100);

                       // stereo down-mix if needed
                       if (mediaInfo.PrimaryAudioStream?.Channels > 2)
                           options.WithCustomArgument("-ac 2");
                   }
               })
               .NotifyOnProgress(uiProgress, mediaInfo.Duration)
               .ProcessSynchronously();

            return File.Exists(outputAudioPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting audio: {ex.Message}");
            return false;
        }
    }

#pragma warning disable CS0612 // ISO 639-2/B needed
    private static int RetrieveAudioChannelByLang(IMediaAnalysis mediaInfo, CultureInfo? lang = null)
    {
        lang ??= CultureInfo.CurrentCulture;
        var isoLang = Language.FromCulture(lang);

        var allAudio = mediaInfo.AudioStreams;
        // try: dedicated Language property

        var selectedAudio = allAudio.FirstOrDefault(s =>
                               string.Equals(s.Language, lang.ThreeLetterISOLanguageName, StringComparison.OrdinalIgnoreCase)
                               || string.Equals(s.Language, isoLang.Part2B, StringComparison.OrdinalIgnoreCase));

        // fall back to the raw ffprobe tags
        selectedAudio ??= allAudio.FirstOrDefault(s => s.Tags?.TryGetValue("language", out var tag) == true
                                            && (string.Equals(tag, lang.ThreeLetterISOLanguageName, StringComparison.OrdinalIgnoreCase)
                                            || string.Equals(tag, isoLang.Part2B, StringComparison.OrdinalIgnoreCase)));

        selectedAudio ??= allAudio.FirstOrDefault(s => s.Tags?.TryGetValue("lang", out var tag) == true
                                            && (string.Equals(tag, lang.ThreeLetterISOLanguageName, StringComparison.OrdinalIgnoreCase)
                                            || string.Equals(tag, isoLang.Part2B, StringComparison.OrdinalIgnoreCase)));

        // finally fall back to the primary audio
        selectedAudio ??= mediaInfo.PrimaryAudioStream;

        // where is that stream inside the *audio* group
        var audioIndexInType = allAudio
                               .OrderBy(s => s.Index)
                               .Select((s, i) => new { s, i })
                               .First(x => x.s == selectedAudio).i;

        return audioIndexInType;
    }
#pragma warning restore CS0612 // ISO 639-2/B needed
}
