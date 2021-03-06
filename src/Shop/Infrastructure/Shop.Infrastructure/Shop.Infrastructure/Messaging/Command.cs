﻿using System;

namespace Shop.Infrastructure.Messaging
{
    public class Command : Message
    {
        public Command()
        {
        }

        public Command(Guid messageId)
            : base(messageId: messageId)
        {
        }

        public Command(string messageType)
            : base(messageType: messageType)
        {
        }

        public Command(Guid messageId, string messageType, DateTime timestamp)
            : base(messageId: messageId, messageType: messageType, timestamp: timestamp)
        {
        }
    }
}
