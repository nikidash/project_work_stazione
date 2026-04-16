using FerroviaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace StazioneFerroviaria.Components.Pages;

public partial class Dashboard
{
    private List<Sector> sectors = new();
    private List<Train> trains = new();
    private List<Maintenance> maintenance = new();
    private List<FerroviaApi.Models.Semaphore> semaphores = new();
    private List<Switch> switches = new();

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
