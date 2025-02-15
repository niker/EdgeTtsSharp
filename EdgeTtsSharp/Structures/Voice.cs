namespace EdgeTtsSharp.Structures
{
    /// <summary>
    /// Represents a voice used for text-to-speech.
    /// </summary>
    public class Voice
    {
        /// <summary>
        /// Gets or sets the name of the voice.
        /// </summary>
        public string Name { get; set; } = default!;

        /// <summary>
        /// Gets or sets the short name of the voice.
        /// </summary>
        public string ShortName { get; set; } = default!;

        /// <summary>
        /// Gets or sets the gender of the voice.
        /// </summary>
        public string? Gender { get; set; }

        /// <summary>
        /// Gets or sets the locale of the voice.
        /// </summary>
        public string Locale { get; set; } = default!;

        /// <summary>
        /// Gets or sets the suggested codec for the voice.
        /// </summary>
        public string SuggestedCodec { get; set; } = default!;

        /// <summary>
        /// Gets or sets the friendly name of the voice.
        /// </summary>
        public string? FriendlyName { get; set; }

        /// <summary>
        /// Gets or sets the status of the voice.
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the voice tag containing additional information about the voice.
        /// </summary>
        public Voicetag? VoiceTag { get; set; }
    }

    /// <summary>
    /// Represents additional information about a voice.
    /// </summary>
    public class Voicetag
    {
        /// <summary>
        /// Gets or sets the content categories associated with the voice.
        /// </summary>
        public string[]? ContentCategories { get; set; }

        /// <summary>
        /// Gets or sets the voice personalities associated with the voice.
        /// </summary>
        public string[]? VoicePersonalities { get; set; }
    }
}