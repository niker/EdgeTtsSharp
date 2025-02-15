namespace EdgeTtsSharp.NAudio;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using global::NAudio.Wave;
using global::NAudio.Wave.SampleProviders;

internal static class Playback
{
    public static async ValueTask PlayStream(Stream stream, float volume = 1f, float speed = 0f, CancellationToken ct = default)
    {
#if NETSTANDARD2_0
        using var reader = new StreamMediaFoundationReader(stream);
#else
        await using var reader = new StreamMediaFoundationReader(stream);
#endif
        var sampleProvider = new SmbPitchShiftingSampleProvider(reader.ToSampleProvider())
        {
            PitchFactor = (float)Math.Pow(2.0, speed / 100.0)
        };

        var volumeProvider = new VolumeSampleProvider(sampleProvider)
        {
            Volume = volume
        };

        var directSoundOut = new DirectSoundOut();
        directSoundOut.Init(volumeProvider.ToWaveProvider());
        directSoundOut.Play();

        try
        {
            while ((directSoundOut.PlaybackState == PlaybackState.Playing) && !ct.IsCancellationRequested)
            {
                await Task.Delay(100, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // do not throw on cancellation
        }
        finally
        {
            directSoundOut.Stop();
        }
    }
}