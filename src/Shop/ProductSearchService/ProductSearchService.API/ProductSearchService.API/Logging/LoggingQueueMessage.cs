using System;
using System.Collections.Generic;

namespace ProductSearchService.API.Logging
{
    public class LoggingQueueMessage
    {
        public DateTime Timestamp { get; set; }

        public string State { get; set; }

        public string Message { get; set; }

        public string StackTrace { get; set; }

        public string ServiceVersion { get; set; }

        public string ServiceName { get; set; }

        public string ServiceArea { get; set; }

        public string ServiceEnvironment { get; set; }

        public string ApiControllerName { get; set; }

        public string ApiActionName { get; set; }

        public string ApiApiHttpVerb { get; set; }

        public string ApiVersion { get; set; }

        public string CorrelationId { get; set; }

        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
}
