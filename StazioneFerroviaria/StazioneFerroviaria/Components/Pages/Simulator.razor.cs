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

    public void Dispose()
    {
        _disposed = true;
        TrainNotification.OnTrainAdded -= OnTrainAddedFromDb;
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
