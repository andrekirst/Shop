using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Text;

namespace ProductSearchService.EventListener.Messaging
{
    public class JsonMessageSerializer : IMessageSerializer
    {
        private readonly JsonSerializerSettings _serializerSettings;

        public JsonMessageSerializer()
        {
            _serializerSettings = new JsonSerializerSettings()
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };

            _serializerSettings.Converters.Add(new StringEnumConverter
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            });
        }

        public string ContentType => "application/json";

        public Encoding Encoding => Encoding.UTF8;

        public string Serialize(object value)
            => JsonConvert.SerializeObject(value: value, settings: _serializerSettings);

        public JObject Deserialize(string value)
            => JsonConvert.DeserializeObject<JObject>(value, _serializerSettings);
    }
}
