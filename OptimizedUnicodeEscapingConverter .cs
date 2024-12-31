using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.ObjectPool;

public class UnicodeEscapingConverter : JsonConverter<string>
{
    // Create an ObjectPool for StringBuilder instances
    private static readonly ObjectPool<StringBuilder> _stringBuilderPool =
        new DefaultObjectPool<StringBuilder>(new DefaultPooledObjectPolicy<StringBuilder>());

    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Get the raw string from the JSON
        string rawString = reader.GetString()!;

        // Convert Unicode escape sequences (e.g., \uXXXX) back to readable characters
        return DecodeUnicodeEscapes(rawString);
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        // Get a StringBuilder from the pool
        StringBuilder escapedString = _stringBuilderPool.Get();

        try
        {
            // Clear the StringBuilder to ensure no leftover data
            escapedString.Clear();

            foreach (char c in value)
            {
                // Handle printable characters or easily serializable characters
                if (c >= 32 && c <= 126)  // Printable ASCII range
                {
                    escapedString.Append(c);
                }
                else
                {
                    // For non-printable characters, use Unicode escape sequences
                    escapedString.AppendFormat("\\u{0:X4}", (int)c);
                }
            }

            // Write the escaped string to the JSON output
            writer.WriteStringValue(escapedString.ToString());
        }
        finally
        {
            // Return the StringBuilder to the pool for reuse
            _stringBuilderPool.Return(escapedString);
        }
    }

    // Convert Unicode escape sequences like \uXXXX back to the actual characters
    private static string DecodeUnicodeEscapes(string input)
    {
        // Regular expression to match Unicode escape sequences like \uXXXX
        string pattern = @"\\u([0-9A-Fa-f]{4})";
        return Regex.Replace(input, pattern, match =>
        {
            // Convert the matched escape sequence to the corresponding character
            return char.ConvertFromUtf32(Convert.ToInt32(match.Groups[1].Value, 16));
        });
    }
}

public class Class1
{
    public Class2 Summary { get; set; }
}

public class Class2
{
    [JsonConverter(typeof(UnicodeEscapingConverter))]
    public string Title { get; set; }
	public string AcNumber {get; set;}
}

public class Program
{
    public static void Main()
    {
        var testObject = new Class2
        {
            Title = "Example \"Name\" with Special \n Characters!",
            AcNumber = "100"
        };
		
		var test1 = new Class1 { Summary = testObject};

        // Serialize
        string json = JsonSerializer.Serialize(test1);
        Console.WriteLine("Serialized JSON:");
        Console.WriteLine(json);

        // Deserialize
		var str = "{\"Summary\":{\"Title\":\"Example \\\\u0022Name\\\\u0022 with Special \\\\u000A Characters!\",\"AcNumber\":\"100\"}}";
        var deserializedObject = JsonSerializer.Deserialize<Class1>(str);
        Console.WriteLine("\nDeserialized Object:");
        Console.WriteLine(deserializedObject.Summary.Title.ToString());
    }
}
