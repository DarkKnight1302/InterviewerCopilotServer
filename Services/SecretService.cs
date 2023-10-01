
using InterviewCopilotServer.Interfaces;

namespace InterviewCopilotServer.Services
{
    public class SecretService : ISecretService
    {
        private readonly IConfiguration _configuration;

        public SecretService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetSecretValue(string key)
        {
            if (_configuration[key] == null)
            {
                return Environment.GetEnvironmentVariable(key) ?? string.Empty;
            }
            return _configuration[key];
        }
    }
}
