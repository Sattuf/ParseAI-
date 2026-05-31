using System.Text.Json.Serialization;
using AnalyzeChat.Application.Interfaces;
using AnalyzeChat.Application.Services;
using AnalyzeChat.Infrastructure.Services;
using AnalyzeChat.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Database Context ────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

// ── CORS ────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin => true) // Allow dynamic origins/ports locally
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Needed usually if dynamic origins are allowed
    });
});

// ── Authentication & Authorization ───────────
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    // Required by Google Auth to temporarily store identity before we issue a JWT
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie()
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? throw new InvalidOperationException("Google ClientId missing");
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? throw new InvalidOperationException("Google ClientSecret missing");
});

builder.Services.AddAuthorization();

// ── Controllers ─────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── HttpClient for Gemini API ───────────────
// ── HttpClient for Gemini API ───────────────
builder.Services.AddHttpClient<IChatAIService, GeminiService>()
    .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromMinutes(5));

// ── Dependency Injection ────────────────────
builder.Services.AddSingleton<IPdfService, PdfParserService>();
builder.Services.AddSingleton<IChunkingService, SmartChunkingService>();
// builder.Services.AddScoped<IChatAIService, GeminiService>(); // Removed, already registered above
builder.Services.AddSingleton<IVectorStore, InMemoryVectorStore>();

// ── Application Services ────────────────────
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<ConversationService>();

var app = builder.Build();

// ── Middleware Pipeline ─────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
