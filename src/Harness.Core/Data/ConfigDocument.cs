using System.Text.Json;

namespace Harness.Core.Data;

public static class ConfigDocument
{
    public const int LeafCount = 2000;
    public const string DeepPath = "Deep:L2:L3:L4:L5";

    public static readonly string[] IndexerKeys = Enumerable.Range(0, 100)
        .Select(i => SectionPath(i * 19))
        .ToArray();

    public static byte[] Create()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            var fillers = LeafCount - 50 - 1;
            for (var section = 0; section * 100 < fillers; section++)
            {
                writer.WriteStartObject($"S{section:00}");
                for (var sub = 0; sub < 10 && section * 100 + sub * 10 < fillers; sub++)
                {
                    writer.WriteStartObject($"Sub{sub}");
                    for (var key = 0; key < 10; key++)
                    {
                        var index = section * 100 + sub * 10 + key;
                        if (index < fillers)
                        {
                            writer.WriteString($"K{key}", $"value-{index}");
                        }
                    }

                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
            }

            writer.WriteStartObject("Deep");
            writer.WriteStartObject("L2");
            writer.WriteStartObject("L3");
            writer.WriteStartObject("L4");
            writer.WriteString("L5", "leaf");
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.WriteStartObject("Poco");
            foreach (var (key, value) in Poco50Values.Create())
            {
                writer.WriteString(key, value);
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    private static string SectionPath(int index) => $"S{index / 100:00}:Sub{index / 10 % 10}:K{index % 10}";
}
