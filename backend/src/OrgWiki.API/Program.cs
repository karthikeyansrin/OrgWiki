using OrgWiki.API.Options;
using OrgWiki.Application;
using OrgWiki.Infrastructure;
using OrgWiki.Application.Analysis;
using OrgWiki.API.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using OrgWiki.Application.Authentication;
using OrgWiki.Application.Ingestion;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options => options.IncludeScopes = true);

var configuredArchiveBytes = builder.Configuration.GetValue<long?>("Ingestion:MaxArchiveBytes") ?? 10 * 1024 * 1024;
if (configuredArchiveBytes <= 0) throw new InvalidOperationException("Ingestion archive size limit must be positive.");
var maxUploadRequestBytes = checked(configuredArchiveBytes + 128 * 1024);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = maxUploadRequestBytes);
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = maxUploadRequestBytes);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<JwtOptions>(options =>
{
    builder.Configuration.GetSection(JwtOptions.SectionName).Bind(options);
    options.SigningKey = builder.Configuration["JWT_SIGNING_KEY"] ?? options.SigningKey;
});
builder.Services.AddOptions<JwtOptions>()
    .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer) && !string.IsNullOrWhiteSpace(options.Audience), "JWT issuer and audience are required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.SigningKey), "JWT SigningKey is not configured.")
    .Validate(options => options.SigningKey.Length >= 32, "JWT SigningKey must be at least 32 characters.")
    .Validate(options => options.ExpirationMinutes > 0, "JWT expiration must be positive.")
    .ValidateOnStart();
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
jwt.SigningKey = builder.Configuration["JWT_SIGNING_KEY"] ?? jwt.SigningKey;
if (string.IsNullOrWhiteSpace(jwt.SigningKey)) throw new InvalidOperationException("JWT SigningKey is not configured.");
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
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
    options.Mode = builder.Configuration["OPENAI_MODE"] ?? options.Mode;
    options.Model = builder.Configuration["OPENAI_MODEL"] ?? options.Model;
    if (bool.TryParse(builder.Configuration["OPENAI_VERBOSE_LOGGING"], out var verboseLogging)) options.VerboseLogging = verboseLogging;
});
builder.Services.Configure<KnowledgeAnalysisOptions>(options =>
{
    builder.Configuration.GetSection(OpenAiOptions.SectionName).Bind(options);
    options.ApiKey = builder.Configuration["OPENAI_API_KEY"] ?? options.ApiKey;
    options.Mode = builder.Configuration["OPENAI_MODE"] ?? options.Mode;
    options.Model = builder.Configuration["OPENAI_MODEL"] ?? options.Model;
    if (bool.TryParse(builder.Configuration["OPENAI_VERBOSE_LOGGING"], out var verboseLogging)) options.VerboseLogging = verboseLogging;
});
builder.Services.AddOptions<KnowledgeAnalysisOptions>()
    .Validate(options => options.TimeoutSeconds > 0, "OpenAI timeout must be positive.")
    .Validate(options => string.Equals(options.Mode, "Replay", StringComparison.OrdinalIgnoreCase) || string.Equals(options.Mode, "Live", StringComparison.OrdinalIgnoreCase), "OpenAI mode must be Replay or Live.")
    .Validate(options => !string.Equals(options.Mode, "Live", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(options.ApiKey), "OPENAI_API_KEY is required when OpenAI mode is Live.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Model), "OpenAI model is required.")
    .ValidateOnStart();

builder.Services.Configure<StorageOptions>(options =>
{
    builder.Configuration.GetSection(StorageOptions.SectionName).Bind(options);
    options.Url = builder.Configuration["SUPABASE_URL"] ?? options.Url;
    options.Key = builder.Configuration["SUPABASE_KEY"] ?? options.Key;
    options.Bucket = builder.Configuration["SUPABASE_STORAGE_BUCKET"] ?? options.Bucket;
});

const string frontendCorsPolicy = "Frontend";
var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];
var allowedOrigins = configuredOrigins.Select(ValidateCorsOrigin).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
if (allowedOrigins.Length == 0) throw new InvalidOperationException("At least one allowed frontend origin must be configured.");

builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
            .WithHeaders("Authorization", "Content-Type")
            .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS"));
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new { error = "Too many requests. Please wait before trying again." }, cancellationToken);
    };
    options.AddPolicy("auth", context => FixedWindow(context, "auth", permitLimit: 10, window: TimeSpan.FromMinutes(1)));
    options.AddPolicy("upload", context => FixedWindow(context, "upload", permitLimit: 5, window: TimeSpan.FromMinutes(5)));
    options.AddPolicy("ai", context => FixedWindow(context, "ai", permitLimit: 4, window: TimeSpan.FromMinutes(10)));
});

var app = builder.Build();

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("OrgWiki.API.UnhandledException");
    logger.LogError(exception, "Unhandled request failure. CorrelationId {CorrelationId}", context.TraceIdentifier);
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(new { error = "The request could not be completed." });
}));

app.Use(async (context, next) =>
{
    var supplied = context.Request.Headers["X-Correlation-ID"].ToString();
    var correlationId = Guid.TryParse(supplied, out var parsed) ? parsed.ToString("D") : Guid.NewGuid().ToString("D");
    context.TraceIdentifier = correlationId;
    context.Response.Headers["X-Correlation-ID"] = correlationId;
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), geolocation=(), microphone=()";
    if (!context.Request.Path.StartsWithSegments("/swagger"))
        context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; base-uri 'none'; frame-ancestors 'none'; form-action 'self'";
    if (context.Request.Path.StartsWithSegments("/api"))
        context.Response.Headers.CacheControl = "no-store";

    using (app.Logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors(frontendCorsPolicy);
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

app.Run();

static RateLimitPartition<string> FixedWindow(HttpContext context, string policy, int permitLimit, TimeSpan window)
    => RateLimitPartition.GetFixedWindowLimiter(
        $"{policy}:{RateLimitKey(context)}",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = window,
            QueueLimit = 0,
            AutoReplenishment = true
        });

static string RateLimitKey(HttpContext context)
{
    var userId = context.User.FindFirst("sub")?.Value;
    return Guid.TryParse(userId, out var parsed)
        ? $"user:{parsed:D}"
        : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
}

static string ValidateCorsOrigin(string origin)
{
    var normalized = origin?.Trim().TrimEnd('/') ?? string.Empty;
    if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
        || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        || string.IsNullOrWhiteSpace(uri.Host)
        || !string.IsNullOrEmpty(uri.UserInfo)
        || !string.Equals(uri.GetLeftPart(UriPartial.Authority), normalized, StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Cors:AllowedOrigins must contain absolute HTTP or HTTPS origins only.");
    return normalized;
}

public partial class Program;
