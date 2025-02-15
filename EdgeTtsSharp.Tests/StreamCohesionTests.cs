namespace EdgeTtsSharp.Tests;

using System.Security.Cryptography;

public class StreamingTests
{
    [SetUp]
    public void Setup() { }

    [Test]
    public async Task TestStreamCohesion()
    {
        var voice = await EdgeTts.GetVoice("en-US-ChristopherNeural");

        // streaming from Microsoft may not be 100% reliable
        // even though sound quality is not affected so we do best of 3

        for (var i = 0; i < 3; i++)
        {
            await using var stream = voice.GetAudioStream("test");

            // calculate MD5 hash from the stream
            var hash = GetMd5Hash(stream);

            if (hash == "2dfbcc9bb47ca08107c6e5a311518190")
            {
                return;
            }
        }

        Assert.Fail("Stream hash does not match, which may indicate error or updated voice model.");
    }

    private static string GetMd5Hash(Stream stream)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}