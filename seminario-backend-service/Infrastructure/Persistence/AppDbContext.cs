using Microsoft.EntityFrameworkCore;
using Seminario.Domain.Entities;

namespace Seminario.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserDocument> UserDocuments => Set<UserDocument>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<MfaCode> MfaCodes => Set<MfaCode>();
    public DbSet<OrdenCompra> OrdenesCompra => Set<OrdenCompra>();
    public DbSet<OrdenCompraDetalle> OrdenesCompraDetalle => Set<OrdenCompraDetalle>();
    public DbSet<PresupuestoBodega> PresupuestosBodega => Set<PresupuestoBodega>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
