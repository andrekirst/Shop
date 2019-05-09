using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ProductSearchService.API.Infrastructure.Json
{
    public class NewtonsoftJsonSerializer : IJsonSerializer
    {
        public string Serialize(object value)
            => JsonConvert.SerializeObject(value: value);

        public T Deserialize<T>(string json)
            => JsonConvert.DeserializeObject<T>(value: json);

        public dynamic Parse(string json)
            => JObject.Parse(json: json);
    }
}
