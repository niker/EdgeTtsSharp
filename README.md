# EdgeTTS Sharp  

## Overview  
**EdgeTTS Sharp** is a .NET Standard 2.1 library that provides an easy-to-use interface for text-to-speech (TTS) synthesis using Microsoft's Edge voices. It is designed to work across different systems and environments, offering flexible ways to handle audio streams. Whether you need real-time streaming, file saving, or direct playback, EdgeTTS Sharp makes it simple to integrate high-quality TTS into your applications.  

### Key Features  
✔ **Real-time audio streaming** – Start playback immediately as the first audio packet arrives.  
✔ **Save to file** – Store generated speech as an audio file while streaming.  
✔ **Stream to third-party services** – Send audio directly to a network stream (e.g., HTTP response).  
✔ **Cross-platform support** – Works on any system supporting .NET Standard 2.1.  
✔ **Windows-specific playback support** – Utilize NAudio for local playback on Windows.  

---

## EdgeTtsSharp  

### `EdgeTts.GetAudioStream`  
Returns a stream that starts playing immediately once the first audio packet arrives.  

```csharp
var voice = await EdgeTts.GetVoice("en-US-ChristopherNeural");
await using var stream = voice.GetAudioStream("test");

// use the stream here
```

### `EdgeTts.SaveAudioToFile`  
Uses `GetAudioStream` internally and redirects the stream to a file. The saving process starts immediately when the first packet arrives.  

```csharp
var voice = await EdgeTts.GetVoice("en-US-ChristopherNeural");
await voice.SaveAudioToFile("test", @"d:\test\test1.mp3");
```

### `EdgeTts.StreamText`  
Streams audio directly to a specified output stream, such as an HTTP response body. If HTTP headers are set correctly, browsers can start playback before the download finishes, showing the progress indicator as the audio loads.  

```csharp
[HttpGet("api/audio/{id}")]
[Produces("audio/mpeg")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]

...

var voice = await EdgeTts.GetVoice("en-US-ChristopherNeural");

// stream audio directly to HttpResponse as it's being downloaded
await voice.StreamText(this.Response.Body, "test");
```

### `EdgeTts.GetVoices`  
Provides an offline lookup of all available voices from **Edge_tts_sharp**. If the list is outdated, feel free to report it.  

### `EdgeTts.GetVoice`  
Retrieves a specific voice using its short name (e.g., `"en-US-ChristopherNeural"`).  

---

## EdgeTtsSharp.NAudio (Windows-Specific)  
This module is for Windows users who want local playback using **NAudio**. However, due to NAudio's limitations, it requires downloading the entire stream before playback begins.  

### `EdgeTtsWindowsPlayback.PlayText`  
```csharp
var voice = await EdgeTts.GetVoice("en-US-ChristopherNeural");
await voice.PlayText("test");
```

---

## License  
**EdgeTtsSharp** is licensed under the **MIT License**.  

> The configuration file containing the list of available voices was taken from [Entity-Now/Edge_tts_sharp](https://github.com/Entity-Now/Edge_tts_sharp), and the authentication method is also inspired by that project.  
