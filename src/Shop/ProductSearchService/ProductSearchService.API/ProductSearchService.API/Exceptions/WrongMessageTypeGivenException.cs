using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace ProductSearchService.API.Exceptions
{
    public class WrongMessageTypeGivenException : Exception
    {
        public WrongMessageTypeGivenException()
        {
        }

        public WrongMessageTypeGivenException(string expectedMessageType, string currentMessageType)
            : base(message: $"Wrong MessageType given. Expected: \"{expectedMessageType}\", Current: \"{currentMessageType}\"")
        {
            ExpectedMessageType = expectedMessageType;
            CurrentMessageType = currentMessageType;
        }

        public WrongMessageTypeGivenException(string message, string expectedMessageType, string currentMessageType)
            : base(message: message)
        {
            ExpectedMessageType = expectedMessageType;
            CurrentMessageType = currentMessageType;
        }

        public WrongMessageTypeGivenException(string message) : base(message)
        {
        }

        public WrongMessageTypeGivenException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected WrongMessageTypeGivenException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
        
        public string ExpectedMessageType { get; }
        
        public string CurrentMessageType { get; }
    }
}
