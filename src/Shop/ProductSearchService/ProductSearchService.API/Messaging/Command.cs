using System;

namespace ProductSearchService.API.Messaging
{
    public class Command : Message
    {
        public Command()
            : base()
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
