using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Microsoft.AspNetCore.Mvc;
using NLog;
using Shop.Infrastructure.Messaging;
using static System.String;
using IDateTimeProvider = Shop.Infrastructure.Infrastructure.IDateTimeProvider;

namespace ProductSearchService.API.Logging
{
    public class ShopApiLogging : IShopApiLogging
    {
        private IMessagePublisher MessagePublisher { get; }

        private IDateTimeProvider DateTimeProvider { get; }

        private ShopLoggingOptions LoggingOptions { get; }

        private Logger NLogLogger { get; }

        public ShopApiLogging(
            IMessagePublisher messagePublisher,
            IDateTimeProvider dateTimeProvider,
            ShopLoggingOptions loggingOptions)
        {
            MessagePublisher = messagePublisher;
            DateTimeProvider = dateTimeProvider;
            LoggingOptions = loggingOptions;

            NLogLogger = LogManager.GetCurrentClassLogger();
        }

        private LoggingQueueMessage CreateMessage(
            LogState logState,
            string message,
            string controllerName,
            string actionName,
            string httpVerb,
            string apiVersion,
            string correlationId,
            ControllerBase controller,
            Dictionary<string, object> parameters,
            Exception exception = null,
            long? elapsedMilliseconds = null)
        {
            if (parameters == null)
            {
                parameters = new Dictionary<string, object>();
            }
            parameters.TryAdd(key: "Debug:Controller.HttpContext.Connection", value: new { controller.HttpContext.Connection.Id });
            parameters.TryAdd(
                key: "Debug:Controller.HttpContext.Request",
                value: new
                {
                    controller.HttpContext.Request.ContentLength,
                    controller.HttpContext.Request.Headers,
                    controller.HttpContext.Request.Method,
                    controller.HttpContext.Request.Path,
                    controller.HttpContext.Request.PathBase,
                    controller.HttpContext.Request.Protocol,
                    controller.HttpContext.Request.Query,
                    controller.HttpContext.Request.QueryString,
                    controller.HttpContext.Request.Cookies,
                    controller.HttpContext.Request.RouteValues,
                    controller.HttpContext.Request.Scheme
                });

            parameters.TryAdd(
                key: "Debug:Controller.HttpContext.Response",
                value: new
                {
                    controller.HttpContext.Response.ContentLength,
                    controller.HttpContext.Response.Cookies,
                    controller.HttpContext.Response.Headers,
                    controller.HttpContext.Response.StatusCode
                });

            parameters.TryAdd(
                key: "Debug:Controller.ControllerContext.ActionDescriptor",
                value: new
                {
                    controller.ControllerContext.ActionDescriptor.ActionName,
                    controller.ControllerContext.ActionDescriptor.ControllerName,
                    controller.ControllerContext.ActionDescriptor.DisplayName,
                    controller.ControllerContext.ActionDescriptor.ActionConstraints,
                    controller.ControllerContext.ActionDescriptor.AttributeRouteInfo,
                    controller.ControllerContext.ActionDescriptor.Id,
                    controller.ControllerContext.ActionDescriptor.RouteValues
                });
            parameters.TryAdd(
                key: "Debug:Controller.HttpContext.User",
                value: new
                {
                    controller.HttpContext.User.Claims,
                    controller.HttpContext.User.Identities,
                    controller.HttpContext.User.Identity
                });

            return new LoggingQueueMessage
            {
                Timestamp = DateTimeProvider.Now,
                State = logState.GetStringValue(),
                Message = message,
                StackTrace =
                    (exception?.StackTrace ?? Environment.StackTrace)
                    .Split('\n', '\r')
                    .Where(s => !IsNullOrEmpty(s))
                    .Select(s => s.Trim())
                    .ToList(),
                ApiActionName = actionName,
                ApiControllerName = controllerName,
                ApiApiHttpVerb = httpVerb,
                ApiVersion = apiVersion,
                Parameters = parameters,
                CorrelationId = correlationId,
                ElapsedMilliseconds = elapsedMilliseconds,
                ServiceArea = LoggingOptions.ServiceArea,
                ServiceEnvironment = LoggingOptions.Environment,
                ServiceName = LoggingOptions.ServiceName,
                ServiceVersion = LoggingOptions.ServiceVersion,
                HostIPAddresses = LoggingOptions.HostIPAddresses,
                HostName = LoggingOptions.HostName
            };
        }

