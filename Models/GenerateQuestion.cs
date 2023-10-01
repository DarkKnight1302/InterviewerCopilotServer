namespace InterviewCopilotServer.Models.InterviewCopilotModels
{
    public class GenerateQuestion
    {
        public string Prompt { get; set; }

        public string SystemContext { get; set; }

        public List<string> PreviouslyAskedQuestion { get; set; }

        public float Temperature { get; set; } = 0.5f;
    }
}
