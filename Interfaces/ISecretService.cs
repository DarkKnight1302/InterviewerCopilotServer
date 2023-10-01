namespace InterviewCopilotServer.Interfaces
{
    public interface ISecretService
    {
        public string GetSecretValue(string key);
    }
}
