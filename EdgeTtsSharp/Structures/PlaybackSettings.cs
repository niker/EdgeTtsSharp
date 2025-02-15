namespace EdgeTtsSharp.Structures
{
    /// <summary>
    /// Playback settings for a voice
    /// </summary>
    public class PlaybackSettings
    {
        /// <summary>
        /// Rate of speech
        /// </summary>
        public int Rate { get; set; }

        /// <summary>
        /// Volume of speech
        /// </summary>
        public float Volume { get; set; } = 1.0f;
    }
}