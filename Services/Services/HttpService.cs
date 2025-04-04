using Newtonsoft.Json;
using Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;
using Services.Models;
using Microsoft.Extensions.Configuration;

namespace Services.Services
{
    public class HttpService : IHttpService
    {
        private readonly ILogger<HttpService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly int _timeout;
        public HttpService(ILogger<HttpService> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;

            _timeout = int.TryParse(configuration["OtherSettings:TimeOutPostGetRequest"], out int resTimeout) ? resTimeout : 30;
        }

        public async Task<ApiResult<T>> SendGetRequest<T>(string url, bool useProxy = false) where T : class
        {
            return await SendRequestAsync<T>(HttpMethod.Get, url, useProxy);
        }

        public async Task<ApiResult<T>> SendPostRequest<T>(string url, object payload, bool useProxy = false) where T : class
        {
            if (payload == null)
            {
                _logger.LogWarning("Payload is null.");
                return new ApiResult<T> { IsSuccess = false, ErrorMessage = "Payload is null." };
            }

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return await SendRequestAsync<T>(HttpMethod.Post, url, useProxy, content);
        }
        public async Task<ApiResult<T>> SendMultipartPostRequest<T>(string url, MultipartFormDataContent payload, bool useProxy = false) where T : class
        {
            if (payload == null)
            {
                _logger.LogWarning("MultipartFormDataContent payload is null.");
                return new ApiResult<T> { IsSuccess = false, ErrorMessage = "MultipartFormDataContent payload is null." };
            }
            return await SendRequestAsync<T>(HttpMethod.Post, url, useProxy, payload);
        }
        private async Task<ApiResult<T>> ProcessHttpResponse<T>(HttpResponseMessage response) where T : class
        {
            var result = new ApiResult<T>();

            if (response == null)
            {
                _logger.LogWarning("Received a null response.");
                return new ApiResult<T> {IsSuccess = false, ErrorMessage = "Null response from server." };
            }

            if (response.Content == null)
            {
                _logger.LogWarning("Received a response with null content.");
                return new ApiResult<T> { IsSuccess = false, ErrorMessage = "Response content is null." };
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            //_logger.LogInformation($"Received JSON response: {jsonResponse}");
            if (string.IsNullOrWhiteSpace(jsonResponse))
            {
                _logger.LogWarning("Received an empty JSON response.");
                result.IsSuccess = false;
                result.ErrorMessage = "Empty response from server.";
                return result;
            }

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    result.Data = JsonConvert.DeserializeObject<T>(jsonResponse);
                    result.IsSuccess = result.Data != null;
                    if (!result.IsSuccess)
                    {
                        _logger.LogWarning("Deserialized object is null.");
                        result.ErrorMessage = "Deserialized object is null.";
                    }
                }
                catch (JsonException ex)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"Failed to deserialize response: {ex.Message}";
                    _logger.LogError(result.ErrorMessage);
                }
            }
            else
            {
                result.IsSuccess = false;
                result.ErrorMessage = await HandleErrorResponse(response);
                _logger.LogWarning($"HTTP Error {response.StatusCode}: {result.ErrorMessage}");
            }

            return result;
        }

        private async Task<ApiResult<T>> SendRequestAsync<T>(HttpMethod method, string url, bool useProxy = false, HttpContent ? content = null) where T : class
        {
            _logger.LogInformation($"Sending {method} request to {url} via {(useProxy ? "PROXY" : "DIRECT")}");
            if (string.IsNullOrWhiteSpace(url))
            {
                _logger.LogWarning("URL is null or empty.");
                return new ApiResult<T> { IsSuccess = false, ErrorMessage = "URL is null or empty." };
            }

            _logger.LogInformation($"Sending {method} request to URL: {url}");

            var client = GetHttpClient(useProxy);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeout));
            
            var request = new HttpRequestMessage(method, url);
            
            if (content != null)
            {
                request.Content = content;
            }

            try
            {
                var response = await client.SendAsync(request, cts.Token);
                return await ProcessHttpResponse<T>(response).ConfigureAwait(false) ?? new ApiResult<T> { IsSuccess = false, ErrorMessage = "Unexpected null response." };
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning($"Request timed out: {url}");
                return new ApiResult<T> { IsSuccess = false, ErrorMessage = $"Request timeout. {ex.Message}" };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"An error occurred during the HTTP request: {ex.Message}");
                return new ApiResult<T> { IsSuccess = false, ErrorMessage = ex.Message };
            }
            catch (Exception ex)
            {
                _logger.LogError($"An unexpected error occurred: {ex.Message}");
                return new ApiResult<T> { IsSuccess = false, ErrorMessage = $"An unexpected error occurred: {ex.Message}" };
            }
        }
        private HttpClient GetHttpClient(bool useProxy)
        {
            return useProxy
                ? _httpClientFactory.CreateClient("ProxyClient")
                : _httpClientFactory.CreateClient("DefaultClient");
        }
        private async Task<string> HandleErrorResponse(HttpResponseMessage response)
        {
            if (response == null)
            {
                _logger.LogWarning("Received a null response in HandleErrorResponse.");
                return "Null response from server.";
            }

            if (response.Content == null)
            {
                _logger.LogWarning("Received a response with null content in HandleErrorResponse.");
                return $"HTTP {response.StatusCode}: No error details provided.";
            }

            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
            {
                return $"HTTP {response.StatusCode}: No error details provided.";
            }

            try
            {
                var error = JsonConvert.DeserializeObject<ApiErrorResponse>(content);
                return error?.reason ?? $"Unexpected error: {content}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to deserialize error response: {ex.Message}");
                return $"Failed to parse error response: {content}";
            }
        }
    }
}
