using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Seminario.Application.Interfaces;
using Seminario.Infrastructure.Persistence;
using Testcontainers.MongoDb;
using Testcontainers.MsSql;

namespace Seminario.IntegrationTests.Infrastructure;

/// <summary>
/// Levanta la API real (Program.cs completo: middleware, JWT, RBAC, rate limiting, EF Core, migraciones)
/// contra SQL Server 2022 y MongoDB 7 en contenedores Docker efimeros (Testcontainers).
/// Las migraciones crean particiones, SP y triggers igual que en produccion.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtSecret = "clave-jwt-solo-para-pruebas-de-integracion-0123456789";

    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private readonly MongoDbContainer _mongo = new MongoDbBuilder()
        .WithImage("mongo:7.0")
        .Build();

    /// <summary>Codigos MFA emitidos, por userId (en desarrollo el codigo solo se escribe en el log).</summary>
    public ConcurrentDictionary<int, string> CodigosMfa { get; } = new();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_sql.StartAsync(), _mongo.StartAsync());

        var sql = new SqlConnectionStringBuilder(_sql.GetConnectionString())
        {
            InitialCatalog = "SeminarioTests",
            TrustServerCertificate = true
        };

        // Program.cs lee la configuracion antes de Build(): se entrega por variables de entorno.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", sql.ConnectionString);
        Environment.SetEnvironmentVariable("MongoDB__ConnectionString", _mongo.GetConnectionString());
        Environment.SetEnvironmentVariable("MongoDB__DatabaseName", "SeminarioAuditTests");
        Environment.SetEnvironmentVariable("Jwt__Secret", JwtSecret);
        Environment.SetEnvironmentVariable("Jwt__Issuer", "SeminarioBackendService");
        Environment.SetEnvironmentVariable("Jwt__Audience", "SeminarioFrontend");
        Environment.SetEnvironmentVariable("Cors__AllowedOrigin", "http://localhost:5173");

        // Fuerza el arranque del host (migraciones + seed) una sola vez para toda la coleccion.
        _ = Services;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await Task.WhenAll(_sql.DisposeAsync().AsTask(), _mongo.DisposeAsync().AsTask());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureLogging(logging => logging.AddProvider(new MfaCodeCaptureProvider(CodigosMfa)));

        builder.ConfigureServices(services =>
            services.AddSingleton<IStartupFilter, TestClientIpStartupFilter>());
    }

    /// <summary>Cliente HTTP con una IP de origen propia (el rate limiting particiona por IP).</summary>
    public HttpClient Cliente(string? ip = null)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        client.DefaultRequestHeaders.Add(TestClientIpStartupFilter.Header, ip ?? NuevaIp());
        return client;
    }

    /// <summary>Cliente autenticado con un JWT real emitido por el TokenService de la API.</summary>
    public async Task<HttpClient> ClienteComo(string username)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokens = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var user = await db.Users.Include(u => u.Role).SingleAsync(u => u.Username == username);

        var client = Cliente();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens.GenerateAccessToken(user).Token);
        return client;
    }

    public async Task<int> UserIdDe(string username)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Users.Where(u => u.Username == username).Select(u => u.Id).SingleAsync();
    }

    /// <summary>Crea un usuario nuevo (rol USER) por el endpoint publico de registro.</summary>
    public async Task<string> RegistrarUsuarioAsync(string password = "Segura#2026")
    {
        var username = "it_" + Guid.NewGuid().ToString("N")[..10];
        using var form = new MultipartFormDataContent
        {
            { new StringContent("Prueba"), "FirstName" },
            { new StringContent("Integracion"), "LastName" },
            { new StringContent(username), "Username" },
            { new StringContent($"{username}@test.local"), "Email" },
            { new StringContent(password), "Password" },
            { new StringContent(password), "ConfirmPassword" }
        };

        var response = await Cliente().PostAsync("/api/auth/register", form);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"No se pudo registrar el usuario de prueba: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        }

        return username;
    }

    private static int _ip = 10;

    private static string NuevaIp()
    {
        var n = Interlocked.Increment(ref _ip);
        return $"10.0.{n / 250}.{n % 250 + 1}";
    }
}

[CollectionDefinition(Nombre)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Nombre = "API con SQL Server y MongoDB";
}

/// <summary>Toma la IP del cliente de la cabecera X-Test-IP (TestServer no tiene RemoteIpAddress).</summary>
internal sealed class TestClientIpStartupFilter : IStartupFilter
{
    public const string Header = "X-Test-IP";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, siguiente) =>
        {
            if (context.Request.Headers.TryGetValue(Header, out var ip) && IPAddress.TryParse(ip, out var direccion))
            {
                context.Connection.RemoteIpAddress = direccion;
            }

            await siguiente();
        });
        next(app);
    };
}

/// <summary>Lee del log el codigo MFA que MfaService emite en desarrollo.</summary>
internal sealed class MfaCodeCaptureProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<int, string> _codigos;

    public MfaCodeCaptureProvider(ConcurrentDictionary<int, string> codigos) => _codigos = codigos;

    public ILogger CreateLogger(string categoryName) =>
        categoryName.EndsWith("MfaService", StringComparison.Ordinal) ? new Capturador(_codigos) : NullLoggerInstance;

    public void Dispose()
    {
    }

    private static readonly ILogger NullLoggerInstance = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

    private sealed class Capturador : ILogger
    {
        private readonly ConcurrentDictionary<int, string> _codigos;

        public Capturador(ConcurrentDictionary<int, string> codigos) => _codigos = codigos;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (state is not IEnumerable<KeyValuePair<string, object?>> valores)
            {
                return;
            }

            var dic = valores.ToDictionary(v => v.Key, v => v.Value);
            if (dic.TryGetValue("UserId", out var userId) && dic.TryGetValue("Code", out var code) && userId is int id)
            {
                _codigos[id] = code?.ToString() ?? string.Empty;
            }
        }
    }
}

/// <summary>Helpers de JSON para las pruebas.</summary>
internal static class HttpExtensions
{
    public static async Task<T> LeerAsync<T>(this HttpResponseMessage response)
    {
        var valor = await response.Content.ReadFromJsonAsync<T>();
        return valor ?? throw new InvalidOperationException("Respuesta vacia");
    }
}
