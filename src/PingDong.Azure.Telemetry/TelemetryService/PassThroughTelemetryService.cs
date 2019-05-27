using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace PingDong.Azure.Telemetry
{
    public class PassThroughTelemetryService : ITelemetryService
    {
        /// <inheritdoc />
        public async Task<HttpResponseMessage> TrackApiCallAsync(HttpRequestMessage request, Func<Task<HttpResponseMessage>> func)
        {
            return await func();
        }
        
        /// <inheritdoc />
        public async Task<HttpResponseMessage> TrackApiCallAsync(string requestMethod, Uri requestUri, Func<Task<HttpResponseMessage>> func)
        {
            return await func();
        }
        
        /// <inheritdoc />
        public async Task<HttpResponseMessage> TrackApiCallAsync(string requestMethod, string requestUri, Func<Task<HttpResponseMessage>> func)
        {
            return await func();
        }

        public async Task<T> TrackCallAsync<T>(string target, string funcName, string data, Func<Task<T>> func)
        {
            return await func();
        }

        public async Task TrackCallAsync(string target, string funcName, string data, Func<Task> func)
        {
            await func();
        }
    }
}
