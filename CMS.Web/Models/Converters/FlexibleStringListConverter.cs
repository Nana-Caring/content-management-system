using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CMS.Web.Models.Converters
{
    public class FlexibleStringListConverter : JsonConverter<List<string>>
    {
        public override List<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                var list = new List<string>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        break;

                    if (reader.TokenType == JsonTokenType.String)
                    {
                        list.Add(reader.GetString() ?? string.Empty);
                    }
                    else if (reader.TokenType == JsonTokenType.Number)
                    {
                        list.Add(reader.GetDouble().ToString());
                    }
                    else if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
                    {
                        list.Add(reader.GetBoolean().ToString());
                    }
                    else if (reader.TokenType == JsonTokenType.Null)
                    {
                        list.Add(string.Empty);
                    }
                    else
                    {
                        // Fallback: read raw and add as string
                        list.Add(JsonDocument.ParseValue(ref reader).RootElement.ToString());
                    }
                }
                return list;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString() ?? string.Empty;
                // If it looks like a comma-separated list, split; else single item list
                if (s.Contains(","))
                {
                    var parts = s.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    var list = new List<string>();
                    foreach (var part in parts)
                        list.Add(part.Trim());
                    return list;
                }
                return new List<string> { s };
            }

            // Numbers/bools: convert to single-item string list
            if (reader.TokenType == JsonTokenType.Number)
            {
                return new List<string> { reader.GetDouble().ToString() };
            }
            if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
            {
                return new List<string> { reader.GetBoolean().ToString() };
            }

            // Fallback: try to parse value and stringify
            try
            {
                var el = JsonDocument.ParseValue(ref reader).RootElement;
                return new List<string> { el.ToString() };
            }
            catch
            {
                return new List<string>();
            }
        }

        public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var s in value)
            {
                writer.WriteStringValue(s);
            }
            writer.WriteEndArray();
        }
    }
}
