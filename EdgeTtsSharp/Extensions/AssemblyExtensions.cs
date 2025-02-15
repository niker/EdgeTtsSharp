namespace EdgeTtsSharp.Extensions
{
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;

    internal static class AssemblyExtensions
    {
        public static async Task<T?> ParseEmbeddedJson<T>(this Assembly assembly, string resourcePath)
        {
            await using var stream = assembly.GetManifestResourceStream(resourcePath);
            if (stream == null)
            {
                return default;
            }

            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            return json.DeserializeNullable<T>();
        }
    }
}