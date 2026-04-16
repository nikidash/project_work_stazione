using FerroviaApi.Models;
using Microsoft.EntityFrameworkCore;
using StazioneFerroviaria.Services;

namespace StazioneFerroviaria.Components.Pages;

public partial class AddTrain
{
    private Train train = new()
    {
        Origin = "L1",
        Destination = "L",
        TrainType = "R",
        Length = 6
    };

    private List<Sector> sectors = new();
    private int selectedPlatform;
    private bool isMaintenance;
    private string? successMessage;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        sectors = await Db.Sectors
            .Where(s => s.PlatformNumber != null)
            .OrderBy(s => s.PlatformNumber)
            .ToListAsync();
    }

    private async Task SaveTrain()
    {
        successMessage = null;
        errorMessage = null;

        train.ScheduledArrivalTime = DateTime.UtcNow;
        train.ScheduledDepartureTime = DateTime.UtcNow.AddMinutes(10);
        train.AssignedPlatform = selectedPlatform > 0 ? selectedPlatform : null;

        Db.Trains.Add(train);

        try
        {
            await Db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            errorMessage = $"Errore nel salvataggio: {ex.InnerException?.Message ?? ex.Message}";
            return;
        }

        if (isMaintenance)
        {
            try
            {
                var log = new TrainsLog
                {
                    TrainNumber = train.TrainNumber,
                    Length = train.Length,
                    Origin = train.Origin,
                    Destination = train.Destination,
                    TrainType = train.TrainType,
                    ScheduledArrivalTime = train.ScheduledArrivalTime,
                    ScheduledDepartureTime = train.ScheduledDepartureTime,
                    IsTransit = train.IsTransit,
                    ActualArrivalTime = DateTime.UtcNow,
                    ActualDepartureTime = DateTime.UtcNow.AddMinutes(10),
                    AssignedPlatform = train.AssignedPlatform
                };
                Db.TrainsLogs.Add(log);
                await Db.SaveChangesAsync();

                var maint = new Maintenance
                {
                    TrainId = log.Id,
                    MaintenanceStart = DateTime.UtcNow,
                    MaintenanceEnd = DateTime.UtcNow.AddHours(2)
                };
                Db.Maintenance.Add(maint);
                await Db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                errorMessage = $"Treno salvato ma errore manutenzione: {ex.InnerException?.Message ?? ex.Message}";
                return;
            }
        }

        TrainNotification.NotifyTrainAdded(train);

        successMessage = $"Treno {train.TrainNumber} salvato" +
            (isMaintenance ? " e registrato in manutenzione." : " e inviato al simulatore.");

        train = new()
        {
            Origin = "L1",
            Destination = "L",
            TrainType = "R",
            Length = 6
        };
        selectedPlatform = 0;
        isMaintenance = false;
    }
}
