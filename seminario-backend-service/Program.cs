using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Seminario.Infrastructure.Health;
using Microsoft.IdentityModel.Tokens;
using Seminario.Api;
using Seminario.Api.Middleware;
using Seminario.Application.Common;
using Seminario.Application.Interfaces;
using Seminario.Application.Services;
using Seminario.Infrastructure.Auth;
using Seminario.Infrastructure.MongoDB;
using Seminario.Infrastructure.Persistence;
using Seminario.Infrastructure.Repositories;
using Seminario.Infrastructure.Services;
using Microsoft.OpenApi.Models;

DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicyName = "Frontend";

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Seminario Backend Service", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese unicamente el token JWT (sin el prefijo 'Bearer ')"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .SelectMany(e => e.Value!.Errors.Select(err => err.ErrorMessage))
            .ToList();

        var response = new ApiErrorResponse
        {
            Success = false,
            Message = "Los datos enviados no son validos",
            StatusCode = StatusCodes.Status422UnprocessableEntity,
            Errors = errors
        };

        return new UnprocessableEntityObjectResult(response);
    };
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("MongoDB"));
builder.Services.AddSingleton<IAuditStore, MongoAuditStore>();
builder.Services.AddSingleton<AuditChannel>();
builder.Services.AddSingleton<IAuditLogger>(sp => sp.GetRequiredService<AuditChannel>());
builder.Services.AddHostedService<AuditBackgroundService>();

// Patron Health Check: sondas reales a las dependencias, expuestas en GET /api/health.
// SQL Server caido => Unhealthy (503); MongoDB caido => Degraded (la bitacora es asincrona).
// MongoHealthCheck es singleton para reutilizar su cliente entre ejecuciones.
builder.Services.AddSingleton<MongoHealthCheck>();
builder.Services.AddHealthChecks()
    .AddCheck<SqlServerHealthCheck>("sqlserver", failureStatus: HealthStatus.Unhealthy)
    .AddCheck<MongoHealthCheck>("mongodb", failureStatus: HealthStatus.Degraded);

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new ApiErrorResponse
                {
                    Success = false,
                    Message = "No autenticado. Inicie sesion nuevamente.",
                    StatusCode = StatusCodes.Status401Unauthorized
                });
            },
            OnForbidden = async context =>
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new ApiErrorResponse
                {
                    Success = false,
                    Message = "No tiene permisos para realizar esta accion.",
                    StatusCode = StatusCodes.Status403Forbidden
                });
            }
        };
    });
// RBAC de la Fase 2: politicas NivelEstandar / NivelIntermedio / NivelAdministrador.
builder.Services.AddRbacPolicies();

builder.Services.AddAuthRateLimiting();

var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:5173";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
        policy.WithOrigins(allowedOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IMfaService, MfaService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IOrdenCompraService, OrdenCompraService>();
builder.Services.AddScoped<IReporteService, ReporteService>();
builder.Services.AddScoped<ISeedDataService, SeedDataService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    // Usuarios de demo (estandar / intermedio) solo en Development o con Seed:DemoUsers=true.
    var sembrarUsuariosDemo = app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Seed:DemoUsers");
    await DbSeeder.SeedAsync(db, passwordHasher, sembrarUsuariosDemo);
}

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(CorsPolicyName);

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Expone la clase Program generada por los top-level statements para WebApplicationFactory (pruebas de integracion).
public partial class Program { }
