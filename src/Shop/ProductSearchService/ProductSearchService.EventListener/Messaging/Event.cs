using Microsoft.Extensions.Logging;
using System;

namespace ProductSearchService.EventListener.Messaging
{
    public class Event : Message
    {
        public Event()
            : base()
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

        public Event(Guid messageId, string messageType, DateTime timestamp)
            : base(messageId: messageId, messageType: messageType, timestamp: timestamp)
        {
        }

        public virtual EventId EventId { get; }
    }
}
