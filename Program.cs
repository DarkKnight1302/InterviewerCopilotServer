using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using InterviewCopilotServer.Interfaces;
using InterviewCopilotServer.Services;
using Microsoft.Extensions.Caching.Memory;
using InterviewCopilotServer.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.Authority = "https://login.microsoftonline.com/consumers/v2.0"; // Replace with your tenant ID
    options.Audience = builder.Configuration.GetValue<string>("AAD_CLIENT_ID") ?? Environment.GetEnvironmentVariable("AAD_CLIENT_ID");
});

string aadClientId = builder.Configuration.GetValue<string>("AAD_CLIENT_ID") ?? Environment.GetEnvironmentVariable("AAD_CLIENT_ID");

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiScope", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", $"api://{aadClientId}/profile");
    });
});
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IOpenAIService, OpenAIService>();
builder.Services.AddSingleton<ISecretService, SecretService>();
builder.Services.AddMemoryCache();
builder.Services.AddCors();
builder.Configuration.AddEnvironmentVariables().AddUserSecrets<StartupBase>();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseMiddleware<ApiKeyRateLimiterMiddleware>(new MemoryCache(new MemoryCacheOptions()), TimeSpan.FromMinutes(1));
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
