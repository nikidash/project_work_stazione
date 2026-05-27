using FerroviaApi.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace StazioneFerroviaria.Components.Pages;

public partial class Analytics : IDisposable
{
    [Inject] private FerroviaApi.Data.FerroviaApiDbContext Db { get; set; } = null!;

    // ── Stato ─────────────────────────────────────────────────────────────
    private bool     loaded      = false;
    private DateTime lastRefresh = DateTime.UtcNow;

    // ── KPI principali ────────────────────────────────────────────────────
    private int    totalLogs        = 0;
    private int    lateCount        = 0;
    private double avgDelayMin      = 0;
    private double punctualityPct   = 0;
    private int    totalMaintenance = 0;

    // ── Saturazione binari morti ───────────────────────────────────────────
    private int    totalSTrains          = 0;    // S totali nel log
    private int    sOnMaintenance        = 0;    // S che hanno usato un binario morto
    private double maintenanceSaturation = 0;    // % S su binari morti

    // ── Aggregati per tipo ────────────────────────────────────────────────
    private List<TrainTypeRow>    byType           = new();
    private List<StationTimeRow>  stationTimes     = new();
    private List<PlatformRow>     platformUsage    = new();
    private List<Maintenance>     lastMaintenances = new();

    // ── DTO interni ───────────────────────────────────────────────────────

    private sealed class TrainTypeRow
    {
        public string TrainType   { get; set; } = string.Empty;
        public int    Count       { get; set; }
        public double AvgDelayMin { get; set; }
    }

    private sealed class StationTimeRow
    {
        public string TrainType     { get; set; } = string.Empty;
        public double AvgMinutes    { get; set; }   // tempo medio in stazione (ArrivedAt → ActualDepart)
        public int    Count         { get; set; }
    }

    private sealed class PlatformRow
    {
        public int    Platform { get; set; }
        public int    Count    { get; set; }
        public double Pct      { get; set; }
    }

    // ── Ciclo di vita ─────────────────────────────────────────────────────

    protected override async Task OnInitializedAsync() => await LoadDataAsync();

    private async Task LoadDataAsync()
    {
        loaded = false;
        StateHasChanged();

        var logs = await Db.TrainsLogs.AsNoTracking().ToListAsync();
        totalLogs = logs.Count;

        // ── Ritardi (arrivo effettivo vs programmato) ──────────────────────
        lateCount   = logs.Count(l => l.ActualArrivalTime > l.ScheduledArrivalTime);
        avgDelayMin = totalLogs > 0
            ? logs.Average(l => (l.ActualArrivalTime - l.ScheduledArrivalTime).TotalMinutes)
            : 0;
        punctualityPct = totalLogs > 0
            ? (double)(totalLogs - lateCount) / totalLogs * 100
            : 100.0;

        // ── Saturazione binari morti ───────────────────────────────────────
        // S trains con AssignedPlatform = null → erano su binario morto
        var sLogs = logs.Where(l => l.TrainType == "S").ToList();
        totalSTrains   = sLogs.Count;
        sOnMaintenance = sLogs.Count(l => !l.AssignedPlatform.HasValue);
        maintenanceSaturation = totalSTrains > 0
            ? (double)sOnMaintenance / totalSTrains * 100
            : 0;

        // ── Distribuzione per tipo ─────────────────────────────────────────
        byType = logs
            .GroupBy(l => l.TrainType)
            .Select(g => new TrainTypeRow
            {
                TrainType   = g.Key,
                Count       = g.Count(),
                AvgDelayMin = g.Average(l =>
                    (l.ActualArrivalTime - l.ScheduledArrivalTime).TotalMinutes)
            })
            .OrderBy(r => r.TrainType)
            .ToList();

        // ── Tempo medio in stazione per tipo ──────────────────────────────
        // ActualDepartureTime - ActualArrivalTime = permanenza totale in stazione
        stationTimes = logs
            .Where(l => l.ActualDepartureTime > l.ActualArrivalTime)
            .GroupBy(l => l.TrainType)
            .Select(g => new StationTimeRow
            {
                TrainType  = g.Key,
                Count      = g.Count(),
                AvgMinutes = g.Average(l =>
                    (l.ActualDepartureTime - l.ActualArrivalTime).TotalMinutes)
            })
            .OrderBy(r => r.TrainType)
            .ToList();

        // ── Occupazione binari di transito ────────────────────────────────
        var platGroups = logs
            .Where(l => l.AssignedPlatform.HasValue)
            .GroupBy(l => l.AssignedPlatform!.Value)
            .Select(g => new { Platform = g.Key, Count = g.Count() })
            .OrderBy(g => g.Platform)
            .ToList();

        int platTotal = platGroups.Sum(g => g.Count);
        platformUsage = platGroups
            .Select(g => new PlatformRow
            {
                Platform = g.Platform,
                Count    = g.Count,
                Pct      = platTotal > 0 ? (double)g.Count / platTotal * 100 : 0
            })
            .ToList();

        // ── Ultime 10 manutenzioni ────────────────────────────────────────
        lastMaintenances = await Db.Maintenance
            .Include(m => m.TrainLog)
            .AsNoTracking()
            .OrderByDescending(m => m.MaintenanceStart)
            .Take(10)
            .ToListAsync();

        totalMaintenance = await Db.Maintenance.CountAsync();

        lastRefresh = DateTime.UtcNow;
        loaded      = true;
    }

    // ── Helper UI ─────────────────────────────────────────────────────────

    private static string GetTypeName(string t) => t switch
    {
        "R" => "Regionale (passeggeri)",
        "V" => "Veloce (passeggeri)",
        "F" => "Freccia (passeggeri)",
        "T" => "Merci in Transito",
        "S" => "Cargo Stazionante",
        _   => "Sconosciuto"
    };

    public void Dispose() { /* nessuna risorsa da liberare */ }
}
