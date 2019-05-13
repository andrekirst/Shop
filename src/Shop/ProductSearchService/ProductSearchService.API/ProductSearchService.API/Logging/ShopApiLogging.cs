using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Elasticsearch.Net;
using NLog;
using ProductSearchService.API.Messaging;
using IDateTimeProvider = ProductSearchService.API.Infrastructure.IDateTimeProvider;

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
            Dictionary<string, object> paramters,
            Exception exception = null)
            => new LoggingQueueMessage
            {
                Timestamp = DateTimeProvider.Now,
                State = logState.GetStringValue(),
                Message = message,
                StackTrace = logState == LogState.Trace ? Environment.StackTrace : exception?.StackTrace,
                ApiActionName = actionName,
                ApiControllerName = controllerName,
                ApiApiHttpVerb = httpVerb,
                ApiVersion = apiVersion,
                Parameters = paramters,
                ServiceArea = LoggingOptions.ServiceArea,
                ServiceEnvironment = LoggingOptions.Environment,
                ServiceName = LoggingOptions.ServiceName,
                ServiceVersion = LoggingOptions.ServiceVersion,
                CorrelationId = correlationId
            };

        public async Task Log(
            LogState logState,
            string message,
            string controllerName,
            string actionName,
            string correlationId,
            string httpVerb = "GET",
            string apiVersion = "1.0",
            Dictionary<string, object> parameters = null,
            Exception exception = null)
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
                paramters: parameters,
                correlationId: correlationId);

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
            string httpVerb = "GET",
            string apiVersion = "1.0",
            Dictionary<string, object> parameters = null)
        {
            LoggingQueueMessage logMessage = CreateMessage(
                logState: LogState.Error,
                message: message,
                controllerName: controllerName,
                actionName: actionName,
                httpVerb: httpVerb,
                apiVersion: apiVersion,
                correlationId: correlationId,
                paramters: parameters);

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
            string httpVerb = "GET",
            string apiVersion = "1.0",
            Dictionary<string, object> parameters = null) =>
            Log(
                logState: LogState.Error,
                message: message,
                controllerName: controllerName,
                actionName: actionName,
                httpVerb: httpVerb,
                apiVersion: apiVersion,
                correlationId: correlationId,
                parameters: parameters);

        public Task LogTrace(
            string message,
            string controllerName,
            string actionName,
            string correlationId,
            string httpVerb = "GET",
            string apiVersion = "1.0",
            Dictionary<string, object> parameters = null) =>
            Log(
                logState: LogState.Trace,
                message: message,
                controllerName: controllerName,
                actionName: actionName,
                httpVerb: httpVerb,
                apiVersion: apiVersion,
                correlationId: correlationId,
                parameters: parameters);

        public Task LogDebug(
            string message,
            string controllerName,
            string actionName,
            string correlationId,
            string httpVerb = "GET",
            string apiVersion = "1.0",
            Dictionary<string, object> parameters = null) =>
            Log(
                logState: LogState.Debug,
                message: message,
                controllerName: controllerName,
                actionName: actionName,
                httpVerb: httpVerb,
                apiVersion: apiVersion,
                correlationId: correlationId,
                parameters: parameters);

        public Task LogInfo(
            string message,
            string controllerName,
            string actionName,
            string httpVerb,
            string apiVersion,
            string correlationId,
            Dictionary<string, object> parameters = null) =>
            Log(
                logState: LogState.Info,
                message: message,
                controllerName: controllerName,
                actionName: actionName,
                httpVerb: httpVerb,
                apiVersion: apiVersion,
                correlationId: correlationId,
                parameters: parameters);

        public Task LogWarning(string message,
            string controllerName,
            string actionName,
            string correlationId,
            string httpVerb = "GET",
            string apiVersion = "1.0",
            Dictionary<string, object> parameters = null) =>
            Log(
                logState: LogState.Warning,
                message: message,
                controllerName: controllerName,
                actionName: actionName,
                httpVerb: httpVerb,
                apiVersion: apiVersion,
                correlationId: correlationId,
                parameters: parameters);

        public async Task LogStartAndEnd(Action action,
            LogState logState,
            string messageStart,
            string messageEnd,
            string controllerName,
            string actionName,
            string correlationId,
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
                parameters: parameters);
        }

        public async Task<T> LogStartAndEnd<T>(Func<T> func,
            LogState logState,
            string messageStart,
            string messageEnd,
            string controllerName,
            string actionName,
            string correlationId,
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
                parameters: parameters);

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
                parameters: parameters);

            return value;
        }
    }
}
