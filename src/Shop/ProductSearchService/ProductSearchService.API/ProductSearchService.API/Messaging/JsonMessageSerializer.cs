using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Text;

namespace ProductSearchService.API.Messaging
{
    public class JsonMessageSerializer : IMessageSerializer
    {
        private readonly JsonSerializerSettings _settings;

        public JsonMessageSerializer()
        {
            _settings = new JsonSerializerSettings()
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };

            _settings.Converters.Add(item: new StringEnumConverter
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            });
        }

        public string ContentType => "application/json";

        public Encoding Encoding => Encoding.UTF8;

        public string Serialize(object value)
            => JsonConvert.SerializeObject(value: value, settings: _settings);

        public JObject Deserialize(string value)
            => JsonConvert.DeserializeObject<JObject>(value: value, settings: _settings);
    }
}
