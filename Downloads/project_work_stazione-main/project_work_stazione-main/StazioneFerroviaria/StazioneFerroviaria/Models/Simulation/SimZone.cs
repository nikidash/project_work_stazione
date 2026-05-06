namespace StazioneFerroviaria.Models.Simulation;

/// <summary>
/// Replica della struttura zone in Simulation.js (frontend mockup-laravel).
/// </summary>
public class SimZone
{
    public int Id { get; set; }
    public bool IsOccupied { get; set; }
    public SimTrain? Train { get; set; }
}
