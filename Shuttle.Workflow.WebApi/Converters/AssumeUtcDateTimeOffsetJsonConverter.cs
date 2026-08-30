using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shuttle.Workflow.WebApi.Converters;

/// <summary>
///     Reads a <see cref="DateTimeOffset" /> that carries no offset designator as UTC rather than as server-local
///     time, which is what <c>System.Text.Json</c> does by default.
///     <para>
///         Clients built against the contracts as they were before the move to <see cref="DateTimeOffset" /> serialise
///         a <see cref="DateTimeKind.Unspecified" /> value with no designator, and every date in this service is UTC by
///         convention -- so server-local would be wrong by the host's offset.  Values that do carry an offset are
///         normalised to zero, which keeps the exact-equality comparisons on <c>StateItem.EffectiveDate</c>
///         unambiguous.
///     </para>
/// </summary>
public class AssumeUtcDateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
    private const DateTimeStyles Styles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected a string value when reading a 'DateTimeOffset' but found '{reader.TokenType}'.");
        }

        var value = reader.GetString();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException("Could not read an empty value as a 'DateTimeOffset'.");
        }

        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, Styles, out var result))
        {
            throw new JsonException($"Could not read value '{value}' as a 'DateTimeOffset'.");
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}