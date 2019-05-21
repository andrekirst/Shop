using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Shop.Infrastructure.Messaging
{
    public class JsonMessageSerializer : IMessageSerializer
    {
        public JsonMessageSerializer()
        {
            Settings = new JsonSerializerSettings()
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };

            Settings.Converters.Add(item: new StringEnumConverter
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            });
        }
        
        private JsonSerializerSettings Settings { get; }

        public string ContentType => "application/json";

        public Encoding Encoding => Encoding.UTF8;

        public string Serialize(object value)
            => JsonConvert.SerializeObject(
                value: value,
                settings: Settings);

        public T Deserialize<T>(string value) =>
            JsonConvert
            .DeserializeObject<JObject>(
                value: value,
                settings: Settings)
            .ToObject<T>();
    }
}
