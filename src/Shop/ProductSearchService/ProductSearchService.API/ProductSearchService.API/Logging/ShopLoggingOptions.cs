using System.Collections.Generic;

namespace ProductSearchService.API.Logging
{
    public class ShopLoggingOptions
    {
        public string ServiceVersion { get; set; }

        public string ServiceName { get; set; }

        public string ServiceArea { get; set; }

        public string Environment { get; set; }
        
        public List<string> HostIPAddresses { get; set; }
        
        public string HostName { get; set; }
    }
}
