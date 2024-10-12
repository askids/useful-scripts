using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class MicrosoftDateFormatConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Handle deserialization if needed (not implemented here)
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            // Convert to UTC to avoid "UTC Time represented" error
            var utcDateTime = value.Value.ToUniversalTime();
            long unixTimeMilliseconds = new DateTimeOffset(utcDateTime).ToUnixTimeMilliseconds();
            string microsoftDateFormat = $"\\/Date({unixTimeMilliseconds})\\/";
            writer.WriteStringValue(microsoftDateFormat);
        }
        else
        {
            // Write a JSON null value if the DateTime is null
            writer.WriteNullValue();
        }
    }
}

class Program
{
    static void Main()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new MicrosoftDateFormatConverter() }
        };

        // Test with non-nullable DateTime
        DateTime dateTime = DateTime.Now;
        string json = JsonSerializer.Serialize(dateTime, options);
        Console.WriteLine("Serialized DateTime: " + json);

        // Test with nullable DateTime? with a value
        DateTime? nullableDateTime = DateTime.Now;
        json = JsonSerializer.Serialize(nullableDateTime, options);
        Console.WriteLine("Serialized DateTime?: " + json);

        // Test with null DateTime?
        nullableDateTime = null;
        json = JsonSerializer.Serialize(nullableDateTime, options);
        Console.WriteLine("Serialized null DateTime?: " + json);
    }
}
