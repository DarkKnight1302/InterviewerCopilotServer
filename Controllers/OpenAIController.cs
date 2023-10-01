using InterviewCopilotServer.Interfaces;
using InterviewCopilotServer.Models.InterviewCopilotModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InterviewCopilotServer.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OpenAIController : ControllerBase
    {
        private readonly IOpenAIService openAIService;
        public OpenAIController(IOpenAIService openAIService)
        {
            this.openAIService = openAIService;
        }

        [RequireHttps]
        [HttpPost("generate-question")]
        public async Task<IActionResult> GenerateQuestion([FromBody] GenerateQuestion generateQuestion)
        {
            if (generateQuestion == null)
            {
                return BadRequest("Bad request");
            }

            string generatedQuestion = await this.openAIService.GenerateQuestionAsync(generateQuestion.Prompt, generateQuestion.SystemContext, generateQuestion.PreviouslyAskedQuestion, generateQuestion.Temperature).ConfigureAwait(false);

            return Ok(generatedQuestion);
        }

        [RequireHttps]
        [HttpPost("analyze-solution")]
        public async Task<IActionResult> AnalyzeSolution([FromBody] AnalyzeSolution analyzeSolution)
        {
            if (analyzeSolution == null)
            {
                return BadRequest("Bad request");
            }

            string analyzedSolution = await this.openAIService.AnalyzeSolutionAsync(analyzeSolution.Question, analyzeSolution.Solution, analyzeSolution.SystemContext).ConfigureAwait(false);

            return Ok(analyzedSolution);
        }
    }
}
