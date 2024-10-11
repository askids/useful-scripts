using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class MicrosoftDateFormatConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Handle the deserialization here if needed (not shown in this example)
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        long unixTimeMilliseconds = new DateTimeOffset(value).ToUnixTimeMilliseconds();
        string microsoftDateFormat = $"\\/Date({unixTimeMilliseconds})\\/";
        writer.WriteStringValue(microsoftDateFormat);
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

        var dateTime = DateTime.Now;
        string json = JsonSerializer.Serialize(dateTime, options);

        Console.WriteLine(json);
    }
}
