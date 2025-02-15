namespace EdgeTtsSharp.Extensions
{
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;

    internal static class AssemblyExtensions
    {
        public static async Task<T?> ParseEmbeddedJson<T>(this Assembly assembly, string resourcePath)
        {
#if NETSTANDARD2_1
            await using var stream = assembly.GetManifestResourceStream(resourcePath);
#else
            using var stream = assembly.GetManifestResourceStream(resourcePath);
#endif
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