namespace StazioneFerroviaria.Models.Simulation;

/// <summary>
/// Replica della struttura track in Simulation.js (frontend mockup-laravel).
/// Inizializzazione come da TrackSeeder.php: 4 transit (20 zone) + 2 maintenance (10 zone).
/// </summary>
public class SimTrack
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = "transit";
    public int MaxZones { get; set; }
    public List<SimZone> Zones { get; set; } = new();
}
