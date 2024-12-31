using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public class OptimizedUnicodeEscapingConverter : JsonConverter<string>
{
    private static readonly ThreadLocal<StringBuilder> StringBuilderPool = new(() => new StringBuilder());

    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Read the string as-is (no special handling needed for reading)
        return reader.GetString()!;
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        // Use the pooled StringBuilder to escape the string
        var stringBuilder = StringBuilderPool.Value!;
        stringBuilder.Clear();

        foreach (var c in value)
        {
            if (c < 32 || c > 126 || c == '"' || c == '\\') // Escape special characters and non-printables
            {
                stringBuilder.AppendFormat("\\u{0:X4}", (int)c);
            }
            else
            {
                stringBuilder.Append(c);
            }
        }

        // Write the escaped string
        writer.WriteStringValue(stringBuilder.ToString());
    }
}
