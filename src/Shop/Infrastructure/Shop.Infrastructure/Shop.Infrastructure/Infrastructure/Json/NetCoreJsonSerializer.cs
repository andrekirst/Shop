using System.Text.Json.Serialization;

namespace Shop.Infrastructure.Infrastructure.Json
{
    public class NetCoreJsonSerializer : IJsonSerializer
    {
        public string Serialize(object value)
            => JsonSerializer.ToString(value: value);

        public T Deserialize<T>(string json)
            => JsonSerializer.Parse<T>(json: json);

        public dynamic Parse(string json)
            => JsonSerializer.Parse<dynamic>(json: json);
    }
}
