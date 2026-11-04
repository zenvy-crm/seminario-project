using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Seminario.Infrastructure.Persistence;

/// <summary>
/// Fabrica de diseno para las herramientas de EF Core (dotnet ef migrations add / script).
/// Evita que las herramientas levanten todo el host web (y su seeding) solo para leer el modelo.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        DotNetEnv.Env.TraversePath().Load();

        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost,1433;Database=SeminarioDb;User Id=sa;Password=placeholder;TrustServerCertificate=True;";

        // CommandTimeout de 10 min: 'dotnet ef database update' puede recorrer TB_Ordenes_Compra
        // completa (rowversion, indices) y con el stress test cargado 30 s no alcanzan.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString, sql => sql.CommandTimeout(600))
            .Options;

        return new AppDbContext(options);
    }
}
