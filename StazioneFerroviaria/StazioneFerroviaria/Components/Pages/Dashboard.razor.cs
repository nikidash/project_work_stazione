using FerroviaApi.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using StazioneFerroviaria.Services;

namespace StazioneFerroviaria.Components.Pages;

public partial class Dashboard : IDisposable
{
    [Inject] private FerroviaApi.Data.FerroviaApiDbContext Db        { get; set; } = null!;
    [Inject] private SimulationService                     SimService { get; set; } = null!;

    private List<Sector>                           sectors       = new();
    private List<Train>                            trains        = new();
    private List<Maintenance>                      maintenance   = new();
    private List<FerroviaApi.Models.Semaphore>     semaphores    = new();
    private List<Switch>                           switches      = new();
    private List<(Train Train, DateTime QueuedAt)> waitingTrains = new();

    private System.Threading.Timer? _layoutTimer;
    private bool _disposed;

    protected override async Task OnInitializedAsync()
    {
        await RefreshDataAsync();

        // Aggiorna dati dal DB e dalla simulazione ogni 2 secondi
        _layoutTimer = new System.Threading.Timer(async _ =>
        {
            if (_disposed) return;
            try
            {
                await InvokeAsync(async () =>
                {
                    await RefreshDataAsync();
                    StateHasChanged();
                });
            }
            catch { /* componente già smontato */ }
        }, null, 2000, 2000);
    }

    private async Task RefreshDataAsync()
    {
        sectors = await Db.Sectors
            .OrderBy(s => s.PlatformNumber)
            .ToListAsync();

        var now = DateTime.UtcNow;
        trains = await Db.Trains
            .Where(t => t.ScheduledArrivalTime <= now && t.ScheduledDepartureTime > now)
            .OrderBy(t => t.ScheduledArrivalTime)
            .ToListAsync();

        foreach (var sector in sectors)
        {
            if (sector.PlatformNumber.HasValue)
                sector.IsAvailable = !trains.Any(t => t.AssignedPlatform == sector.PlatformNumber);
        }

        maintenance   = await Db.Maintenance
            .Include(m => m.TrainLog)
            .Where(m => m.MaintenanceEnd >= now)
            .OrderBy(m => m.MaintenanceStart)
            .ToListAsync();
        semaphores    = await Db.Semaphores.ToListAsync();
        switches      = await Db.Switches.ToListAsync();
        waitingTrains = SimService.GetWaitingQueue().ToList();
    }

    public void Dispose()
    {
        _disposed = true;
        _layoutTimer?.Dispose();
        _layoutTimer = null;
    }

    // ── Helpers binari ─────────────────────────────────────────────────

    private string GetTrackClass(Sector s)
    {
        if (s.IsStop)       return "bg-warning text-dark";
        if (!s.IsAvailable) return "bg-danger";
        return "bg-success";
    }

    private string GetTrackStatusText(Sector s)
    {
        if (s.IsStop)       return "Stop";
        if (!s.IsAvailable) return "Occupato";
        return "Libero";
    }

    // ── Helpers ritardo ────────────────────────────────────────────────

    private string GetDelayText(Train t)
    {
        var delay = (DateTime.UtcNow - t.ScheduledArrivalTime).TotalMinutes;
        if (delay <= 0) return "In orario";
        if (delay < 1)  return "< 1 min";
        return $"+{(int)delay} min";
    }

    private string GetDelayClass(Train t)
    {
        var delay = (DateTime.UtcNow - t.ScheduledArrivalTime).TotalMinutes;
        if (delay <= 0) return "text-success fw-semibold";
        if (delay < 5)  return "text-warning fw-semibold";
        return "text-danger fw-semibold";
    }

    // ── Helpers priorità ───────────────────────────────────────────────

    private static string GetPriorityLabel(string trainType) => trainType switch
    {
        "T" => "P5 – Merci Transito",
        "F" => "P4 – Freccia",
        "V" => "P3 – Veloce",
        "R" => "P2 – Regionale",
        "S" => "P1 – Cargo Sosta",
        _   => "-"
    };

    private static string GetPriorityBadge(string trainType) => trainType switch
    {
        "T" => "bg-danger",
        "F" => "bg-warning text-dark",
        "V" => "bg-info text-dark",
        "R" => "bg-primary",
        "S" => "bg-secondary",
        _   => "bg-light text-dark"
    };
}
