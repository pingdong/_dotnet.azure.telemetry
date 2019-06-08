using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace PingDong.Azure.Telemetry
{
    public class TelemetryService : ITelemetryService
    {
        private readonly TelemetryClient _telemetry;

        public TelemetryService(TelemetryClient telemetry)
        {
            _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        }


        #region ITelemetryService

        /// <inheritdoc />
        public Task<HttpResponseMessage> TrackWebApiAsync(string correlationId, HttpRequestMessage request, Func<Task<HttpResponseMessage>> func)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return TrackWebApiAsync(correlationId, request.Method.Method, request.RequestUri, func);
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> TrackWebApiAsync(string correlationId, string requestMethod, string requestUri, Func<Task<HttpResponseMessage>> func)
        {
            if (string.IsNullOrWhiteSpace(requestMethod))
                throw new ArgumentNullException(nameof(requestMethod));
            if (string.IsNullOrWhiteSpace(requestUri))
                throw new ArgumentNullException(nameof(requestUri));

            return TrackWebApiAsync(correlationId, requestMethod, new Uri(requestUri), func);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> TrackWebApiAsync(string correlationId, string requestMethod, Uri requestUri, Func<Task<HttpResponseMessage>> func)
        {
            var start = DateTime.UtcNow;

            var funcName = $"{requestMethod.ToUpperInvariant()} {requestUri.Fragment}";

            // Start
            TrackStart(correlationId, funcName);
            
            // Execute function
            HttpResponseMessage message;
            try
            {
                message = await func();
            }
            catch (Exception ex)
            {
                TrackError(correlationId, funcName, ex);

                throw;
            }

            // Complete

            var complete = DateTime.UtcNow;
            var duration = complete - start;

            TrackDependency(funcName, DependencyType.WebAPI, requestUri.Host, requestUri.Query, start, duration, message.StatusCode.ToString());

            TrackComplete(correlationId, funcName, duration.TotalMilliseconds);

            return message;
        }

        /// <inheritdoc />
        public Task<T> TrackFunctionAsync<T>(string funcName, string type, Func<Task<T>> func)
        {
            return TrackFunctionAsync(null, funcName, type, null, null, func);
        }

        /// <inheritdoc />
        public Task<T> TrackFunctionAsync<T>(string funcName, string type, string target, Func<Task<T>> func)
        {
            return TrackFunctionAsync(null, funcName, type, target, null, func);
        }

        /// <inheritdoc />
        public Task<T> TrackFunctionAsync<T>(string funcName, string type, string target, string data, Func<Task<T>> func)
        {
            return TrackFunctionAsync(null, funcName, type, target, data, func);
        }

        /// <inheritdoc />
        public Task<T> TrackFunctionAsync<T>(string correlationId, string funcName, string type, string target, string data, Func<Task<T>> func)
        {
            var start = DateTime.UtcNow;

            TrackStart(correlationId, funcName);
            
            // Execute function
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                TrackError(correlationId, funcName, ex);

                throw;
            }
            finally
            {
                // Complete

                var complete = DateTime.UtcNow;
                var duration = complete - start;

                // Track a Dependency
                TrackDependency(funcName, type, target, data, start, duration);

                TrackComplete(correlationId, funcName, duration.TotalMilliseconds);
            }
        }

        /// <inheritdoc />
        public Task TrackActionAsync(string funcName, string type, Func<Task> action)
        {
            return TrackActionAsync(funcName, type, null, null, action);
        }

        /// <inheritdoc />
        public Task TrackActionAsync(string funcName, string type, string target, Func<Task> action)
        {
            return TrackActionAsync(funcName, type, target, null, action);
        }

        /// <inheritdoc />
        public Task TrackActionAsync(string funcName, string type, string target, string data, Func<Task> action)
        {
            return TrackActionAsync(null, funcName, type, target, data, action);
        }

        /// <inheritdoc />
        public Task TrackActionAsync(string correlationId, string funcName, string type, string target, string data, Func<Task> action)
        {
            var start = DateTime.UtcNow;

            TrackStart(correlationId, funcName);
            
            // Execute function
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                TrackError(correlationId, funcName, ex);

                throw;
            }
            finally
            {
                // Complete

                var complete = DateTime.UtcNow;
                var duration = complete - start;

                // Track a Dependency
                TrackDependency(funcName, type, target, data, start, duration);

                TrackComplete(correlationId, funcName, duration.TotalMilliseconds);
            }
        }

        #endregion

        #region Private

        private void TrackStart(string correlationId, string funcName)
        {
            var evt = new EventTelemetry
            {
                Name = $"[CALLING] {funcName}"
            };
            evt.Context.Operation.Name = funcName;
            if (!string.IsNullOrWhiteSpace(correlationId))
                evt.Context.Operation.CorrelationVector = correlationId;

            _telemetry.TrackEvent(evt);
        }

        private void TrackError(string correlationId, string funcName, Exception exception)
        {
            _telemetry.TrackException(exception);

            var evt = new EventTelemetry
            {
                Name = $"[ERROR] {funcName}"
            };
            evt.Context.Operation.Name = funcName;
            if (!string.IsNullOrWhiteSpace(correlationId))
                evt.Context.Operation.CorrelationVector = correlationId;

            _telemetry.TrackEvent(evt);
        }
        
        private void TrackComplete(string correlationId, string funcName, double duration)
        {
            var evt = new EventTelemetry
            {
                Name = $"[CALLED] {funcName}"
            };
            evt.Context.Operation.Name = funcName;
            if (!string.IsNullOrWhiteSpace(correlationId))
                evt.Context.Operation.CorrelationVector = correlationId;
            evt.Metrics.Add("Duration", duration);

            _telemetry.TrackEvent(evt);
        }

        private void TrackDependency(string funcName, string type, string target, string data, DateTimeOffset timestamp, TimeSpan duration, string resultCode = null)
        {
            var dependency = new DependencyTelemetry
            {
                Name = funcName,
                Target = target,
                Data = data,
                Timestamp = timestamp,
                Duration = duration,
                Type = type
            };
            if (!string.IsNullOrWhiteSpace(resultCode))
                dependency.ResultCode = resultCode;

            _telemetry.TrackDependency(dependency);
        }

        #endregion
    }
}
