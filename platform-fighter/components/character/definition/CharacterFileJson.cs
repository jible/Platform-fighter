using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// Reading and writing a character's files. Where the files live is left to the caller for now
public static class CharacterFileJson
{
    private static readonly JsonSerializerSettings settings = new()
    {
        Converters = { new StringEnumConverter(), new DM64JsonConverter(), new FrameRangeJsonConverter() },
        // Decimal instead of double, so DM64 values never pass through a float
        FloatParseHandling = FloatParseHandling.Decimal,
    };

    public static CharacterFile ReadCharacter(string json) => JsonConvert.DeserializeObject<CharacterFile>(json, settings);
    public static string WriteCharacter(CharacterFile file) => JsonConvert.SerializeObject(file, Formatting.Indented, settings);

    public static BakedSkeletonFile ReadSkeleton(string json) => JsonConvert.DeserializeObject<BakedSkeletonFile>(json, settings);
    // Not indented, since nobody edits the bake by hand and it holds a value per bone per tick
    public static string WriteSkeleton(BakedSkeletonFile file) => JsonConvert.SerializeObject(file, Formatting.None, settings);
}

// Writes DM64 as a plain decimal like 0.99 instead of its raw bits.
// 10 decimal places is finer than half a DM64 step (2^-33), so reading back always rounds to the exact same raw value.
// Uses System.Decimal, which is integer math, so no float rounding gets in
public class DM64JsonConverter : JsonConverter<DM64>
{
    private const decimal Scale = 4294967296m; // 2^32, matching DM64's fractional bits

    public override void WriteJson(JsonWriter writer, DM64 value, JsonSerializer serializer)
    {
        decimal number = Math.Round(value.RawValue / Scale, 10, MidpointRounding.AwayFromZero);
        writer.WriteRawValue(number.ToString("0.##########", CultureInfo.InvariantCulture));
    }

    public override DM64 ReadJson(JsonReader reader, Type objectType, DM64 existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        decimal number = reader.TokenType switch
        {
            JsonToken.Integer => Convert.ToDecimal(reader.Value, CultureInfo.InvariantCulture),
            JsonToken.Float => Convert.ToDecimal(reader.Value, CultureInfo.InvariantCulture),
            JsonToken.String => decimal.Parse((string)reader.Value, CultureInfo.InvariantCulture),
            _ => throw new JsonSerializationException($"Expected a number for DM64, got {reader.TokenType}"),
        };
        return DM64.FromRaw((long)Math.Round(number * Scale, MidpointRounding.AwayFromZero));
    }
}

// Writes a frame range as [start, end]
public class FrameRangeJsonConverter : JsonConverter<FrameRange>
{
    public override void WriteJson(JsonWriter writer, FrameRange value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        writer.WriteValue(value.Start);
        writer.WriteValue(value.End);
        writer.WriteEndArray();
    }

    public override FrameRange ReadJson(JsonReader reader, Type objectType, FrameRange existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        int[] pair = serializer.Deserialize<int[]>(reader);
        if (pair == null || pair.Length != 2)
        {
            throw new JsonSerializationException("Frame range must be [start, end]");
        }
        return new FrameRange(pair[0], pair[1]);
    }
}
