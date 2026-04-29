using FerroviaApi.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace StazioneFerroviaria.Components.Pages;

public partial class Dashboard : IDisposable
{
    private List<Sector> sectors = new();
    private List<Train> trains = new();
    private List<Maintenance> maintenance = new();
    private List<FerroviaApi.Models.Semaphore> semaphores = new();
    private List<Switch> switches = new();

    private System.Threading.Timer? _layoutTimer;
    private bool _disposed;

    protected override async Task OnInitializedAsync()
    {
        sectors = await Db.Sectors.OrderBy(s => s.PlatformNumber).ToListAsync();

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

        maintenance = await Db.Maintenance.Include(m => m.TrainLog).ToListAsync();
        semaphores = await Db.Semaphores.ToListAsync();
        switches = await Db.Switches.ToListAsync();

        _layoutTimer = new System.Threading.Timer(_ =>
        {
            if (_disposed) return;
            _ = InvokeAsync(StateHasChanged);
        }, null, 1000, 1000);
    }

    public void Dispose()
    {
        _disposed = true;
        _layoutTimer?.Dispose();
        _layoutTimer = null;
    }

    private string GetTrackClass(Sector s)
    {
        if (s.IsStop)
            return "track-stop";

        if (!s.IsAvailable)
            return "track-occupied";

        return "track-free";
    }

    private string GetTrackStatusText(Sector s)
    {
        if (s.IsStop)
            return "Stop";

        if (!s.IsAvailable)
            return "Occupato";

        return "Libero";
    }
}
