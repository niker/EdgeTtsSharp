namespace EdgeTtsSharp.NAudio;

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Structures;

/// <summary>
/// Use Edge TTS
/// </summary>
public static class EdgeTtsWindowsPlayback
{
    /// <summary>
    /// Play TTS message directly on this system.
    /// NAudio requires for the download to be completed before starting playback.
    /// </summary>
    public static async ValueTask PlayText(this Voice voice, string text, PlaybackSettings? playbackSettings = null, CancellationToken ct = default)
    {
        var audioStream = voice.GetAudioStream(text, playbackSettings, ct);
        using var ms = new MemoryStream();
        await audioStream.CopyToAsync(ms, ct);
        playbackSettings ??= new PlaybackSettings();
        await Playback.PlayStream(ms, playbackSettings.Volume, playbackSettings.Rate, ct);
    }
}