        public async Task Log(
            LogState logState,
            string message,
            string controllerName,
            string actionName,
            string correlationId,
            ControllerBase controller,
            string httpVerb = "GET",
            string apiVersion = "1.0",
            Dictionary<string, object> parameters = null,
            Exception exception = null,
            long? elapsedMilliseconds = null)
        {
            if (logState == LogState.Error)
            {
                await LogError(
                    exception: exception,
                    message: message,
                    controllerName: controllerName,
                    actionName: actionName,
                    httpVerb: httpVerb,
                    apiVersion: apiVersion,
                    correlationId: correlationId,
                    controller: controller,
                    parameters: parameters);
                return;
            }
            LoggingQueueMessage logMessage = CreateMessage(
                logState: logState,
                message: message,
                controllerName: controllerName,
                actionName: actionName,
                httpVerb: httpVerb,
                apiVersion: apiVersion,
                parameters: parameters,
                correlationId: correlationId,
                controller: controller,
                elapsedMilliseconds: elapsedMilliseconds);

            Task taskPushMessageToQueue = PushMessageToQueue(logMessage: logMessage);
            Task taskWriteMessageToNLog = WriteMessageToNLog(logState: logState, logMessage: logMessage);

            await Task.WhenAll(taskPushMessageToQueue, taskWriteMessageToNLog);
        }

