using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using InterviewCopilotServer.Helpers;
using InterviewCopilotServer.Interfaces;

namespace InterviewCopilotServer.Services
{
    public class OpenAIService : IOpenAIService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly RequestThresholdPerDay requestThresholdPerDay;

        // --- Rate Limiting State ---
        private static readonly int MaxRequests = 9;
        private static readonly TimeSpan TimePeriod = TimeSpan.FromMinutes(1);
        private static readonly Queue<DateTime> RequestTimestamps = new Queue<DateTime>();
        private static readonly SemaphoreSlim RateLimitSemaphore = new SemaphoreSlim(1, 1);

        private const string GroqChatCompletionsUrl = "https://api.groq.com/openai/v1/chat/completions";
        private const string DefaultModel = "openai/gpt-oss-120b";

        // --- Retry ---
        private const int MaxRetryAttempts = 5;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan TooManyRequestsRetryDelay = TimeSpan.FromSeconds(6);

        public OpenAIService(ISecretService secretService)
        {
            string groqApiKey = secretService.GetSecretValue("groq_api_key");
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", groqApiKey);
            this.requestThresholdPerDay = new RequestThresholdPerDay(300);
        }

        public async Task<string> GenerateQuestionAsync(string prompt, string systemContext, List<string> previouslyAskedQuestion, float temperature = 0.5f)
        {
            if (!requestThresholdPerDay.AllowRequest())
            {
                return "Day limit exceeded";
            }

            return await ExecuteWithRetryAsync(
                operationName: "[Groq][GenerateQuestion]",
                operation: async () =>
                {
                    await WaitForRateLimitSlotAsync();

                    var messages = new List<ChatMessage>
                    {
                        new ChatMessage { Role = "system", Content = systemContext }
                    };

                    foreach (var message in previouslyAskedQuestion)
                    {
                        messages.Add(new ChatMessage { Role = "assistant", Content = message });
                    }

                    messages.Add(new ChatMessage { Role = "user", Content = prompt });

                    var requestBody = new ChatCompletionsRequest
                    {
                        Model = DefaultModel,
                        Temperature = temperature,
                        MaxCompletionTokens = 10000,
                        Messages = messages
                    };

                    return await SendRequestAsync(requestBody);
                });
        }

        public async Task<string> AnalyzeSolutionAsync(string question, string solution, string systemContext)
        {
            if (!requestThresholdPerDay.AllowRequest())
            {
                return "Day limit exceeded";
            }

            return await ExecuteWithRetryAsync(
                operationName: "[Groq][AnalyzeSolution]",
                operation: async () =>
                {
                    await WaitForRateLimitSlotAsync();

                    var messages = new List<ChatMessage>
                    {
                        new ChatMessage { Role = "system", Content = systemContext },
                        new ChatMessage { Role = "assistant", Content = question },
                        new ChatMessage { Role = "user", Content = solution }
                    };

                    var requestBody = new ChatCompletionsRequest
                    {
                        Model = DefaultModel,
                        Temperature = 0,
                        MaxCompletionTokens = 20000,
                        Messages = messages
                    };

                    return await SendRequestAsync(requestBody);
                });
        }

        private async Task<string> SendRequestAsync(ChatCompletionsRequest requestBody)
        {
            var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, GroqChatCompletionsUrl)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var groqResponse = JsonSerializer.Deserialize<ChatCompletionsResponse>(responseBody);

            if (groqResponse?.Choices != null && groqResponse.Choices.Count > 0)
            {
                return groqResponse.Choices[0].Message.Content;
            }
            throw new Exception("No choices in response");
        }

        private async Task<string> ExecuteWithRetryAsync(string operationName, Func<Task<string>> operation)
        {
            for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {operationName} Rate limited (HTTP 429) (attempt {attempt}/{MaxRetryAttempts}): {ex.Message}");
                    if (attempt == MaxRetryAttempts) throw;
                    await Task.Delay(TooManyRequestsRetryDelay);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {operationName} Error (attempt {attempt}/{MaxRetryAttempts}): {ex.Message}");
                    if (attempt == MaxRetryAttempts) return "Please try again...";
                    await Task.Delay(RetryDelay);
                }
            }
            return "Please try again...";
        }

        private async Task WaitForRateLimitSlotAsync()
        {
            await RateLimitSemaphore.WaitAsync();
            try
            {
                while (RequestTimestamps.Any() && (DateTime.UtcNow - RequestTimestamps.Peek()) > TimePeriod)
                {
                    RequestTimestamps.Dequeue();
                }

                if (RequestTimestamps.Count >= MaxRequests)
                {
                    var oldestRequestTime = RequestTimestamps.Peek();
                    var timePassedSinceOldest = DateTime.UtcNow - oldestRequestTime;
                    var timeToWait = TimePeriod - timePassedSinceOldest;

                    if (timeToWait > TimeSpan.Zero)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [Groq] Rate limit hit. Waiting for {timeToWait.TotalSeconds:F1} seconds...");
                        await Task.Delay(timeToWait);
                    }
                    RequestTimestamps.Dequeue();
                }

                RequestTimestamps.Enqueue(DateTime.UtcNow);
            }
            finally
            {
                RateLimitSemaphore.Release();
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            GC.SuppressFinalize(this);
        }

        #region DTOs

        private class ChatCompletionsRequest
        {
            [JsonPropertyName("messages")] public List<ChatMessage> Messages { get; set; }
            [JsonPropertyName("model")] public string Model { get; set; }
            [JsonPropertyName("temperature")] public float Temperature { get; set; } = 1;
            [JsonPropertyName("max_completion_tokens")] public int MaxCompletionTokens { get; set; } = 8192;
            [JsonPropertyName("top_p")] public int TopP { get; set; } = 1;
            [JsonPropertyName("stream")] public bool Stream { get; set; } = false;
            [JsonPropertyName("stop")] public object Stop { get; set; }
        }

        private class ChatMessage
        {
            [JsonPropertyName("role")] public string Role { get; set; }
            [JsonPropertyName("content")] public string Content { get; set; }
        }

        private class ChatCompletionsResponse
        {
            [JsonPropertyName("choices")] public List<Choice> Choices { get; set; }
        }

        private class Choice
        {
            [JsonPropertyName("message")] public ChatMessage Message { get; set; }
        }

        #endregion
    }
}
