using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Messaging
{
    public static class MessageSerializer
    {
        private static JsonSerializerSettings _serializerSettings;

        static MessageSerializer()
        {
            _serializerSettings = new JsonSerializerSettings()
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            };

            _serializerSettings.Converters.Add(new StringEnumConverter
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            });
        }

        public static string Serialize(object value)
            => JsonConvert.SerializeObject(value: value, settings: _serializerSettings);

        public static JObject Deserialize(string value)
            => JsonConvert.DeserializeObject<JObject>(value, _serializerSettings);
    }
}
