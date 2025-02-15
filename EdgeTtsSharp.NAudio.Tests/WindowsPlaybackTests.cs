namespace EdgeTtsSharp.NAudio.Tests;

public class WindowsPlaybackTests
{
    [SetUp]
    public void Setup() { }

    [Test]
    public async Task TestWindowsPlayback()
    {
        var voice = await EdgeTts.GetVoice("en-US-ChristopherNeural");
        await voice.PlayText("App will bind the authentication session to the provided nonce, and the same value will be included in the response.");
    }
}