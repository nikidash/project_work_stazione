namespace StazioneFerroviaria.Models.Simulation;

/// <summary>
/// Rappresenta un settore del tracciato occupabile da un treno.
/// Nel nuovo modello a grafo, corrisponde a un segmento (StationSegment).
/// </summary>
public class SimZone
{
    public int Id { get; set; }
    public string SegmentId { get; set; } = string.Empty;
    public bool IsOccupied { get; set; }
    public SimTrain? Train { get; set; }
}
