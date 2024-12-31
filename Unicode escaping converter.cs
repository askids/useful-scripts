using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public class UnicodeEscapingConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Convert from \uXXXX format back to a readable string
        return reader.GetString()!;
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        // Convert to \uXXXX format during serialization
        var escapedString = EscapeToUnicode(value);
        writer.WriteStringValue(escapedString);
    }

    private static string EscapeToUnicode(string value)
    {
        var stringBuilder = new StringBuilder();
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
        return stringBuilder.ToString();
    }
}
