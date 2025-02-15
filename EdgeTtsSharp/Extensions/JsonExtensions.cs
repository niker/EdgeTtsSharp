namespace EdgeTtsSharp.Extensions;

using System.Text.Json;

internal static class JsonExtensions
{
    public static T? DeserializeNullable<T>(this string? json)
    {
        return json == null ? default : JsonSerializer.Deserialize<T>(json);
    }
}