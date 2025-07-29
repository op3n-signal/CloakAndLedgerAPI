using System.Reflection.PortableExecutable;
using System.Text.Json;

namespace EncompassAPI.Services
{
    public class HttpService : IHttpService
    {
        private const double Timeout = 300000; //5 minutes
        private readonly ILogger<HttpService> _logger;
        private HttpClient _httpClient;
        private readonly object _lock = new object();

        public HttpClient HttpClient
        {
            get
            {
                return GetHttpClient();
            }

            set             
            {
                lock (_lock)
                {
                    _httpClient = value;
                }
            }
        }

        public HttpService(ILogger<HttpService> logger)
        {
            _httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(Timeout)
            };
            _logger = logger;
        }

        public async Task<HttpResponseMessage> GetAsync(string url, Dictionary<string, string> headers, int maxRetries = 3, int delayMilliseconds = 2000)
        {
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);

                    if (headers != null)
                    {
                        foreach (var kvp in headers)
                        {
                            request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
                        }
                    }

                    HttpResponseMessage response = await _httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        return response;

                        
                    }
                    else
                    {
                        throw new HttpRequestException($"Server returned error code: {response.StatusCode}. Request URL: {url}");
                    }
                }
                catch (Exception ex)
                {
                    //This catches the exception thrown above so we can retry 

                    _logger.LogError(ex.ToString());

                    //todo if (ex.Message.ToLower().Contains("unauthorized"))
                    //terminate = true;

                    //This is to avoid this section on the last retry
                    if (retryCount < maxRetries)
                    {
                        retryCount++;
                        RecreateHttpClient();
                        await Task.Delay(delayMilliseconds * retryCount);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Only for JSON responses
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="headers"></param>
        /// <param name="maxRetries"></param>
        /// <param name="delayMilliseconds"></param>
        /// <returns></returns>
        public async Task<T> GetAsync<T>(string url, Dictionary<string, string> headers, int maxRetries = 3, int delayMilliseconds = 2000)
        {
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);

                    if (headers != null)
                    {
                        foreach (var kvp in headers)
                        {
                            request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
                        }
                    }

                    HttpResponseMessage response = await _httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();

                        return JsonSerializer.Deserialize<T>(json);
                    }
                    else
                    {
                        throw new HttpRequestException($"Server returned error code: {response.StatusCode}. Request URL: {url}");
                    }
                }
                catch (Exception ex)
                {
                    //This catches the exception thrown above so we can retry 

                    _logger.LogError(ex.ToString());

                    //todo if (ex.Message.ToLower().Contains("unauthorized"))
                    //terminate = true;

                    //This is to avoid this section on the last retry
                    if (retryCount < maxRetries)
                    {
                        retryCount++;
                        RecreateHttpClient();
                        await Task.Delay(delayMilliseconds * retryCount);
                    }
                }
            }

            throw new Exception();
        }

        /// <summary>
        /// Only for JSON responses
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="headers"></param>
        /// <param name="body"></param>
        /// <param name="maxRetries"></param>
        /// <param name="delayMilliseconds"></param>
        /// <returns></returns>
        public async Task<T> PostAsync<T>(string url, Dictionary<string, string> headers, string body, int maxRetries = 3, int delayMilliseconds = 2000)
        {
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);

                    if (body != null) request.Content = new StringContent(body, null, "application/json");

                    if (headers != null)
                    {
                        foreach (var kvp in headers)
                        {
                            request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
                        }
                    }

                    HttpResponseMessage response = await _httpClient.SendAsync(request);
                    string json = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {


                        return JsonSerializer.Deserialize<T>(json);
                    }
                    else
                    {
                        throw new HttpRequestException($"Server returned error code: {response.StatusCode}. Request URL: {url}");
                    }
                }
                catch (Exception ex)
                {
                    //This catches the exception thrown above so we can retry 

                    _logger.LogError(ex.ToString());

                    //This is to avoid this section on the last retry
                    if (retryCount < maxRetries)
                    {
                        retryCount++;
                        RecreateHttpClient();
                        await Task.Delay(delayMilliseconds * retryCount);
                    }
                }
            }

            return default(T);
        }

        public async Task<T> PostAsync<T>(string url, FormUrlEncodedContent formContent, int maxRetries = 3, int delayMilliseconds = 2000)
        {
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);

                    if (formContent != null) request.Content = formContent;

                    HttpResponseMessage response = await _httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();

                        return JsonSerializer.Deserialize<T>(json);
                    }
                    else
                    {
                        throw new HttpRequestException($"Server returned error code: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    //This catches the exception thrown above so we can retry 
                    _logger.LogError(ex.ToString());

                    //This is to avoid this section on the last retry
                    if (retryCount < maxRetries)
                    {
                        retryCount++;
                        RecreateHttpClient();
                        await Task.Delay(delayMilliseconds * retryCount);
                    }
                }
            }

            return default(T);
        }

        /// <summary>
        /// For posting json content
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="body"></param>
        /// <param name="maxRetries"></param>
        /// <param name="delayMilliseconds"></param>
        /// <returns></returns>
        public async Task<T> PostAsync<T>(string url, string body, int maxRetries = 3, int delayMilliseconds = 2000)
        {
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);

                    if (body != null)
                    {
                        request.Content = new StringContent(body, null, "application/json");
                        //Need to explicitly set the content header type
                        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    }

                    HttpResponseMessage response = await _httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();

                        return JsonSerializer.Deserialize<T>(json);
                    }
                    else
                    {
                        throw new HttpRequestException($"Server returned error code: {response.StatusCode}. Request URL: {url}");
                    }
                }
                catch (Exception ex)
                {
                    //This catches the exception thrown above so we can retry 

                    _logger.LogError(ex.ToString());

                    //This is to avoid this section on the last retry
                    if (retryCount < maxRetries)
                    {
                        retryCount++;
                        RecreateHttpClient();
                        await Task.Delay(delayMilliseconds * retryCount);
                    }
                }
            }

            throw new Exception();
        }

        /// <summary>
        /// This is when you only want to return a string.
        /// This should be used when expecting XML respones.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="headers"></param>
        /// <param name="body"></param>
        /// <param name="maxRetries"></param>
        /// <param name="delayMilliseconds"></param>
        /// <returns></returns>
        public async Task<string> PostAsync(string url, Dictionary<string, string> headers, StringContent body, int maxRetries = 3, int delayMilliseconds = 2000)
        {
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);

                    if (body != null) request.Content = body;

                    if (headers != null)
                    {
                        foreach (var kvp in headers)
                        {
                            request.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
                        }
                    }

                    HttpResponseMessage response = await _httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        string strResponse = await response.Content.ReadAsStringAsync();

                        return strResponse;
                    }
                    else
                    {
                        throw new HttpRequestException($"Server returned error code: {response.StatusCode}. Request URL: {url}");
                    }
                }
                catch (Exception ex)
                {
                    //This catches the exception thrown above so we can retry 

                    _logger.LogError(ex.ToString());

                    //This is to avoid this section on the last retry
                    if (retryCount < maxRetries)
                    {
                        retryCount++;
                        RecreateHttpClient();
                        await Task.Delay(delayMilliseconds * retryCount);
                    }
                }
            }

            return null;
        }

        private HttpClient GetHttpClient()
        {
            lock (_lock)
            {
                if (_httpClient == null)
                {
                    _httpClient = CreateHttpClient();
                }
                return _httpClient;
            }
        }

        private HttpClient CreateHttpClient()
        {
            return new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(Timeout)
            };
        }

        // Method to recreate HttpClient in case of issues with the static HttpClient
        private void RecreateHttpClient()
        {
            lock (_lock)
            {
                _httpClient?.Dispose();  // Dispose of the old client, releasing resources
                _httpClient = CreateHttpClient();  // Create a new HttpClient instance
            }
        }
    }

    public interface IHttpService
    {
        HttpClient HttpClient { get; set; }

        Task<T> GetAsync<T>(string url, Dictionary<string, string> headers, int maxRetries = 3, int delayMilliseconds = 2000);
        /// <summary>
        /// Only for JSON responses
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="headers"></param>
        /// <param name="maxRetries"></param>
        /// <param name="delayMilliseconds"></param>
        /// <returns></returns>
        Task<HttpResponseMessage> GetAsync(string url, Dictionary<string, string> headers, int maxRetries = 3, int delayMilliseconds = 2000);

        /// <summary>
        /// Only for JSON responses
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="headers"></param>
        /// <param name="body"></param>
        /// <param name="maxRetries"></param>
        /// <param name="delayMilliseconds"></param>
        /// <returns></returns>
        Task<T> PostAsync<T>(string url, Dictionary<string, string> headers, string body, int maxRetries = 3, int delayMilliseconds = 2000);

        Task<T> PostAsync<T>(string url, FormUrlEncodedContent formContent, int maxRetries = 3, int delayMilliseconds = 2000);

        Task<T> PostAsync<T>(string url, string body, int maxRetries = 3, int delayMilliseconds = 2000);

        /// <summary>
        /// This is when you only want to return a string.
        /// This should be used when expecting XML respones. You can use ParseSoapResponse method on XML to convert to models.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="headers"></param>
        /// <param name="body"></param>
        /// <param name="maxRetries"></param>
        /// <param name="delayMilliseconds"></param>
        /// <returns></returns>
        Task<string> PostAsync(string url, Dictionary<string, string> headers, StringContent body, int maxRetries = 3, int delayMilliseconds = 2000);
    }
}
