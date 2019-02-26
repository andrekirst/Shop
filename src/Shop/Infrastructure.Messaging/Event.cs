using System;

namespace Infrastructure.Messaging
{
    public class Event : Message
    {
        public Event()
        {
        }

        public Event(Guid messageId)
            : base(messageId: messageId)
        {
        }

        public Event(string messageType)
            : base(messageType: messageType)
        {
        }

        public Event(Guid messageId, string messageType)
            : base(messageId: messageId, messageType: messageType)
        {
        }
    }
}
