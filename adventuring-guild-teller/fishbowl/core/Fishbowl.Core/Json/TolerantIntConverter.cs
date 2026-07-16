using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fishbowl.Core.Json;

/// <summary>
/// Parses an <see cref="int"/> from JSON that may carry it as a float — <c>4</c> OR
/// <c>4.0</c>. Registered from day one because Godot's <c>JSON.stringify</c> float-ifies
/// whole numbers (<c>4</c> → <c>4.0</c>), and strict System.Text.Json int binding rejects
/// those engine-round-tripped payloads. Tests that feed raw file text never catch it, so
/// the round-trip suite (VFB.M0) replays the Godot stringify over real data/ files.
/// A fractional value that is not integral (e.g. 4.5) is a genuine error and still throws.
/// </summary>
public sealed class TolerantIntConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                if (reader.TryGetInt32(out int i)) return i;
                double d = reader.GetDouble();
                if (d == Math.Floor(d) && d >= int.MinValue && d <= int.MaxValue) return (int)d;
                throw new JsonException($"Expected an integer, got non-integral {d}.");
            case JsonTokenType.String:
                // Godot occasionally string-wraps numbers; tolerate "4" and "4.0".
                string s = reader.GetString()!;
                if (int.TryParse(s, System.Globalization.NumberStyles.Integer,
                        System.Globalization.CultureInfo.InvariantCulture, out int si)) return si;
                if (double.TryParse(s, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double sd)
                    && sd == Math.Floor(sd)) return (int)sd;
                throw new JsonException($"Expected an integer string, got \"{s}\".");
            default:
                throw new JsonException($"Expected a number for int, got {reader.TokenType}.");
        }
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}

/// <summary>The <see cref="long"/> counterpart to <see cref="TolerantIntConverter"/> — the
/// world seed is a long, and Godot floatifies it too (1123 → 1123.0).</summary>
public sealed class TolerantInt64Converter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                if (reader.TryGetInt64(out long l)) return l;
                double d = reader.GetDouble();
                if (d == Math.Floor(d) && d >= long.MinValue && d <= long.MaxValue) return (long)d;
                throw new JsonException($"Expected an integer, got non-integral {d}.");
            case JsonTokenType.String:
                string s = reader.GetString()!;
                if (long.TryParse(s, System.Globalization.NumberStyles.Integer,
                        System.Globalization.CultureInfo.InvariantCulture, out long sl)) return sl;
                if (double.TryParse(s, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double sd)
                    && sd == Math.Floor(sd)) return (long)sd;
                throw new JsonException($"Expected an integer string, got \"{s}\".");
            default:
                throw new JsonException($"Expected a number for long, got {reader.TokenType}.");
        }
    }

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}
