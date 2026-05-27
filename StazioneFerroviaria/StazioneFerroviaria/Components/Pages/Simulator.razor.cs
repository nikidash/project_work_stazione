using FerroviaApi.Data;
using FerroviaApi.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StazioneFerroviaria.Services;

namespace StazioneFerroviaria.Components.Pages;

public partial class Simulator : IDisposable
{
    [Inject] private TrainNotificationService TrainNotification { get; set; } = null!;
    [Inject] private IServiceScopeFactory     ScopeFactory      { get; set; } = null!;

    private CancellationTokenSource? _cts;
    private bool _isRunning;
    private int  _speed = 1000;
    private bool _disposed;
    private int  _ticksSinceRefresh;

    // ── Inizializzazione ────────────────────────────────────────────────────

    protected override async Task OnInitializedAsync()
    {
        SimService.LoadLayout();
        TrainNotification.OnTrainAdded += OnTrainAddedFromDb;
        await LoadCurrentTrainsAsync();
    }

    // ── Caricamento treni attivi dal DB ─────────────────────────────────────

    private async Task LoadCurrentTrainsAsync()
    {
        using var scope = ScopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FerroviaApiDbContext>();

        var now = DateTime.UtcNow;

        // Carica solo i treni che:
        //   1. Hanno raggiunto l'orario di arrivo schedulato
        //   2. NON hanno ancora una riga in TrainsLogs (non ancora simulati o in corso)
        // Questo impedisce il ricaricamento infinito di treni già completati.
        var activeTrains = await db.Trains
            .Where(t => t.ScheduledArrivalTime <= now
                     && !db.TrainsLogs.Any(l => l.TrainNumber          == t.TrainNumber
                                             && l.ScheduledArrivalTime == t.ScheduledArrivalTime))
            .AsNoTracking()
            .ToListAsync();

        foreach (var t in activeTrains)
        {
            if (!SimService.HasTrainWithDbId(t.Id))
                SimService.AddTrainFromDatabase(t);
        }
    }

    // ── Notifica treno aggiunto dal form AddTrain ───────────────────────────

    private async void OnTrainAddedFromDb(Train train)
    {
        if (_disposed) return;
        if (train.ScheduledArrivalTime <= DateTime.UtcNow)
            SimService.AddTrainFromDatabase(train);

        await InvokeAsync(StateHasChanged);
    }

    // ── Controlli di riproduzione ───────────────────────────────────────────

    private void Play()
    {
        if (_isRunning) return;
        _isRunning = true;
        _cts = new CancellationTokenSource();
        _ = RunSimulationLoopAsync(_cts.Token);
    }

    private async Task RunSimulationLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try { await Task.Delay(_speed, token); }
            catch (TaskCanceledException) { break; }

            if (token.IsCancellationRequested) break;

            // ── Tick della simulazione ──────────────────────────────────────
            SimService.Tick();

            // ── Persisti i treni partiti (TrainsLog) ────────────────────────
            var departed = SimService.DrainDepartedTrains();
            if (departed.Count > 0)
                await PersistDepartedTrainsAsync(departed);

            // ── Ricarica periodicamente i treni attivi dal DB ───────────────
            _ticksSinceRefresh++;
            if (_ticksSinceRefresh >= 5)
            {
                _ticksSinceRefresh = 0;
                await LoadCurrentTrainsAsync();
            }

            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Scrive una riga TrainsLog per ogni treno che ha completato il percorso.
    /// Usa IServiceScopeFactory per ottenere un DbContext scoped fresco,
    /// evitando problemi di lifetime con il singleton SimulationService.
    /// </summary>
    private async Task PersistDepartedTrainsAsync(
        IReadOnlyList<SimulationService.DepartedEntry> departed)
    {
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FerroviaApiDbContext>();

            foreach (var entry in departed)
            {
                // Evita duplicati: non inserire se esiste già un log per questo treno
                bool alreadyLogged = await db.TrainsLogs
                    .AnyAsync(l => l.TrainNumber == entry.DbTrain.TrainNumber
                                && l.ScheduledArrivalTime == entry.DbTrain.ScheduledArrivalTime);
                if (alreadyLogged) continue;

                db.TrainsLogs.Add(new TrainsLog
                {
                    TrainNumber           = entry.DbTrain.TrainNumber,
                    Length                = entry.DbTrain.Length,
                    Origin                = entry.DbTrain.Origin,
                    Destination           = entry.DbTrain.Destination,
                    TrainType             = entry.DbTrain.TrainType,
                    IsTransit             = entry.DbTrain.IsTransit,
                    ScheduledArrivalTime  = DateTime.SpecifyKind(
                                               entry.DbTrain.ScheduledArrivalTime,
                                               DateTimeKind.Utc),
                    ScheduledDepartureTime = DateTime.SpecifyKind(
                                               entry.DbTrain.ScheduledDepartureTime,
                                               DateTimeKind.Utc),
                    ActualArrivalTime     = DateTime.SpecifyKind(
                                               entry.ActualArrival,
                                               DateTimeKind.Utc),
                    ActualDepartureTime   = DateTime.SpecifyKind(
                                               entry.ActualDepart,
                                               DateTimeKind.Utc),
                    AssignedPlatform      = entry.Platform
                });
            }

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Non fare crash del loop di simulazione per errori di persistenza
            Console.Error.WriteLine($"[Simulator] Errore scrittura TrainsLog: {ex.Message}");
        }
    }

    // ── Pausa / Step / Reset ────────────────────────────────────────────────

    private void Pause()
    {
        _isRunning = false;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async Task Step()
    {
        SimService.Tick();

        var departed = SimService.DrainDepartedTrains();
        if (departed.Count > 0)
            await PersistDepartedTrainsAsync(departed);

        StateHasChanged();
    }

    private async Task DoReset()
    {
        Pause();
        SimService.Reset();
        await LoadCurrentTrainsAsync();
    }

    private void OnSpeedChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int speed))
            _speed = speed;
    }

    // ── Dispose ─────────────────────────────────────────────────────────────

    public void Dispose()
    {
        _disposed = true;
        TrainNotification.OnTrainAdded -= OnTrainAddedFromDb;
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
