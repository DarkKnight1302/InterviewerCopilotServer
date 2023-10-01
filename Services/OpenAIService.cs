using Azure;
using Azure.AI.OpenAI;
using InterviewCopilotServer.Helpers;
using InterviewCopilotServer.Interfaces;

namespace InterviewCopilotServer.Services
{
    public class OpenAIService : IOpenAIService
    {
        private const string GPT_35_Model = "gpt-35-turbo";
        private HttpClient httpClient;
        private OpenAIClient openAIClient;
        private RequestThresholdPerDay requestThresholdPerDay;

        public OpenAIService(ISecretService secretService)
        {
            this.httpClient = new HttpClient();
            string openAIKey = secretService.GetSecretValue("OPEN_AI_KEY");
            string openAIUrl = secretService.GetSecretValue("OPEN_AI_URL");
            this.httpClient.DefaultRequestHeaders.Add("api-key", openAIKey);
            this.httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {openAIKey}");
            this.openAIClient = new OpenAIClient(new Uri(openAIUrl), new AzureKeyCredential(openAIKey));
            this.requestThresholdPerDay = new RequestThresholdPerDay(10000);
        }

        public async Task<string> GenerateQuestionAsync(string prompt, string systemContext, List<string> previouslyAskedQuestion, float temperature = 0.5f)
        {
            if (!requestThresholdPerDay.AllowRequest())
            {
                return "Day limit exceeded";
            }
            var systemContextMessage = new ChatMessage(ChatRole.System, systemContext);
            var promptMessage = new ChatMessage(ChatRole.User, prompt);
            ChatCompletionsOptions options = new ChatCompletionsOptions
            {
                ChoiceCount = 1,
                MaxTokens = 1000,
                PresencePenalty = 2,
                Temperature = temperature,
            };
            options.Messages.Add(systemContextMessage);
            foreach (var message in previouslyAskedQuestion)
            {
                options.Messages.Add(new ChatMessage(ChatRole.Assistant, message));
            }
            options.Messages.Add(promptMessage);
            NullableResponse<ChatCompletions> response = await this.openAIClient.GetChatCompletionsAsync(GPT_35_Model, options).ConfigureAwait(false);
            if (response.HasValue)
            {
                var chatCompletions = response.Value;
                var chatMessage = chatCompletions.Choices[0].Message;
                return chatMessage.Content;
            }
            return "Please try again...";
        }

        public async Task<string> AnalyzeSolutionAsync(string question, string solution, string systemContext)
        {
            if (!requestThresholdPerDay.AllowRequest())
            {
                return "Day limit exceeded";
            }
            var systemContextMessage = new ChatMessage(ChatRole.System, systemContext);
            var questionMessage = new ChatMessage(ChatRole.Assistant, question);
            var solutionMessage = new ChatMessage(ChatRole.User, solution);
            ChatCompletionsOptions options = new ChatCompletionsOptions
            {
                ChoiceCount = 1,
                MaxTokens = 2000,
                Temperature = 0,
            };
            options.Messages.Add(systemContextMessage);
            options.Messages.Add(questionMessage);
            options.Messages.Add(solutionMessage);
            NullableResponse<ChatCompletions> response = await this.openAIClient.GetChatCompletionsAsync(GPT_35_Model, options).ConfigureAwait(false);
            {
                var chatCompletions = response.Value;
                var chatMessage = chatCompletions.Choices[0].Message;
                return chatMessage.Content;
            }
        }
    }
}
