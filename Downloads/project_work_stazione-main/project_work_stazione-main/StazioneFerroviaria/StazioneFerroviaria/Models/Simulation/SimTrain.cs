namespace StazioneFerroviaria.Models.Simulation;

/// <summary>
/// Stato runtime di un treno nella simulazione.
/// </summary>
public class SimTrain
{
    public string Id        { get; set; } = string.Empty;
    public string Code      { get; set; } = string.Empty;
    public string Type      { get; set; } = string.Empty;
    public int    Length    { get; set; }
    public int    ZoneCount      { get; set; }
    public int    Direction      { get; set; }  // 1 = destra, -1 = sinistra

    /// <summary>
    /// Zone attualmente visibili sul binario. Parte da 1 (solo la testa) e
    /// cresce di 1 ad ogni tick di movimento fino a raggiungere ZoneCount.
    /// Durante l'uscita (IsExiting=true) decresce di 1 per tick fino a 0.
    /// </summary>
    public int    VisibleZoneCount { get; set; } = 1;

    /// <summary>
    /// True quando la testa ha superato il limite del binario e il treno
    /// sta uscendo gradualmente. Ogni tick viene rimossa una zona di coda.
    /// La rimozione definitiva avviene quando VisibleZoneCount raggiunge 0.
    /// </summary>
    public bool   IsExiting { get; set; }

    // Stato sosta
    public bool IsStopping    { get; set; }
    public int  TimeRemaining { get; set; }

    /// <summary>
    /// Indice della zona dove la TESTA del treno deve fermarsi (piattaforma centrale).
    /// -1 = il treno attraversa il binario per intero senza fermarsi (solo tipo T).
    ///   direction=+1: StopHeadZone = Count/2 + zonesNeeded/2 - 1
    ///   direction=-1: StopHeadZone = Count/2 - zonesNeeded/2
    /// </summary>
    public int StopHeadZone { get; set; } = -1;

    /// <summary>
    /// True dopo che il treno ha avviato la sosta in piattaforma.
    /// Impedisce di ri-calcolare il tempo di sosta al tick successivo.
    /// </summary>
    public bool HasStoppedAtPlatform { get; set; }

    // Dati DB
    public int?      DbId          { get; set; }
    public DateTime? DepartureTime { get; set; }

    /// <summary>Timestamp UTC di quando il treno e' stato posizionato in simulazione.</summary>
    public DateTime ArrivedAt { get; set; }
}
