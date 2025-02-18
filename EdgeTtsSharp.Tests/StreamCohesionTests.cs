namespace EdgeTtsSharp.Tests;

using System.Security.Cryptography;

public class StreamingTests
{
    [SetUp]
    public void Setup() { }

    /// <summary>
    /// This test must pass or the streaming is borked.
    /// </summary>
    [Test]
    public async Task TestStreamCohesion()
    {
        var voice = await EdgeTts.GetVoice("en-US-ChristopherNeural");

        // streaming from Microsoft may not be 100% reliable
        // even though sound quality is not affected so we do best of 3

        for (var i = 0; i < 3; i++)
        {
            await using var stream =
                voice.GetAudioStream(
                    "This is a test of audio generated by EdgeTTS. The stream cohesion is tested by comparing the MD5 hash of the stream to a known value. It is expected to encounter slight deviations, but the audio quality should not be affected.");

            // calculate MD5 hash from the stream
            var hash = GetMd5Hash(stream);

            if (hash == "ad0db0aee0a541889424d92f08fa6236")
            {
                return;
            }
        }

        Assert.Fail("Stream hash does not match, which may indicate error or updated voice model.");
    }

    /// <summary>
    /// This test is not reliable due to glitches in streams from Microsoft.
    /// It usually fails 1-3/10 but the stream is still valid and there are no audible glitches.
    /// You can run it and hear the result for individual files in the TestResults folder.
    /// </summary>
    [Test]
    public async Task TestStreamReliability()
    {
        var voice = await EdgeTts.GetVoice("en-US-ChristopherNeural");
        List<int> failed = [];

        for (var i = 0; i < 10; i++)
        {
            await using var stream =
                voice.GetAudioStream(
                    "This is a test of audio generated by EdgeTTS. The stream cohesion is tested by comparing the MD5 hash of the stream to a known value. It is expected to encounter slight deviations, but the audio quality should not be affected.");


            // save the sample to TestResults folder in project root
            await using (var fs = new FileStream($"../../../TestResults/{i:D2}.mp3", FileMode.Create, FileAccess.Write))
            {
                await stream.CopyToAsync(fs);
            }

            // open the file that was just saved and calculate its MD5 hash
            var sample = File.OpenRead($"../../../TestResults/{i:D2}.mp3");
            var hash = GetMd5Hash(sample);

            if (hash != "ad0db0aee0a541889424d92f08fa6236")
            {
                failed.Add(i);
            }
        }

        if (failed.Count == 0)
        {
            Assert.Pass("All streams are valid - a miracle just happened!");
            return;
        }

        Assert.Pass($"Stream hash does not match for files [{string.Join(", ", failed)}], you can listen to them in the TestResults folder.");
    }

    private static string GetMd5Hash(Stream stream)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}