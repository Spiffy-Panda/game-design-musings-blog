using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fishbowl.Core.Model;

namespace Fishbowl.Core.Json;

/// <summary>
/// Parses a <see cref="RateModDto"/> from either shape:
/// <c>"restlessness": 1.3</c> (legacy, bare) or <c>"restlessness": {"gain": 1.3, "decay": 0.7}</c>.
///
/// <para><b>The bare form is not deprecated-and-broken; it is exactly-preserved.</b> It reads as
/// <c>{gain: n, decay: n}</c>, which is arithmetically what the single scalar always did — so every
/// un-migrated trait keeps its current meaning to the last bit, and the frozen golden fixture
/// (<c>PNO.D2</c>) is untouched by the split. Migration is a judgement about what a word means; the
/// loader must never make it silently. <c>--lint</c>'s <c>legacy-rate-mods</c> names the ones still
/// bare so the judgement is visible rather than forgotten.</para>
///
/// <para><b>Unknown keys throw.</b> <c>{"gian": 1.3}</c> is a typo that would otherwise leave a trait
/// at a silent 1.0/1.0 and read as "this trait does nothing" forever — the same class of defect as
/// <c>--lint</c>'s <c>unknown-drive</c> (a fiction that ships because it renders plausibly). It is
/// cheaper to refuse to load.</para>
/// </summary>
public sealed class RateModConverter : JsonConverter<RateModDto>
{
    public override RateModDto Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            // Legacy: a bare scalar. Same magnitude in both directions = today's exact behaviour.
            case JsonTokenType.Number:
            case JsonTokenType.String:
            {
                double v = ReadNumber(ref reader, "pressure_rate_mods");
                return new RateModDto { Gain = v, Decay = v, Legacy = true };
            }

            case JsonTokenType.StartObject:
            {
                double gain = 1.0, decay = 1.0;
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                        return new RateModDto { Gain = gain, Decay = decay, Legacy = false };
                    if (reader.TokenType != JsonTokenType.PropertyName)
                        throw new JsonException($"Malformed pressure rate mod: expected a key, got {reader.TokenType}.");

                    string name = reader.GetString()!;
                    reader.Read();
                    if (string.Equals(name, "gain", StringComparison.OrdinalIgnoreCase))
                        gain = ReadNumber(ref reader, name);
                    else if (string.Equals(name, "decay", StringComparison.OrdinalIgnoreCase))
                        decay = ReadNumber(ref reader, name);
                    else
                        throw new JsonException(
                            $"Unknown pressure rate mod key '{name}' — expected 'gain' (scales the drift where "
                            + "the base rule pushes the drive up) or 'decay' (where it pushes down). A bare number "
                            + "is also legal and means {gain: n, decay: n}.");
                }
                throw new JsonException("Unterminated pressure rate mod object.");
            }

            default:
                throw new JsonException(
                    $"Expected a number or a {{gain, decay}} object for a pressure rate mod, got {reader.TokenType}.");
        }
    }

    /// <summary>Number-or-numeric-string, for the same reason <see cref="TolerantIntConverter"/> exists:
    /// Godot's <c>JSON.stringify</c> float-ifies and occasionally string-wraps numbers, and the VFB.M0
    /// round-trip replays exactly that over every authored file.</summary>
    private static double ReadNumber(ref Utf8JsonReader reader, string what) => reader.TokenType switch
    {
        JsonTokenType.Number => reader.GetDouble(),
        JsonTokenType.String when double.TryParse(reader.GetString(), NumberStyles.Float,
            CultureInfo.InvariantCulture, out double s) => s,
        _ => throw new JsonException($"Expected a number for '{what}', got {reader.TokenType}."),
    };

    /// <summary>Always writes the explicit pair. Nothing in the engine serialises a
    /// <see cref="Model.TraitDto"/> today (<c>Api/WorldView</c> emits only the townee's trait <i>ids</i>),
    /// so this exists to keep the converter total rather than to round-trip authored files — and it
    /// deliberately does <b>not</b> re-emit the bare form, because writing back a shape whose whole
    /// purpose is "a human has not looked at this yet" would launder the legacy flag away.</summary>
    public override void Write(Utf8JsonWriter writer, RateModDto value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("gain", value.Gain);
        writer.WriteNumber("decay", value.Decay);
        writer.WriteEndObject();
    }
}
