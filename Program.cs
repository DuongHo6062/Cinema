using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MovieStreaming.Configurations;
using MovieStreaming.Data;
using MovieStreaming.Middleware;
using MovieStreaming.Models.Entities;
using MovieStreaming.Repositories;
using MovieStreaming.Services;
using MovieStreaming.Services.Admin;
using MovieStreaming.Services.Authentication;
using MovieStreaming.Services.Comments;
using MovieStreaming.Services.Favorites;
using MovieStreaming.Services.Ratings;
using MovieStreaming.Services.Recommendations;
using MovieStreaming.Services.WatchHistory;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// 1. Database Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

// 2. Repository Layer
builder.Services.AddScoped<IMovieRepository, MovieRepository>();
builder.Services.AddScoped<IGenreRepository, GenreRepository>();
builder.Services.AddScoped<IEpisodeRepository, EpisodeRepository>();
builder.Services.AddScoped<IWatchHistoryRepository, WatchHistoryRepository>();
builder.Services.AddScoped<IFavoriteRepository, FavoriteRepository>();
builder.Services.AddScoped<IMovieRatingRepository, MovieRatingRepository>();
builder.Services.AddScoped<IMovieCommentRepository, MovieCommentRepository>();

// 3. Service Layer
builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<IGenreService, GenreService>();
builder.Services.AddScoped<IEpisodeService, EpisodeService>();
builder.Services.AddScoped<IWatchHistoryService, WatchHistoryService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IMovieRatingService, MovieRatingService>();
builder.Services.AddScoped<IMovieCommentService, MovieCommentService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IAdminMovieService, AdminMovieService>();
builder.Services.AddScoped<IAdminGenreService, AdminGenreService>();
builder.Services.AddScoped<IAdminCategoryService, AdminCategoryService>();
builder.Services.AddScoped<IAdminEpisodeService, AdminEpisodeService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IAdminModerationService, AdminModerationService>();
builder.Services.Configure<RecommendationOptions>(builder.Configuration.GetSection(RecommendationOptions.SectionName));
builder.Services.AddScoped<IRecommendationScorer, RecommendationScorer>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();

// 4. KKPhim External API Integration
var kkphimBaseUrl = builder.Configuration.GetValue<string>("KKPhim:BaseUrl") ?? "https://phimapi.com";
var kkphimTimeout = builder.Configuration.GetValue<int?>("KKPhim:TimeoutSeconds") ?? 30;

builder.Services.AddHttpClient<MovieStreaming.Services.KKPhim.IKKPhimClient, MovieStreaming.Services.KKPhim.KKPhimClient>(client =>
{
    client.BaseAddress = new Uri(kkphimBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(kkphimTimeout);
    client.DefaultRequestHeaders.Add("User-Agent", "MovieStreamingApp/1.0");
});

builder.Services.AddScoped<MovieStreaming.Services.KKPhim.IKKPhimImportService, MovieStreaming.Services.KKPhim.KKPhimImportService>();

// 5. Authentication & JWT Configuration
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    if (builder.Environment.IsProduction())
    {
        throw new InvalidOperationException("Production JWT key ('Jwt:Key' or environment variable 'Jwt__Key') must be configured.");
    }
    // Development-only fallback if unconfigured
    jwtKey = "DevelopmentOnlyPlaceholderSecretKey2026!MustBeReplacedInProductionWithUserSecretsOrEnvVars";
}
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "MovieStreaming";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "MovieStreamingClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// 6. Controllers
builder.Services.AddControllers();

// 7. Reverse Proxy & Forwarded Headers for Production (TLS Termination / Nginx / Cloud)
builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                               Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// 8. CORS for React Frontend (Production & Development)
var configOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
var allowedOriginsList = new HashSet<string>(configOrigins, StringComparer.OrdinalIgnoreCase);

if (builder.Environment.IsDevelopment())
{
    allowedOriginsList.Add("http://localhost:3000");
    allowedOriginsList.Add("http://localhost:5173");
    allowedOriginsList.Add("http://localhost:5174");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactFrontendPolicy", policy =>
    {
        if (allowedOriginsList.Count > 0)
        {
            policy.WithOrigins(allowedOriginsList.ToArray())
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            policy.SetIsOriginAllowed(_ => false);
        }
    });
});

// 9. Swagger / OpenAPI with JWT Bearer Authentication
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Movie Streaming API",
        Version = "v1",
        Description = "Movie Streaming Backend API - Phase 1 through Phase 11 Production"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Forwarded headers for reverse proxies must be executed first
app.UseForwardedHeaders();

// Global Exception Handling Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure Swagger
var enableSwagger = app.Environment.IsDevelopment() ||
                    app.Configuration.GetValue<bool>("EnableSwaggerInProduction");

if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Movie Streaming API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseCors("ReactFrontendPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
