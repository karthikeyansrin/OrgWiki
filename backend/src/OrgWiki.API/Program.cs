using OrgWiki.API.Options;
using OrgWiki.Application;
using OrgWiki.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<OpenAiOptions>(options =>
{
    builder.Configuration.GetSection(OpenAiOptions.SectionName).Bind(options);
    options.ApiKey = builder.Configuration["OPENAI_API_KEY"] ?? options.ApiKey;
});

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
app.MapControllers();

app.Run();

public partial class Program;
