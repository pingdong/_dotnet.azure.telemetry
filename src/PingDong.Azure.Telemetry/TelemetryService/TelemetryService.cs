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
            _telemetry = telemetry;
        }
        
        /// <inheritdoc />
        public async Task<HttpResponseMessage> TrackApiCallAsync(HttpRequestMessage request, Func<Task<HttpResponseMessage>> func)
        {
            return await TrackApiCallAsync(request.Method.Method, request.RequestUri, func);
        }
        
        /// <inheritdoc />
        public async Task<HttpResponseMessage> TrackApiCallAsync(string requestMethod, string requestUri, Func<Task<HttpResponseMessage>> func)
        {
            return await TrackApiCallAsync(requestMethod, new Uri(requestUri), func);
        }
        
        /// <inheritdoc />
        public async Task<HttpResponseMessage> TrackApiCallAsync(string requestMethod, Uri requestUri, Func<Task<HttpResponseMessage>> func)
        {
            var name = $"{requestMethod.ToUpperInvariant()} {requestUri.Fragment}";

            return await TrackCallAsync<HttpResponseMessage>(name, requestUri.Host, requestUri.AbsolutePath, func);
        }
        
        /// <inheritdoc />
        public async Task<T> TrackCallAsync<T>(string target, string funcName, string data, Func<Task<T>> func)
        {
            #region Start

            var start = DateTime.UtcNow;
            
            // Track start Event
            var startEvent = new EventTelemetry($"'{funcName}' calling");
            startEvent.Context.User.Id = funcName;
            _telemetry.TrackEvent(startEvent);

            // Track start Metric
            var startMetric = new MetricTelemetry($"'{funcName}'", DateTime.UtcNow.Millisecond);
            startMetric.Context.User.Id = funcName;
            _telemetry.TrackMetric(startMetric);

            #endregion

            // Execute function
            T result;

            try
            {
                result = await func();
            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);

                throw;
            }
            
            // Track a Dependency
            var dependency = new DependencyTelemetry
            {
                Name = funcName,
                Target = target,
                Data = data,
                Timestamp = start,
                Duration = DateTime.UtcNow - start,
                Success = true
            };
            _telemetry.TrackDependency(dependency);

            return result;
        }
        
        /// <inheritdoc />
        public async Task TrackCallAsync(string target, string funcName, string data, Func<Task> func)
        {
            #region Start

            var start = DateTime.UtcNow;
            
            // Track start Event
            var startEvent = new EventTelemetry($"'{funcName}' calling");
            startEvent.Context.User.Id = funcName;
            _telemetry.TrackEvent(startEvent);

            // Track start Metric
            var startMetric = new MetricTelemetry($"'{funcName}'", DateTime.UtcNow.Millisecond);
            startMetric.Context.User.Id = funcName;
            _telemetry.TrackMetric(startMetric);

            #endregion

            // Execute function
            try
            {
                await func();
            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);

                throw;
            }
            
            // Track a Dependency
            var dependency = new DependencyTelemetry
            {
                Name = funcName,
                Target = target,
                Data = data,
                Timestamp = start,
                Duration = DateTime.UtcNow - start,
                Success = true
            };
            _telemetry.TrackDependency(dependency);
        }
    }
}
