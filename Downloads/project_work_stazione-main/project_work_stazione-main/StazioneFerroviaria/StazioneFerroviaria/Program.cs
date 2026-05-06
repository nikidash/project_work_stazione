using FerroviaApi.Data;
using FerroviaApi.Models;
using Microsoft.EntityFrameworkCore;
using StazioneFerroviaria.Components;
using StazioneFerroviaria.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<FerroviaApiDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("FerroviaDb")));

builder.Services.AddSingleton<SimulationService>();
builder.Services.AddSingleton<TrainNotificationService>();

// Abilita i controller REST API
builder.Services.AddControllers();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FerroviaApiDbContext>();
    if (!db.Sectors.Any())
    {
        db.Sectors.AddRange(
            new Sector { PlatformNumber = 1, Length = 20, IsAvailable = true, IsStop = false },
            new Sector { PlatformNumber = 2, Length = 20, IsAvailable = true, IsStop = false },
            new Sector { PlatformNumber = 3, Length = 20, IsAvailable = true, IsStop = false },
            new Sector { PlatformNumber = 4, Length = 20, IsAvailable = true, IsStop = false }
        );
        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Mappa le route dei controller REST API
app.MapControllers();

app.Run();
