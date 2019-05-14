using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductSearchService.API.Logging
{
    public interface IShopCommonLogging
    {
        Task Log(
            LogState logState,
            string message,
            string className,
            string methodName,
            string correlationId,
            Dictionary<string, object> parameters = null,
            Exception exception = null);
    }
}
