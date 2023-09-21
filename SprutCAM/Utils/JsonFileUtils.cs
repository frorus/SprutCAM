using System.IO;
using System.Text.Json;

namespace SprutCAM.Utils
{
    public static class JsonFileUtils
    {
        public static void Write(object obj, string fileName)
        {
            var options = new JsonWriterOptions
            {
                Indented = true
            };

            using var fileStream = File.Create(fileName);
            using var utf8JsonWriter = new Utf8JsonWriter(fileStream, options);
            JsonSerializer.Serialize(utf8JsonWriter, obj);
        }
    }
}