﻿#pragma warning disable CS1998 // Limited support for ValueTask
namespace EdgeTtsSharp
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensions;
    using Structures;

    /// <summary>
    /// Use Edge TTS
    /// </summary>
    public static class EdgeTts
    {
        private static readonly byte[] EolSeq = "\r\n"u8.ToArray();
        private static readonly byte[] EocSeq = [0x00, 0x80];
        private static readonly byte[] EosSeq = "\0gX"u8.ToArray();
        private static readonly byte[] RequestIdSeq = "X-RequestId:"u8.ToArray();
        private static readonly byte[] ContentTypeSeq = "Content-Type:"u8.ToArray();
        private static readonly byte[] StreamIdSeq = "X-StreamId:"u8.ToArray();
        private static readonly byte[] PathAudioSeq = "Path:audio"u8.ToArray();
        private static readonly byte[] DropChunkBoundarySeq = [0x00, 0x80, 0x58, 0x2D];
        private static readonly byte[] PathResponseSeq = "Path:response"u8.ToArray();
        private static readonly byte[] PathTurnEndSeq = "Path:turn.end"u8.ToArray();
        private static readonly byte[] PathTurnStartSeq = "Path:turn.start"u8.ToArray();

        /// <summary>
        /// When a communication or processing error occurs, this action is called
        /// </summary>
        public static Func<Exception, ValueTask>? ErrorAction = async e => Console.WriteLine(e.Message);

        /// <summary>
        /// Stream to target stream
        /// </summary>
        public static async ValueTask StreamTo(this Voice voice,
                                               Stream targetStream,
                                               string text,
                                               PlaybackSettings? playbackSettings,
                                               Action? streamingFinished,
                                               CancellationToken ct = default)
        {
            playbackSettings ??= new PlaybackSettings();

            var sendRequestId = Guid.NewGuid().ToString("N");
            var incoming = new List<byte>();

            var wss = new WebSocketClientWrapper(
                $"wss://speech.platform.bing.com/consumer/speech/synthesize/readaloud/edge/v1?TrustedClientToken=6A5AA1D4EAFF4E9FB37E23D68491D6F4&Sec-MS-GEC={GenerateSecMsGecToken()}&Sec-MS-GEC-Version=1-130.0.2849.68",
                async (ws, data) =>
                {
                    try
                    {
                        incoming.AddRange(data);

                        // find first occurence of any terminator sequence in the incoming list,
                        // isolate a single candidate chunk and repeat until we have no valid chunks left
                        // or we reach the stream terminator
                        var eocIndex = FindSequenceIndex(incoming, EocSeq);
                        var eosIndex = FindSequenceIndex(incoming, EosSeq);
                        if ((eocIndex == -1) && (eosIndex == -1)) // eoc not found yet and not the end of stream
                        {
                            return;
                        }

                        while ((eocIndex != -1) || (eosIndex != -1))
                        {
                            // aggregate information about the terminator sequence
                            var endIndex = Math.Max(eocIndex, eosIndex);
                            var terminatorLength = eocIndex == -1 ? EosSeq.Length : EocSeq.Length;

                            // check the data immediately following the terminator sequence
                            // to determine if it is part of the binary data or a chunk boundary
                            var terminationSequenceContinuation = incoming.Skip(endIndex).Take(Math.Min(DropChunkBoundarySeq.Length, incoming.Count - endIndex)).ToList();

                            // are we positioned before the first chunk boundary?
                            if (endIndex == 0)
                            {
                                eocIndex = FindSequenceIndex(incoming, EocSeq);
                                eosIndex = FindSequenceIndex(incoming, EosSeq);
                                continue;
                            }

                            // isolate the chunk and remove it from the incoming list
                            var chunk = incoming.Take(endIndex).ToList();
                            incoming.RemoveRange(0, endIndex + terminatorLength);

                            // process the chunk, parse any text and pass binary data to the target stream
                            await ProcessChunk(chunk, targetStream, ct);

                            // if the terminator sequence is not a chunk boundary, we need to retain it
                            if (!SequenceStartsWith(terminationSequenceContinuation, DropChunkBoundarySeq) && (eosIndex == -1))
                            {
                                // this is not a real chunk boundary but part of the binary data, so we need to retain the terminator
                                await targetStream.WriteAsync(EocSeq, ct);
                            }

                            // check if we have reached the end of the stream
                            if (eosIndex != -1)
                            {
                                await ws.Close();
                                return;
                            }

                            // find the next terminator sequence
                            eocIndex = FindSequenceIndex(incoming, EocSeq);
                            eosIndex = FindSequenceIndex(incoming, EosSeq);
                        }

                        await targetStream.FlushAsync(ct);
                    }
                    catch (Exception ex)
                    {
                        if (ErrorAction != null)
                        {
                            await ErrorAction(ex);
                        }
                    }
                },
                async () =>
                {
                    try
                    {
                        await targetStream.FlushAsync(ct);
                        streamingFinished?.Invoke();
                    }
                    catch
                    {
                        // ignore
                    }
                },
                async ex =>
                {
                    if (ErrorAction != null)
                    {
                        await ErrorAction(ex);
                    }
                },
                ct);

            await wss.Connect();
            await wss.Send(CreateAudioFormatRequest(voice.SuggestedCodec));
            await wss.Send(CreateSsmlRequest(sendRequestId, voice.Locale, voice.Name, playbackSettings.Rate, (int)playbackSettings.Volume * 100, text));
        }

        /// <summary>
        /// Get a stream that contains the audio
        /// </summary>
        public static PipedAudioStream.Reader GetAudioStream(this Voice voice, string text, PlaybackSettings? playbackSettings = null, CancellationToken ct = default)
        {
            var pipeStream = new PipedAudioStream();
            _ = Task.Run(async () =>
                         {
                             try
                             {
                                 await voice.StreamTo(pipeStream, text, playbackSettings, () => pipeStream.Complete(), ct);
                             }
                             catch (Exception ex)
                             {
                                 Console.WriteLine(ex.Message);
                             }
                         },
                         ct);

            return pipeStream.GetReader();
        }


        /// <summary>
        /// Save audio to a file
        /// </summary>
        public static async ValueTask SaveAudioToFile(this Voice voice, string text, string path, PlaybackSettings? playbackSettings = null, CancellationToken ct = default)
        {
            var audioStream = voice.GetAudioStream(text, playbackSettings, ct);
            await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            await audioStream.CopyToAsync(fs, ct);
        }

        /// <summary>
        /// Stream audio to a target stream
        /// </summary>
        public static async ValueTask StreamText(this Voice voice, Stream targetStream, string text, PlaybackSettings? playbackSettings = null, CancellationToken ct = default)
        {
            var streaming = true;
            await voice.StreamTo(targetStream, text, playbackSettings, () => streaming = false, ct);
            while (!ct.IsCancellationRequested && streaming)
            {
                await Task.Delay(50, ct);
            }
        }

        /// <summary>
        /// Get all available voices from offline lookup (this may not be up to date)
        /// </summary>
        public static async Task<List<Voice>> GetVoices()
        {
            return await Assembly
                         .GetExecutingAssembly()
                         .ParseEmbeddedJson<List<Voice>>("EdgeTtsSharp.Embedded.VoiceList.json")
                   ?? throw new Exception("Voice list not found.");
        }

        /// <summary>
        /// Get a voice from offline lookup by short name
        /// </summary>
        /// <param name="shortName">For example 'en-US-ChristopherNeural'</param>
        public static async Task<Voice> GetVoice(string shortName)
        {
            return (await GetVoices()).FirstOrDefault(v => v.ShortName.Equals(shortName, StringComparison.InvariantCultureIgnoreCase))
                   ?? throw new Exception($"Voice [{shortName}] not found.");
        }

        private static async ValueTask ProcessChunk(List<byte> chunk, Stream targetStream, CancellationToken ct)
        {
            // find the first occurence of the EOL sequence
            var eolIndex = FindSequenceIndex(chunk, EolSeq);
            if (eolIndex == -1)
            {
                // this is the last line or blob of pure binary data
                await targetStream.WriteAsync(chunk.ToArray().AsMemory(0, chunk.Count), ct);
            }

            // process the chunk line by line
            var processed = 0;
            while (eolIndex != -1)
            {
                var line = chunk.Skip(processed).Take(eolIndex - processed).ToList();

                // process the line and determine the action to take according to the content
                var chunkAction = ProcessLine(line);

                // move the processed index to the start of the next line
                processed = eolIndex + EolSeq.Length;

                // if the action is to drop the chunk, we can stop processing
                if (chunkAction == ChunkAction.Drop)
                {
                    return;
                }

                // if the action is to pass the binary data to the target stream, we can do that now
                if (chunkAction == ChunkAction.Binary)
                {
                    await targetStream.WriteAsync(chunk.ToArray().AsMemory(processed, chunk.Count - processed), ct);
                    return;
                }

                // find the next EOL sequence
                eolIndex = FindSequenceIndex(chunk, EolSeq, eolIndex + EolSeq.Length);
            }
        }

        private static ChunkAction ProcessLine(List<byte> line)
        {
            if (FindSequenceIndex(line, RequestIdSeq) != -1)
            {
                // request id is ignored
                return ChunkAction.None;
            }

            if (FindSequenceIndex(line, ContentTypeSeq) != -1)
            {
                // content type is ignored
                return ChunkAction.None;
            }

            if (FindSequenceIndex(line, StreamIdSeq) != -1)
            {
                // stream id is ignored
                return ChunkAction.None;
            }

            if (FindSequenceIndex(line, PathAudioSeq) != -1)
            {
                // audio path marks beginning of binary data
                return ChunkAction.Binary;
            }

            if (FindSequenceIndex(line, PathResponseSeq) != -1)
            {
                // response path ignore whole chunk
                return ChunkAction.Drop;
            }

            if (FindSequenceIndex(line, PathTurnEndSeq) != -1)
            {
                // turn end path ignore whole chunk
                return ChunkAction.Drop;
            }

            if (FindSequenceIndex(line, PathTurnStartSeq) != -1)
            {
                // turn start path ignore whole chunk
                return ChunkAction.Drop;
            }

            throw new Exception($"Unknown line type: [{Encoding.UTF8.GetString(line.ToArray())}]");
        }

        /// <summary>
        /// Find the index of a sequence in a list of bytes
        /// </summary>
        /// <returns></returns>
        private static int FindSequenceIndex(List<byte> data, byte[] pattern, int startIndex = 0)
        {
            for (var i = startIndex; i <= data.Count - pattern.Length; i++)
            {
                var found = true;
                for (var j = 0; j < pattern.Length; j++)
                {
                    if (data[i + j] != pattern[j])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Check if a sequence starts with a pattern
        /// </summary>
        private static bool SequenceStartsWith(List<byte> data, byte[] pattern, int startIndex = 0)
        {
            if (data.Count - startIndex < pattern.Length)
            {
                return false;
            }

            for (var i = 0; i < pattern.Length; i++)
            {
                if (data[startIndex + i] != pattern[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static string GenerateSecMsGecToken()
        {
            // see: https://github.com/STBBRD/EdgeTTS_dotNET_Framework/
            // see: https://github.com/rany2/edge-tts/issues/290#issuecomment-2464956570
            var ticks = (DateTime.UtcNow.Ticks / 3_000_000_000) * 3_000_000_000;
            var str = $"{ticks}6A5AA1D4EAFF4E9FB37E23D68491D6F4";
            return ToHexString(HashData(Encoding.ASCII.GetBytes(str)));
        }

        private static string ToHexString(byte[] byteArray)
        {
            return string.Concat(byteArray.Select(b => b.ToString("x2")));
        }

        private static byte[] HashData(byte[] data)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(data);
        }

        private static string CreateAudioFormatRequest(string outputformat)
        {
            return
                $$$$$"""
                     Content-Type:application/json; charset=utf-8
                     Path:speech.config

                     {"context":{"synthesis":{"audio":{"metadataoptions":{"sentenceBoundaryEnabled":"false","wordBoundaryEnabled":"false"},"outputFormat":"{{{{{outputformat}}}}}"}}}}
                     """;
        }

        private static string ConvertToSsmlText(string language, string voice, int rate, int volume, string text)
        {
            return
                $"""
                 <speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{language}'>
                     <voice name='{voice}'>
                        <prosody pitch='+0Hz' rate ='{rate:+#;-#;0}%' volume='{volume}'>{text}</prosody>
                     </voice>
                 </speak>
                 """;
        }

        private static string CreateSsmlRequest(string requestId, string lang, string voice, int rate, int volume, string msg)
        {
            return $"""
                    X-RequestId:{requestId}
                    Content-Type:application/ssml+xml
                    Path:ssml

                    {ConvertToSsmlText(lang, voice, rate, volume, msg)}
                    """;
        }
    }
}