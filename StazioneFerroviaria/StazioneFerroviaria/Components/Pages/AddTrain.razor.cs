using FerroviaApi.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using StazioneFerroviaria.Services;

namespace StazioneFerroviaria.Components.Pages;

public partial class AddTrain
{
    [Inject] private FerroviaApi.Data.FerroviaApiDbContext Db                { get; set; } = null!;
    [Inject] private TrainNotificationService              TrainNotification  { get; set; } = null!;

    private Train train = new()
    {
        Origin                 = "L1",
        Destination            = "L",
        TrainType              = "R",
        Length                 = 6,
        IsTransit              = false,
        ScheduledArrivalTime   = DateTime.UtcNow,
        ScheduledDepartureTime = DateTime.UtcNow.AddMinutes(10)
    };

    private bool         isMaintenance;
    private string?      successMessage;
    private string?      errorMessage;

    protected override async Task OnInitializedAsync()
    {
        // Nessun dato da pre-caricare
        await Task.CompletedTask;
    }

    /// <summary>
    /// Aggiorna IsTransit automaticamente in base al tipo:
    /// - T (merci transito) → IsTransit = true
    /// - S (cargo stazionante) → IsTransit = false
    /// </summary>
    private void OnTrainTypeChanged()
    {
        if (train.TrainType == "T") train.IsTransit = true;
        if (train.TrainType == "S") train.IsTransit = false;
    }

    private async Task SaveTrain()
    {
        successMessage = null;
        errorMessage   = null;

        if (string.IsNullOrWhiteSpace(train.TrainNumber))
        {
            errorMessage = "Il numero treno è obbligatorio.";
            return;
        }

        // Garantiamo Kind=Utc per Npgsql (timestamp with time zone)
        train.ScheduledArrivalTime   = DateTime.SpecifyKind(train.ScheduledArrivalTime,   DateTimeKind.Utc);
        train.ScheduledDepartureTime = DateTime.SpecifyKind(train.ScheduledDepartureTime, DateTimeKind.Utc);

        if (train.ScheduledDepartureTime <= train.ScheduledArrivalTime)
        {
            errorMessage = "La partenza deve essere successiva all'arrivo.";
            return;
        }

        train.AssignedPlatform = null; // Assegnato dal routing

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
                    TrainNumber            = train.TrainNumber,
                    Length                 = train.Length,
                    Origin                 = train.Origin,
                    Destination            = train.Destination,
                    TrainType              = train.TrainType,
                    ScheduledArrivalTime   = train.ScheduledArrivalTime,
                    ScheduledDepartureTime = train.ScheduledDepartureTime,
                    IsTransit              = train.IsTransit,
                    ActualArrivalTime      = DateTime.SpecifyKind(DateTime.UtcNow,                  DateTimeKind.Utc),
                    ActualDepartureTime    = DateTime.SpecifyKind(DateTime.UtcNow.AddMinutes(10),   DateTimeKind.Utc),
                    AssignedPlatform       = train.AssignedPlatform
                };
                Db.TrainsLogs.Add(log);
                await Db.SaveChangesAsync();

                var maint = new Maintenance
                {
                    TrainId          = log.Id,
                    MaintenanceStart = DateTime.SpecifyKind(DateTime.UtcNow,          DateTimeKind.Utc),
                    MaintenanceEnd   = DateTime.SpecifyKind(DateTime.UtcNow.AddHours(2), DateTimeKind.Utc)
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

        successMessage = $"Treno {train.TrainNumber} salvato con successo" +
            (isMaintenance ? " e registrato in manutenzione." : " e inviato al simulatore.");

        // Reset form con valori di default
        train = new()
        {
            Origin                 = "L1",
            Destination            = "L",
            TrainType              = "R",
            Length                 = 6,
            IsTransit              = false,
            ScheduledArrivalTime   = DateTime.UtcNow,
            ScheduledDepartureTime = DateTime.UtcNow.AddMinutes(10)
        };
        isMaintenance    = false;
    }
}