        private Task WriteMessageToNLog(LogState logState, LoggingQueueMessage logMessage, Exception exception = null)
        {
            switch (logState)
            {
                case LogState.Trace:
                    {
                        NLogLogger.Trace(message: logMessage.Message);
                        break;
                    }
                case LogState.Debug:
                    {
                        NLogLogger.Debug(message: logMessage.Message);
                        break;
                    }
                case LogState.Warning:
                    {
                        NLogLogger.Warn(message: logMessage.Message);
                        break;
                    }
                case LogState.Info:
                    {
                        NLogLogger.Info(message: logMessage.Message);
                        break;
                    }
                case LogState.Error:
                case LogState.Fatal:
                    {
                        NLogLogger.Error(exception: exception, message: logMessage.Message, args: null);
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException(
                        paramName: nameof(logState),
                        actualValue: logState,
                        message: null);
            }

            return Task.FromResult(result: 0);
        }

        private Task PushMessageToQueue(LoggingQueueMessage logMessage) =>
            MessagePublisher.SendMessageAsync(
                message: logMessage,
                messageType: "ServiceLogging",
                exchange: "ServiceLogging");

        private Task PushErrorMessageToQueue(LoggingQueueMessage logMessage) =>
            MessagePublisher.SendMessageAsync(
                message: logMessage,
                messageType: "ServiceErrorLogging",
                exchange: "ServiceErrorLogging");

        public async Task LogError(
            Exception exception,
            string message,
            string controllerName,
            string actionName,
            string correlationId,
            ControllerBase controller,
            string httpVerb = "GET",
            string apiVersion = "1.0",
            Dictionary<string, object> parameters = null,
            long? elapsedMilliseconds = null)
        {
            LoggingQueueMessage logMessage = CreateMessage(
                logState: LogState.Error,
                message: message,
                controllerName: controllerName,
                actionName: actionName,
                httpVerb: httpVerb,
                apiVersion: apiVersion,
                correlationId: correlationId,
                controller: controller,
                parameters: parameters,
                elapsedMilliseconds: elapsedMilliseconds);

            Task taskPushErrorMessageToQueue = PushErrorMessageToQueue(logMessage: logMessage);
            Task taskPushMessageToQueue = PushMessageToQueue(logMessage: logMessage);
            Task taskWriteMessageToNLog = WriteMessageToNLog(logState: LogState.Error, logMessage: logMessage, exception: exception);

            await Task.WhenAll(taskPushErrorMessageToQueue, taskPushMessageToQueue, taskWriteMessageToNLog);
        }

        public Task LogError(
            string message,
            string controllerName,
            string actionName,
            string correlationId,
            ControllerBase controller,
            string httpVerb = "GET",
            string apiVersion = "1.0",
            Dictionary<string, object> parameters = null,
            long? elapsedMilliseconds = null) =>
            Log(
                logState: LogState.Error,
                message: message,
                controllerName: controllerName,
                actionName: actionName,
                httpVerb: httpVerb,
                apiVersion: apiVersion,
                correlationId: correlationId,
                controller: controller,
                parameters: parameters,
                elapsedMilliseconds: elapsedMilliseconds);

        public Task LogTrace(
            string message,
            string controllerName,
            string actionName,
            string correlationId,
            ControllerBase controller,
            string httpVerb = "GET",
            string apiVersion = "1.0",
            Dictionary<string, object> parameters = null,
            long? elapsedMilliseconds = null) =>
            Log(
                logState: LogState.Trace,
                message: message,
                controllerName: controllerName,
                actionName: actionName,
                httpVerb: httpVerb,
                apiVersion: apiVersion,
                correlationId: correlationId,
                controller: controller,
                parameters: parameters,
                elapsedMilliseconds: elapsedMilliseconds);

        public Task LogDebug(
            string message,
            string controllerName,
            string actionName,
            string correlationId,
            ControllerBase controller,
            string httpVerb = "GET",
            string apiVersion = "1.0",
            Dictionary<string, object> parameters = null,
            long? elapsedMilliseconds = null) =>
            Log(
                logState: LogState.Debug,
                message: message,
                controllerName: controllerName,
                actionName: actionName,
                httpVerb: httpVerb,
                apiVersion: apiVersion,
                correlationId: correlationId,
                controller: controller,
                parameters: parameters,
                elapsedMilliseconds: elapsedMilliseconds);

        public Task LogInfo(
            string message,
            string controllerName,
            string actionName,
            string correlationId,
            ControllerBase controller,
            string httpVerb = "GET",
            string apiVersion = "1.0",
            Dictionary<string, object> parameters = null,
            long? elapsedMilliseconds = null) =>
            Log(
                logState: LogState.Info,
                message: message,
                controllerName: controllerName,
                actionName: actionName,
                httpVerb: httpVerb,
                apiVersion: apiVersion,
                correlationId: correlationId,
                controller: controller,
                parameters: parameters,
                elapsedMilliseconds: elapsedMilliseconds);

        public Task LogWarning(string message,
            string controllerName,
            string actionName,
            string correlationId,
            ControllerBase controller,
            string httpVerb = "GET",
            string apiVersion = "1.0",
            Dictionary<string, object> parameters = null,
            long? elapsedMilliseconds = null) =>
            Log(
                logState: LogState.Warning,
                message: message,
                controllerName: controllerName,
                actionName: actionName,
                httpVerb: httpVerb,
                apiVersion: apiVersion,
                correlationId: correlationId,
                controller: controller,
                parameters: parameters,
                elapsedMilliseconds: elapsedMilliseconds);

        public async Task LogStartAndEnd(Action action,
            LogState logState,
            string messageStart,
            string messageEnd,
            string controllerName,
            string actionName,
            string correlationId,
            ControllerBase controller,
            string httpVerb = "GET",
            string apiVersion = "1.0",
            Dictionary<string, object> parameters = null)
        {
            await Log(
                logState: LogState.Warning,
                message: $"START: {messageStart}",
                controllerName: controllerName,
                actionName: actionName,
                httpVerb: httpVerb,
                apiVersion: apiVersion,
                correlationId: correlationId,
                controller: controller,
                parameters: parameters);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            action();

            stopwatch.Stop();

            await Log(
                logState: LogState.Warning,
                message: $"END: {messageEnd}. Tooked {stopwatch.ElapsedMilliseconds}ms",
                controllerName: controllerName,
                actionName: actionName,
                httpVerb: httpVerb,
                apiVersion: apiVersion,
                correlationId: correlationId,
                controller: controller,
                parameters: parameters);
        }

        public async Task<T> LogStartAndEnd<T>(Func<T> func,
            LogState logState,
            string messageStart,
            string messageEnd,
            string controllerName,
            string actionName,
            string correlationId,
            ControllerBase controller,
            string httpVerb = "GET",
            string apiVersion = "1.0",
            Dictionary<string, object> parameters = null)
        {
            await Log(
                logState: logState,
                message: $"START: {messageStart}",
                controllerName: controllerName,
                actionName: actionName,
                httpVerb: httpVerb,
                apiVersion: apiVersion,
                correlationId: correlationId,
                controller: controller,
                parameters: parameters);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            T value = func();

            stopwatch.Stop();

            await Log(
                logState: logState,
                message: $"END: {messageEnd}. Tooked {stopwatch.ElapsedMilliseconds}ms",
                controllerName: controllerName,
                actionName: actionName,
                httpVerb: httpVerb,
                apiVersion: apiVersion,
                correlationId: correlationId,
                controller: controller,
                parameters: parameters,
                elapsedMilliseconds: stopwatch.ElapsedMilliseconds);

            return value;
        }

        public async Task<T> LogStartAndEnd<T>(
            Func<Task<T>> func,
            LogState logState,
            string messageStart,
            string messageEnd,
            string controllerName,
            string actionName,
            string correlationId,
            ControllerBase controller,
            string httpVerb = "GET",
            string apiVersion = "1.0",
            Dictionary<string, object> parameters = null)
        {
            await Log(
                logState: logState,
                message: $"START: {messageStart}",
                controllerName: controllerName,
                actionName: actionName,
                httpVerb: httpVerb,
                apiVersion: apiVersion,
                correlationId: correlationId,
                controller: controller,
                parameters: parameters);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            T value = await func();

            stopwatch.Stop();

            await Log(
                logState: logState,
                message: $"END: {messageEnd}. Tooked {stopwatch.ElapsedMilliseconds}ms",
                controllerName: controllerName,
                actionName: actionName,
                httpVerb: httpVerb,
                apiVersion: apiVersion,
                correlationId: correlationId,
                controller: controller,
                parameters: parameters,
                elapsedMilliseconds: stopwatch.ElapsedMilliseconds);

            return value;
        }
    }
}
