using Microsoft.EntityFrameworkCore;
using Seminario.Application.Interfaces;
using Seminario.Infrastructure.MongoDB;
using Seminario.Infrastructure.Persistence;

DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("MongoDB"));
builder.Services.AddSingleton<AuditChannel>();
builder.Services.AddSingleton<IAuditLogger>(sp => sp.GetRequiredService<AuditChannel>());
builder.Services.AddHostedService<AuditBackgroundService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
