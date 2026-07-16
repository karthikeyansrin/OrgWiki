using OrgWiki.API.Options;
using OrgWiki.Application;
using OrgWiki.Infrastructure;
using OrgWiki.Application.Analysis;
using OrgWiki.API.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using OrgWiki.Application.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddOptions<JwtOptions>()
    .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer) && !string.IsNullOrWhiteSpace(options.Audience), "JWT issuer and audience are required.")
    .Validate(options => options.SecretKey.Length >= 32, "JWT secret key must be at least 32 characters.")
    .Validate(options => options.ExpirationMinutes > 0, "JWT expiration must be positive.")
    .ValidateOnStart();
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "name"
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
builder.Services.AddScoped<IAccessTokenService, JwtAccessTokenService>();

builder.Services.Configure<OpenAiOptions>(options =>
{
    builder.Configuration.GetSection(OpenAiOptions.SectionName).Bind(options);
    options.ApiKey = builder.Configuration["OPENAI_API_KEY"] ?? options.ApiKey;
});
builder.Services.Configure<KnowledgeAnalysisOptions>(options =>
{
    builder.Configuration.GetSection(OpenAiOptions.SectionName).Bind(options);
    options.ApiKey = builder.Configuration["OPENAI_API_KEY"] ?? options.ApiKey;
});
builder.Services.AddOptions<KnowledgeAnalysisOptions>().Validate(options => options.TimeoutSeconds > 0, "OpenAI timeout must be positive.").ValidateOnStart();

builder.Services.Configure<StorageOptions>(options =>
{
    builder.Configuration.GetSection(StorageOptions.SectionName).Bind(options);
    options.Url = builder.Configuration["SUPABASE_URL"] ?? options.Url;
    options.Key = builder.Configuration["SUPABASE_KEY"] ?? options.Key;
    options.Bucket = builder.Configuration["SUPABASE_STORAGE_BUCKET"] ?? options.Bucket;
});

const string frontendCorsPolicy = "Frontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors(frontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
