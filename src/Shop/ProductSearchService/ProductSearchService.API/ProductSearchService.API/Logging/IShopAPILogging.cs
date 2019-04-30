using System.Collections.Generic;

namespace ProductSearchService.API.Logging
{
    public interface IShopAPILogging
    {
        void Log(
            LogState logState,
            string message,
            string controllerName,
            string actionName,
            string httpVerb = "GET",
            string apiVersion = "1.0",
            bool useVersionFromApiVersionAttribute = true,
            Dictionary<string, object> parameters = null);
    }
}
