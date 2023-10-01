namespace InterviewCopilotServer.Interfaces
{
    public interface IOpenAIService
    {
        public Task<string> GenerateQuestionAsync(string prompt, string systemContext, List<string> previouslyAskedQuestion, float temperature);

        public Task<string> AnalyzeSolutionAsync(string question, string solution, string systemContext);
    }
}
