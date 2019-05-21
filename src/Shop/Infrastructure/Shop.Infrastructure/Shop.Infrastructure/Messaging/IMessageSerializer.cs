using System.Text;

namespace Shop.Infrastructure.Messaging
{
    public interface IMessageSerializer
    {
        string ContentType { get; }

        string Serialize(object value);

        T Deserialize<T>(string value);

        Encoding Encoding { get; }
    }
}
