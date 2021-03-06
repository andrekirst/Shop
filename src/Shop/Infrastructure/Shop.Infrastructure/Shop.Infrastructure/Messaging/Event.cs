﻿using System;
using Microsoft.Extensions.Logging;

namespace Shop.Infrastructure.Messaging
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

        public Event(Guid messageId, string messageType, DateTime timestamp)
            : base(messageId: messageId, messageType: messageType, timestamp: timestamp)
        {
        }

        public virtual EventId EventId { get; }
    }
}
