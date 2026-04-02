using System.Text;
using System.Text.Json;

namespace ScriptManager.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("api");

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            var response = await _httpClient.GetAsync(endpoint);
            if (!response.IsSuccessStatusCode) return default;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }

        public async Task<List<T>> GetListAsync<T>(string endpoint)
        {
            var response = await _httpClient.GetAsync(endpoint);
            if (!response.IsSuccessStatusCode) return new List<T>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? new List<T>();
        }

        public async Task<TResult?> PostAsync<TRequest, TResult>(string endpoint, TRequest request)
        {
            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(endpoint, content);
            var json = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(json)) return default;

            return JsonSerializer.Deserialize<TResult>(json, _jsonOptions);
        }

        public async Task<TResult?> PutAsync<TRequest, TResult>(string endpoint, TRequest request)
        {
            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PutAsync(endpoint, content);
            var json = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(json)) return default;

            return JsonSerializer.Deserialize<TResult>(json, _jsonOptions);
        }

        public async Task<TResult?> DeleteAsync<TRequest, TResult>(string endpoint, TRequest request)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete, endpoint)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json")
            };

            var response = await _httpClient.SendAsync(message);
            var json = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(json)) return default;

            return JsonSerializer.Deserialize<TResult>(json, _jsonOptions);
        }
    }
}