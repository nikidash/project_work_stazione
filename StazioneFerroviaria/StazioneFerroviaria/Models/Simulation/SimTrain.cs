namespace StazioneFerroviaria.Models.Simulation;

/// <summary>
/// Replica della struttura train in Simulation.js (frontend mockup-laravel).
/// </summary>
public class SimTrain
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Length { get; set; }
    public int ZoneCount { get; set; }
    public int Direction { get; set; } // 1 = destra, -1 = sinistra
    public bool IsStopping { get; set; }
    public int TimeRemaining { get; set; }
    public int? DbId { get; set; }
    public DateTime? DepartureTime { get; set; }
}
