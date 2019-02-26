using System;

namespace Infrastructure.Messaging
{
    public class Message
    {
        public Message() :
            this(messageId: Guid.NewGuid())
        {
        }

        public Message(Guid messageId)
        {
            MessageId = messageId;
            MessageType = GetType().Name;
        }

        public Message(string messageType)
            : this(messageId: Guid.NewGuid())
        {
            MessageType = messageType;
        }

        public Message(Guid messageId, string messageType)
        {
            MessageId = messageId;
            MessageType = messageType;
        }

        public Guid MessageId { get; private set; }

        public string MessageType { get; private set; }
    }
}
