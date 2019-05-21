using System;

namespace Shop.Infrastructure.Messaging
{
    public class Message
    {
        public Message() :
            this(messageId: Guid.NewGuid(), timestamp: DateTime.UtcNow)
        {
        }

        public Message(Guid messageId)
        {
            MessageId = messageId;
            MessageType = GetType().Name;
        }

        public Message(Guid messageId, DateTime timestamp)
            : this(messageId: messageId)
        {
            Timestamp = timestamp;
        }

        public Message(string messageType)
            : this(messageId: Guid.NewGuid(), timestamp: DateTime.UtcNow)
        {
            MessageType = messageType;
        }

        public Message(Guid messageId, string messageType, DateTime timestamp)
        {
            MessageId = messageId;
            MessageType = messageType;
            Timestamp = timestamp;
        }

        public Guid MessageId { get; protected set; }

        public string MessageType { get; protected set; }

        public DateTime Timestamp { get; protected set; }
    }
}
