using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using StazioneFerroviaria.Models.Simulation;
using StazioneFerroviaria.Services;
using FerroviaApi.Data;
using FerroviaApi.Models;

namespace StazioneFerroviaria.Components.Pages;

public partial class Simulator : IDisposable
{
    [Inject] private TrainNotificationService TrainNotification { get; set; } = null!;
    [Inject] private FerroviaApiDbContext Db { get; set; } = null!;

    private CancellationTokenSource? _cts;
    private bool _isRunning;
    private int _speed = 1000;
    private bool _disposed;
    private int _ticksSinceRefresh;

    protected override async Task OnInitializedAsync()
    {
        TrainNotification.OnTrainAdded += OnTrainAddedFromDb;
        await LoadCurrentTrains();
    }

    private async Task LoadCurrentTrains()
    {
        var now = DateTime.UtcNow;
        var activeTrains = await Db.Trains
            .Where(t => t.ScheduledArrivalTime <= now && t.ScheduledDepartureTime > now)
            .ToListAsync();

        foreach (var t in activeTrains)
        {
            if (!SimService.HasTrainWithDbId(t.Id))
                SimService.AddTrainFromDatabase(t);
        }
    }

    private async void OnTrainAddedFromDb(Train train)
    {
        if (_disposed) return;
        if (train.ScheduledArrivalTime <= DateTime.UtcNow)
        {
            SimService.AddTrainFromDatabase(train);
        }
        await InvokeAsync(StateHasChanged);
    }

    private void Play()
    {
        if (_isRunning) return;
        _isRunning = true;
        _cts = new CancellationTokenSource();
        _ = RunSimulationLoop(_cts.Token);
    }

    private async Task RunSimulationLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_speed, token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            if (token.IsCancellationRequested) break;

            SimService.Tick();

            _ticksSinceRefresh++;
            if (_ticksSinceRefresh >= 5)
            {
                _ticksSinceRefresh = 0;
                await LoadCurrentTrains();
            }

            await InvokeAsync(StateHasChanged);
        }
    }

    private void Pause()
    {
        _isRunning = false;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private void Step()
    {
        SimService.Tick();
    }

    private async Task DoReset()
    {
        Pause();
        SimService.Reset();
        await LoadCurrentTrains();
    }

    private void OnSpeedChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int speed))
        {
            _speed = speed;
        }
    }

    private string GetZoneClass(SimZone zone, SimTrack track)
    {
        if (!zone.IsOccupied || zone.Train == null)
            return "bg-light border";

        if (zone.Train.IsStopping)
        {
            return zone.Train.Type switch
            {
                "R" or "V" or "F" => "bg-primary stopping-pattern",
                "T" or "S" => "bg-danger stopping-pattern",
                _ => "bg-dark stopping-pattern"
            };
        }

        return zone.Train.Type switch
        {
            "R" or "V" or "F" => "bg-primary",
            "T" or "S" => "bg-danger",
            _ => "bg-dark"
        };
    }

    private bool IsTrainHead(SimZone zone, int index, SimTrack track)
    {
        if (zone.Train == null) return false;
        var dir = zone.Train.Direction;

        if (dir == 1)
        {
            return index == 0 ||
                   track.Zones[index - 1].Train == null ||
                   track.Zones[index - 1].Train!.Id != zone.Train.Id;
        }
        if (dir == -1)
        {
            return index == track.Zones.Count - 1 ||
                   track.Zones[index + 1].Train == null ||
                   track.Zones[index + 1].Train!.Id != zone.Train.Id;
        }
        return false;
    }

    private string GetZoneTitle(SimZone zone)
    {
        if (zone.Train == null)
            return $"Zona {zone.Id}";

        var stoppingStatus = zone.Train.IsStopping
            ? $"In sosta (attesa: {zone.Train.TimeRemaining})"
            : "In transito";

        return $"Treno: {zone.Train.Code} | Lunghezza: {zone.Train.Length} vagoni | {stoppingStatus}";
    }

    public void Dispose()
    {
        _disposed = true;
        TrainNotification.OnTrainAdded -= OnTrainAddedFromDb;
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
