using Newtonsoft.Json.Linq;
using System.Text;

namespace ProductSearchService.EventListener.Messaging
{
    public interface IMessageSerializer
    {
        string ContentType { get; }

        string Serialize(object value);

        JObject Deserialize(string value);

        Encoding Encoding { get; }
    }
}
