using System.Text.Json;
using System.Text.Json.Serialization;

namespace MorningQueue.Core;

// Godot's JSON is lossy about integers: GDScript's JSON.parse_string reads EVERY JSON number
// as a float, and JSON.stringify re-emits whole numbers with a trailing ".0" (e.g. 4 -> 4.0).
// The real boot path (DeckLoader.gd -> JSON.stringify -> CoreBridge) therefore hands Core text
// like {"accept":{"min":2.0,"max":4.0,...}}, and stock System.Text.Json refuses to bind a
// fractional-looking number to an `int` property — it throws
//   "The JSON value could not be converted to System.Int32. Path: $.accept.max".
// The unit tests never saw this because they feed the RAW file text, where the numbers are
// still authored as integers.
//
// These converters make every `int` / `int?` model field tolerant of that round trip: a JSON
// number is read as a double and rounded to the nearest integer, so 4 and 4.0 bind identically.
// Registered once in Json.Options, they cover every int field in the model uniformly — Accept,
// Total, RankupThresholds, RosterParty.ReachFloor, DropEntry, Logbook, Visit.Order, … — so this
// whole class of Godot-float-vs-C#-int mismatch cannot recur field-by-field.

/// <summary>Reads a JSON number (integer or Godot's "4.0" float form) into an <see cref="int"/>.</summary>
public sealed class TolerantIntConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt32(out var i))
                return i;
            return (int)Math.Round(reader.GetDouble());
        }
        // Fall back to a string that holds a number (rare, but harmless to tolerate).
        if (reader.TokenType == JsonTokenType.String &&
            double.TryParse(reader.GetString(), out var d))
            return (int)Math.Round(d);

        throw new JsonException($"Expected a number for Int32, got {reader.TokenType}.");
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}

/// <summary>Nullable counterpart: null stays null; a number (incl. "4.0") rounds to int.</summary>
public sealed class TolerantNullableIntConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt32(out var i))
                return i;
            return (int)Math.Round(reader.GetDouble());
        }
        if (reader.TokenType == JsonTokenType.String &&
            double.TryParse(reader.GetString(), out var d))
            return (int)Math.Round(d);

        throw new JsonException($"Expected a number or null for Int32?, got {reader.TokenType}.");
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }
}
