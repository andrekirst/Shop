using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductSearchService.API.Logging
{
    public interface IShopApiLogging
    {
        Task Log(
            LogState logState,
            string message,
            string controllerName,
            string actionName,
            string correlationId,
            string httpVerb = "GET",
            string apiVersion = "1.0",
            Dictionary<string, object> parameters = null,
            Exception exception = null);

        Task LogError(
            Exception exception,
            string message,
            string controllerName,
            string actionName,
            string correlationId,
            string httpVerb = "GET",
            string apiVersion = "1.0",
            Dictionary<string, object> parameters = null);

        Task LogError(
            string message,
            string controllerName,
            string actionName,
            string correlationId,
            string httpVerb = "GET",
            string apiVersion = "1.0",
            Dictionary<string, object> parameters = null);

        Task LogTrace(
            string message,
            string controllerName,
            string actionName,
            string correlationId,
            string httpVerb = "GET",
            string apiVersion = "1.0",
            Dictionary<string, object> parameters = null);

        Task LogDebug(
            string message,
            string controllerName,
            string actionName,
            string correlationId,
            string httpVerb = "GET",
            string apiVersion = "1.0",
            Dictionary<string, object> parameters = null);

        Task LogInfo(
            string message,
            string controllerName,
            string actionName,
            string httpVerb,
            string apiVersion,
            string correlationId,
            Dictionary<string, object> parameters = null);

        Task LogWarning(
            string message,
            string controllerName,
            string actionName,
            string correlationId,
            string httpVerb = "GET",
            string apiVersion = "1.0",
            Dictionary<string, object> parameters = null);

        Task LogStartAndEnd(
            Action action,
            LogState logState,
            string messageStart,
            string messageEnd,
            string controllerName,
            string actionName,
            string correlationId,
            string httpVerb = "GET",
            string apiVersion = "1.0",
            Dictionary<string, object> parameters = null);

        Task<T> LogStartAndEnd<T>(
            Func<T> func,
            LogState logState,
            string messageStart,
            string messageEnd,
            string controllerName,
            string actionName,
            string correlationId,
            string httpVerb = "GET",
            string apiVersion = "1.0",
            Dictionary<string, object> parameters = null);

        Task<T> LogStartAndEnd<T>(
            Func<Task<T>> func,
            LogState logState,
            string messageStart,
            string messageEnd,
            string controllerName,
            string actionName,
            string correlationId,
            string httpVerb = "GET",
            string apiVersion = "1.0",
            Dictionary<string, object> parameters = null);
    }
}
