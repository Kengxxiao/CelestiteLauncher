using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Celestite.Utils.Converters
{
    public class UnixEpochDateTimeConverter : JsonConverter<DateTimeOffset>
    {
        private static DateTimeOffset Epoch => new(1970, 1, 1, 0, 0, 0, 0, TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.Now));
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var epochTime = reader.GetUInt64();
            return Epoch.AddMilliseconds(epochTime);
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            var unixTime = Convert.ToInt64((value - Epoch).TotalMilliseconds);
            writer.WriteNumberValue(unixTime);
        }
    }
}
