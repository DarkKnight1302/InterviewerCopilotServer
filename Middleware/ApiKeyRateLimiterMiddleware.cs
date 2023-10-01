using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Security.Claims;

namespace InterviewCopilotServer.Middleware
{
    public class ApiKeyRateLimiterMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _period;
        private readonly Dictionary<string, int> RateLimitedApisPerMinRate = new Dictionary<string, int>()
        {
            { "/api/OpenAI/generate-question", 20 },
            { "/api/OpenAI/analyze-solution", 20 },
        };

        public ApiKeyRateLimiterMiddleware(RequestDelegate next, IMemoryCache cache, TimeSpan period, ILogger<ApiKeyRateLimiterMiddleware> logger)
        {
            _next = next;
            _cache = cache;
            _period = period;
        }

        public async Task Invoke(HttpContext context)
        {
            var identity = context.User.Identity as ClaimsIdentity;
            var userId = identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            string apiKey = RateLimitedApisPerMinRate.Keys.Where(api => context.Request.Path.StartsWithSegments(api)).First();
            int limit = RateLimitedApisPerMinRate[apiKey];

            var key = userId;

            if (string.IsNullOrEmpty(key))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("API key required in Header under X-Api-Key");
                return;
            }
            key += context.Request.Path;

            var counter = _cache.GetOrCreate(key, e =>
            {
                e.AbsoluteExpirationRelativeToNow = _period;
                return 0;
            });

            if (counter > limit)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync($"API rate limit exceeded ({limit} requests per minute)");
                return;
            }

            _cache.Set(key, counter + 1, _period);

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Something Went Wrong");
                Debug.WriteLine($"Exception : {ex.Message} : {ex.StackTrace}");
            }
        }
    }
}
